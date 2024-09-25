using System;
using System.Globalization;
using System.Net;
using System.Xml;

namespace AntonPaar
{
    public class AntonPaarXml : AntonPaar
    {

		#region Private fields
		// URI to connect to instrument
		Uri instrumentUri;
        // fields for XML work
        string instrumentResponse;
        XmlDocument xmlDoc = new XmlDocument();
		#endregion

		#region Ctor
		public AntonPaarXml(string host):base()
        {
            // build the fully qualified URI from the host name
            const string scheme = @"http";
            const string pathValue = @"cgi/xml";
            UriBuilder ub = new UriBuilder(scheme, host, -1, pathValue);
            instrumentUri = ub.Uri;
            instrumentPort = instrumentUri.ToString();
            // load and parse the data for the very first time
            _UpdateValues(true);
        }
        #endregion

        #region Private methods
		/// <summary>
		/// Loads the XML data string from the instrument.
		/// </summary>
		/// <returns><c>true</c>, if data was loaded, <c>false</c> otherwise.</returns>
		bool LoadData()
        {
            instrumentResponse = "";
            using (WebClient client = new WebClient())
            {
                try
                {
                    instrumentResponse = client.DownloadString(instrumentUri);
                    xmlDoc.LoadXml(instrumentResponse);
                    dataValid = true;
                    if (String.IsNullOrEmpty(instrumentResponse)) dataValid = false;
                }
                catch (WebException ex)
                {
                    dataValid = false;
                    //TODO better error handling
                }
            }
            return dataValid;
        }

        /// <summary>
        /// Updates values without raising an event. (used by the ctor only).
        /// </summary>
        /// <param name="parseAll">If <c>true</c> parse instrument settings also.</param>
        /// <returns><c>true</c> if updated; <c>false otherwise.</c></returns>
        protected override bool _UpdateValues(bool parseAll)
        {
            if (!LoadData()) return false;
            if (parseAll) ParseDeviceData();
            ParseSensorData();
            measurementTimeStamp = DateTime.UtcNow;
            return true;
        }

        #region Private methods - XML parsing crab

        /// <summary>
        /// Parses XML data for the device specific values.
        /// </summary>
        void ParseDeviceData()
        {
            InValidate();
            if (!dataValid) return;
            // parse the instrument details
            XmlNodeList nodes = xmlDoc.DocumentElement.SelectNodes("/devicedata/device");
            string attrValue;
            foreach (XmlNode node in nodes)
            {
                if ((attrValue = GetAttribute(node, "serialnumber")) != "")
                    instrumentSerialNumber = attrValue.Trim();
                if ((attrValue = GetAttribute(node, "name")) != "")
                    instrumentType = attrValue.Trim();
                if ((attrValue = GetAttribute(node, "version")) != "")
                    instrumentFirmwareVersion = attrValue.Trim();
                if ((attrValue = GetAttribute(node, "MAC")) != "")
                    instrumentMac = attrValue.Trim();
                if ((attrValue = GetAttribute(node, "date")) != "")
                    instrumentDate = attrValue.Trim();
                if ((attrValue = GetAttribute(node, "time")) != "")
                    instrumentTime = attrValue.Trim();
            }
        }

        /// <summary>
        /// Parses XML data for the sensor values.
        /// </summary>
        void ParseSensorData()
        {
            // first invalidate all values
            channel1.InValidate();
            channel2.InValidate();
            ParseSensorID();
            ParseSensorValues();
        }

        /// <summary>
        /// Parses XML data for both sensor types and ID numbers (and number of samples, too!).
        /// </summary>
        void ParseSensorID()
        {
            if (!dataValid) return;
            numberOfSamples = 1; // if not in statistic mode
            XmlNodeList nodes = xmlDoc.DocumentElement.SelectNodes("/devicedata/settings/setting");
            string attrValue;
            foreach (XmlNode node in nodes)
            {
                if ((attrValue = GetAttribute(node, "name")) != "")
                {
                    if (attrValue == "Samples") Int32.TryParse(node.InnerText, out numberOfSamples);
                    if (attrValue == "Sensor1") ParseSensorType(node, channel1);
                    if (attrValue == "Sensor2") ParseSensorType(node, channel2);
					if (attrValue == "Displaymode") dMode = StringToDisplayMode(node.InnerText);
                }
            }
        }

