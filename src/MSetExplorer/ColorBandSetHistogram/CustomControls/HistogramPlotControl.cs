using MSetExplorer;
using MSS.Types;
using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
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
		private ScatterPlot? _thePlot;

		//private double _viewportOffsetX;
		//private double _viewportWidth;

		private ControlXPositionAndWidth _viewportOffsetAndWidth;

		private readonly bool _useDetailedDebug = false;

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
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<ControlXPositionAndWidth, ControlXPositionAndWidth>>? ViewportOffsetAndWidthChanged;

		#endregion

		#region Public Properties

		public WpfPlot? WpfPlot1
		{
			get => _wpfPlot1;
			set
			{
				if (_wpfPlot1 != null)
				{
					_wpfPlot1.SizeChanged -= WpfPlot1_SizeChanged;
				}

				_wpfPlot1 = value;

				if (_wpfPlot1 == null)
				{
					return;
				}

				//_wpfPlot1.Configuration.Zoom = false;
				//_wpfPlot1.Configuration.Pan = false;
				//_wpfPlot1.Configuration.LeftClickDragPan = false;
				_wpfPlot1.Configuration.RightClickDragZoom = false;
				//_wpfPlot1.Configuration.ScrollWheelZoom = false;
				_wpfPlot1.Configuration.MiddleClickDragZoom = false;
				_wpfPlot1.Configuration.AltLeftClickDragZoom = false;
				_wpfPlot1.Configuration.LockHorizontalAxis = true;

				_wpfPlot1.SizeChanged += WpfPlot1_SizeChanged;

				// Create some data -- so that we can create a plot -- so that we update the Viewport Pixel Offset.
				var seriesData = new HPlotSeriesData(10);

				_thePlot = CreateScatterPlot(_wpfPlot1, seriesData);

				_wpfPlot1.Refresh();
				var xAxisDimensions = _wpfPlot1.Plot.XAxis.Dims;
				UpdateViewportPixelOffsetAndWidth(xAxisDimensions);

				// Remove the plot -- to keep the display clean until we receive some actual data.
				_wpfPlot1.Plot.Remove(_thePlot);
			}
		}

		private void WpfPlot1_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_wpfPlot1!= null)
			{
				if (!SeriesData.IsEmpty())
				{
					var xAxisDimensions = _wpfPlot1.Plot.XAxis.Dims;
					UpdateViewportPixelOffsetAndWidth(xAxisDimensions);
				}
			}
		}

		public ControlXPositionAndWidth ViewportOffsetAndWidth
		{
			get => _viewportOffsetAndWidth;
			set
			{
				if (value != _viewportOffsetAndWidth)
				{
					var previousValue = _viewportOffsetAndWidth;
					_viewportOffsetAndWidth = value;

					Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl is updating the ViewportWidth from {previousValue} to {value}.");

					ViewportOffsetAndWidthChanged?.Invoke(this, new (previousValue, value));
				}
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

		public double PlotWidth => WpfPlot1?.Plot.Width ?? 0f;

		public double PlotDataWidth => WpfPlot1?.Plot.XAxis.Dims.DataSizePx ?? 0f;

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

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

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

			c.UpdatePlotDataWidth(previousValue.Width, newValue.Width);
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

			_ = c.UpdatePlotDataStart(previousValue.X, newValue.X);
		}

		#endregion

		#region Private Methods - Content

		private void DisplayPlot(HPlotSeriesData seriesData)
		{
			var wpfPlot = WpfPlot1;

			if (wpfPlot == null)
			{
				return;
			}

			if (seriesData.IsEmpty())
			{
				wpfPlot.Plot.Clear();
				_thePlot = null;
				return;
			}

			if (_thePlot == null)
			{
				_thePlot = CreateScatterPlot(wpfPlot, seriesData);
			}
			else
			{
				if (seriesData.DataX.Length == _thePlot.Ys.Length)
				{
					ClearPlotLimits(wpfPlot.Plot);
					_thePlot.UpdateY(seriesData.DataY);
					Debug.WriteLineIf(_useDetailedDebug, $"The Series has {seriesData.DataX.Length}, updating existing Scatter Plot.");
				}
				else
				{
					wpfPlot.Plot.Clear();
					_thePlot = CreateScatterPlot(wpfPlot, seriesData);

					Debug.WriteLineIf(_useDetailedDebug, $"The Series has {seriesData.DataX.Length}, not updating -- creating new Scatter Plot.");
				}
			}

			SetPlotLimits(wpfPlot, DisplayPosition.X, ContentViewportSize.Width);
			UpdateViewportPixelOffsetAndWidth(wpfPlot.Plot.XAxis.Dims);

			wpfPlot.Refresh();
		}

		private ScatterPlot CreateScatterPlot(WpfPlot wpfPlot, HPlotSeriesData seriesData)
		{
			var result = wpfPlot.Plot.AddScatter(seriesData.DataX, seriesData.DataY);
			return result;
		}

		private void SetPlotLimits(WpfPlot wpfPlot, double startingIndex, double viewportWidth)
		{
			var plot = wpfPlot.Plot;

			var endingIndex = startingIndex + viewportWidth;

			if (endingIndex - startingIndex > 0)
			{
				plot.AxisAutoY();
				wpfPlot.Refresh();

				var axLimits = plot.GetAxisLimits();
				Debug.WriteLineIf(_useDetailedDebug, $"Setting the XPlot Limits. Start: {startingIndex}, End: {endingIndex}. Current: X1: {axLimits.XMin}, X2: {axLimits.XMax}, Y1: {axLimits.YMin}, Y2: {axLimits.YMax}");

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
				Debug.WriteLineIf(_useDetailedDebug, $"Setting the Plot Limits, Start = End, Clearing instead of Setting.");
				plot.SetAxisLimits(AxisLimits.NoLimits);
			}
		}

		private void ClearPlotLimits(Plot? plot)
		{
			if (plot != null)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Clearing the Plot Limits.");

				plot.SetAxisLimits(AxisLimits.NoLimits);
			}
		}

		private bool UpdatePlotDataStart(double previousValue, double newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotControl's StartingIndex is being set to {newValue} from {previousValue}.");

			var wpfPlot = WpfPlot1;

			if (wpfPlot != null)
			{
				// Use the newValue to limit the Plot's range of X values. (Starting Index)
				SetPlotLimits(wpfPlot, newValue, ContentViewportSize.Width);

				wpfPlot.Refresh();
				UpdateViewportPixelOffsetAndWidth(wpfPlot.Plot.XAxis.Dims);
			}

			return true;
		}

		private bool UpdatePlotDataWidth(double previousValue, double newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotControl's PlotViewportSize is being updated from {previousValue} to {newValue}.");

			var wpfPlot = WpfPlot1;

			if (wpfPlot != null)
			{
				// Use the newValue to limit the Plot's range of X values. (Starting Index)
				SetPlotLimits(wpfPlot, DisplayPosition.X, newValue);

				wpfPlot.Refresh();
				UpdateViewportPixelOffsetAndWidth(wpfPlot.Plot.XAxis.Dims);
			}

			return true;
		}

		private void UpdateViewportPixelOffsetAndWidth(AxisDimensions axisDimensions)
		{
			if (axisDimensions.HasBeenSet)
			{
				var viewportOffsetX = axisDimensions.DataOffsetPx;
				var viewportWidth = axisDimensions.DataSizePx;

				var figureWidth = axisDimensions.FigureSizePx;
				var marginRight = figureWidth - (viewportWidth + viewportOffsetX);

				var controlSize = new SizeDbl(ActualWidth, ActualHeight);

				Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl.WpfPlot1_SizeChanged. Preparing to set the ViewportOffsetX and Width: X:{viewportOffsetX}, W: {viewportWidth}. " +
					$"NOTE: ControlSize: {controlSize}. The FigureWidth: {figureWidth}, Margin Right {marginRight}");

				var pxPerUnit = axisDimensions.PxPerUnit;
				//Debug.WriteLine($"****HistogramPlotControl.WpfPlot1_SizeChanged. PixlesPerUnit: {pxPerUnit}.");


				ViewportOffsetAndWidth = new ControlXPositionAndWidth(viewportOffsetX, viewportWidth);
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotControl.WpfPlot1_SizeChanged: Cannot set the ViewportOffset and Width, the yAxisDimensions.HasBeenSet = false.");
			}
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
