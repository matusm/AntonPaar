using System;
using System.Threading;
using Bev.Transmitter;

namespace AntonPaar
{
    class Program
    {

        static void Main(string[] args)
        {
            //var ap = new AntonPaarRS232("COM110");
            var hmt = new AntonPaarXml("10.10.10.150");
            var ptu = new BevVaisalaHmt("10.10.10.152");

            while (true)
            {
                Thread.Sleep(60000);
                hmt.UpdateValuesSync(true);
                Console.WriteLine("{0,6:F3} °C    {1,6:F3} °C    {2,6:F0} Pa    {3,6:F2} %", hmt.Temperature1, ptu.AirTemperature, ptu.BarometricPressure, ptu.RelHumidity);
            }
        }
    }
    }