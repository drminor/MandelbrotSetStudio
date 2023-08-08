using MSS.Types;
using ScottPlot;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class HistogramPlotCustomControl : ContentControl, IContentScaler
	{
		#region Private Fields 

		private readonly static bool CLIP_IMAGE_BLOCKS = false;
		private readonly static int COLOR_BAND_HEIGHT = 40;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement _ourContent;

		private WpfPlot? _wpfPlot1;
		private ScottPlot.Plottable.ScatterPlot? _thePlot;
		private int _thePlotExtent;

		private Canvas _canvas;
		private Image _image;

		//private SizeDbl _viewportSizeInternal;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;
		private SizeDbl _logicalViewportSize;

		private SizeDbl _viewportSize;
		//private SizeDbl _contentViewportSize;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static HistogramPlotCustomControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramPlotCustomControl), new FrameworkPropertyMetadata(typeof(HistogramPlotCustomControl)));
		}

		public HistogramPlotCustomControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_ourContent = new FrameworkElement();

			_wpfPlot1 = null;
			_thePlot = null;
			_thePlotExtent = 400;

			_canvas = new Canvas();
			_image = new Image();
			//_image.SizeChanged += Image_SizeChanged;

			//_viewportSizeInternal = new SizeDbl();

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_contentScale = new SizeDbl(1);
			_translationAndClipSize = new RectangleDbl();

			_viewportSize = new SizeDbl();
			//_contentViewportSize = SizeDbl.NaN;
			_logicalViewportSize = new SizeDbl();

			//MouseEnter += HistogramDisplayControl_MouseEnter;
			//MouseLeave += HistogramDisplayControl_MouseLeave;
		}

		//private void HistogramDisplayControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		//{
		//	//SetTheCanvasTranslateTransform(new VectorDbl(), _canvasOffset);
		//	//Clip = _clipT;

		//	if (_clipT != null)
		//	{
		//		_canvas.ClipToBounds = false;
		//		_canvas.Clip = _clipT;
		//	}
		//	else
		//	{
		//		_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
		//	}
		//}

		//private void HistogramDisplayControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		//{
		//	//SetTheCanvasTranslateTransform(_canvasOffset, new VectorDbl());
		//	//Clip = null;

		//	_canvas.ClipToBounds = false;
		//	_canvas.Clip = null;
		//}

		#endregion

		#region Event Handlers

		//private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//	//Debug.WriteLine($"The HistogramDisplayControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
		//	Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}.");

		//	UpdateImageOffset(ImageOffset);
		//}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

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

				//_thePlot = _wpfPlot1.Plot.AddScatter(seriesData.DataX, seriesData.DataY);
				_thePlot = CreateScatterPlot(_wpfPlot1, seriesData);

				_thePlotExtent = seriesData.DataX.Length;

				_wpfPlot1.Refresh();
			}
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas = value;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.RenderTransform = _canvasRenderTransform;
			}
		}

		public Image Image
		{
			get => _image;
			set
			{
				if (_image != value)
				{
					//_image.SizeChanged -= Image_SizeChanged;
					_image = value;
					//_image.SizeChanged += Image_SizeChanged;

					_image.Source = HistogramImageSource;
					_image.SetValue(Panel.ZIndexProperty, 20);
					UpdateImageOffset(ImageOffset);

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
		}

		//private SizeDbl ViewportSizeInternal
		//{
		//	get => _viewportSizeInternal;
		//	set
		//	{
		//		if (value.Width > 1 && value.Height > 1 && _viewportSizeInternal != value)
		//		{
		//			var previousValue = _viewportSizeInternal;
		//			_viewportSizeInternal = value;

		//			//Debug.WriteLine($"HistogramDisplayControl: Viewport is changing: Old size: {previousValue}, new size: {_viewPort}.");

		//			var newViewportSize = value;

		//			if (previousValue.Width < 25 || previousValue.Height < 25)
		//			{
		//				// Update the 'real' value immediately
		//				Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize immediately. Previous Size: {previousValue}, New Size: {value}.");
		//				ViewportSize = newViewportSize;
		//			}
		//			else
		//			{
		//				// Update the screen immediately, while we are 'holding' back the update.
		//				//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
		//				var tempOffset = GetTempImageOffset(ImageOffset, ViewportSize, newViewportSize);
		//				_ = UpdateImageOffset(tempOffset);

		//				// Delay the 'real' update until no futher updates in the last 150ms.
		//				_viewPortSizeDispatcher.Debounce(
		//					interval: 150,
		//					action: parm =>
		//					{
		//						Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize after debounce. Previous Size: {ViewportSize}, New Size: {newViewportSize}.");
		//						ViewportSize = newViewportSize;
		//					},
		//					param: null
		//				);
		//			}
		//		}
		//		else
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"Skipping the update of the ViewportSize, the new value {value} is the same as the old value. {ViewportSizeInternal}.");
		//		}
		//	}
		//}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					//Debug.Assert(_viewportSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The HistogramPlotCustomControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
				}
			}
		}

		public HPlotSeriesData SeriesData
		{
			get => (HPlotSeriesData)GetValue(SeriesDataProperty);
			set => SetCurrentValue(SeriesDataProperty, value);
		}

		public ImageSource HistogramImageSource
		{
			get => (ImageSource)GetValue(HistogramImageSourceProperty);
			set => SetCurrentValue(HistogramImageSourceProperty, value);
		}

		public VectorDbl ImageOffset
		{
			get => (VectorDbl)GetValue(ImageOffsetProperty);
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(ImageOffset, value, 0.00001))
				{
					SetCurrentValue(ImageOffsetProperty, value);
				}
			}
		}

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;
					SetTheCanvasScale(_contentScale);
				}
			}
		}

		public RectangleDbl TranslationAndClipSize
		{
			get => _translationAndClipSize; 
			set
			{
				var previousVal = _translationAndClipSize;
				_translationAndClipSize = value;

				LogicalViewportSize = value.Size;

				ClipAndOffset(previousVal, value);
			}
		}

		public SizeDbl LogicalViewportSize
		{
			get => _logicalViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(value, _logicalViewportSize))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}.");
					_logicalViewportSize = value;
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

			//ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

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

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotCustomControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;

			if (canvas.ActualWidth != finalSize.Width)
			{
				canvas.Width = finalSize.Width;
			}

			if (canvas.ActualHeight != finalSize.Height)
			{
				canvas.Height = COLOR_BAND_HEIGHT; //finalSize.Height;
			}

			ViewportSize = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramPlotCustomControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			return finalSize;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();
				(WpfPlot1, Canvas, Image) = BuildContentModel(_ourContent);
			}
			else
			{
				throw new InvalidOperationException("Did not find the HistogramDisplayControl_Content template.");
			}
		}

		private (WpfPlot, Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Grid gr)
				{
					if (gr.Children[0] is Border br && gr.Children[1] is Canvas ca)
					{
						if (br.Child is WpfPlot wp && ca.Children[0] is Image im)
						{
							return (wp, ca, im);
						}

					}
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the HistogramPlotCustomControl's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region SeriesData Dependency Property

		public static readonly DependencyProperty SeriesDataProperty = DependencyProperty.Register(
					"SeriesData", typeof(HPlotSeriesData), typeof(HistogramPlotCustomControl),
					new FrameworkPropertyMetadata(HPlotSeriesData.Empty, FrameworkPropertyMetadataOptions.None, SeriesData_PropertyChanged));

		private static void SeriesData_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotCustomControl)o;

			//var previousValue = (HPlotSeriesData)e.OldValue;
			var newValue = (HPlotSeriesData)e.NewValue;

			c.DisplayPlot(newValue);
		}

		#endregion

		#region HistogramImageSource Dependency Property

		public static readonly DependencyProperty HistogramImageSourceProperty = DependencyProperty.Register(
					"HistogramImageSource", typeof(ImageSource), typeof(HistogramPlotCustomControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, HistogramImageSource_PropertyChanged));

		private static void HistogramImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotCustomControl)o;
			var previousValue = (ImageSource)e.OldValue;
			var value = (ImageSource)e.NewValue;

			if (value != previousValue)
			{
				c.Image.Source = value;
			}
		}

		#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(HistogramPlotCustomControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotCustomControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
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
			
			var startingIndex = ImageOffset.X;
			var endingIndex = startingIndex + _translationAndClipSize.Size.Width;
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

		//private HPlotSeriesData BuildTestSeries()
		//{
		//	double[] dataX = new double[] { 1, 2, 3, 4, 5 };
		//	double[] dataY = new double[] { 1, 4, 9, 16, 25 };

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		//private HPlotSeriesData BuildTestSeries2()
		//{
		//	double[] dataX = new double[] { 1, 2, 3, 4, 5 };
		//	double[] dataY = new double[] { 1, 14, 29, 16, 5 };

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		#endregion

		#region Private Methods - Canvas

		//private void SetTheCanvasSize(SizeDbl contentViewportSize, SizeDbl contentScale)
		//{
		//	var viewportSize = new SizeDbl(ActualWidth, ActualHeight);
		//	var newCanvasSize = viewportSize.Divide(contentScale);

		//	Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's ContentViewportSize is being set to {contentViewportSize} from {_contentViewportSize}. Setting the Canvas Size to {newCanvasSize}.");

		//	Canvas.Width = newCanvasSize.Width;
		//	Canvas.Height = newCanvasSize.Height;
		//}

		private void SetTheCanvasScale(SizeDbl contentScale)
		{
			var currentScaleX = _canvasScaleTransform.ScaleX;
			Debug.WriteLineIf(_useDetailedDebug, $"\n\nThe HistogramPlotCustomControl's Image ScaleTransform is being set from {currentScaleX} to {contentScale.Width}.");

			_canvasScaleTransform.ScaleX = contentScale.Width;
			//_canvasScaleTransform.ScaleY = contentScale.Height;
		}

		private void ClipAndOffset(RectangleDbl? previousValue, RectangleDbl? newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's {nameof(TranslationAndClipSize)} is being from {previousValue} to {newValue}.");

			if (newValue != null && WpfPlot1 != null)
			{
				// Use the newValue.size to limit the Plot's range of X values (Ending Index).
				var startingIndex = ImageOffset.X;
				var endingIndex = startingIndex + newValue.Value.Size.Width;
				SetPlotLimits(WpfPlot1, startingIndex, endingIndex);
			}

			//if (newValue != null)
			//{
			//	_canvasTranslateTransform.X = newValue.Value.Position.X;
			//	_canvasTranslateTransform.Y = newValue.Value.Position.Y;
			//	_canvas.Clip  = new RectangleGeometry(new Rect(ScreenTypeHelper.ConvertToSize(newValue.Value.Size)));
			//}
			//else
			//{
			//	_canvasTranslateTransform.X = 0;
			//	_canvasTranslateTransform.Y = 0;
			//	_canvas.Clip = null;
			//}
		}

		#endregion

		#region Image and Image Offset

		// Determines the StartingIndex

		private bool UpdateImageOffset(VectorDbl rawValue)
		{
			//var newValue = rawValue.Scale(_scaleTransform.ScaleX);
			//Debug.WriteLine($"Updating ImageOffset: raw: {rawValue}, scaled: {newValue}. CanvasOffset: {_canvasOffset}. ImageScaleTransform: {_scaleTransform.ScaleX}.");

			var newValue = rawValue;

			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)Image.GetValue(Canvas.LeftProperty),
				(double)Image.GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.00001))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's ImageOffset is being set to {invertedValue} from {currentValue}.");

				Image.SetValue(Canvas.LeftProperty, invertedValue.X);
				//Image.SetValue(Canvas.BottomProperty, invertedValue.Y);

				if (WpfPlot1 != null)
				{
					// Use the newValue to limit the Plot's range of X values. (Starting Index)
					var startingIndex = newValue.X;
					var endingIndex = startingIndex + _translationAndClipSize.Size.Width;
					SetPlotLimits(WpfPlot1, startingIndex, endingIndex);
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		private VectorDbl GetTempImageOffset(VectorDbl originalOffset, SizeDbl originalSize, SizeDbl newSize)
		{
			var diff = newSize.Sub(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void CheckThatImageIsAChildOfCanvas(Image image, Canvas canvas)
		{
			foreach (var v in canvas.Children)
			{
				if (v == image)
				{
					return;
				}
			}

			throw new InvalidOperationException("The image is not a child of the canvas.");
		}

		#endregion
	}
}
