using MSS.Common;
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

		private SizeDbl _logicalViewportSize;

		private bool _useScaling;

		private bool _useDetailedDebug = false;

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
			_translationAndClipSize = new RectangleDbl();

			//_viewportSize = new SizeDbl();
			//_contentViewportSize = SizeDbl.NaN;
			_logicalViewportSize = new SizeDbl();

			_useScaling = false;
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		// TODO: Make the BitmapGridControl's UseScaling property a Dependency Property.
		public bool UseScaling
		{
			get => _useScaling;
			set
			{
				_useScaling = value;
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
					_image = value;
					_image.Source = BitmapGridImageSource;
					_ = UpdateImageOffset(ImageOffset);

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
		}

		//private SizeDbl ViewportSizeInternalNew
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

					Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl: Viewport is changing from {previousValue} to {value}.");

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

		public VectorInt ImageOffset
		{
			get => (VectorInt)GetValue(ImageOffsetProperty);
			set
			{
				if (value != ImageOffset)
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
				_contentScale = value;

				if (!UseScaling)
				{
					if (value.Width != 1 | value.Height != 1)
					{
						Debug.WriteLine("WARNING: The ContentScale is being set but UseScaling is set to false.");
						//Debug.Assert(value == new SizeDbl(1, 1), "Setting the ContentScale to a value other than 1, when UseScaling is set to false.");
					}
				}

				SetTheCanvasScale(_contentScale);
			}
		}

		public RectangleDbl TranslationAndClipSize
		{
			get => _translationAndClipSize;
			set
			{
				if (ScreenTypeHelper.IsRectangleDblChanged(_translationAndClipSize, value))
				{
					if (!UseScaling)
					{
						Debug.WriteLine("WARNING: The TranslationAndClipSize is being set but UseScaling is set to false.");
					}

					var previousVal = _translationAndClipSize;
					_translationAndClipSize = value;

					LogicalViewportSize = ClipAndOffset(previousVal, value);
				}
			}
		}

		public SizeDbl LogicalViewportSize
		{
			get => _logicalViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(value, _logicalViewportSize))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}.");
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

			if (childSize != finalSize)
			{
				Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl is about to call _ourContent.Arrange({finalSize}). The canvas size is {new Size(Canvas.Width, Canvas.Height)}, actual canvas size: {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl has called _ourContent.Arrange({finalSize}). The canvas size is {new Size(Canvas.Width, Canvas.Height)}, actual canvas size: {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			if (!UseScaling)
			{ 
				var canvas = Canvas;

				if (canvas.ActualWidth != finalSize.Width)
				{
					canvas.Width = finalSize.Width;
				}

				if (canvas.ActualHeight != finalSize.Height)
				{
					canvas.Height = finalSize.Height;
				}
			}

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			return finalSize;
		}

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

			throw new InvalidOperationException("Cannot find a child image element of the BitmapGridControls's Content, or the Content is not a Canvas element.");
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

			c.LogicalViewportSize = newValue;
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
					"ImageOffset", typeof(VectorInt), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorInt.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			//var previousValue = (VectorInt)e.OldValue;
			var newValue = (VectorInt)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		#endregion

		#region Private Methods - Canvas

		private void SetTheCanvasScale(SizeDbl contentScale)
		{
			if (LogicalViewportSize.Width > 5 && LogicalViewportSize.Height > 5)
			{
				var (_, relativeScale) = ContentScalerHelper.GetBaseFactorAndRelativeScale(contentScale.Width);
				_canvasScaleTransform.ScaleX = relativeScale;
				_canvasScaleTransform.ScaleY = relativeScale;
			}
			else
			{
				_canvasScaleTransform.ScaleX = 1;
				_canvasScaleTransform.ScaleY = 1;
			}
		}

		private SizeDbl ClipAndOffset(RectangleDbl previousValue, RectangleDbl newValue)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's {nameof(TranslationAndClipSize)} is being updated " +
			//	$"from {RectangleDbl.FormatNully(previousValue)} to {RectangleDbl.FormatNully(newValue)}." +
			//	$"The CanvasScale is {new SizeDbl(_canvasScaleTransform.ScaleX, _canvasScaleTransform.ScaleY)}.");

			Debug.Assert(newValue.X1 >= 0 && newValue.Y1 >= 0, "ClipAndOffset is receiving a negative position.");
			var previousCanvasSize = new SizeDbl(Canvas.ActualWidth, Canvas.ActualHeight);
			var previousTranslation = new SizeDbl(_canvasTranslateTransform.X, _canvasTranslateTransform.Y);

			// Compensate for the fact that this implementation has alredy reduced the content by a factor of BaseScale.
			var baseScale = ContentScalerHelper.GetBaseScale(ContentScale.Width);

			// The Position is in physical pixels. We need to convert to Content Units
			// The Size is already in Logical Display Units, it only needs to be scaled by BaseScale.

			// Physcial pixels * Scale = logical display size.
			var scaledPosition = newValue.Position.Scale(ContentScale.Width);

			// Since our content is 2, 4, 8 times smaller, we need to compensate by multiplying by the 
			// BaseScale of 0.5, 0.25, 0.125, etc.

			var logicalPosition = scaledPosition.Scale(baseScale);
			var logicalViewportSize = newValue.Size.Scale(baseScale);

			Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl is handling ClipAndOffset. Setting the Canvas Size from {previousCanvasSize.ToString("F2")} to {logicalViewportSize.ToString("F2")}. " +
				$"Translation from: {previousTranslation.ToString("F2")} to {newValue.Position.ToString("F2")}. ");

			Canvas.Width = logicalViewportSize.Width;
			Canvas.Height = logicalViewportSize.Height;

			if (UseScaling)
			{
				// Translate using the unscaled value
				_canvasTranslateTransform.X = newValue.Position.X;
				_canvasTranslateTransform.Y = newValue.Position.Y;

				// Clip using the scaled value.
				var clipOrigin = new Point(Math.Max(logicalPosition.X, 0), Math.Max(logicalPosition.Y, 0));
				Canvas.Clip = new RectangleGeometry(new Rect(clipOrigin, ScreenTypeHelper.ConvertToSize(logicalViewportSize)));
			}
			else
			{
				_canvasTranslateTransform.X = 0;
				_canvasTranslateTransform.Y = 0;

				Canvas.Clip = null;
			}

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

		private bool UpdateImageOffset(VectorInt newValue)
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

			VectorDbl previousValue = new VectorDbl((double)Image.GetValue(Canvas.LeftProperty), (double)Image.GetValue(Canvas.TopProperty));

			// newValue.Y is the # of pixels of the first block to skip.

			var sizeOfLastBlock = RMapHelper.GetSizeOfLastBlock(LogicalViewportSize, newValue);

			var adjustedNewValue = new VectorDbl
				(
					-1 * newValue.X,
					sizeOfLastBlock.Height == 0 ? 0 : -1 * (128 - sizeOfLastBlock.Height)
				);

			if (adjustedNewValue.Y > 0)
			{
				Debug.WriteLine("WARNING: The ImageOffset Y value is positive.");
			}

			if (ScreenTypeHelper.IsVectorDblChanged(adjustedNewValue, previousValue))
			{
				Image.SetValue(Canvas.LeftProperty, adjustedNewValue.X);
				Image.SetValue(Canvas.TopProperty, adjustedNewValue.Y);

				Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's ImageOffset is being set from x: {previousValue.X.ToString("F2")} to {adjustedNewValue.X.ToString("F2")}({newValue.X.ToString("F2")})" +
					$"y: {previousValue.Y.ToString("F2")} to {adjustedNewValue.Y.ToString("F2")}({newValue.Y.ToString("F2")})");

				return true;
			}
			else
			{
				return false;
			}
		}

		//private VectorDbl GetTempImageOffset(VectorDbl originalOffset, SizeDbl originalSize, SizeDbl newSize)
		//{
		//	var diff = newSize.Sub(originalSize);
		//	var half = diff.Scale(0.5);
		//	var result = originalOffset.Sub(half);

		//	return result;
		//}

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
