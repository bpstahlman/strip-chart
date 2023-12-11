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
        internal const double ShiftLeftAtFractionDefault = 0.1;
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
        public double ShiftLeftAtFraction {
			get => _shiftLeftAtFraction;
            set
            {
                _shiftLeftAtFraction = value;
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
        public double ShiftLeftToFraction {
			get => _shiftLeftToFraction;
            set
            {
                _shiftLeftToFraction = value;
            }
		}
        [StripChartConfig]
        public double ShiftToFraction {
			get => _shiftToFraction;
            set
            {
                _shiftToFraction = value;
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
        private double _shiftLeftAtFraction;
        private double _shiftAtFraction;
        private double _shiftLeftToFraction;
        private double _shiftToFraction;
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
        // TODO: Consider whether this could be obtained without an explicit flag, simply by comparing _timeOffset with
        // previous _timeOffset.
        bool InReverseMode { get => _inReverseMode;  }
        Sample FirstSample { get => _samples.Count > 0 ? _samples[0] : null; }
        Sample LastSample { get => _samples.Count > 0 ? _samples[_samples.Count - 1] : null; }
        bool HaveSamples { get => _samples.Count > 0; }
        public DateTime StartTime {
            get => _startTime;
            set => _startTime = value;
        }
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
        private DateTime _startTime = DateTime.MinValue;
        // TimeSpan representing current time offset from _startTime
        private TimeSpan _timeOffset = TimeSpan.Zero;
        private TimeSpan _lastTimeOffset = TimeSpan.Zero;
        //private DateTime _firstVisibleTime = DateTime.MinValue;
        private TimeSpan _firstVisibleTimeOffset = TimeSpan.Zero;
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
        private bool _inReverseMode = false;

        #endregion Fields
        #region Types
        class ScrollInfo
        {
            public double SecondsPerSmallChange;
            public double SecondsPerLargeChange;
            public int ScrollValue;
            // Note: This one is saved for convenience, as it can be calculated from the scrollbar properties.
            public TimeSpan TotalTimeSpan;
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
            public TimeSpan TimeOffset;
            public TimeSpan TimeOffsetInView;
            public bool Visible;
            public int PixelOffset;
            public double FractionalOffset;
            public bool InEndZone;
        }

        class Sample : IComparable<Sample>
        {
            public TimeSpan TimeOffset;
            public double Value;
            public int CompareTo(Sample rhs)
            {
                if (TimeOffset < rhs.TimeOffset) { return -1; }
                else if (TimeOffset > rhs.TimeOffset) { return 1; }
                else { return 0; }
            }
            public static bool operator <(Sample lhs, Sample rhs) => lhs.TimeOffset < rhs.TimeOffset;
            public static bool operator <=(Sample lhs, Sample rhs) => lhs.TimeOffset <= rhs.TimeOffset;
            public static bool operator >(Sample lhs, Sample rhs) => lhs.TimeOffset > rhs.TimeOffset;
            public static bool operator >=(Sample lhs, Sample rhs) => lhs.TimeOffset <= rhs.TimeOffset;
        }

        #endregion Types


        #region Methods
        public StripChart()
        {
            InitializeComponent();

            // TODO: Initialize config properties to defaults.
            InitializePropertyDefaults();

            // Initialize timer for adding accumulated samples to the plot periodically.
            // TODO: Time will be used for automated playback only.
            //_updateTimer = new Timer { Enabled = true, Interval = UpdatePeriodDefault };
            //_updateTimer.Tick += UpdateTimerTick;

            // Initialize timer used to prevent auto scroll for brief interval following any user-initiated scroll activity.
            _scrollActivityTimer = new Timer { Enabled = false, Interval = 50 };
            _scrollActivityTimer.Tick += OnScrollActivityTimerExpired;

            // Draw plot once even before samples have been provided.
            panel.Invalidate();
        }

        public void SetTime(TimeSpan timeOffset)
        {
            _timeOffset = timeOffset;
            // TODO: Somehow, we need to check for shift here, possibly in lieu of what's done in ProcessSamples(),
            // since shift is now possible even when no samples have been added.
            UpdatePlot();

            _lastTimeOffset = _timeOffset;
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
            ShiftLeftAtFraction = ShiftLeftAtFractionDefault;
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
        public void AddSample(TimeSpan timeOffset, double value)
        {
            if (_startTime == DateTime.MinValue) {
                // Design Decision: Don't require client to set start time explicitly.
                _startTime = DateTime.Now;
                _firstVisibleTimeOffset = TimeSpan.Zero;
            }

            // Accumulate a single sample.
            _samples.Add( new Sample { TimeOffset = timeOffset, Value = value } );
        }

        // Process recently added samples.
        private void UpdatePlot()
        {
            // Brainstorming... start/end could be firstNew and final *or* in reverse mode, it could be current time and
            // what was current time last time.
            // Actually, it's always about current time, never about last sample added, since there's nothing that would
            // preclude client adding samples in advance!


            // Get the times in left-to-right order.
            // TODO: How to bootstrap _lastTimeOffset on direction change?
            var (t1, t2) = InReverseMode ? (_timeOffset, _lastTimeOffset) : (_lastTimeOffset, _timeOffset);

            // Caveat: Clipping rect must be extended to include all samples that weren't drawn on last update;
            // otherwise, the curve can appear dotted when sampling rate is low.
            if (InReverseMode)
            {

            } else
            {

            }
            var leftIdx = GetIndexOfSampleAtTimeOffset(t1, true);
            var rightIdx = GetIndexOfSampleAtTimeOffset(t2);

            // Calculate a clipping rect covering the new time range.
            var clipRect = GetClipRectForTimeRange(t1, t2);

            // Find the intersection of the sample range rect with the visible area.
            clipRect.Intersect(GetPlotArea());

            if (clipRect.Width > 0)
                panel.Invalidate(clipRect);

        }

        // Schedule shift if appropriate, returning true if shift will be performed.
        private bool ShiftViewportMaybe()
        {
            // Skip shift if user is overriding position (e.g., with scrollbars).
            if (IsUserControllingScroll)
                return false;

            // Also skip shift if neither last time offset nor current time offset is in view.
            if (!IsTimeOffsetInView(_lastTimeOffset) && !IsTimeOffsetInView(_timeOffset))
                return false;

            bool shift = false;
            var timeOffsetInView = _timeOffset - _firstVisibleTimeOffset;
            var fractionalOffset = TimeSpanToFraction(timeOffsetInView);
            if (InReverseMode)
            {
                if (fractionalOffset < ShiftLeftAtFraction)
                {
                    // Left shift needed!
                    _firstVisibleTimeOffset -= FractionToTimeSpan(ShiftLeftToFraction - fractionalOffset);
                    shift = true;
                }
            } else if (fractionalOffset >= ShiftAtFraction)
            {
                // Right shift needed!
                _firstVisibleTimeOffset += FractionToTimeSpan(fractionalOffset - ShiftToFraction);
                shift = true;
            } 

            if (shift)
                panel.Invalidate();

            return shift;
        }
        private void panel_Paint(object sender, PaintEventArgs e)
        {
            // Attempt at vertical text.
            var size = panel.Size;
            var g = e.Graphics;
            // Cache for later...
            _paintInfo = GetPaintInfo(g, e.ClipRectangle);

            // Get time offset to both edges of clip area.
            TimeSpan leftSampleTimeOffset =
                _firstVisibleTimeOffset + TimeSpan.FromSeconds(e.ClipRectangle.Left / _paintInfo.PixelsPerSecond);
            TimeSpan rightSampleTimeOffset =
                _firstVisibleTimeOffset + TimeSpan.FromSeconds(e.ClipRectangle.Right / _paintInfo.PixelsPerSecond);
            // Sample draw loop runs from sample1 to sample2, determined according to the following logic:
            // Sample1: Later of the following:
            //   first sample
            //   latest sample <= left edge of clipping area
            // Sample2: Earlier of the following:
            //   final sample
            //   earliest sample > right edge of clipping area
            var leftIdx = GetIndexOfSampleAtTimeOffset(leftSampleTimeOffset, true);
            var rightIdx = GetIndexOfSampleAtTimeOffset(rightSampleTimeOffset);

            if (leftIdx < 0)
                // Nothing to do...
                return;

            // Decide whether a shift is required.
            if (ShiftViewportMaybe())
                // Shift invalidates the paint we were about to do, so just return.
                return;

            // Redraw range of samples.
            PlotSampleRange(g, leftIdx, rightIdx);

            // Update scrollbar to account for added samples and possible shift.
            UpdateScrollBar();
        }

        private void PlotSampleRange(Graphics graphics, int leftSampleIdx, int rightSampleIdx)
        {
            // Loop over all samples not yet added.
            var pt1 = GetSamplePoint(_samples[leftSampleIdx]);
            int idx;
            Console.WriteLine($"Painting...");
            for (idx = leftSampleIdx + 1; idx <= rightSampleIdx; idx++)
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
            _firstVisibleTimeOffset += TimeSpan.FromSeconds(timeOffset);
            // Limit leftward and rightward travel.
            // Assumption: Scroll limits calculated such that final sample is at left edge of viewport when scrolled all the way right.
            if (_firstVisibleTimeOffset < TimeSpan.Zero)
                _firstVisibleTimeOffset = TimeSpan.Zero;
            else if (_firstVisibleTimeOffset > _scrollInfo.TotalTimeSpan)
                _firstVisibleTimeOffset = _scrollInfo.TotalTimeSpan;

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

        // Calculate and apply updated parameters for horizontal scrollbar, taking full time extents into account, and
        // cache the parameters that will be needed by the scroll handler in _scrollInfo.
        private void UpdateScrollBar()
        {
            // Calculate time span of single screenful.
            double screenTimeWidth = _paintInfo.PlotArea.Width / _paintInfo.PixelsPerSecond;
            // Calculate total time span required to cover all samples, rounding up so that the final screen will have
            // the current time just in view.
            // Rationale: This will significantly reduce the required number of scrollbar changes.
            double totalTimeWidth = Math.Ceiling(_timeOffset.TotalSeconds / screenTimeWidth) * screenTimeWidth;

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

            // Calculate number of small steps between start time and first visible time.
            // Design Decision: When the division produces a remainder, we round to nearest whole number, safe in the
            // knowledge that scroll position zero's time is always hard-coded, never calculated from _firstVisibleTime.
            int offset =
                (int)Math.Round(_firstVisibleTimeOffset.TotalSeconds / SecondsPerSmallChange);

            // Cache scroll info for use in scroll handler.
            _scrollInfo = new ScrollInfo {
                SecondsPerLargeChange = secondsPerLargeChange,
                SecondsPerSmallChange = SecondsPerSmallChange,
                ScrollValue = offset,
                TotalTimeSpan = TimeSpan.FromSeconds(totalTimeWidth)
            };

            // TODO: Any reason to avoid property sets when no change? I'm assuming scrollbar checks for changes.
            // Update scrollbar with events inhibited so as not to perform a scroll.
            inhibitScrollEvent = true;
            hScrollBar.Minimum = 0;
            hScrollBar.SmallChange = 1;
            hScrollBar.LargeChange = largeChange;
            hScrollBar.Maximum = maxValue;
            hScrollBar.Value = offset;
            inhibitScrollEvent = false;
        }

        // Return a clipping rectangle that covers the input pair of samples.
        private Rectangle GetClipRectForTimeRange(TimeSpan timeOffset1, TimeSpan timeOffset2)
        {
            // Determine time distance from left visible edge
            int x1 = TimeSpanToPixels(timeOffset1 - _firstVisibleTimeOffset);
            int x2 = TimeSpanToPixels(timeOffset2 - _firstVisibleTimeOffset);

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
        // Return a clipping rectangle that covers the input pair of samples.
        private Rectangle GetClipRect(Sample firstSample, Sample lastSample)
        {
            // Determine time distance from left visible edge
            int x1 = (int)((firstSample.TimeOffset - _firstVisibleTimeOffset).TotalSeconds *
                    _paintInfo.PixelsPerSecond);
            int x2 = x1 + (int)Math.Ceiling((lastSample.TimeOffset - _firstVisibleTimeOffset).TotalSeconds *
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

        // Find and return Sample most nearly corresponding to input time, considering lessOrEqual to determine whether
        // to return index just below or above where no exact match exists.
        // Note: -1 is returned only for an empty list; for a non-empty list, first or last element index is returned if
        // the preferred element isn't found.
        private int GetIndexOfSampleAtTimeOffset(TimeSpan timeOffset, bool lessOrEqual = false)
        {
            if (!HaveSamples)
                return -1;

            // Note: If no exact match, BinarySearch finds sample just past the sought time.
            int index = _samples.BinarySearch(new Sample { TimeOffset = timeOffset, Value = 0.0 },
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
                        if (timeOffset <= _samples[i].TimeOffset)
                            return i;
                    // If here, reached beginning of list without finding one <= input time, so return index 0.
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
            int x = (int)Math.Round(((sample.TimeOffset - _firstVisibleTimeOffset).TotalSeconds * _paintInfo.PixelsPerSecond));

            // Range
            var ratio = (sample.Value - _rangeMin) / (_rangeMax - _rangeMin);
            int y = (int)Math.Round(_paintInfo.PlotArea.Bottom - ratio * _paintInfo.PlotArea.Height);
            return new Point(x, y);
        }

        private SampleInfo GetSampleInfo(Sample sample)
        {
            TimeSpan viewportTimeOffset = sample.TimeOffset - _firstVisibleTimeOffset;
            double fractionalOffset = TimeSpanToFraction(viewportTimeOffset);
            bool visible = fractionalOffset >= 0 && fractionalOffset <= 1;
            return new SampleInfo
            {
                TimeOffset = sample.TimeOffset,
                TimeOffsetInView = sample.TimeOffset - _firstVisibleTimeOffset,
                PixelOffset = (int)TimeSpanToPixels(viewportTimeOffset),
                FractionalOffset = fractionalOffset,
                Visible = visible,
                InEndZone = fractionalOffset >= ShiftAtFraction && visible
            };
        }

        // Pre-Condition: Chart info has been cached.
        private int ViewportWidth
        {
            get => _paintInfo.PlotArea.Width;
        }
        private TimeSpan ViewportTimeSpan
        {
            get => TimeSpan.FromSeconds(ViewportWidth / _paintInfo.PixelsPerSecond);
        }
        private double FractionToSeconds(double fraction)
        {
            return fraction * ViewportTimeSpan.TotalSeconds;
        }
        private TimeSpan FractionToTimeSpan(double fraction)
        {
            return TimeSpan.FromSeconds(fraction * ViewportTimeSpan.TotalSeconds);
        }
        private double TimeSpanToFraction(TimeSpan timeSpan)
        {
            return timeSpan.TotalSeconds / ViewportTimeSpan.TotalSeconds;
        }
        private int TimeSpanToPixels(TimeSpan timeSpan)
        {
            return (int)(TimeSpanToFraction(timeSpan) * ViewportWidth);
        }

        private TimeSpan LastVisibleTimeOffset
        {
            get => _firstVisibleTimeOffset + ViewportTimeSpan;
        }
        private bool IsTimeOffsetInView(TimeSpan timeOffset)
        {
            return timeOffset >= _firstVisibleTimeOffset && timeOffset <= LastVisibleTimeOffset;
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
