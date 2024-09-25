namespace AntonPaar
{
    /// <summary>
    /// Enumeration for possible sensor types (industrial vs SPRT).
    /// </summary>
    public enum SensorType 
	{
        Unknown,    // unspecified sensor
        Iec751,     // industrial Pt sensor, eg PT100
        Its90,      // regular SPRT
        Its90A,     // two region SPRT
        Polynom4    // fourth order polynom
    }

	/// <summary>
	/// Enumeration of display modes.
	/// </summary>
	public enum DisplayMode
	{
		Unknown,
		Resistance,
		Temperature,
		ResistanceStatistics,
		TemperatureStatistics,
		ResistanceRatioReference,
		TemperatureDifference,
		ResistanceRatio
	}

	/// <summary>
    /// Enumeration for self heating status (SHT).
    /// </summary>
    public enum ShtStatus
	{
		Unknown,    // status undetermined
		On,         // SHT status on - reduced current
		Off         // SHT status off - standard current
	}

}
