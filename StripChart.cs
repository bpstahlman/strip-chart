using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RTD
{
    public partial class StripChart : UserControl
    {
        #region Const
        private const double InchesPerSecondDefault = 0.1;
        private const double SecondsPerSmallChangeDefault = 0.1;
        private const int LargeStepsPerScreenDefault = 2;
        internal const double ShiftAtFractionDefault = 0.75;
        internal const double ShiftToFractionDefault = 0.1;
        internal const double ShiftToMaxFractionDefault = 0.5;
        private const double RangeMinDefault = -2;
        private const double RangeMaxDefault = 2;
        private const string YAxisLabelDefault = "Y Axis";
        private const int UpdatePeriodDefault = 100;
        private const int AutoScrollInhibitTimeDefault = 250;
        #endregion Const

        #region Attributes
        [AttributeUsage(AttributeTargets.Property, Inherited = false)]
        public class StripChartConfigAttribute : Attribute
        {
            // TODO: Members needed?
        }
        #endregion Attributes
        #region Config Properties
        // These properties are exposed to component user.
        [StripChartConfig]
        public double RangeMin {
			get => _rangeMin;
            set
            {
                _rangeMin = value;
            }
		}
        [StripChartConfig]
        public double RangeMax {
			get => _rangeMax;
            set
            {
                _rangeMax = value;
            }
		}
        [StripChartConfig]
        public int LargeStepsPerScreen {
			get => _largeStepsPerScreen;
            set
            {
                _largeStepsPerScreen = value;
            }
		}
        [StripChartConfig]
        public double ShiftAtFraction {
			get => _shiftAtFraction;
            set
            {
                _shiftAtFraction = value;
            }
		}
        [StripChartConfig]
        public double SecondsPerSmallChange {
			get => _secondsPerSmallChange;
            set
            {
                _secondsPerSmallChange = value;
            }
		}
        [StripChartConfig]
        public double InchesPerSecond {
			get => _inchesPerSecond;
            set
            {
                _inchesPerSecond = value;
                InvalidatePlot();
            }
		}
        [StripChartConfig]
        public Font LabelFont {
			get => _labelFont;
            set
            {
                _labelFont = value;
            }
		}
        [StripChartConfig]
        public Brush LabelBrush {
			get => _labelBrush;
            set
            {
                _labelBrush = value;
            }
		}
        [StripChartConfig]
        public Pen GraphPen {
			get => _graphPen;
            set
            {
                _graphPen = value;
            }
		}
        [StripChartConfig]
        public int UpdatePeriod {
			get => _updatePeriod;
            set
            {
                _updatePeriod = value;
            }
		}
        // The following properties are probably less used and may eventually not be exported.
        [StripChartConfig]
        public int AutoScrollInhibitTime {
			get => _autoScrollInhibitTime;
            set
            {
                _autoScrollInhibitTime = value;
            }
		}


        #endregion Config Properties

        #region Config Fields
        private double _rangeMin;
        private double _rangeMax;
        private int _largeStepsPerScreen;
        private double _shiftAtFraction;
        private double _secondsPerSmallChange;
        private double _inchesPerSecond;
        private int _updatePeriod;
        private int _autoScrollInhibitTime;

        // Appearance
        private Font _labelFont;
        private Brush _labelBrush;
        private Pen _graphPen;
        #endregion Config Fields

        #region Helper Properties
        // Internal use properties
        Sample FirstSample { get => _samples.Count > 0 ? _samples[0] : null; }
        Sample LastSample { get => _samples.Count > 0 ? _samples[_samples.Count - 1] : null; }
        bool HaveNewSamples
        {
            get => _firstNewSampleIndex >= 0;
        }
        bool HaveSamples { get => _samples.Count > 0; }
        // TODO: Redo StartTime property to allow user to set a start time earlier than the first sample.
        public DateTime StartTime { get => _samples.Count > 0 ? _samples[0].Time : DateTime.MinValue; }
        bool LastSampleVisible
        {
            get => HaveSamples && GetSampleInfo(LastSample).Visible;
        }
        bool IsUserControllingScroll
        {
            get => _isScrollDragInProgress || _isScrollActivity;
        }
        #endregion Helper Properties

        #region Fields
        private PaintInfo _paintInfo;
        private ScrollInfo _scrollInfo = null;
        private DateTime _firstVisibleTime = DateTime.MinValue;
        private int _firstNewSampleIndex = -1;
        private List<Sample> _samples = new List<Sample>();
        private Timer _updateTimer;
        private bool inhibitScrollEvent;
        // TODO: Consider setting this flag for a period of time after each user scroll operation (e.g., click in channel).
        // Rationale: Even if user isn't actively dragging scrollbar, clicking over and over in the scrollbar should be the same.
        private bool _isScrollDragInProgress;
        private bool _isScrollActivity;
        private bool _isLastSampleVisible;
        private Timer _scrollActivityTimer;

        #endregion Fields
        #region Types
        class ScrollInfo
        {
            public double SecondsPerSmallChange;
            public double SecondsPerLargeChange;
            public int ScrollValue;
        }
        class PaintInfo
        {
            public int FirstSampleIdx;
            public int LastSampleIdx;
            public DateTime FirstVisibleTime;
            public Rectangle PlotArea;
            public double PixelsPerSecond;
        }
        class SampleInfo
        {
            public DateTime Time;
            public bool Visible;
            public int PixelOffset;
            public double FractionalOffset;
            public bool InEndZone;
        }

        class Sample : IComparable<Sample>
        {
            public DateTime Time;
            public double Value;
            public int CompareTo(Sample rhs)
            {
                if (Time < rhs.Time) { return -1; }
                else if (Time > rhs.Time) { return 1; }
                else { return 0; }
            }
            public static bool operator <(Sample lhs, Sample rhs) => lhs.Time < rhs.Time;
            public static bool operator <=(Sample lhs, Sample rhs) => lhs.Time <= rhs.Time;
            public static bool operator >(Sample lhs, Sample rhs) => lhs.Time > rhs.Time;
            public static bool operator >=(Sample lhs, Sample rhs) => lhs.Time <= rhs.Time;
        }

        #endregion Types


        #region Methods
        public StripChart()
        {
            InitializeComponent();

            // TODO: Initialize config properties to defaults.
            InitializePropertyDefaults();


            // TODO: Remove this if no longer needed.
            //hScrollBar.Visible = true;
            //hScrollBar.BringToFront();

            // Initialize timer for adding accumulated samples to the plot periodically.
            _updateTimer = new Timer { Enabled = true, Interval = UpdatePeriodDefault };
            _updateTimer.Tick += UpdateTimerTick;

            // Initialize timer used to prevent auto scroll for brief interval following any user-initiated scroll activity.
            _scrollActivityTimer = new Timer { Enabled = false, Interval = 250 };
            _scrollActivityTimer.Tick += OnScrollActivityTimerExpired;

            // Draw plot once even before samples have been provided.
            panel.Invalidate();
        }

        public void Reset()
        {
            // Question: Stop timer?
            _samples.Clear();
            panel.Invalidate();
            _firstNewSampleIndex = -1;
        }

        // Invalidate just the plot area (not labels and axes).
        private void InvalidatePlot()
        {
            panel.Invalidate(GetPlotArea());
        }
        private void InitializePropertyDefaults()
        {
            RangeMin = RangeMinDefault;
            RangeMax = RangeMaxDefault;
            UpdatePeriod = UpdatePeriodDefault;
            LargeStepsPerScreen = LargeStepsPerScreenDefault;
            ShiftAtFraction = ShiftAtFractionDefault;
            SecondsPerSmallChange = SecondsPerSmallChangeDefault;
            InchesPerSecond = InchesPerSecondDefault;

            // Lesser used...
            AutoScrollInhibitTime = AutoScrollInhibitTimeDefault;

            // Create and cache fonts, brushes, etc.
            _labelFont = new Font(Font, FontStyle.Bold);
            _labelBrush = new SolidBrush(Color.Black);
            _graphPen = new Pen(Color.Black);
        }

        // TODO: Alter api to allow multiple samples at same time (AddSamples) or like this for separate but specifying which trace.
        public void AddSample(DateTime time, double value)
        {
            if (!HaveSamples)
                _firstVisibleTime = time;
            if (!HaveNewSamples)
                _firstNewSampleIndex = _samples.Count;
            // Accumulate a single sample.
            _samples.Add( new Sample { Time = time, Value = value } );
        }

        // Return a clipping rectangle that covers the input pair of samples.
        private Rectangle GetClipRect(Sample firstSample, Sample lastSample)
        {
            // Determine time distance from left visible edge
            int x1 = (int)((firstSample.Time - _firstVisibleTime).TotalSeconds *
                    _paintInfo.PixelsPerSecond);
            int x2 = x1 + (int)Math.Ceiling((lastSample.Time - _firstVisibleTime).TotalSeconds *
                    _paintInfo.PixelsPerSecond);

            var plotRect = GetPlotArea();

            Rectangle clip = new Rectangle
            {
                X = x1,
                Y = plotRect.Top,
                Width = x2 - x1,
                Height = plotRect.Height
            };

            return clip;
        }

        // Return Rectangle corresponding to the plot area (excluding axes, labels, title, etc.).
        // TODO: Take axes, labels, etc. into account.
        private Rectangle GetPlotArea()
        {
            var ret = panel.ClientRectangle;
            ret.Height = panel.Height - hScrollBar.Height;
            return ret;
        }

        // Process samples added since the last tick...
        private void UpdateTimerTick(object sender, EventArgs e)
        {
            if (!HaveNewSamples)
                // Nothing to do...
                return;

            // Calculate a clipping rect covering the range of samples added.
            var clipRect = GetClipRect(_samples[_firstNewSampleIndex], _samples[_samples.Count - 1]);

            // Find the intersection of the sample range rect with the visible area.
            clipRect.Intersect(GetPlotArea());

            if (clipRect.Width > 0)
                panel.Invalidate(clipRect);

        }

        // Calculate index of sample most nearly corresponding to input time, considering lessOrEqual to determine
        // whether to return index just below or above where no exact match exists.
        private int GetSampleIndexAtTime(DateTime time, bool lessOrEqual = false)
        {
            if (!HaveSamples)
                return -1;

            // Note: If no exact match, BinarySearch finds sample just past the sought time.
            int index = _samples.BinarySearch(new Sample { Time = time, Value = 0.0 },
                Comparer<Sample>.Create((Sample a, Sample b) => a.CompareTo(b)));
            if (index < 0)
            {
                // Didn't find exact match. Bitwise complement of index points either just past end of list or to the
                // first sample larger than requested time.
                index = ~index;
                if (lessOrEqual)
                {
                    // Work backwards to find first element <= target.
                    for (int i = index - 1; i >= 0; i--)
                        if (time <= _samples[i].Time)
                            return i;
                    // If here, reached beginning of list.
                    return 0;
                }
                else // greater or equal
                    // If index points past end, return final element of list.
                    return index < _samples.Count ? index : _samples.Count - 1;
            }
            // Found exact match.
            return index;
        }

        // Convert sample to pixel offset in client area.
        // TODO: Decide what to do if the point is not in view: return null or just return Point outside viewport?
        private Point GetSamplePoint(Sample sample)
        {
            // Domain
            TimeSpan timeOffset = sample.Time - _firstVisibleTime;
            int x = (int)Math.Round((timeOffset.TotalSeconds * _paintInfo.PixelsPerSecond));

            // Range
            var ratio = (sample.Value - _rangeMin) / (_rangeMax - _rangeMin);
            int y = (int)Math.Round(_paintInfo.PlotArea.Bottom - ratio * _paintInfo.PlotArea.Height);
            return new Point(x, y);
        }

        private SampleInfo GetSampleInfo(Sample sample)
        {
            double offset = (sample.Time - _firstVisibleTime).TotalSeconds * _paintInfo.PixelsPerSecond;
            double fractionalOffset = (offset - _paintInfo.PlotArea.Left) / _paintInfo.PlotArea.Width;
            bool visible = fractionalOffset >= 0 && fractionalOffset <= 1;
            return new SampleInfo
            {
                Time = sample.Time,
                PixelOffset = (int)offset,
                FractionalOffset = fractionalOffset,
                Visible = visible,
                InEndZone = fractionalOffset >= ShiftAtFraction && visible
            };
        }

        // Pre-Condition: Chart info has been cached.
        private double FractionToSeconds(double fraction)
        {
            return fraction * _paintInfo.PlotArea.Width / _paintInfo.PixelsPerSecond;
        }
        private TimeSpan FractionToTimeSpan(double fraction)
        {
            return TimeSpan.FromSeconds(fraction * _paintInfo.PlotArea.Width / _paintInfo.PixelsPerSecond);
        }

        // If end of samples was in view when the unprocessed samples were added to the queue and the most recently
        // added sample is at least ShiftAtFraction from left edge of viewport, perform a shift (by modifying
        // _firstVisibleTime) that puts the most recently added samples a configurable distance from the left edge of
        // the viewport and return true.
        // Return: true if and only if shift has been performed.
        private bool ShiftViewportMaybe()
        {
            // Shift can be required only when new samples are added.
            if (_firstNewSampleIndex < 0)
                return false;

            // Where was last sample added?
            var firstSampleInfo = GetSampleInfo(_samples[_firstNewSampleIndex]);
            var lastSampleInfo = GetSampleInfo(_samples[_samples.Count - 1]);
            // Is shift required?
            // Logic: Never shift while user is actively controlling position (e.g. with scrollbar) or if the user has
            // scrolled leftward such that the added samples are entirely off screen. OTOH, Do shift if the final sample
            // was visible at the time of the last paint but has been moved (e.g., due to zoom).
            if (!IsUserControllingScroll && firstSampleInfo.Visible && (lastSampleInfo.InEndZone || !lastSampleInfo.Visible))
            {
                // Shift needed.
                // Logic: Prefer putting first added sample at start of visible area, but if this puts last added sample
                // past a max, slide back till last added sample doesn't exceed the max.
                _firstVisibleTime = _samples[_firstNewSampleIndex].Time - FractionToTimeSpan(ShiftToFractionDefault);
                if (lastSampleInfo.FractionalOffset - firstSampleInfo.FractionalOffset > ShiftAtFractionDefault)
                {
                    // Set _firstVisibleTime to place last added sample at max.
                    DateTime maxTime = _firstVisibleTime + FractionToTimeSpan(ShiftToMaxFractionDefault);
                    _firstVisibleTime += lastSampleInfo.Time - maxTime;
                }

                UpdateScrollBar();

                panel.Invalidate();
                return true;

            }

            return false;
        }

        private void RecordScrollActivity()
        {
            // Cancel timer (if active) and reset.
            _scrollActivityTimer.Enabled = false;
            _scrollActivityTimer.Interval = AutoScrollInhibitTime;
            _isScrollActivity = true;
            _scrollActivityTimer.Enabled = true;
            _isScrollDragInProgress = hScrollBar.Capture;
            Console.WriteLine($"Capture: {_isScrollDragInProgress}");
        }

        // Handle a single scroll event using information cached in _scrollInfo at the time of the last paint.
        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            RecordScrollActivity();
            if (inhibitScrollEvent) return;
            if (!HaveSamples) return;
            // Do nothing if scroll info isn't cached.
            if (_scrollInfo == null) return;
            var sb = sender as HScrollBar;
            // Calculate signed time offset from current position.
            var timeOffset = (e.NewValue - _scrollInfo.ScrollValue) * _scrollInfo.SecondsPerSmallChange;
            _firstVisibleTime += TimeSpan.FromSeconds(timeOffset);
            // Limit leftward and rightward travel.
            // Assumption: Scroll limits calculated such that final sample is at left edge of viewport when scrolled all the way right.
            if (_firstVisibleTime < _samples[0].Time)
                _firstVisibleTime = _samples[0].Time;
            else if (_firstVisibleTime > _samples[_samples.Count - 1].Time)
                _firstVisibleTime = _samples[_samples.Count - 1].Time;

            // Assume plot needs full redraw.
            panel.Invalidate();
        }

        private void OnScrollActivityTimerExpired(object sender, EventArgs e)
        {
            _isScrollActivity = false;
        }

        private void hScrollBar_MouseCaptureChanged(object sender, EventArgs e)
        {
            var sb = sender as HScrollBar;
            Console.WriteLine("Capture lost!");
            _isScrollDragInProgress = false;
        }

        // Calculate and apply updated parameters for horizontal scrollbar, taking full time extent of samples into
        // account, and cache the parameters that will be needed by the scroll handler in _scrollInfo.
        private void UpdateScrollBar()
        {
            if (!HaveSamples)
                return;

            // Calculate total time span required to cover all samples.
            double totalTimeWidth = (LastSample.Time - FirstSample.Time).TotalSeconds;
            // Calculate time span of single screen full.
            double screenTimeWidth = _paintInfo.PlotArea.Width / _paintInfo.PixelsPerSecond;

            // Logic: SmallChange is always 1, and LargeChange is scaled to give an approximate number of steps across
            // the viewport.
            var largeChange = (int)Math.Ceiling(screenTimeWidth / LargeStepsPerScreen / SecondsPerSmallChange);
            double secondsPerLargeChange = largeChange * SecondsPerSmallChange;
            // Note: Documentation advises subtracting visible width from total width when calculating max value;
            // however, this makes sense only if we don't want to be able to scroll the final sample time in from the
            // right edge of the window (as would be the case when displaying an image). In our case, we needn't
            // subtract anything from totalTimeWidth, unless we want to ensure that more than one sample is visible when
            // we're scrolled all the way right.
            // TODO: Consider adding a margin for this...
            var maxValue =
                (int)Math.Ceiling(Math.Max(0, totalTimeWidth) / SecondsPerSmallChange) + largeChange - 1;

            // Calculate number of small steps between first sample time and first visible time.
            // Design Decision: When the division produces a remainder, we round to nearest whole number, safe in the
            // knowledge that scroll position zero's time is always hard-coded, never calculated from _firstVisibleTime.
            int offset =
                (int)Math.Round((_firstVisibleTime - FirstSample.Time).TotalSeconds / SecondsPerSmallChange);

            // Cache scroll info for use in scroll handler.
            _scrollInfo = new ScrollInfo {
                SecondsPerLargeChange = secondsPerLargeChange,
                SecondsPerSmallChange = SecondsPerSmallChange,
                ScrollValue = offset
            };

            // Update scrollbar with events inhibited so as not to perform a scroll.
            inhibitScrollEvent = true;
            hScrollBar.Minimum = 0;
            hScrollBar.SmallChange = 1;
            hScrollBar.LargeChange = largeChange;
            hScrollBar.Maximum = maxValue;
            hScrollBar.Value = offset;
            inhibitScrollEvent = false;
        }

        private PaintInfo GetPaintInfo(Graphics graphics, Rectangle clipRect)
        {
            var ret = new PaintInfo();

            // Note: A change to pixels per second would invalidate previously drawn portions of the graph, so either
            // don't allow it, or else invalidate upon change.
            ret.PixelsPerSecond = graphics.DpiX * _inchesPerSecond;
            ret.PlotArea = GetPlotArea();

            return ret;
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            // Attempt at vertical text.
            var size = panel.Size;
            var g = e.Graphics;
            // Cache for later...
            _paintInfo = GetPaintInfo(g, e.ClipRectangle);

            // Get time offset to both edges of clip area.
            DateTime timeLeft = _firstVisibleTime + TimeSpan.FromSeconds(e.ClipRectangle.Left / _paintInfo.PixelsPerSecond);
            DateTime timeRight = _firstVisibleTime + TimeSpan.FromSeconds(e.ClipRectangle.Right / _paintInfo.PixelsPerSecond);
            var firstSampleIdx = GetSampleIndexAtTime(timeLeft, true);
            var lastSampleIdx = GetSampleIndexAtTime(timeRight);

            if (firstSampleIdx < 0)
                // Nothing to do...
                return;

            // Decide whether a shift is required.
            if (ShiftViewportMaybe())
                // Shift invalidates the paint we were about to do, so just return.
                return;

            // Redraw range of new and/or shifted samples.
            PlotSampleRange(g, firstSampleIdx, lastSampleIdx);

            // Make note of whether final sample was visible at time of paint, which matters (eg) when zoom causes the
            // last sample to leave viewport abruptly.
            // TODO: Consider adding these to a post-paint cache, analogous to _paintInfo; alternatively, consider just
            // breaking PaintInfo out into individual fields.
            _isLastSampleVisible = LastSampleVisible;

            _firstNewSampleIndex = -1;

            // Update scrollbar to account for added samples and possible shift.
            UpdateScrollBar();
        }

        private void PlotSampleRange(Graphics graphics, int firstSampleIdx, int lastSampleIdx)
        {
            // Loop over all samples not yet added.
            var pt1 = GetSamplePoint(_samples[firstSampleIdx]);
            int idx;
            Console.WriteLine($"Painting...");
            for (idx = firstSampleIdx + 1; idx <= lastSampleIdx; idx++)
            {
                var pt2 = GetSamplePoint(_samples[idx]);

                // TODO: Connected curve or points only?
                //Console.WriteLine($"ClipRect: {e.ClipRectangle}");
                //Console.WriteLine($"PlotArea: {GetPlotArea()}");
                //Console.WriteLine($"Sample: ({_samples[idx].time}, {_samples[idx].value})");
                //Console.WriteLine($"Drawing line from {pt1} to {pt2}");
                graphics.DrawLine(_graphPen, pt1, pt2);
                pt1 = pt2;
            }
        }

        private void LabelStuff()
        {
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
        #endregion Methods

        }

    }
}
