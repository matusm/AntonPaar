using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace AntonPaar
{
	public class AntonPaarRS232 : AntonPaar
	{
		private SerialPort portCOM;
		private const int DELAY = 150;
		private const int MAXLOOP = 50;
		private bool bConnected;
		private string instrumentResponse;

		public AntonPaarRS232(string host) : base()
		{
			instrumentPort = host.Trim().ToUpper();
			try
			{
				portCOM = new SerialPort(instrumentPort, 9600, Parity.None, 8, StopBits.One);
				portCOM.ReadTimeout = 20000;
				portCOM.Open();
				portCOM.Handshake = Handshake.None;
				portCOM.DiscardInBuffer();
				bConnected = true;
				_UpdateValues(true);
			}
			catch
			{
				portCOM = null;
				bConnected = false;
			}
		}

		/// <summary>
		/// Sets display mode to resistance.
		/// </summary>
		/// <returns><c>true</c>, if mode was set, <c>false</c> otherwise.</returns>
		public bool SetResistance()
		{
			if (!SetMode(1)) return false;
			dMode = DisplayMode.Resistance;
			return true;
		}

		/// <summary>
		/// Sets display mode to temperature.
		/// </summary>
		/// <returns><c>true</c>, if mode was set, <c>false</c> otherwise.</returns>
		public bool SetTemperature()
		{
			if (!SetMode(2)) return false;
			dMode = DisplayMode.Temperature;
			return true;
		}

		/// <summary>
		/// Sets display mode to resistance statistics.
		/// </summary>
		/// <returns><c>true</c>, if mode was set, <c>false</c> otherwise.</returns>
		public bool SetResistanceStatistics()
		{
			if (!SetMode(3)) return false;
			dMode = DisplayMode.ResistanceStatistics;
			return true;
		}

		/// <summary>
		/// Sets display mode to temperature statistics.
		/// </summary>
		/// <returns><c>true</c>, if mode was set, <c>false</c> otherwise.</returns>
		public bool SetTemperatureStatistics()
		{
			if (!SetMode(4)) return false;
			dMode = DisplayMode.TemperatureStatistics;
			return true;
		}

		/// <summary>
		/// Turns on the SHT mode.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
		public bool ShtOn()
		{
			if (!LoadInstrumentResponse("SHT ON")) return false;
			if (!instrumentResponse.Contains("SET SHT ON")) return false;
			sht = ShtStatus.On;
			return true;
		}

		/// <summary>
		/// Turns off the SHT mode.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
		public bool ShtOff()
		{
			if (!LoadInstrumentResponse("SHT OFF")) return false;
			if (!instrumentResponse.Contains("SET SHT OFF")) return false;
			sht = ShtStatus.Off;
			return true;
		}

		/// <summary>
		/// Toggles the SHT mode.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
		public bool ShtToggle()
		{
			QuerySht();
			switch (sht)
			{
				case ShtStatus.Unknown:
					return false;
				case ShtStatus.On:
					return ShtOff();
				case ShtStatus.Off:
					return ShtOn();
				default:
					return false;
			}
		}

		/// <summary>
		/// Queries the SHT mode of instrument and sets the private field accordingly.
		/// </summary>
		/// <returns>The <c>ShtStatus</c> value.</returns>
		public ShtStatus QuerySht()
		{
			sht = _QuerySht();
			return sht;
		}


		/// <summary>
		/// Loads the data string from the instrument.
		/// </summary>
		/// <returns><c>true</c> if successful; <c>false</c> otherwise.</returns>
		bool LoadInstrumentResponse(string sCommand)
		{
			dataValid = false;
			if (portCOM == null || bConnected == false) { return dataValid; }
			Query(sCommand);
			if (instrumentResponse.Contains("???")) { return dataValid; }
			dataValid = true;
			return dataValid;
		}

		/// <summary>
		/// Updates values without raising an event. (used by the ctor only).
		/// </summary>
		/// <param name="parseAll">If <c>true</c> parse instrument settings also.</param>
		/// <returns><c>true</c> if updated; <c>false</c> otherwise.</returns>
		protected override bool _UpdateValues(bool parseAll)
		{
			if (!QuerySensorValuesAndID()) return false;
			measurementTimeStamp = DateTime.UtcNow;
			if (parseAll)
			{
				QuerySht();
				if (!QueryTypeSerialAndMac()) return false;
				if (!QuerySensorTypes()) return false;
				if (!QueryFirmwareVersion()) return false;
			}
			return true;
		}

		/// <summary>
		/// Tries to determine the SHT status of the instrument
		/// </summary>
		/// <returns>The SHT status.</returns>
		ShtStatus _QuerySht()
		{
			if (!LoadInstrumentResponse("GET SHT")) return ShtStatus.Unknown;
			if (instrumentResponse.Contains("SHT: OFF")) return ShtStatus.Off;
			if (instrumentResponse.Contains("SHT: ON")) return ShtStatus.On;
			return ShtStatus.Unknown;
		}

		bool SetMode(int i)
		{
			if (i < 1 || i > 7) return false;
			string cmd = string.Format("MODE {0}", i);
			if (!LoadInstrumentResponse("GET " + cmd)) return false;
			if (!instrumentResponse.Contains(cmd)) return false;
			return true;
		}



		bool QuerySensorValuesAndID()
		{
			if (!LoadInstrumentResponse("GET DATA")) return false;
			if (!ParseSensorValuesAndID()) return false;
			return true;
		}

		bool QueryTypeSerialAndMac()
		{
			if (!LoadInstrumentResponse("GET STATUS")) return false;
			if (!ParseTypeSerialMac()) return false;
			return true;
		}

		bool QuerySensorTypes()
		{
			if (!LoadInstrumentResponse("GET SENSOR")) return false;
			if (!ParseSensorTypes()) return false;
			return true;
		}

		bool QueryFirmwareVersion()
		{
			if (!LoadInstrumentResponse("GET CONFIG")) return false;
			if (!ParseVersion()) return false;
			return true;
		}



		bool ParseVersion()
		{
			char[] sepLines = { '\n', '\r' };
			char[] sepTokens = { ' ' };
			string[] lines = instrumentResponse.Split(sepLines, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				string[] tokens = line.Split(sepTokens, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length == 2)
					if (tokens[0] == "Software")
					{
						instrumentFirmwareVersion = tokens[1];
						return true;
					}
			}
			return false;
		}

		bool ParseSensorTypes()
		{
			int hits = 0;
			char[] sepLines = { '\n', '\r' };
			string[] lines = instrumentResponse.Split(sepLines, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Contains("Sensor 1 ="))
				{
					channel1.Type = Sensor.StringToSensorType(lines[i + 1]);
					hits++;
				}
				if (lines[i].Contains("Sensor 2 ="))
				{
					channel2.Type = Sensor.StringToSensorType(lines[i + 1]);
					hits++;
				}
			}
			if (hits == 2) return true;
			return false;
		}

		bool ParseTypeSerialMac()
		{
			int hits = 0;
			char[] sepLines = { '\n', '\r' };
			char[] sepTokens = { ' ' };
			string[] lines = instrumentResponse.Split(sepLines, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				string[] tokens = line.Split(sepTokens, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length == 0) break;
				if (tokens.Length == 1)
					instrumentType = tokens[0];
				else
				{
					switch (tokens[0])
					{
						case "S/N:":
							instrumentSerialNumber = tokens[1];
							hits++;
							break;
						case "MAC:":
							instrumentMac = tokens[1];
							hits++;
							break;
						default:
							break;
					}
				}
			}
			if (hits == 2) return true;
			return false;
		}

		bool ParseSensorValuesAndID()
		{
			int hits = 0;
			char[] sepLines = { '\n', '\r' };
			char[] sepTokens = { ' ' };
			channel1.InValidate();
			channel2.InValidate();
			string[] lines = instrumentResponse.Split(sepLines, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				string[] tokens = line.Split(sepTokens, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length <= 1) break;
				switch (tokens[0])
				{
					case "R1=":
						channel1.Resistance = StringToNumber(tokens[1]);
						hits++;
						break;
					case "R2=":
						channel2.Resistance = StringToNumber(tokens[1]);
						hits++;
						break;
					case "T1=":
						channel1.Temperature = StringToNumber(tokens[1]);
						hits++;
						break;
					case "T2=":
						channel2.Temperature = StringToNumber(tokens[1]);
						hits++;
						break;
					case "SENSOR1=":
						channel1.ID = tokens[1].Replace("No:", "");
						hits++;
						break;
					case "SENSOR2=":
						channel2.ID = tokens[1].Replace("No:", "");
						hits++;
						break;
					default:
						break;
				}
			}
			if (hits == 0) return false;
			return true;
		}

		/// <summary>
		/// Parses a string to a double or null.
		/// </summary>
		/// <param name="s">String to be parsed.</param>
		/// <returns>The numerical value or <c>null</c>.</returns>
		double? StringToNumber(string s)
		{
			double x;
			if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return null;
			return x;
		}


		/// <summary>
		/// Sends a command to the instrument and stores the response in the private <c>instrumentResponse</c> string.
		/// </summary>
		/// <param name="sCommand">Command to be sent.</param>
		/// <returns><c>true</c> if sucessfull, <false> otherwise.</false></returns>
		bool Query(string sCommand)
		{
			bool b = SendCommand(sCommand);
			instrumentResponse = ReadLine();
			return b;
		}

		/// <summary>
		/// Appends <c>"\r"</c> to string and sends as command to the instrument.
		/// </summary>
		/// <remarks>
		/// A delay of <c>DELAY</c> ms is added after the actual sending to the instrument.
		/// </remarks>
		/// <param name="sCommand">Command to be sent.</param>
		bool SendCommand(string sCommand)
		{
			if (portCOM == null || bConnected == false) return false;
			portCOM.DiscardInBuffer();
			sCommand += "\r"; // or \n?
			byte[] cmd = Encoding.ASCII.GetBytes(sCommand);
			try
			{
				portCOM.Write(cmd, 0, cmd.Length);
			}
			catch (Exception)
			{
				return false;
			}
			Thread.Sleep(DELAY);
			return true;
		}

		/// <summary>
		/// Receives a string form the instrument and.
		/// </summary>
		/// <returns>The string received.</returns>
		/// <remarks>
		/// Can take a long time to finish.
		/// </remarks>
		string ReadLine()
		{
			string temp = "";
			string rb = "";
			int i = 0;
			while (i < MAXLOOP)
			{
				rb = ReadBytes();
				if (rb == "") break;
				temp += rb;
				i++;
				Thread.Sleep(DELAY);
			}
			return temp;
		}

		/// <summary>
		/// Receives a string from the instrument.
		/// </summary>
		/// <remarks>Can be an empty string even when working.</remarks>
		/// <returns>The string received.</returns>
		string ReadBytes()
		{
			if (portCOM == null || bConnected == false) return "";
			byte[] buffer = new byte[portCOM.BytesToRead];
			try
			{
				portCOM.Read(buffer, 0, buffer.Length);
				return Encoding.UTF8.GetString(buffer);
			}
			catch (Exception)
			{
				return "";
			}
		}

	}
}
