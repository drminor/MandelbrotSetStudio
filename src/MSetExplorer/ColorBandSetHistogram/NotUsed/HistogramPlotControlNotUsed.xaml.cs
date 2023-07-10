using MSetExplorer.ColorBandSetHistogram.Support;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer.ColorBandSetHistogram.NotUsed
{
	/// <summary>
	/// Interaction logic for HistogramPlotControl.xaml
	/// </summary>
	public partial class HistogramPlotControl : UserControl, IContentScaler
	{
		#region Private Fields 

		private readonly static bool CLIP_IMAGE_BLOCKS = false;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewportSizeInternal;
		private SizeDbl _viewportSize;
		private SizeDbl _contentViewportSize;

		private ScaleTransform _controlScaleTransform;

		private TransformGroup _canvasRenderTransform;
		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;

		private VectorDbl _contentOffset;
		private RectangleGeometry? _canvasClip;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		static HistogramPlotControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramPlotControl), new FrameworkPropertyMetadata(typeof(HistogramPlotControl)));
		}

		public HistogramPlotControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_image = new Image();
			//_image.SizeChanged += Image_SizeChanged;

			_viewportSizeInternal = new SizeDbl();
			_viewportSize = new SizeDbl();
			_contentViewportSize = SizeDbl.NaN;

			_controlScaleTransform = new ScaleTransform();
			_controlScaleTransform.Changed += _controlScaleTransform_Changed;

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_contentOffset = new VectorDbl();
			_canvasClip = null;

			InitializeComponent();

			//_canvas = MainCanvas;
			//_canvas.RenderTransform = _canvasRenderTransform;

			Canvas = MainCanvas;
			Image = MainImage;

		}

		#endregion

		#region Event Handlers

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine($"The HistogramDisplayControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramDisplayControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}.");

			UpdateImageOffset(ImageOffset);
		}

		private void _controlScaleTransform_Changed(object? sender, EventArgs e)
		{
			SetTheCanvasScaleTransform(_controlScaleTransform);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		#endregion

		#region Public Properties

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
				if (ScreenTypeHelper.IsVectorDblChanged(ImageOffset, value))
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
				if (ScreenTypeHelper.IsSizeDblChanged(_contentViewportSize, value))
				{
					_contentViewportSize = value;
					SetTheCanvasSize(value, _controlScaleTransform);
				}
			}
		}

		ScaleTransform IContentScaler.ScaleTransform => _controlScaleTransform;

		public ScaleTransform ScaleTransform
		{
			get => _controlScaleTransform;
			set
			{
				if (_controlScaleTransform != value)
				{
					_controlScaleTransform.Changed -= _controlScaleTransform_Changed;
					_controlScaleTransform = value;
					_controlScaleTransform.Changed += _controlScaleTransform_Changed;

					SetTheCanvasScaleTransform(_controlScaleTransform);

					UpdateImageOffset(ImageOffset);
				}
			}
		}

		TranslateTransform IContentScaler.TranslateTransform => _canvasTranslateTransform;

		public RectangleGeometry? CanvasClip
		{
			get => _canvasClip;
			set
			{
				_canvasClip = value;
				_canvas.Clip = value;
			}
		}

		public VectorDbl ContentOffset
		{
			get => _contentOffset;
			set
			{
				var previousVal = _contentOffset;
				_contentOffset = value;

				SetTheCanvasTranslateTransform(previousVal, value);
			}
		}

		#endregion


		#region Private Methods - Canvas

		private void SetTheCanvasSize(SizeDbl contentViewportSize, ScaleTransform st)
		{
			var viewportSize = new SizeDbl(ActualWidth, ActualHeight);
			var contentScale = new SizeDbl(st.ScaleX, 1);

			var newCanvasSize = viewportSize.Divide(contentScale);

			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramDisplayControl's ContentViewportSize is being set to {contentViewportSize} from {_contentViewportSize}. Setting the Canvas Size to {newCanvasSize}.");

			Width = Math.Max(newCanvasSize.Width, 0);
			Height = Math.Max(newCanvasSize.Height, 0);

			ViewportSizeInternal = new SizeDbl(Width, Height);

			Canvas.Width = Math.Max(newCanvasSize.Width - 2, 0);
			Canvas.Height = Math.Max(newCanvasSize.Height - 2, 0);
		}

		private void SetTheCanvasScaleTransform(ScaleTransform st)
		{
			var currentScaleX = _canvasScaleTransform.ScaleX;
			Debug.WriteLineIf(_useDetailedDebug, $"\n\nThe HistogramDisplayControl's Image ScaleTransform is being set to {_canvasScaleTransform.ScaleX} from {currentScaleX}. The CanvasOffset is {_contentOffset}.");

			_canvasScaleTransform.ScaleX = st.ScaleX;
			//_canvasScaleTransform.ScaleY = st.ScaleY;
		}

		private void SetTheCanvasTranslateTransform(VectorDbl previousValue, VectorDbl canvasOffset)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramDisplayControl's CanvasOffset is being set to {canvasOffset} from {previousValue}. The ImageOffset is {ImageOffset}.");

			_canvasTranslateTransform.X = canvasOffset.X;
			_canvasTranslateTransform.Y = canvasOffset.Y;
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

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramDisplayControl's ImageOffset is being set to {newValue} from {currentValue}. CanvasOffset: {_contentOffset}. ImageScaleTransform: {_controlScaleTransform.ScaleX}.");

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

			//var (baseScale, relativeScale) = ZoomSlider.GetBaseAndRelative(_controlScaleTransform.ScaleX);

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
					"SeriesData", typeof(HPlotSeriesData), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(HPlotSeriesData.Zero, FrameworkPropertyMetadataOptions.None, SeriesData_PropertyChanged));

		private static void SeriesData_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;
			//var previousValue = (HPlotSeriesData)e.OldValue;
			var newValue = (HPlotSeriesData)e.NewValue;

			//_ = c.UpdateImageOffset(newValue);
		}

		#endregion


		#region HistogramImageSource Dependency Property

		public static readonly DependencyProperty HistogramImageSourceProperty = DependencyProperty.Register(
					"HistogramImageSource", typeof(ImageSource), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, HistogramImageSource_PropertyChanged));

		private static void HistogramImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;
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
					"ImageOffset", typeof(VectorDbl), typeof(HistogramPlotControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (HistogramPlotControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		#endregion



	}
}
