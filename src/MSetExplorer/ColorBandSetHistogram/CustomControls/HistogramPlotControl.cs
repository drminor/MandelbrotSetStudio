using MSS.Types;
using ScottPlot;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	public class HistogramPlotControl : ContentControl
	{
		#region Private Fields 

		private FrameworkElement _ourContent;

		private WpfPlot? _wpfPlot1;
		private ScottPlot.Plottable.ScatterPlot? _thePlot;
		private int _thePlotExtent;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static HistogramPlotControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramPlotControl), new FrameworkPropertyMetadata(typeof(HistogramPlotControl)));
		}

		public HistogramPlotControl()
		{
			_ourContent = new FrameworkElement();

			_wpfPlot1 = null;
			_thePlot = null;
			_thePlotExtent = 400;
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ContentViewportSizeChanged;

		#endregion

		#region Public Properties

		public WpfPlot? WpfPlot1
		{
			get => _wpfPlot1;
			set
			{
				_wpfPlot1 = value;

				if (_wpfPlot1 == null)
				{
					return;
				}

				var seriesData = SeriesData;

				if (seriesData.IsEmpty())
				{
					return;
				}

				_thePlot = CreateScatterPlot(_wpfPlot1, seriesData);
				_thePlotExtent = seriesData.DataX.Length;

				_wpfPlot1.Refresh();
			}
		}

		public HPlotSeriesData SeriesData
		{
			get => (HPlotSeriesData)GetValue(SeriesDataProperty);
			set => SetCurrentValue(SeriesDataProperty, value);
		}

		public SizeDbl ContentViewportSize
		{
			get => (SizeDbl)GetValue(ContentViewportSizeProperty);
			set => SetCurrentValue(ContentViewportSizeProperty, value);
		}

		public VectorDbl DisplayPosition
		{
			get => (VectorDbl)GetValue(DisplayPositionProperty);
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(value, DisplayPosition))
				{
					SetCurrentValue(DisplayPositionProperty, value);
				}
			}
		}

		#endregion

		#region Private Methods - Control

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_ourContent.Measure(availableSize);

			double width = availableSize.Width;
			double height = availableSize.Height;

			if (double.IsInfinity(width))
			{
				width = childSize.Width;
			}

			if (double.IsInfinity(height))
			{
				height = childSize.Height;
			}

			var result = new Size(width, height);

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGripControl Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			return result;
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = base.ArrangeOverride(finalSize);

			if (childSize != finalSize) Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");

			//Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");
			Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl - Before Arrange{finalSize}. Base returns {childSize}.");

			_ourContent.Arrange(new Rect(finalSize));

			if (WpfPlot1 != null)
			{
				WpfPlot1.Width = childSize.Width;
				WpfPlot1.Height = childSize.Height;
			}

			// The ViewportSize is the logical ContentViewportSizxe
			//ViewportSize = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			//Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			return finalSize;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();
				WpfPlot1 = BuildContentModel(_ourContent);
			}
			else
			{
				throw new InvalidOperationException("Did not find the HistogramDisplayControl_Content template.");
			}
		}

		private WpfPlot BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is WpfPlot wp)
				{
					return wp;
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the HistogramPlotControl's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region SeriesData Dependency Property

		public static readonly DependencyProperty SeriesDataProperty = DependencyProperty.Register(
					"SeriesData", typeof(HPlotSeriesData), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(HPlotSeriesData.Empty, FrameworkPropertyMetadataOptions.None, SeriesData_PropertyChanged));

		private static void SeriesData_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;

			//var previousValue = (HPlotSeriesData)e.OldValue;
			var newValue = (HPlotSeriesData)e.NewValue;

			c.DisplayPlot(newValue);
		}

		#endregion

		#region ContentViewportSize Dependency Property

		public static readonly DependencyProperty ContentViewportSizeProperty = DependencyProperty.Register(
					"ContentViewportSize", typeof(SizeDbl), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(SizeDbl.Zero, FrameworkPropertyMetadataOptions.None, ContentViewportSize_PropertyChanged));

		private static void ContentViewportSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;

			var previousValue = (SizeDbl)e.OldValue;
			var newValue = (SizeDbl)e.NewValue;

			if (newValue.Width == 0 && newValue.Height == 0)
			{
				return;
			}

			Debug.WriteLineIf(c._useDetailedDebug, $"\n\t\t====== The HistogramPlotControl's ContentViewportSize is being updated from {previousValue} to {newValue}.");

			c.UpdatePlotViewportSize(previousValue.Width, newValue.Width);
			c.ContentViewportSizeChanged?.Invoke(c, new ValueTuple<SizeDbl, SizeDbl>(previousValue, newValue));
		}

		#endregion

		#region DisplayPosition Dependency Property

		public static readonly DependencyProperty DisplayPositionProperty = DependencyProperty.Register(
					"DisplayPosition", typeof(VectorDbl), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, DisplayPosition_PropertyChanged));

		private static void DisplayPosition_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;
			var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdatePlotStartX(previousValue.X, newValue.X);
		}

		#endregion

		#region Private Methods - Content

		private void DisplayPlot(HPlotSeriesData seriesData)
		{
			if (WpfPlot1 == null)
			{
				return;
			}

			if (seriesData.IsEmpty())
			{
				//WpfPlot1.Plot.Clear();
				return;
			}

			if (_thePlot == null)
			{
				_thePlot = CreateScatterPlot(WpfPlot1, seriesData);
				_thePlotExtent = seriesData.DataX.Length;
			}
			else
			{
				if (seriesData.DataX.Length == _thePlotExtent)
				{
					ClearPlotLimits(WpfPlot1.Plot);
					_thePlot.UpdateY(seriesData.DataY);
					Debug.WriteLine($"The Series has {seriesData.DataX.Length}, updating existing Scatter Plot.");
				}
				else
				{
					WpfPlot1.Plot.Clear();
					_thePlot = CreateScatterPlot(WpfPlot1, seriesData);
					_thePlotExtent = seriesData.DataX.Length;

					Debug.WriteLine($"The Series has {seriesData.DataX.Length}, not updating -- creating new Scatter Plot.");
				}
			}
			
			var startingIndex = DisplayPosition.X;
			var endingIndex = startingIndex + ContentViewportSize.Width;
			SetPlotLimits(WpfPlot1, startingIndex, endingIndex);

			WpfPlot1.Refresh();
		}

		private ScottPlot.Plottable.ScatterPlot CreateScatterPlot(WpfPlot wpfPlot, HPlotSeriesData seriesData)
		{
			var result = wpfPlot.Plot.AddScatter(seriesData.DataX, seriesData.DataY);
			//WpfPlot1.Plot.Title("Hi There, I'm initialized.");

			//WpfPlot1.Plot.Frame(visible: true, color: System.Drawing.SystemColors.HotTrack, left: true, right: false, bottom: true, top: false);

			//WpfPlot1.Plot.XLabel("XLabel");
			//WpfPlot1.Plot.YLabel("YLabel");

			//WpfPlot1.Plot.AxisScale();
			return result;
		}

		private void SetPlotLimits(WpfPlot wpfPlot, double startingIndex, double endingIndex)
		{
			var plot = wpfPlot.Plot;

			if (endingIndex - startingIndex > 0)
			{
				plot.AxisAutoY();
				wpfPlot.Refresh();

				var axLimits = plot.GetAxisLimits();
				Debug.WriteLine($"Setting the XPlot Limits. Start: {startingIndex}, End: {endingIndex}. Current: X1: {axLimits.XMin}, X2: {axLimits.XMax}, Y1: {axLimits.YMin}, Y2: {axLimits.YMax}");

				plot.SetAxisLimitsX(startingIndex, endingIndex);
				plot.XAxis.SetBoundary(startingIndex - 1000, endingIndex + 1000);

				//var axLimits2 = plot.GetAxisLimits();

				//Debug.WriteLine($"Setting the YPlot Limits. Start: {axLimits2.YMin}, End: {axLimits2.YMax}. Before Setting the Limits for AxisX: Start: {axLimits.YMin}, End: {axLimits.YMax}" +
				//	$" Current: X1: {axLimits2.XMin}, X2: {axLimits2.XMax}, Y1: {axLimits2.YMin}, Y2: {axLimits2.YMax}");


				//plot.SetAxisLimitsY(axLimits2.YMin, axLimits2.YMax);
				//plot.YAxis.SetBoundary(axLimits2.YMin -10, axLimits2.YMax + 10);
			}
			else
			{
				Debug.WriteLine($"Setting the Plot Limits, Start = End, Clearing instead of Setting.");
				plot.SetAxisLimits(AxisLimits.NoLimits);
			}
		}

		private void ClearPlotLimits(Plot? plot)
		{
			if (plot != null)
			{
				Debug.WriteLine($"Clearing the Plot Limits.");

				plot.SetAxisLimits(AxisLimits.NoLimits);
			}
		}

		private bool UpdatePlotStartX(double previousValue, double newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotControl's StartingIndex is being set to {newValue} from {previousValue}.");

			if (WpfPlot1 != null)
			{
				// Use the newValue to limit the Plot's range of X values. (Starting Index)
				var startingIndex = newValue;
				var endingIndex = startingIndex + ContentViewportSize.Width;
				SetPlotLimits(WpfPlot1, startingIndex, endingIndex);
			}

			return true;
		}

		private bool UpdatePlotViewportSize(double previousValue, double newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotControl's PlotViewportSize is being set to {newValue} from {previousValue}.");

			if (WpfPlot1 != null)
			{
				// Use the newValue to limit the Plot's range of X values. (Starting Index)
				var startingIndex = DisplayPosition.X;
				var endingIndex = startingIndex + newValue;
				SetPlotLimits(WpfPlot1, startingIndex, endingIndex);
			}

			return true;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void X()
		{
		}

		#endregion
	}
}
