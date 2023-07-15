﻿using MSetExplorer.ColorBandSetHistogram.Support;
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

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement _ourContent;

		private WpfPlot? _wpfPlot1;
		private ScottPlot.Plottable.ScatterPlot? _thePlot;
		private int _thePlotExtent;

		private int[] _theXValues;


		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewportSizeInternal;
		private SizeDbl _viewportSize;
		private SizeDbl _contentViewportSize;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private RectangleGeometry? _canvasClip;
		private SizeDbl _contentScale;
		private RectangleDbl? _scaledContentArea;

		private bool _useDetailedDebug = false;

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

			_theXValues = new int[_thePlotExtent];

			for(int i = 0; i < _thePlotExtent; i++)
			{
				_theXValues[i] = i;
			}

			_canvas = new Canvas();
			_image = new Image();
			_image.SizeChanged += Image_SizeChanged;

			_viewportSizeInternal = new SizeDbl();
			_viewportSize = new SizeDbl();
			_contentViewportSize = SizeDbl.NaN;


			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_canvasClip = null;

			_contentScale = new SizeDbl(1);
			_scaledContentArea = new RectangleDbl();

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

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine($"The HistogramDisplayControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}.");

			UpdateImageOffset(ImageOffset);
		}

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

				if (seriesData.IsZero())
				{
					return;
				}

				//var seriesData = BuildTestSeries();

				_thePlot = _wpfPlot1.Plot.AddScatter(seriesData.DataX, seriesData.DataY);

				_wpfPlot1.Plot.Title("Hi There, I'm initialized.");
				_wpfPlot1.Plot.XLabel("XLabel");
				_wpfPlot1.Plot.YLabel("YLabel");

				_wpfPlot1.Refresh();
			}
		}

		private void DisplayPlot(HPlotSeriesData seriesData)
		{
			if (WpfPlot1 == null)
			{
				return;
			}

			//WpfPlot1.Plot.Clear();

			if (seriesData.IsZero())
			{
				return;
			}

			if (_thePlot == null)
			{
				_thePlot = WpfPlot1.Plot.AddScatter(seriesData.DataX, seriesData.DataY);

				WpfPlot1.Plot.Title("Hi There, I'm initialized.");
				WpfPlot1.Plot.XLabel("XLabel");
				WpfPlot1.Plot.YLabel("YLabel");

				_thePlotExtent = seriesData.DataX.Length;

				WpfPlot1.Refresh();
			}
			else
			{
				if (seriesData.DataX.Length == _thePlotExtent)
				{
					_thePlot.UpdateY(seriesData.DataY);
					WpfPlot1.Refresh();
				}
				else
				{
					Debug.WriteLine($"The Series has {seriesData.DataX.Length}, not updating.");
				}

			}
		}

		private HPlotSeriesData BuildTestSeries()
		{
			double[] dataX = new double[] { 1, 2, 3, 4, 5 };
			double[] dataY = new double[] { 1, 4, 9, 16, 25 };

			var result = new HPlotSeriesData(dataX, dataY);

			return result;
		}

		private HPlotSeriesData BuildTestSeries2()
		{
			double[] dataX = new double[] { 1, 2, 3, 4, 5 };
			double[] dataY = new double[] { 1, 14, 29, 16, 5 };

			var result = new HPlotSeriesData(dataX, dataY);

			return result;
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas = value;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.Clip = _canvasClip;
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
					_image.SizeChanged -= Image_SizeChanged;
					_image = value;
					_image.SizeChanged += Image_SizeChanged;

					_image.Source = HistogramImageSource;

					_image.SetValue(Panel.ZIndexProperty, 20);

					UpdateImageOffset(ImageOffset);

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
		}

		private SizeDbl ViewportSizeInternal
		{
			get => _viewportSizeInternal;
			set
			{
				if (value.Width > 1 && value.Height > 1 && _viewportSizeInternal != value)
				{
					var previousValue = _viewportSizeInternal;
					_viewportSizeInternal = value;

					//Debug.WriteLine($"HistogramDisplayControl: Viewport is changing: Old size: {previousValue}, new size: {_viewPort}.");

					var newViewportSize = value;

					if (previousValue.Width < 25 || previousValue.Height < 25)
					{
						// Update the 'real' value immediately
						Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize immediately. Previous Size: {previousValue}, New Size: {value}.");
						ViewportSize = newViewportSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
						var tempOffset = GetTempImageOffset(ImageOffset, ViewportSize, newViewportSize);
						_ = UpdateImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize after debounce. Previous Size: {ViewportSize}, New Size: {newViewportSize}.");
								ViewportSize = newViewportSize;
							},
							param: null
						);
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Skipping the update of the ViewportSize, the new value {value} is the same as the old value. {ViewportSizeInternal}.");
				}
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramDisplayControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					Debug.Assert(_viewportSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The HistogramDisplayControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
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

		public SizeDbl ContentViewportSize
		{
			get => _contentViewportSize.IsNAN() ? ViewportSizeInternal : _contentViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(_contentViewportSize, value, 0.00001))
				{
					_contentViewportSize = value;
					SetTheCanvasSize(value, ContentScale);
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

		public RectangleDbl? ScaledContentArea
		{
			get => _scaledContentArea; 
			set
			{
				var previousVal = _scaledContentArea;
				_scaledContentArea = value;

				ClipAndOffset(previousVal, value);
			}
		}

		public RectangleGeometry? CanvasClip
		{
			get => _canvasClip;
			set
			{
				_canvasClip = value;
				_canvas.Clip = value;
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

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

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

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramDisplayControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;

			if (canvas.ActualWidth != finalSize.Width)
			{
				canvas.Width = finalSize.Width;
			}

			if (canvas.ActualHeight != finalSize.Height)
			{
				canvas.Height = finalSize.Height;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramDisplayControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

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

			throw new InvalidOperationException("Cannot find a child image element of the BitmapGrid3's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region Private Methods - Plot



		#endregion

		#region Private Methods - Canvas

		private void SetTheCanvasSize(SizeDbl contentViewportSize, SizeDbl contentScale)
		{
			var viewportSize = new SizeDbl(ActualWidth, ActualHeight);
			var newCanvasSize = viewportSize.Divide(contentScale);

			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's ContentViewportSize is being set to {contentViewportSize} from {_contentViewportSize}. Setting the Canvas Size to {newCanvasSize}.");

			Canvas.Width = newCanvasSize.Width;
			Canvas.Height = newCanvasSize.Height;
		}

		private void SetTheCanvasScale(SizeDbl contentScale)
		{
			var currentScaleX = _canvasScaleTransform.ScaleX;
			Debug.WriteLineIf(_useDetailedDebug, $"\n\nThe HistogramPlotCustomControl's Image ScaleTransform is being set to {_canvasScaleTransform.ScaleX} from {currentScaleX}.");

			_canvasScaleTransform.ScaleX = contentScale.Width;
			_canvasScaleTransform.ScaleY = contentScale.Height;
		}

		private void ClipAndOffset(RectangleDbl? previousValue, RectangleDbl? newValue)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's {nameof(ScaledContentArea)} is being set to {newValue} from {previousValue}.");

			if (newValue != null)
			{ 
				_canvasTranslateTransform.X = newValue.Value.Position.X;
				_canvasTranslateTransform.Y = newValue.Value.Position.Y;
				CanvasClip = new RectangleGeometry(new Rect(ScreenTypeHelper.ConvertToSize(newValue.Value.Size)));
			}
			else
			{
				_canvasTranslateTransform.X = 0;
				_canvasTranslateTransform.Y = 0;
				CanvasClip = null;
			}
		}

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
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramPlotCustomControl's ImageOffset is being set to {newValue} from {currentValue}.");

				CompareCanvasAndControlHeights();

				Image.SetValue(Canvas.LeftProperty, invertedValue.X);
				Image.SetValue(Canvas.BottomProperty, invertedValue.Y);

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

		[Conditional("DEBUG2")]
		private void CompareCanvasAndControlHeights()
		{
			// The contentViewportSize when reduced by the BaseScale Factor
			// should equal the ViewportSize when it is expanded by the RelativeScale

			//var (baseFactor, relativeScale) = ZoomSlider.GetBaseAndRelative(_controlScaleTransform.ScaleX);

			//var canvasHeightScaled = Canvas.ActualHeight * relativeScale;

			if (Math.Abs(Canvas.ActualHeight - ActualHeight) > 0.1)
			{
				Debug.WriteLine($"WARNING: The Canvas Height : {Canvas.ActualHeight} does not match the HistogramDisplayControl's height: {ActualHeight}.");
			}
		}

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

		#region SeriesData Dependency Property

		public static readonly DependencyProperty SeriesDataProperty = DependencyProperty.Register(
					"SeriesData", typeof(HPlotSeriesData), typeof(HistogramPlotCustomControl),
					new FrameworkPropertyMetadata(HPlotSeriesData.Zero, FrameworkPropertyMetadataOptions.None, SeriesData_PropertyChanged));

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

	}
}
