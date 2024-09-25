using System;
using System.Threading;

namespace AntonPaar
{
    public abstract class AntonPaar
    {
        #region Private fields
        const string unKnwn = "<----->";
        const double minimumUpdateIntervall = 1.44; // in seconds
        bool loopActive;
        bool loopStopRequest;
        Thread thread;
        int loopSamples;
        #endregion

        #region Protected fields
        protected string instrumentManufacturer;
        protected string instrumentSerialNumber;
        protected string instrumentType;
        protected string instrumentFirmwareVersion;
        protected string instrumentPort;
        protected string instrumentMac; // not used
        protected string instrumentDate; // not used
        protected string instrumentTime; // not used
        protected Sensor channel1 = new Sensor();
        protected Sensor channel2 = new Sensor();
        protected DateTime initTimeStamp;
        protected DateTime measurementTimeStamp;
        protected double updateIntervall;
        protected int numberOfSamples = 0;
        protected ShtStatus sht = ShtStatus.Unknown;
		protected DisplayMode dMode = DisplayMode.Unknown;
        protected bool dataValid;
        #endregion

        #region Properties
        public Sensor Channel1 { get { return channel1; } }
        public Sensor Channel2 { get { return channel2; } }
		public double? Temperature1 { get { return channel1.Temperature; } }
		public double? Temperature2 { get { return channel2.Temperature; } }
		public string InstrumentManufacturer { get { return instrumentManufacturer; } }
        public string InstrumentSerialNumber { get { return instrumentSerialNumber; } }
        public string InstrumentType { get { return instrumentType; } }
        public string InstrumentFirmwareVersion { get { return instrumentFirmwareVersion; } }
        public string InstrumentPort { get { return instrumentPort; } }
        public DateTime MeasurementTimeStamp { get { return measurementTimeStamp; } }
        public DateTime InitTimeStamp { get { return initTimeStamp; } }
        public int NumberOfSamples { get { return numberOfSamples; } }
        public ShtStatus SHT { get { return sht; } }
		public DisplayMode DisplayMode { get { return dMode; } }
        public bool ValidSample { get { return dataValid; } }
        public double UpdateIntervall
        {
            get { return updateIntervall; }
            set
            {
                updateIntervall = value;
                if (updateIntervall < minimumUpdateIntervall) updateIntervall = minimumUpdateIntervall;
            }
        }
        #endregion

        #region Ctor
        protected AntonPaar()
        {
            InValidate();
            instrumentPort = unKnwn;
            instrumentManufacturer = "AntonPaar";
            initTimeStamp = DateTime.UtcNow;
            measurementTimeStamp = DateTime.UtcNow; // just to have a value
            updateIntervall = minimumUpdateIntervall;
            loopActive = false;
            loopStopRequest = false;
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Loads instrument data, updates the sensor values and optionally the device parameters.
        /// </summary>
        /// <param name="parseAll">If <c>true</c> parse instrument settings also.</param>
        /// <returns><c>true</c> if updated; <c>false</c> otherwise.</returns>
        /// <remarks>When updated does NOT rises an event.</remarks>
        public bool UpdateValuesSync(bool parseAll)
        {
            if (!NeedsUpdate(updateIntervall)) return false;
            if (!_UpdateValues(parseAll)) return false;
            return true;
        }

        /// <summary>
        /// Loads instrument data, updates the sensor values and optionally the device parameters.
        /// </summary>
        /// <param name="parseAll">If <c>true</c> parse instrument settings also.</param>
        /// <returns><c>true</c> if updated; <c>false</c> otherwise.</returns>
        /// <remarks>When updated rises an event.</remarks>
        public bool UpdateValues(bool parseAll)
        {
            if (!UpdateValuesSync(parseAll))
                return false;
            UpdatedEventHandler(this, new EventArgs());
            return true;
        }

        /// <summary>
        /// Loads instrument data and updates the sensor values.
        /// </summary>
        /// <returns><c>true</c> if updated; <c>false</c> otherwise.</returns>
        /// <remarks>When updated rises an event.</remarks>
        public bool UpdateValues() { return UpdateValues(false); }

        /// <summary>
        /// Regulary samples <c>n</c> measurement samples.
        /// Returns after comleting all measurements.
        /// </summary>
        /// <param name="n">Number of samples to acquire.</param>
        /// <remarks>Might never stop!</remarks>
        public void StartMeasurementLoop(int n)
        {
            if (loopActive) return; // otherwise may change loopSamples
            if (n < 1) n = 1;
            loopSamples = n;
            StartMeasurementLoop();
        }

        /// <summary>
        /// Starts a thread wich regulary samples measurement values for a predefine number.
        /// </summary>
        /// <param name="n">Number of samples.</param>
        public void StartMeasurementLoopThread(int n)
        {
            if (loopActive) return;
            if (n < 1) n = 1;
            loopSamples = n;
            loopStopRequest = false;
            thread = new Thread(new ThreadStart(StartMeasurementLoop));
            thread.Start();
        }

        /// <summary>
        /// Starts a thread wich regulary samples measurement values forever.
        /// </summary>
        public void StartMeasurementLoopThread() { StartMeasurementLoopThread(Int32.MaxValue); }

        /// <summary>
        /// Sets <c>stopRequest</c> to true.
        /// </summary>
        public void RequestStopMeasurementLoop()
        {
            loopStopRequest = true;
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Updates values without raising an event. (used by the ctor only).
        /// </summary>
        /// <param name="parseAll">If <c>true</c> parse instrument settings also.</param>
        /// <returns><c>true</c> if updated; <c>false otherwise.</c></returns>
        /// <remarks>Implementation should set <c>measurementTimeStamp</c>.</remarks>
        protected abstract bool _UpdateValues(bool parseAll);

        /// <summary>
        /// Invalidate private fields.
        /// </summary>
        protected void InValidate()
        {
            instrumentSerialNumber = unKnwn;
            instrumentFirmwareVersion = unKnwn;
            instrumentType = unKnwn;
            instrumentMac = unKnwn;
            instrumentDate = unKnwn;
            instrumentTime = unKnwn;
        }

        /// <summary>
        /// Determines if measurement values need update.
        /// </summary>
        /// <returns><c>true</c>, if update is needed, <c>false</c> otherwise.</returns>
        /// <param name="updIntv">Update intervall in seconds</param>
        protected bool NeedsUpdate(double updIntv)
        {
            TimeSpan ts = DateTime.UtcNow - measurementTimeStamp;
            if (ts.TotalSeconds > updIntv)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Starts a loop wich regulary samples <c>loopSamples</c> measurement values..
        /// </summary>
        void StartMeasurementLoop()
        {
            if (loopActive) return;
            loopActive = true;
            int i = 0;
            while (!loopStopRequest && i < loopSamples)
            {
                if (UpdateValues((i == 0))) i++; // the first measurement only should update all properties
            }
            loopActive = false;
            LoopReadyEventHandler(this, new EventArgs());
        }

        #endregion

        #region event declarations

        public delegate void APEventHandler(object obj, EventArgs e);

        /// <summary>
        /// Occurs when UpdateValues() was successful.
        /// </summary>
        public event APEventHandler UpdatedEventHandler;

		/// <summary>
		/// Occurs when measurement loop is ready.
		/// </summary>
        public event APEventHandler LoopReadyEventHandler;

		#endregion

    }
}
