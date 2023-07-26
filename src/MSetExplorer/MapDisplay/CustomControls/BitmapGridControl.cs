using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGridControl: ContentControl, IContentScaler
	{
		#region Private Fields

		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewportSizeInternal;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static BitmapGridControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl), new FrameworkPropertyMetadata(typeof(BitmapGridControl)));
		}

		public BitmapGridControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_image = new Image();

			_viewportSizeInternal = new SizeDbl();

			_canvasScaleTransform = new ScaleTransform();
			_canvasTranslateTransform = new TranslateTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_contentScale = new SizeDbl(1, 1);

			//MouseEnter += BitmapGridControl_MouseEnter;
			//MouseLeave += BitmapGridControl_MouseLeave;

			//_canvas.SizeChanged += Canvas_SizeChanged;
		}

		private SizeDbl _savedTranslation = new SizeDbl();
		private Geometry? _savedClip;

		//private SizeDbl _savedCanvasSize = new SizeDbl(10, 10);
		//private SizeDbl _savedContentScale = new SizeDbl(1);
		//private Point _savedRenderTransformOrigin = new Point();

		private void BitmapGridControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			//if (_clipT != null)
			//{
			//	_canvas.ClipToBounds = false;
			//	_canvas.Clip = _clipT;
			//}
			//else
			//{
			//	_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
			//}

			Canvas.Clip = _savedClip;

			_canvasTranslateTransform.X = _savedTranslation.Width;
			_canvasTranslateTransform.Y = _savedTranslation.Height;
			

			//Canvas.Width = _savedCanvasSize.Width;
			//Canvas.Height = _savedCanvasSize.Height;

			//_canvas.RenderTransformOrigin = _savedRenderTransformOrigin;

			//_canvasScaleTransform.ScaleX = _savedContentScale.Width;
			//_canvasScaleTransform.ScaleY = _savedContentScale.Height;

		}

		private void BitmapGridControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			//_canvas.ClipToBounds = false;

			_savedClip = _canvas.Clip;
			_canvas.Clip = null;

			_savedTranslation = new SizeDbl(_canvasTranslateTransform.X, _canvasTranslateTransform.Y);
			_canvasTranslateTransform.X = 0;
			_canvasTranslateTransform.Y = 0;

			//_savedCanvasSize = new SizeDbl(Canvas.ActualWidth, Canvas.ActualHeight);
			//_savedContentScale = new SizeDbl(_canvasScaleTransform.ScaleX, _canvasScaleTransform.ScaleY);

			//_savedRenderTransformOrigin = _canvas.RenderTransformOrigin;

			//_canvas.RenderTransformOrigin = new Point(0, 0);
			//_canvasScaleTransform.ScaleX = 1;
			//_canvasScaleTransform.ScaleY = 1;

			//Canvas.Width = ActualWidth;
			//Canvas.Height = ActualHeight;
		}

		#endregion

		#region Event Handlers

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine($"The BitmapGridControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
			//Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}.");
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
				//_canvas.SizeChanged -= Canvas_SizeChanged;
				_canvas = value;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.RenderTransform = _canvasRenderTransform;

				//_canvas.SizeChanged += Canvas_SizeChanged;		// TODO: Unregister this event handler on dispose.

				//var sz = ViewportSize;
				//if (sz.Width < 5 || sz.Height < 5)
				//{
				//	sz = new SizeDbl(ActualWidth, ActualHeight);
				//}

				//SetTheCanvasSize(sz, ContentScale);
			}
		}

		private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//try
			//{
			//	if (_canvas.ActualHeight != double.NaN)
			//	{
			//		_canvas.RenderTransformOrigin = new Point(0, _canvas.ActualHeight);
			//	}
			//}
			//catch (Exception ex)
			//{
			//	Debug.WriteLine($"Got exception: {ex} while setting the Canvas' RenderTransformOrigin.");
			//}
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

					_image.Source = BitmapGridImageSource;

					UpdateImageOffset(ImageOffset);

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
		}

		//private SizeDbl ViewportSizeInternalOld
		//{
		//	get => _viewportSizeInternal;
		//	set
		//	{
		//		if (value.Width > 1 && value.Height > 1 && _viewportSizeInternal != value)
		//		{
		//			var previousValue = _viewportSizeInternal;
		//			_viewportSizeInternal = value;

		//			//Debug.WriteLine($"BitmapGridControl: Viewport is changing: Old size: {previousValue}, new size: {_viewPort}.");

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

		private SizeDbl ViewportSizeInternal
		{
			get => _viewportSizeInternal;
			set
			{
				if (value == _viewportSizeInternal)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Skipping the update of the ViewportSize, the new value {value} is the same as the old value. {ViewportSizeInternal}.");
					return;
				}

				if (value.Width > 1 && value.Height > 1)
				{
					var previousValue = _viewportSizeInternal;
					_viewportSizeInternal = value;

					Debug.WriteLine($"BitmapGridControl: Viewport is changing from {previousValue} to {value}.");

					ViewportSize = value;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Skipping the update of the ViewportSize, the new value {value} is close to zero. The current value is {ViewportSizeInternal}.");
				}
			}
		}

		public SizeDbl ViewportSize
		{
			get => (SizeDbl)GetValue(ViewportSizeProperty);
			set => SetCurrentValue(ViewportSizeProperty, value);
		}

		public ImageSource BitmapGridImageSource
		{
			get => (ImageSource)GetValue(BitmapGridImageSourceProperty);
			set => SetCurrentValue(BitmapGridImageSourceProperty, value);
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

		public VectorDbl ImagePositionYInv
		{
			get
			{
				var result = new VectorDbl(
					(double)Image.GetValue(Canvas.LeftProperty),
					(double)Image.GetValue(Canvas.TopProperty)
				);

				return result;
			}
		}

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				_contentScale = value;
				//SetTheCanvasScaleAndSize(ViewportSize, _contentScale);
				SetTheCanvasScaleAndSize(LogicalViewportSize, _contentScale);
			}
		}

		public RectangleDbl TranslationAndClipSize
		{
			get => _translationAndClipSize;
			set
			{
				if (ScreenTypeHelper.IsRectangleDblChanged(_translationAndClipSize, value))
				{
					var previousVal = _translationAndClipSize;
					_translationAndClipSize = value;

					//var baseScale = ContentScalerHelper.GetBaseScale(_contentScale.Width);

					//LogicalViewportSize = value.Size.Scale(baseScale);
					//Canvas.Width = LogicalViewportSize.Width;
					//Canvas.Height = LogicalViewportSize.Height;

					LogicalViewportSize = ClipAndOffset(previousVal, value);
				}
			}
		}

		public SizeDbl LogicalViewportSize { get; set; }

		#endregion

		#region Private Methods - Control

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_ourContent.Measure(availableSize);

			//UpdateViewportSize(availableSize);

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

			// TODO: Figure out when its best to call UpdateViewportSize.
			//UpdateViewportSize(childSize);
			//UpdateViewportSize(result);

			return result;
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			//var finalSize = ForceSize(finalSizeRaw);
			Size childSize = base.ArrangeOverride(finalSize);

			if (childSize != finalSize)
			{
				Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");
			}

			//UpdateViewportSize(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;

			if (canvas.Width != finalSize.Width)
			{
				canvas.Width = finalSize.Width;
			}

			if (canvas.Height != finalSize.Height)
			{
				canvas.Height = finalSize.Height;
			}

			//Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			//Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - After Arrange: Is setting the ViewportSizeInternal property to {childSize}.");

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			return finalSize;
		}

		//private Size ForceSize(Size finalSize)
		//{
		//	if (finalSize.Width > 1000 && finalSize.Width < 1040 && finalSize.Height > 1000 && finalSize.Height < 1040)
		//	{
		//		return new Size(1024, 1024);
		//	}
		//	else
		//	{
		//		return finalSize;
		//	}
		//}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();
				(Canvas, Image) = BuildContentModel(_ourContent);
			}
			else
			{
				throw new InvalidOperationException("Did not find the BitmapGridControl_Content template.");
			}
		}

		private (Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					if (ca.Children[0] is Image im)
					{
						return (ca, im);
					}
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the BitmapGrid3's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region Private Methods - Canvas

		private void SetTheCanvasScaleAndSize(SizeDbl logicalViewportSize, SizeDbl contentScale)
		{
			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale.Width);
			var baseScale = ContentScalerHelper.GetBaseScaleFromBaseFactor(baseFactor);

			//var newCanvasSize = viewportSize.Divide(relativeScale);

			var newCanvasSize = logicalViewportSize; //.Scale(baseScale);

			if (newCanvasSize.Width > 5 && newCanvasSize.Height > 5)
			{
				var previousCanvasSize = new SizeDbl(Canvas.ActualWidth, Canvas.ActualHeight);
				var previousScale = _canvasScaleTransform.ScaleX;
				Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl is handling SetTheCanvasScaleAndSize. Setting the Canvas Size from {previousCanvasSize} to {newCanvasSize}. Scale from: {previousScale} to {relativeScale}.");

				if (relativeScale < previousScale)
				{
					// Canvas is getting larger, set the scale and then the size to avoid having the content grow too large

					_canvasScaleTransform.ScaleX = relativeScale;
					_canvasScaleTransform.ScaleY = relativeScale;

					Canvas.Width = newCanvasSize.Width;
					Canvas.Height = newCanvasSize.Height;
				}
				else
				{
					// Canvas is getting smaller, set the canvas size first
					Canvas.Width = newCanvasSize.Width;
					Canvas.Height = newCanvasSize.Height;

					_canvasScaleTransform.ScaleX = relativeScale;
					_canvasScaleTransform.ScaleY = relativeScale;
				}
			}
			else
			{
				_canvasScaleTransform.ScaleX = 1;
				_canvasScaleTransform.ScaleY = 1;
			}
		}

		//private void SetTheCanvasSize(SizeDbl viewportSize, SizeDbl contentScale)
		//{
		//	var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale.Width);
		//	//var baseScale = ContentScalerHelper.GetBaseScaleFromBaseFactor(baseFactor);

		//	//Debug.Assert(_lastKnownRelativeScale == _canvasScaleTransform.ScaleX, "LastKnownRelativeScale is out of sync.");
		//	//Debug.Assert(relativeScale == _lastKnownRelativeScale, "The relativeScale calculated from what is presumably the same ContentScale used to calculate the LastKnownRelativeScale does not match the LastKnownRelativeScale.");

		//	var newCanvasSize = viewportSize.Divide(relativeScale);

		//	if (newCanvasSize.Width > 5 && newCanvasSize.Height > 5)
		//	{
		//		var previousCanvasSize = new SizeDbl(Canvas.ActualWidth, Canvas.ActualHeight);
		//		Debug.WriteLineIf(_useDetailedDebug, $"Setting the Canvas Size from {previousCanvasSize} to {newCanvasSize}.");

		//		Canvas.Width = newCanvasSize.Width;
		//		Canvas.Height = newCanvasSize.Height;
		//	}

		//	_lastKnownBaseFactor = baseFactor;
		//	_lastKnownRelativeScale = relativeScale;
		//}

		//private void SetTheCanvasScale(SizeDbl previousContentScale, SizeDbl newContentScale)
		//{
		//	var contentScaleX = newContentScale.Width;
		//	var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScaleX);

		//	//Debug.Assert(_lastKnownRelativeScale == _canvasScaleTransform.ScaleX, "LastKnownRelativeScale is out of sync.");

		//	Debug.WriteLineIf(_useDetailedDebug, $"\nThe BitmapGridControl's Canvas ContentScale is being updated from {previousContentScale} to {newContentScale}. " +
		//		$"RelativeScale from {_lastKnownRelativeScale} to {relativeScale}. " +
		//		$"BaseFactor from {_lastKnownBaseFactor} to {baseFactor}." +
		//		$"Canvas Clip: {GetClipBoundsStr()}");

		//	//_canvas.RenderTransformOrigin = new Point(0, ActualHeight);

		//	_canvasScaleTransform.ScaleX = relativeScale;
		//	_canvasScaleTransform.ScaleY = relativeScale;

		//	_lastKnownBaseFactor = baseFactor;
		//	_lastKnownRelativeScale = relativeScale;
		//}

		//private void ClipAndOffsetOld(RectangleDbl? previousValue, RectangleDbl? newValue)
		//{
		//	Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's {nameof(TranslationAndClipSize)} is being updated " +
		//		$"from {RectangleDbl.FormatNully(previousValue)} to {RectangleDbl.FormatNully(newValue)}." +
		//		$"The CanvasScale is {new SizeDbl(_canvasScaleTransform.ScaleX, _canvasScaleTransform.ScaleY)}.");

		//	if (newValue != null)
		//	{
		//		var verticalAdj = _canvas.ActualHeight - ViewportSize.Height;
		//		//var verticalAdj = 0;

		//		var contentScaleX = ContentScale.Width;
		//		var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScaleX);

		//		var cSize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
		//		var acSize = cSize.Scale(relativeScale);

		//		var verticalAdjComp = acSize.Height - ViewportSize.Height;

		//		Debug.WriteLine($"At ClipAndOffset: Canvas is {verticalAdj} taller than the control. After compensation: it is only: {verticalAdjComp} taller.");

		//		var vpRat = newValue.Value.Width / newValue.Value.Height;
		//		var csRat = _canvas.ActualWidth / _canvas.ActualHeight;

		//		Debug.WriteLine($"At ClipAndOffset: The ViewportSize has aspect ratio of {vpRat}. The Canvas has an aspect ratio of {csRat}");

		//		//var offset = newValue.Value.Position.Max(0);
		//		//var size = newValue.Value.Size;

		//		var x = newValue.Value.Scale(1 / relativeScale);
		//		var offset = x.Position.Max(0);
		//		var size = x.Size;

		//		_canvasTranslateTransform.X = offset.X;
		//		_canvasTranslateTransform.Y = offset.Y;

		//		var clipOrigin = new Point(0, 0);
		//		//var clipOrigin = new Point(0, 0);

		//		Image.SetValue(Canvas.LeftProperty, offset.X - ImageOffset.X);
		//		Image.SetValue(Canvas.TopProperty, offset.Y - ImageOffset.Y);

		//		Canvas.Clip = new RectangleGeometry(new Rect(clipOrigin, ScreenTypeHelper.ConvertToSize(size)));
		//	}
		//	else
		//	{
		//		_canvasTranslateTransform.X = 0;
		//		_canvasTranslateTransform.Y = 0;
		//		Canvas.Clip = null;
		//	}
		//}

		private SizeDbl ClipAndOffset(RectangleDbl previousValue, RectangleDbl newValue)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's {nameof(TranslationAndClipSize)} is being updated " +
			//	$"from {RectangleDbl.FormatNully(previousValue)} to {RectangleDbl.FormatNully(newValue)}." +
			//	$"The CanvasScale is {new SizeDbl(_canvasScaleTransform.ScaleX, _canvasScaleTransform.ScaleY)}.");

			// Compensate for the fact that this implementation has alredy reduced the content by a factor of BaseScale.
			var (baseFactor, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(ContentScale.Width);

			//var baseScale = ContentScalerHelper.GetBaseScale(ContentScale.Width);
			var baseScale = ContentScalerHelper.GetBaseScaleFromBaseFactor(baseFactor);

			//var pos = newValue.Position.Scale(ContentScale.Width);
			//var logicalViewportSize = newValue.Size.Scale(baseScale);

			//var scaledArea = newValue.Scale(baseScale);
			//var pos = scaledArea.Position;
			//var logicalViewportSize = scaledArea.Size;

			var offset = newValue.Position;
			//var pos = newValue.Position.Scale(baseScale);
			var pos = newValue.Position.Scale(ContentScale.Width).Scale(baseScale);

			var logicalViewportSize = newValue.Size.Scale(baseScale);

			Debug.Assert(offset.X >= 0 && offset.Y >= 0, "ClipAndOffset is receiving a negative position.");

			var previousCanvasSize = new SizeDbl(Canvas.ActualWidth, Canvas.ActualHeight);
			var previousTranslation = new SizeDbl(_canvasTranslateTransform.X, _canvasTranslateTransform.Y);

			Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl is handling ClipAndOffset. Setting the Canvas Size from {previousCanvasSize} to {logicalViewportSize}. Translation from: {previousTranslation} to {offset}.");
			//Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's {nameof(TranslationAndClipSize)} is being set. Pos: {pos}, Size: {logicalViewportSize}.");

			Canvas.Width = logicalViewportSize.Width;
			Canvas.Height = logicalViewportSize.Height;

			//var offsetA = offset.Max(0);

			// Translate using the unscaled value
			_canvasTranslateTransform.X = offset.X;
			_canvasTranslateTransform.Y = offset.Y;

			// Clip using the Scaled value.
			var clipOrigin = new Point(pos.X, pos.Y);
			Canvas.Clip = new RectangleGeometry(new Rect(clipOrigin, ScreenTypeHelper.ConvertToSize(logicalViewportSize)));

			return logicalViewportSize;
		}

		private string GetClipBoundsStr()
		{
			if (_canvas.Clip == null)
			{
				return "null";
			}
			else
			{
				return _canvas.Clip.Bounds.ToString();
			}
		}

		#endregion

		#region Image and Image Offset

		private bool UpdateImageOffset(VectorDbl newValue)
		{
			//var newValue = rawValue.Scale(_scaleTransform.ScaleX);
			//Debug.WriteLine($"Updating ImageOffset: raw: {rawValue}, scaled: {newValue}. ContentPresenterOffset: {ContentPresenterOffset}. ImageScaleTransform: {_scaleTransform.ScaleX}.");

			// For a positive offset, we "pull" the image down and to the left.
			//var invertedValue = newValue.Invert();

			// Move the image, left by the Offset -- so that the first pixel on the canvas is at the ImageOffset

			// The vertical offset is given from the beginning of the image to the beginning of the map content.
			// Subtracting this vertical offset from 128, gives the distance from the end of the image to the end of the map content.

			// Move the image, up by this complimentary amount, relative to the start of the canvas 128 - Offset -- so that the last pixel row is at canvas position = map size,
			// and the first pixel shown is some # of pixel rows into the image.

			// TODO: Calculate the ImageOffset.Y using the CanvasSize, ContentSize and ImageSize

			//var adjustedNewValue = new VectorDbl(-1 * newValue.X, newValue.Y - 128);
			var adjustedNewValue = new VectorDbl
				(
					-1 * newValue.X,
					-256 + (newValue.Y == 0 ? 0 : -1 * (128 - newValue.Y))
				);

			//var adjustedNewValue = newValue.Invert();

			// TODO: NOTE: This assumed that the size of the canvas, rounded up to the nearest whole block matches the size of the image, rounded up to the nearest whole block.
			// It is possible that the canvas size is larger,

			VectorDbl previousValue = new VectorDbl((double)Image.GetValue(Canvas.LeftProperty), (double)Image.GetValue(Canvas.TopProperty));

			if (ScreenTypeHelper.IsVectorDblChanged(adjustedNewValue, previousValue))
			{
				Image.SetValue(Canvas.LeftProperty, adjustedNewValue.X);
				Image.SetValue(Canvas.TopProperty, adjustedNewValue.Y);

				Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's ImageOffset is being set from {previousValue} to {adjustedNewValue}.");

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

		#region ViewportSize Dependency Property (Unscaled, aka Device Pixels)

		public static readonly DependencyProperty ViewportSizeProperty = DependencyProperty.Register(
					"ViewportSize", typeof(SizeDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(SizeDbl.Zero, FrameworkPropertyMetadataOptions.None, ViewportSize_PropertyChanged));

		private static void ViewportSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl)o;

			var previousValue = (SizeDbl)e.OldValue;
			var newValue = (SizeDbl)e.NewValue;

			if (newValue.Width == 0 && newValue.Height == 0)
			{
				return;
			}

			Debug.WriteLineIf(c._useDetailedDebug, $"\n\t\t====== The BitmapGridControl's ViewportSize is being updated from {previousValue} to {newValue}.");

			c.ViewportSizeChanged?.Invoke(c, new ValueTuple<SizeDbl, SizeDbl>(previousValue, newValue));
		}

		#endregion

		#region BitmapGridImageSource Dependency Property

		public static readonly DependencyProperty BitmapGridImageSourceProperty = DependencyProperty.Register(
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, BitmapGridImageSource_PropertyChanged));

		private static void BitmapGridImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl)o;
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
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void CheckViewportSize(SizeDbl previousValue, SizeDbl newValue)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			Debug.WriteLine($"The BitmapGridControl is having its ViewportSize updated from {previousValue} to {newValue}.");

			//Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(newValue, c._viewportSizeInternal), "The container size has been updated since the Debouncer fired.");

			if (!newValue.IsNearZero() && ScreenTypeHelper.IsSizeDblChanged(newValue, _viewportSizeInternal))
			{
				Debug.WriteLine("The ViewportSize is being updated from some source other than the ViewportSizeInternal property. If the ViewportSize property is holding back updates, the value just set may be undone.");
			}
		}

		[Conditional("DEBUG")]
		private void CompareCanvasAndControlHeights()
		{
			if (Math.Abs(Canvas.ActualHeight - ActualHeight) > 5)
			{
				Debug.WriteLine($"WARNING: The Canvas Height : {Canvas.ActualHeight} does not match the BitmapGridControl's height: {ActualHeight}.");
			}
		}

		[Conditional("DEBUG")]
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


		[Conditional("DEBUG")]
		private void CheckNewCanvasSize(SizeDbl canvasSize, double relativeScale)
		{
			// The Canvas Size (which is equal to the contentViewportSize reduced by the BaseScale Factor)
			// should equal the ViewportSize when it is expanded by the RelativeScale

			var viewportSize = new SizeDbl(ActualWidth, ActualHeight);
			var viewportSizeExpanded = viewportSize.Scale(1 / relativeScale);

			// TODO: Take into account the scrollbar widths.
			if (ScreenTypeHelper.IsSizeDblChanged(canvasSize, viewportSizeExpanded, threshold: 0.0001))
			{
				Debug.WriteLine("WARNING: The new CanvasSize is not equal to the Control's ActualSize  * 1 / relativeScale.");
			}
		}

		#endregion
	}
}