        /// <summary>
        /// Parses a single XML node for sensor type and ID number.
        /// </summary>
        void ParseSensorType(XmlNode n, Sensor ch)
        {
            ch.ID = n.InnerText;
            string attrValue = GetAttribute(n, "type");
            if (attrValue != "")
                ch.Type = Sensor.StringToSensorType(attrValue);
        }

        /// <summary>
        /// Parses XML data for the two channels sensor values.
        /// </summary>
        void ParseSensorValues()
        {
            if (!dataValid) return;
            XmlNodeList nodes = xmlDoc.DocumentElement.SelectNodes("/devicedata/results/channel");
            string attrValue;
            foreach (XmlNode node in nodes)
            {
                // node must have number attribute
                attrValue = GetAttribute(node, "number");
                if (attrValue == "1")
                    ParseChannel(node.ChildNodes, channel1);
                if (attrValue == "2")
                    ParseChannel(node.ChildNodes, channel2);
            }
        }

        /// <summary>
        /// Parses the values of a single channel and assigns value to the <c>Sensor</c> object.
        /// </summary>
        /// <param name="nl">Specific child node list.</param>
        /// <param name="ch">The sensor channel.</param>
        void ParseChannel(XmlNodeList nl, Sensor ch)
        {
            string sName;
            string sUnit;
            string sStatus;
            string sValue;
            bool bStatus; // true if valid; false otherwise.
            double dValue;
            foreach (XmlNode node in nl)
            {
                sName = GetAttribute(node, "name");
                sUnit = GetAttribute(node, "unit");
                sStatus = GetAttribute(node, "status");
                bStatus = (sStatus == "valid");
                if (bStatus)
                {
                    sValue = node.InnerText;
                    Double.TryParse(sValue, NumberStyles.AllowTrailingSign | NumberStyles.Float, CultureInfo.InvariantCulture, out dValue);
                    switch (sName)
                    {
                        case "Temperature":
                            ch.Temperature = dValue;
                            break;
                        case "Mean":
                            if (sUnit == "ohm")
                                ch.RMean = dValue;
                            else
                                ch.TMean = dValue;
                            break;
                        case "S.Dev":
                            if (sUnit == "ohm")
                                ch.RStdDev = dValue;
                            else
                                ch.TStdDev = dValue;
                            break;
                        case "Resistance":
                            ch.Resistance = dValue;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Helper method to parse attributes within the XmlDocument class.
        /// </summary>
        /// <returns>The attribute's value (trimmed).</returns>
        /// <param name="node">Node to check for attribute.</param>
        /// <param name="str">Attribute name.</param>
        string GetAttribute(XmlNode node, string str)
        {
            if (node.Attributes.GetNamedItem(str) == null)
                return "";
            return node.Attributes.GetNamedItem(str).Value.Trim();
        }

		/// <summary>
		/// Interpretes a string as a <c>>DisplayMode</c> member.
		/// </summary>
		/// <returns>The to display mode.</returns>
		/// <param name="s">The string to be interpreted.</param>
		/// <remarks>There is a bug in firmware version V2.04. The XML response is not error free! </remarks>
		DisplayMode StringToDisplayMode(string s)
		{
			switch (s.Trim())
			{
				case "Temperature":
					return DisplayMode.Temperature;
				case "R1/R2, R2/R1":
					return DisplayMode.ResistanceRatio;
				case "R1/RR, R2/RR":
					return DisplayMode.ResistanceRatioReference;
				case "Temperature Stat.":
					return DisplayMode.TemperatureStatistics;
				case "Resistance":
					return DisplayMode.Resistance;
				case "Resistance Stat.":
					return DisplayMode.ResistanceStatistics;
				default:
					return DisplayMode.Unknown; ;
			}
		}

		#endregion

		#endregion

	}
}
