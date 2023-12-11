using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTD
{
    public partial class stripChartDemoForm : Form
    {
        #region Const
        // How many samples to add between increments of plot time.
        double DesiredPlotUpdatesPerSecond = 10;
        // Target samples per second.
        int DesiredSamplesPerSecond = 50;
        #endregion Const
        public stripChartDemoForm()
        {
            InitializeComponent();

            stripChart.InchesPerSecond = 2.0;
            stripChart.RangeMin = -2.5;
            stripChart.RangeMax = 2.5;

            stripChartPropertyGrid.SelectedObject = stripChart;
            stripChartPropertyGrid.BrowsableAttributes = new AttributeCollection(new StripChart.StripChartConfigAttribute());
            stripChartPropertyGrid.Refresh();

        }

        #region Public Properties
        #endregion Public Properties

        #region Implementation Methods
        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            // Skip the first call.
            if (_lastSampleGenerationTime == DateTime.MinValue)
            {
                // Bootstrap...
                _lastSampleGenerationTime = DateTime.Now;
                return;
            }

            // Determine # of samples to generate as a function of time elapsed since last generation time and desired
            // time between samples.
            var deltaT = DateTime.Now - _lastSampleGenerationTime;
            // Calculate number of samples based on desired sampling rate.
            int samples = (int)Math.Ceiling(deltaT.TotalSeconds * DesiredSamplesPerSecond);
            if (samples <= 1)
                // Don't update _lastSampleGenerationTime.
                return;
            // Note: # of samples is 1 greater than # of steps.
            // Assumption: Earlier test precludes possiblity of zero in denominator.
            TimeSpan sampleTimeDelta = TimeSpan.FromSeconds(deltaT.TotalSeconds / (samples - 1));
            // Loop over samples...
            TimeSpan sampleTimeOffset = _lastSampleGenerationTime - _sampleGenerator.StartTime;

            // Generate and add the samples.
            for (int i = 0; i < samples - 1; i++, sampleTimeOffset += sampleTimeDelta)
            {
                double value = _sampleGenerator.Next(sampleTimeOffset.TotalSeconds);
                this.stripChart.AddSample(sampleTimeOffset, value);
            }
            _lastSampleGenerationTime = DateTime.Now;

            // Is it time to update the plot?
            if ((DateTime.Now - _lastPlotUpdateTime).TotalSeconds >= 1.0 / DesiredPlotUpdatesPerSecond)
            {
                stripChart.SetTime(sampleTimeOffset);
                _lastPlotUpdateTime = DateTime.Now;
            }

        }
        private void StartPlotting()
        {
            _sampleGenerator = new SampleGenerator(DateTime.Now, 2, 0.1, 0);

            stripChart.StartTime = _sampleGenerator.StartTime;
            _updateTimer = new Timer { Enabled = true, Interval = 10 };
            _updateTimer.Tick += _updateTimer_Tick;

        }
        private void StopPlotting()
        {
            _updateTimer.Enabled = false;
            _updateTimer.Dispose();

            stripChart.Reset();
        }
        private void pauseButton_CheckedChanged(object sender, EventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb.Checked)
                StartPlotting();
            else
                StopPlotting();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        #endregion Implementation Methods

        #region Fields
        private Timer _updateTimer;
        private SampleGenerator _sampleGenerator;
        private DateTime _startTime;
        private int _tickCount = 0;
        private DateTime _lastSampleGenerationTime = DateTime.MinValue;
        private DateTime _lastPlotUpdateTime = DateTime.MinValue;
        #endregion Fields

        #region Types
        class SampleGenerator
        {
            private DateTime _startTime;
            private double _amplitude;
            private double _bias;
            private double _frequency;

            public DateTime StartTime { get => _startTime; }

            public SampleGenerator(DateTime? startTime = null, double amplitude = 1, double frequency = 1, double bias = 0)
            {
                _startTime = startTime ?? DateTime.Now;
                _amplitude = amplitude;
                _frequency = frequency;
                _bias = bias;
            }
            public double Next(double t)
            {
                return _bias + _amplitude * Math.Sin(2 * Math.PI * _frequency * t);
            }
        }
        #endregion Types

    }
}
