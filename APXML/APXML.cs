using System;
using System.Threading;
using System.Globalization;
using System.IO;

namespace AntonPaar
{
    class APXML
    {
        static string sFileName;
        static StreamWriter hFile;

        static void Main(string[] args)
        {
            var ap = new AntonPaarXml("10.10.10.150");
            ap.UpdatedEventHandler += UpdateView;
            ap.LoopReadyEventHandler += (object sender, EventArgs e) =>
            {
                var ob = sender as AntonPaar;
                Console.WriteLine("\nToggle SHT\n");
            };

            Console.WriteLine("Manufacturer:    " + ap.InstrumentManufacturer);
            Console.WriteLine("Instrument Type: " + ap.InstrumentType);
            Console.WriteLine("Serial number:   " + ap.InstrumentSerialNumber);
            Console.WriteLine("Firmware:        " + ap.InstrumentFirmwareVersion);
            Console.WriteLine("Port:            " + ap.InstrumentPort);
            Console.WriteLine();
            Console.WriteLine("Channel 1: " + ap.Channel1.Type + " " + ap.Channel1.ID);
            Console.WriteLine("Channel 2: " + ap.Channel2.Type + " " + ap.Channel2.ID);
            Console.WriteLine();

            sFileName = "SHT_XML.txt";

            hFile = new StreamWriter(sFileName);
            hFile.WriteLine("Manufacturer:    " + ap.InstrumentManufacturer);
            hFile.WriteLine("Instrument Type: " + ap.InstrumentType);
            hFile.WriteLine("Serial number:   " + ap.InstrumentSerialNumber);
            hFile.WriteLine("Firmware:        " + ap.InstrumentFirmwareVersion);
            hFile.WriteLine("Port:            " + ap.InstrumentPort);
            hFile.WriteLine("# Samples:       " + ap.NumberOfSamples);
            hFile.WriteLine("Channel 1:       " + ap.Channel1.Type + " " + ap.Channel1.ID);
            hFile.WriteLine("Channel 2:       " + ap.Channel2.Type + " " + ap.Channel2.ID);
            hFile.WriteLine("@@@@");
            hFile.Close();

            ap.UpdateIntervall = 20;

            ap.StartMeasurementLoopThread();

            while (true) {}

        }

        static void UpdateView(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var ob = sender as AntonPaar;

            if (ob.Channel1.TMean == null) return;

            double t = (ob.MeasurementTimeStamp - ob.InitTimeStamp).TotalSeconds;
            string line = string.Format("{0,8:F1} {1,11:F5} ± {2:F5}  {3,11:F5} ± {4:F5}", t, ob.Channel1.TMean, ob.Channel1.TStdDev, ob.Channel2.TMean, ob.Channel2.TStdDev);
            Console.WriteLine(line);

            hFile = File.AppendText(sFileName);
            hFile.WriteLine(line);
            hFile.Close();
        }

    }
}
