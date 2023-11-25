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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            stripChart1.MinValue = -2;
            stripChart1.MaxValue = 2;
            stripChart1.InchesPerSecond = 0.5;

            _sampleGenerator = new SampleGenerator(DateTime.Now, 2, 0.1, 0);
            _updateTimer = new Timer { Enabled = true, Interval = 10 };
            _updateTimer.Tick += _updateTimer_Tick;

        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            (DateTime time, double value) = _sampleGenerator.Next();
            this.stripChart1.AddSample(time, value);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private Timer _updateTimer;
        private SampleGenerator _sampleGenerator;

        class SampleGenerator
        {
            private DateTime _startTime;
            private double _amplitude;
            private double _bias;
            private double _frequency;

            public SampleGenerator(DateTime? startTime = null, double amplitude = 1, double frequency = 1, double bias = 0)
            {
                _startTime = startTime ?? DateTime.Now;
                _amplitude = amplitude;
                _frequency = frequency;
                _bias = bias;
            }
            public (DateTime, double) Next()
            {
                DateTime dt = DateTime.Now;
                var t = (dt - _startTime).TotalSeconds;
                double value = _bias + _amplitude * Math.Sin(2 * Math.PI * _frequency * t);
                return (dt, value);
            }
        }
    }
}
