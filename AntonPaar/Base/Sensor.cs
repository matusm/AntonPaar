namespace AntonPaar
{
	public class Sensor
	{
		#region Properties
		public string ID { get; set; }
		public SensorType Type { get; set; }
		public double? Temperature { get; set; }    // in °C
		public double? TMean { get; set; }          // in °C
		public double? TStdDev { get; set; }        // in °C
		public double? Resistance { get; set; }     // in Ohm
		public double? RMean { get; set; }          // in Ohm
		public double? RStdDev { get; set; }        // in Ohm
		#endregion

		#region Ctor
		/// <summary>
		/// The Ctor just sets all properties to initial states.
		/// </summary>
		public Sensor()
		{
			InValidate();
		}
		#endregion

		/// <summary>
		/// Sets all properties to initial state.
		/// </summary>
		public void InValidate()
		{
			ID = "<----->";
			Type = SensorType.Unknown;
			Temperature = null;
			TMean = null;
			TStdDev = null;
			Resistance = null;
			RMean = null;
			RStdDev = null;
		}

		/// <summary>
		/// Useful for basic debugging only. 
		/// </summary>
		/// <returns>The string.</returns>
		public override string ToString()
		{
			return string.Format("[Sensor: ID={0}, Type={1}]", ID, Type);
		}

		#region Internal static methods

		internal static SensorType StringToSensorType(string s)
		{
			if (s.Contains("IEC751")) return SensorType.Iec751;
			if (s.Contains("ITS-90")) return SensorType.Its90;
			if (s.Contains("Polyn")) return SensorType.Polynom4;
			if (s.Contains("ITS90A")) return SensorType.Its90A;
			return SensorType.Unknown;
		}

		internal static double? ConvertKelvin(double? t)
		{
			if (t == null) return null;
			if (t < 0) return null;
			return t - 273.15;
		}

		internal static double? ConvertFahrenheit(double? t)
		{
			if (t == null) return null;
			if (t < -459.67) return null;
			return (t - 32) / 1.8;
		}

		#endregion

	}
}
