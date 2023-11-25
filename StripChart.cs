using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RTD
{
    public partial class StripChart : UserControl
    {
        #region Const
        private const double InchesPerSecondDefault = 0.1;
        private const string YAxisLabelDefault = "Y Axis";
        private const double MinValueDefault = 0;
        private const double MaxValueDefault = 100;

        #endregion Const

        #region Fields
        private bool _debug = false;
        private Font _labelFont;
        private Brush _labelBrush;
        private Pen _graphPen;

        private double _minValue;
        private double _maxValue;
        private string _yAxisLabel;
        private double _inchesPerSecond;
        private ChartInfo _chartInfo;

        // TODO: Decide whether to maintain visible area start time separately, or just calculate from scroll.
        private DateTime _startTime;
        private DateTime _time;
        private int _firstVisibleIdx = 0;
        private DateTime _firstVisibleTime;
        private int _firstNewSampleIndex = 0;
        private List<Sample> _samples = new List<Sample>();
        private Timer _updateTimer;


        #endregion Fields
        #region Types
        class Sample : IComparable<Sample>
        {
            public DateTime time;
            public double value;
            public int CompareTo(Sample rhs)
            {
                if (time < rhs.time) { return -1; }
                else if (time > rhs.time) { return 1; }
                else { return 0; }
            }
            public static bool operator <(Sample lhs, Sample rhs) => lhs.time < rhs.time;
            public static bool operator <=(Sample lhs, Sample rhs) => lhs.time <= rhs.time;
            public static bool operator >(Sample lhs, Sample rhs) => lhs.time > rhs.time;
            public static bool operator >=(Sample lhs, Sample rhs) => lhs.time <= rhs.time;
        }

        class ChartInfo
        {
            public int firstSampleIdx;
            public int lastSampleIdx;
            public DateTime firstVisibleTime;
            public Rectangle GraphArea;
            public double PixelsPerSecond;
        }

        #endregion Types

        #region Properties
        // TODO: Remove this. Was added to fix designer error.
        public string YAxisLabel { get; set; }


        #endregion Properties

        #region Methods
        public StripChart(DateTime? startTime = null,
            double minValue = MinValueDefault,
            double maxValue = MaxValueDefault,
            double inchesPerSecond = InchesPerSecondDefault,
            string yAxisLabel = YAxisLabelDefault)
        {
            InitializeComponent();

            _startTime = startTime ?? DateTime.Now;
            _firstVisibleTime = _startTime; // TEMP DEBUG - scroll-dependent
            _minValue = minValue;
            _maxValue = maxValue;
            _inchesPerSecond = inchesPerSecond;
            _yAxisLabel = yAxisLabel;

            // Create and cache fonts, brushes, etc.
            _labelFont = new Font(Font, FontStyle.Bold);
            _labelBrush = new SolidBrush(Color.Black);
            _graphPen = new Pen(Color.Black);

            hScrollBar.Visible = true;
            hScrollBar.BringToFront();
            Console.WriteLine($"Size: {hScrollBar.Size}");
            Console.WriteLine($"Position: ({hScrollBar.Left},{hScrollBar.Top})");

            // Process newly-added samples.
            _updateTimer = new Timer { Enabled = true, Interval = 1000 };
            _updateTimer.Tick += UpdateTimerTick;

            panel.Invalidate();
        }

        private Rectangle GetClipRect(Sample firstSample, Sample lastSample)
        {
            // Determine time distance from left visible edge
            // TODO: This assumes _firstVisibleIdx will be reliably set on scroll.
            int x1 = (int)((firstSample.time -
                    _samples[_firstVisibleIdx].time).TotalSeconds *
                    _chartInfo.PixelsPerSecond);
            int x2 = x1 + (int)Math.Ceiling((lastSample.time -
                    _samples[_firstVisibleIdx].time).TotalSeconds *
                    _chartInfo.PixelsPerSecond);

            var plotRect = GetPlotArea();

            // TODO: Need a way to get plot area.
            Rectangle clip = new Rectangle
            {
                X = x1,
                Y = plotRect.Top,
                Width = x2 - x1,
                Height = plotRect.Height
            };

            return clip;
        }

        private Rectangle GetPlotArea()
        {
            // TODO: May eventually need something more complex that accounts for axes, etc...
            return panel.ClientRectangle;
        }

        private void UpdateTimerTick(object sender, EventArgs e)
        {
            if (_firstNewSampleIndex >= _samples.Count)
                // Nothing to do...
                return;

            (Sample firstSample, Sample lastSample) =
                (_samples[_firstNewSampleIndex], _samples[_samples.Count - 1]);
            var clipRect = GetClipRect(firstSample, lastSample);

            // Actual clip rect is the intersection of the one calculated above with the visible area.
            clipRect.Intersect(GetPlotArea());
            Console.WriteLine($"Final ClipRect: {clipRect}");

            if (clipRect.Width > 0)
                panel.Invalidate(clipRect);

        }

        public void AddSample(DateTime time, double value)
        {
            _samples.Add( new Sample { time = time, value = value } );
        }

        #endregion Methods


        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            var sb = sender as HScrollBar;
            Console.WriteLine($"HScrollBar.Value: {sb.Value}");
        }

        private int GetSampleIndexAtTime(DateTime time, bool lessOrEqual = false)
        {
            if (_samples.Count == 0)
                return -1;

            int index = _samples.BinarySearch(new Sample { time = time, value = 0.0 },
                Comparer<Sample>.Create((Sample a, Sample b) => a.CompareTo(b)));
            if (index < 0)
            {
                // Didn't find exact match.
                index = ~index;
                // index points either to 1 past end of list or to element just larger than requested.
                if (lessOrEqual)
                {
                    // Work backwards to find first element <= target.
                    for (int i = index; i >= 0; i--)
                    {
                        if (_samples[i].time <= time)
                        {
                            return i;
                        }
                    }
                    // If here, reached beginning of list.
                    return 0;
                }
                else // greater or equal
                {
                    // If index points past end, return final element of list.
                    return index < _samples.Count ? index : _samples.Count - 1;
                }
            }
            // Found exact match.
            return index;
        }

        private ChartInfo GetChartInfo(Graphics graphics, Rectangle clipRect)
        {
            var ret = new ChartInfo {
                firstSampleIdx = -1,
                lastSampleIdx = -1
            };
            // in/s px/in
            // Note: A change to pixels per second would invalidate previously drawn portions of the graph, so either
            // don't allow it, or else discard upon change.
            ret.PixelsPerSecond = graphics.DpiX * _inchesPerSecond;
            ret.GraphArea = panel.ClientRectangle;
            // TODO: This is crude...
            ret.GraphArea.Height = panel.Height - hScrollBar.Height;
            // Get time offset to both edges of clip area.
            // TODO: _startTime needs to be replaced with time of left of visible area.
            DateTime timeLeft = _firstVisibleTime + TimeSpan.FromSeconds(clipRect.Left / ret.PixelsPerSecond);
            DateTime timeRight = _firstVisibleTime + TimeSpan.FromSeconds(clipRect.Right / ret.PixelsPerSecond);

            // TODO: Make sure this can't happen.
            if (_samples.Count > 0)
            {
                ret.firstSampleIdx = GetSampleIndexAtTime(timeLeft, true);
                ret.lastSampleIdx = GetSampleIndexAtTime(timeRight);
            }

            return ret;
        }

        // Convert sample to point.
        private Point GetPoint(ChartInfo chartInfo, Sample sample)
        {
            // Domain
            TimeSpan timeOffset = sample.time - _firstVisibleTime;
            int x = (int)Math.Round((timeOffset.TotalSeconds * chartInfo.PixelsPerSecond));

            // Range
            var ratio = (sample.value - _minValue) / (_maxValue - _minValue);
            int y = (int)Math.Round(chartInfo.GraphArea.Bottom - ratio * chartInfo.GraphArea.Height);
            return new Point(x, y);
        }

        private void stripChart_Paint(object sender, PaintEventArgs e)
        {

        }
        // TODO: Rename.
        private void panel_Paint(object sender, PaintEventArgs e)
        {
            if (_debug) return;
            // Attempt at vertical text.
            var size = panel.Size;
            var g = e.Graphics;
            // Cache for later...
            // What we need:
            // 1. Range of samples that fall within clip rect's x range.
            // 2. Start time corresponding to client area.
            _chartInfo = GetChartInfo(g, e.ClipRectangle);
            if (_chartInfo == null || _chartInfo.firstSampleIdx < 0)
                // Nothing to do...
                return;

            // Loop over all samples not yet added.
            var pt1 = GetPoint(_chartInfo, _samples[_chartInfo.firstSampleIdx]);
            int idx;
            for (idx = _chartInfo.firstSampleIdx + 1; idx <= _chartInfo.lastSampleIdx; idx++)
            {
                var pt2 = GetPoint(_chartInfo, _samples[idx]);

                // TODO: Connected curve or points only?
                Console.WriteLine($"ClipRect: {e.ClipRectangle}");
                Console.WriteLine($"PlotArea: {GetPlotArea()}");
                Console.WriteLine($"Sample: ({_samples[idx].time}, {_samples[idx].value})");
                Console.WriteLine($"Drawing line from {pt1} to {pt2}");
                g.DrawLine(_graphPen, pt1, pt2);
                pt1 = pt2;
            }
            _firstNewSampleIndex = idx;

            /*
            g.TranslateTransform(20, panel.Height / 2);
            g.RotateTransform(90);
            g.DrawString("Foo Bar", _labelFont,
                _labelBrush, new Point(-50, 0));

            g.ResetTransform();
            for (int i = 0; i < 20; i++)
            {
                g.DrawString($"Foo Bar {i}", _labelFont,
                    _labelBrush, new Point(i * 150, 100));
            }
            */
            
        }
    }
}
