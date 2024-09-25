using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntonPaar
{
    class SHT
    {
        static void Main(string[] args)
        {
            var apSerial = new AntonPaarRS232("COM110");
            var apEth = new AntonPaarXml("10.10.10.150");
            apEth.UpdatedEventHandler += UpdateView;

            apSerial.UpdateValuesSync(true);
            Thread.Sleep(1000);
            apEth.UpdateValuesSync(true);
            Thread.Sleep(1000);

            for (int i = 0; i < 100000; i++)
            {
                apSerial.ShtToggle();
                Thread.Sleep(1000);
                bool statValid = false;
                while (!statValid)
                {
                    apEth.UpdateValuesSync(true);
                    if (apEth.Channel1.TMean != null) statValid = true;
                };
                double t = (apEth.MeasurementTimeStamp - apEth.InitTimeStamp).TotalSeconds;
                string line = string.Format("{0,8:F1} {1,10:F5} {2,10:F5} {3}", t, apEth.Channel1.TMean, apEth.Channel1.TStdDev, apSerial.SHT);
                Console.WriteLine(line);
            }


        }


        static void UpdateView(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            var ob = sender as AntonPaar;

            double t = (ob.MeasurementTimeStamp - ob.InitTimeStamp).TotalSeconds;
            string line = string.Format("{0,8:F1} {1,9:F4} {2,9:F4} {3}", t, ob.Temperature1, ob.Temperature2, ob.SHT);
            Console.WriteLine(line);

            //hFile = File.AppendText(sFileName);
            //hFile.WriteLine(line);
            //hFile.Close();
        }


    }
}
