using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public partial class BitmapGridControl2 : ContentControl
	{
		#region Private Fields

		private const double VERTICAL_SCROLL_BAR_WIDTH = 17;
		private const double HORIZONTAL_SCROLL_BAR_WIDTH = 17;

		public static readonly double DefaultContentScale = 1.0;
		public static readonly double DefaultMinContentScale = 0.0625;
		public static readonly double DefaultMaxContentScale = 1.0;

		private bool _canHScroll = true;
		private bool _canVScroll = true;
		private bool _canZoom = true;

		private DebounceDispatcher _viewPortSizeDispatcher;
		private ScrollViewer? _scrollOwner;
		private ZoomSlider? _zoomSlider;

		private FrameworkElement? _content;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewPortSizeInternal;
		private SizeDbl _viewPortSize;

		private TranslateTransform? _contentOffsetTransform = null;
		private ScaleTransform? _contentScaleTransform = null;

		private bool _enableContentOffsetUpdateFromScale = false;
		private bool _disableScrollOffsetSync = false;
		private bool _disableContentFocusSync = false;

		private double _constrainedContentViewportWidth = 0.0;
		private double _constrainedContentViewportHeight = 0.0;

		#endregion

		#region Constructor

		public BitmapGridControl2()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_scrollOwner = null;
			_zoomSlider = null;

			_content = null;
			_canvas = new Canvas();
			_image = new Image();
			_canvas.Children.Add(_image);

			//_containerSize = new SizeDbl();
			_viewPortSizeInternal = new SizeDbl();
			_viewPortSize = new SizeDbl();

			ContentViewportSize = new SizeDbl();

			IsMouseWheelScrollingEnabled = false;
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("Image SizeChanged");
			UpdateImageOffset(ImageOffset);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;
		public event EventHandler? ContentOffsetXChanged;
		public event EventHandler? ContentOffsetYChanged;
		public event EventHandler? ContentScaleChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas
		{
			get => _canvas;
			set => _canvas = value;
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

					_image.Source = BitmapGridImageSource;

					UpdateImageOffset(ImageOffset);
				}
			}
		}

		//private SizeDbl ContainerSize
		//{
		//	get => _containerSize;
		//	set
		//	{
		//		var contentViewPortSize = ContentViewportSize;
		//		Debug.WriteLine($"The BitmapGridControl is having its ContainerSize updated to {value}, the current value is {_containerSize}. The ContentViewPortSize is {contentViewPortSize}.");

		//		_containerSize = value;
				
		//		//InvalidateScrollInfo();
				
		//		ViewPortSizeInternal = value;
		//	}
		//}

		private SizeDbl ViewPortSizeInternal
		{
			get => _viewPortSizeInternal;
			set
			{
				if (value.Width > 1 && value.Height > 1 && _viewPortSizeInternal != value)
				{
					var previousValue = _viewPortSizeInternal;
					_viewPortSizeInternal = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");

					var newViewPortSize = value;

					if (previousValue.Width < 25 || previousValue.Height < 25)
					{
						// Update the 'real' value immediately
						Debug.WriteLine($"Updating the ViewPortSize immediately. Previous Size: {previousValue}, New Size: {value}.");
						ViewPortSize = newViewPortSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
						var tempOffset = GetTempImageOffset(ImageOffset, ViewPortSize, newViewPortSize);
						_ = UpdateImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLine($"Updating the ViewPortSize after debounce. Previous Size: {ViewPortSize}, New Size: {newViewPortSize}.");
								ViewPortSize = newViewPortSize;
							},
							param: null
						);
					}
				}
				else
				{
					Debug.WriteLine($"Skipping the update of the ViewPortSize, the new value {value} is the same as the old value. {ViewPortSizeInternal}.");
				}
			}
		}

		public SizeDbl ViewPortSize
		{
			get => _viewPortSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewPortSize updated to {value}, the current value is {_viewPortSize}; will raise the ViewPortSizeChanged event.");

					var previousValue = ViewPortSize;
					_viewPortSize = value;

					Debug.Assert(_viewPortSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					//UpdateViewportSize()

					ViewPortSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewPortSize updated to {value}, the current value is already: {_viewPortSize}; not raising the ViewPortSizeChanged event.");
				}
			}
		}

		public SizeDbl ContentViewportSize { get; set; }

		public bool IsMouseWheelScrollingEnabled { get; set; }

		#endregion

		#region Private Methods - Control

		protected override Size MeasureOverride(Size constraint)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Size childSize = base.MeasureOverride(infiniteSize);

			double width = constraint.Width;
			double height = constraint.Height;

			if (double.IsInfinity(width))
			{
				// Make sure we don't return infinity!
				width = childSize.Width;
			}

			if (double.IsInfinity(height))
			{
				// Make sure we don't return infinity!
				height = childSize.Height;
			}

			// Update the size of the viewport onto the content based on the passed in 'constraint'.
			var theNewSize = new Size(width, height);
			UpdateViewportSize(theNewSize);

			return theNewSize;
		}

		protected override Size ArrangeOverride(Size finalSizeRaw)
		{
			var finalSize = ForceSize(finalSizeRaw);

			UpdateViewportSize(finalSize);

			return finalSize;
		}

		private Size ForceSize(Size finalSize)
		{
			if (finalSize.Width > 1020 && finalSize.Width < 1030 && finalSize.Height > 1020 && finalSize.Height < 1030)
			{
				return new Size(1024, 1024);
			}
			else
			{
				return finalSize;
			}
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = Template.FindName("BitmapGridControl_Content", this) as FrameworkElement;
			if (_content != null)
			{
				Debug.WriteLine($"Found the BitmapGridControl_Content template.");

				// Setup the transform on the content so that we can position the Bitmap to "pull" it left and up so that the
				// portion of the bitmap that is visible corresponds with the requested map coordinates.

				//_content.RenderTransformOrigin = _contentRenderTransformOrigin;
				//_content.RenderTransform = _transformGroup;

				(Canvas, Image) = BuildContentModel(_content);

				//
				// Setup the transform on the content so that we can scale it by 'ContentScale'.
				//
				_contentScaleTransform = new ScaleTransform(ContentScale, ContentScale);


				//
				// Setup the transform on the content so that we can translate it by 'ContentOffsetX' and 'ContentOffsetY'.
				//
				//_contentOffsetTransform = new TranslateTransform();
				UpdateTranslationX();
				UpdateTranslationY();

				//
				// Setup a transform group to contain the translation and scale transforms, and then
				// assign this to the content's 'RenderTransform'.
				//
				//TransformGroup transformGroup = new TransformGroup();
				
				//transformGroup.Children.Add(_contentOffsetTransform);
				//transformGroup.Children.Add(_contentScaleTransform);

				//_content.RenderTransform = transformGroup;

				_content.RenderTransform = _contentScaleTransform;

			}
			else
			{
				Debug.WriteLine($"WARNING: Did not find the BitmapGridControl_Content template.");
			}
		}

		private (Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					//return ca;
					if (ca.Children[0] is Image im)
					{
						return (ca, im);
					}
				}
			}

			throw new InvalidOperationException("Cannot find the bmgcImage element on the BitmapGridControl_Content.");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateScrollInfo()
		{
			if (_scrollOwner != null && !_disableScrollOffsetSync)
			{
				_scrollOwner.InvalidateScrollInfo();
			}
		}

		#endregion

		#region ImageSource Dependency Property

		public static readonly DependencyProperty BitmapGridImageSourceProperty = DependencyProperty.Register(
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl2),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, BitmapGridImageSource_PropertyChanged));

		private static void BitmapGridImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl2)o;
			var previousValue = (ImageSource)e.OldValue;
			var value = (ImageSource)e.NewValue;

			if (value != previousValue)
			{
				c.Image.Source = value;
			}
		}

		public ImageSource BitmapGridImageSource
		{
			get => (ImageSource)GetValue(BitmapGridImageSourceProperty);
			set => SetCurrentValue(BitmapGridImageSourceProperty, value);
		}

		#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl2),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			if (c.UpdateImageOffset(newValue))
			{
				c.InvalidateScrollInfo();
			}
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

		private VectorDbl GetTempImageOffset(VectorDbl originalOffset, SizeDbl originalSize, SizeDbl newSize)
		{
			var diff = newSize.Diff(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		#endregion

		#region UnscaledExtent Dependency Property

		public static readonly DependencyProperty UnscaledExtentProperty = DependencyProperty.Register(
					"UnscaledExtent", typeof(Size), typeof(BitmapGridControl2),
					new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.None, UnscaledExtent_PropertyChanged));

		private static void UnscaledExtent_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;
			var previousValue = (Size)e.OldValue;
			var value = (Size)e.NewValue;

			if (value != previousValue)
			{
				c.ContentOffsetX = 0;
				c.ContentOffsetY = 0;
			}

			c.InvalidateMeasure();
		}

		public Size UnscaledExtent
		{
			get => (Size)GetValue(UnscaledExtentProperty);
			set => SetCurrentValue(UnscaledExtentProperty, value);
		}

		#endregion

		#region ContentScale Dependency Property

		// TODO: Make this use two-way binding
		public static readonly DependencyProperty ContentScaleProperty =
				DependencyProperty.Register("ContentScale", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(DefaultContentScale, ContentScale_PropertyChanged, ContentScale_Coerce));

		/// <summary>
		/// Event raised when the 'ContentScale' property has changed value.
		/// </summary>
		private static void ContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"BitmapGridControl: ContentScale is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");

			BitmapGridControl2 c = (BitmapGridControl2)o;

			var newValue = (double)e.NewValue;

			Debug.Assert(newValue == c.ContentScale, "The NewValue for Content Scale does not match the ContentScale.");

			if (!c.UpdateScale(newValue))
			{
				// The new value is the same as the both the ScaleX and ScaleY value on the _contentScaleTransform property.
				return;
			}

			c.UpdateContentViewportSize();

			c.UpdateContentOffsetsFromScale();

			c.ContentScaleChanged?.Invoke(c, EventArgs.Empty);

			c.ZoomSliderOwner?.ContentScaleWasUpdated(c.ContentScale);

			c.InvalidateVisual(); // Is this really necessary?
		}

		/// <summary>
		/// Method called to clamp the 'ContentScale' value to its valid range.
		/// </summary>
		private static object ContentScale_Coerce(DependencyObject d, object baseValue)
		{
			BitmapGridControl2 c = (BitmapGridControl2)d;
			double value = (double)baseValue;
			value = Math.Min(Math.Max(value, c.MinContentScale), c.MaxContentScale);
			return value;
		}

		/// <summary>
		/// Get/set the current scale (or zoom factor) of the content.
		/// </summary>
		public double ContentScale
		{
			get => (double)GetValue(ContentScaleProperty);
			set => SetValue(ContentScaleProperty, value);
		}

		#endregion

		#region MinContentScale and MaxContentScale Dependency Properties

		public static readonly DependencyProperty MinContentScaleProperty =
				DependencyProperty.Register("MinContentScale", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(DefaultMinContentScale, MinOrMaxContentScale_PropertyChanged));


		public static readonly DependencyProperty MaxContentScaleProperty =
				DependencyProperty.Register("MaxContentScale", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(DefaultMaxContentScale, MinOrMaxContentScale_PropertyChanged));

		/// <summary>
		/// Event raised 'MinContentScale' or 'MaxContentScale' has changed.
		/// </summary>
		private static void MinOrMaxContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;
			c.ContentScale = Math.Min(Math.Max(c.ContentScale, c.MinContentScale), c.MaxContentScale);

			c.ZoomSliderOwner?.InvalidateScaleContentInfo();
		}

		public double MinContentScale
		{
			get => (double)GetValue(MinContentScaleProperty);
			set => SetValue(MinContentScaleProperty, value);
		}

		/// <summary>
		/// Get/set the maximum value for 'ContentScale'.
		/// </summary>
		public double MaxContentScale
		{
			get => (double)GetValue(MaxContentScaleProperty);
			set => SetValue(MaxContentScaleProperty, value);
		}

		#endregion

		#region ContentOffsetX Dependency Property

		public static readonly DependencyProperty ContentOffsetXProperty =
				DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetX' property has changed value.
		/// </summary>
		private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;

			c.UpdateTranslationX();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocusX();
			}

			// Raise an event to let users of the control know that the content offset has changed.
			c.ContentOffsetXChanged?.Invoke(c, EventArgs.Empty);

			c.InvalidateScrollInfo();
		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetX' value to its valid range.
		/// </summary>
		private static object ContentOffsetX_Coerce(DependencyObject d, object baseValue)
		{
			BitmapGridControl2 c = (BitmapGridControl2)d;

			var gap = c.ContentViewportSize.Width != c.ViewportWidth;

			var gap2a = Math.Min(c.ContentViewportSize.Width - HORIZONTAL_SCROLL_BAR_WIDTH, c.UnscaledExtent.Width);
			var gap2 = c._constrainedContentViewportWidth != gap2a;

			double value = (double)baseValue;
			double minOffsetX = 0.0;
			double maxOffsetX = c.UnscaledExtent.IsEmpty ? 0.0 : Math.Max(0.0, c.UnscaledExtent.Width - c._constrainedContentViewportWidth);
			value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);

			if (gap || gap2)
			{
				Debug.WriteLine($"CoerceOffsetX got: {baseValue} and returned {value}. UnscaledExtent.Width: {c.UnscaledExtent.Width}, ContrainedContentViewportWidth: {c._constrainedContentViewportWidth}. " +
					$"ViewportWidth: {c.ViewportWidth} ContentViewPortWidth: {c.ContentViewportSize.Width}. " +
					$"Gaps: {gap}, {gap2a}, {gap2}");
			}

			return value;
		}

		/*

			ContentViewportWidth = ViewportWidth / ContentScale;
			_constrainedContentViewportWidth = Math.Min(ContentViewportWidth - HORIZONTAL_SCROLL_BAR_WIDTH, UnscaledExtent.Width);

		*/



		/// <summary>
		/// Get/set the X offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetX
		{
			get => (double)GetValue(ContentOffsetXProperty);
			set => SetValue(ContentOffsetXProperty, value);
		}

		#endregion

		#region ContentOffsetY Dependency Property

		public static readonly DependencyProperty ContentOffsetYProperty =
				DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetY' property has changed value.
		/// </summary>
		private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;

			c.UpdateTranslationY();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocusY();
			}

			// Raise an event to let users of the control know that the content offset has changed.
			c.ContentOffsetYChanged?.Invoke(c, EventArgs.Empty);

			c.InvalidateScrollInfo();
		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetY' value to its valid range.
		/// </summary>
		private static object ContentOffsetY_Coerce(DependencyObject d, object baseValue)
		{
			BitmapGridControl2 c = (BitmapGridControl2)d;
			double value = (double)baseValue;
			double minOffsetY = 0.0;
			double maxOffsetY = c.UnscaledExtent.IsEmpty ? 0.0  : Math.Max(0.0, c.UnscaledExtent.Height - c._constrainedContentViewportHeight);
			value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);

			Debug.WriteLine($"CoerceOffsetY got: {baseValue} and returned {value}.");
			
			return value;
		}

		/// <summary>
		/// Get/set the Y offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetY
		{
			get => (double)GetValue(ContentOffsetYProperty);
			set => SetValue(ContentOffsetYProperty, value);
		}

		#endregion
		
		#region Private Methods - Scroll Support

		private void ResetViewportZoomFocus()
		{
			//ViewportZoomFocusX = ViewportWidth / 2;
			//ViewportZoomFocusY = ViewportHeight / 2;
		}

		private void UpdateViewportSize(Size newSize)
		{
			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(newSize);

			if (ViewPortSizeInternal == newSizeDbl)
			{
				// The viewport is already the specified size.
				return;
			}

			if (_content != null)
			{
				_content.Arrange(new Rect(newSize));
			}

			ViewPortSizeInternal = newSizeDbl;

			// Update the viewport size in content coordiates.
			UpdateContentViewportSize();

			// Initialise the content zoom focus point.
			UpdateContentZoomFocusX();
			UpdateContentZoomFocusY();

			// Reset the viewport zoom focus to the center of the viewport.
			ResetViewportZoomFocus();

			// Update content offset from itself when the size of the viewport changes.
			// This ensures that the content offset remains properly clamped to its valid range.
			ContentOffsetX = ContentOffsetX;
			ContentOffsetY = ContentOffsetY;

			InvalidateScrollInfo();
		}

		private void UpdateContentViewportSize()
		{
			//ContentViewportWidth = ViewportWidth / ContentScale;
			//ContentViewportHeight = ViewportHeight / ContentScale;
			
			ContentViewportSize = ViewPortSizeInternal.Divide(ContentScale);

			if (UnscaledExtent.IsEmpty)
			{
				_constrainedContentViewportWidth = ContentViewportSize.Width;
				_constrainedContentViewportHeight = ContentViewportSize.Height;
			}
			else
			{
				Debug.Assert(UnscaledExtent.Width != double.NegativeInfinity && UnscaledExtent.Height != double.NegativeInfinity, "UnscaledExtent is not empty but the Width or Height is Negative Infinity.");
				_constrainedContentViewportWidth = Math.Min(ContentViewportSize.Width - HORIZONTAL_SCROLL_BAR_WIDTH, UnscaledExtent.Width);
				_constrainedContentViewportHeight = Math.Min(ContentViewportSize.Height - VERTICAL_SCROLL_BAR_WIDTH, UnscaledExtent.Height);
			}

			UpdateTranslationX();
			UpdateTranslationY();
		}

		private void UpdateTranslationX()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentWidth = UnscaledExtent.Width * ContentScale;
				if (scaledContentWidth < ViewportWidth)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.X = (ContentViewportSize.Width - UnscaledExtent.Width) / 2;
				}
				else
				{
					_contentOffsetTransform.X = -ContentOffsetX;
				}
			}
		}

		private void UpdateTranslationY()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentHeight = UnscaledExtent.Height * ContentScale;
				if (scaledContentHeight < ViewportHeight)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.Y = (ContentViewportSize.Height - UnscaledExtent.Height) / 2;
				}
				else
				{
					_contentOffsetTransform.Y = -ContentOffsetY;
				}
			}
		}

		private void UpdateContentZoomFocusX()
		{
			//ContentZoomFocusX = ContentOffsetX + (_constrainedContentViewportWidth / 2);
		}

		private void UpdateContentZoomFocusY()
		{
			//ContentZoomFocusY = ContentOffsetY + (_constrainedContentViewportHeight / 2);
		}

		private bool UpdateImageOffset(VectorDbl newValue)
		{
			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)Image.GetValue(Canvas.LeftProperty),
				(double)Image.GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				Image.SetValue(Canvas.LeftProperty, invertedValue.X);
				Image.SetValue(Canvas.BottomProperty, invertedValue.Y);

				return true;
			}
			else
			{
				return false;
			}
		}

		private bool UpdateScale(double contentScale)
		{
			var result = false;

			if (_contentScaleTransform != null)
			{
				if (_contentScaleTransform.ScaleX != contentScale)
				{
					_contentScaleTransform.ScaleX = contentScale;
					result = true;
				}

				if (_contentScaleTransform.ScaleY != contentScale)
				{
					_contentScaleTransform.ScaleY = contentScale;
					result = true;
				}
			}

			return result;
		}

		private void UpdateContentOffsetsFromScale()
		{
			if (_enableContentOffsetUpdateFromScale)
			{
				try
				{
					// 
					// Disable content focus syncronization.  We are about to update content offset whilst zooming
					// to ensure that the viewport is focused on our desired content focus point.  Setting this
					// to 'true' stops the automatic update of the content focus when content offset changes.
					//
					_disableContentFocusSync = true;

					//
					// Whilst zooming in or out keep the content offset up-to-date so that the viewport is always
					// focused on the content focus point (and also so that the content focus is locked to the 
					// viewport focus point - this is how the google maps style zooming works).
					//
					//double viewportOffsetX = c.ViewportZoomFocusX - (c.ViewportWidth / 2);
					//double viewportOffsetY = c.ViewportZoomFocusY - (c.ViewportHeight / 2);

					//double contentOffsetX = viewportOffsetX / c.ContentScale;
					//double contentOffsetY = viewportOffsetY / c.ContentScale;

					//c.ContentOffsetX = (c.ContentZoomFocusX - (c.ContentViewportWidth / 2)) - contentOffsetX;
					//c.ContentOffsetY = (c.ContentZoomFocusY - (c.ContentViewportHeight / 2)) - contentOffsetY;
				}
				finally
				{
					_disableContentFocusSync = false;
				}
			}
		}

		#endregion



		#region Unused SetCanvasTransform / ReportSizes / CheckImageSize

		//private void SetCanvasTransform(PointDbl scale)
		//{
		//	_canvas.RenderTransformOrigin = new Point(0.5, 0.5);
		//	_canvas.RenderTransform = new ScaleTransform(scale.X, scale.Y);
		//}

		public void ReportSizes(string label)
		{
			var controlSize = new SizeInt(ActualWidth, ActualHeight);
			var canvasSize = new SizeInt(_canvas.ActualWidth, _canvas.ActualHeight);
			var imageSize = new Size(Image.ActualWidth, Image.ActualHeight);

			//var bitmapSize = new Size(_bitmapGrid.Bitmap.Width, _bitmapGrid.Bitmap.Height);

			//var canvasSizeInBlocks = _bitmapGrid.CanvasSizeInBlocks;

			//Debug.WriteLine($"At {label}, Control: {controlSize}, Canvas: {canvasSize}, Image: {imageSize}, Bitmap: {bitmapSize}, CanvasSizeInBlocks: {canvasSizeInBlocks}.");
			Debug.WriteLine($"At {label}, Control: {controlSize}, Canvas: {canvasSize}, Image: {imageSize}.");
		}

		//private bool IsCanvasSizeInWBsReasonable(SizeInt canvasSizeInWholeBlocks)
		//{
		//	var result = !(canvasSizeInWholeBlocks.Width > 50 || canvasSizeInWholeBlocks.Height > 50);
		//	return result;
		//}

		///// <summary>
		///// The position of the canvas' origin relative to the Image Block Data
		///// </summary>
		//private void SetCanvasOffset(VectorInt value, double displayZoom)
		//{
		//	//if (value != _offset || Math.Abs(displayZoom - _offsetZoom) > 0.001)
		//	//{
		//	//	//Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}. The ScreenCollection Index is {_vm.ScreenCollectionIndex}");
		//	//	Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}.");
		//	//	Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

		//	//	_offset = value;
		//	//	_offsetZoom = displayZoom;

		//	//	// For a postive offset, we "pull" the image down and to the left.
		//	//	var invertedOffset = value.Invert();

		//	//	var roundedZoom = RoundZoomToOne(displayZoom);
		//	//	var scaledInvertedOffset = invertedOffset.Scale(1 / roundedZoom);

		//	//	_image.SetValue(Canvas.LeftProperty, (double)scaledInvertedOffset.X);
		//	//	_image.SetValue(Canvas.BottomProperty, (double)scaledInvertedOffset.Y);

		//	//	ReportSizes("SetCanvasOffset");
		//	//}
		//}

		//private double RoundZoomToOne(double scale)
		//{
		//	var zoomIsOne = Math.Abs(scale - 1) < 0.001;

		//	if (!zoomIsOne)
		//	{
		//		Debug.WriteLine($"WARNING: MapSectionDisplayControl: Display Zoom is not one.");
		//	}

		//	return zoomIsOne ? 1d : scale;
		//}

		#endregion

		//#region ViewPortSize Dependency Property

		////public static readonly DependencyProperty ViewPortSizeProperty = DependencyProperty.Register(
		////			"ViewPortSize", typeof(Size), typeof(BitmapGridControl2),
		////			new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ViewPortSize_PropertyChanged));


		//public static readonly DependencyProperty ViewPortSizeProperty = DependencyProperty.Register(
		//			"ViewPortSize", typeof(Size), typeof(BitmapGridControl2),
		//			new FrameworkPropertyMetadata(ViewPortSize_PropertyChanged));

		//private static void ViewPortSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	BitmapGridControl2 c = (BitmapGridControl2)o;

		//	var dpValue = o.GetValue(ViewPortSizeProperty);

		//	var previousValueDbl = ScreenTypeHelper.ConvertToSizeDbl((Size)e.OldValue);
		//	var valueDbl = ScreenTypeHelper.ConvertToSizeDbl((Size)e.NewValue);

		//	if (ScreenTypeHelper.IsSizeDblChanged(previousValueDbl, valueDbl))
		//	{
		//		Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");

		//		var value = (Size)e.NewValue;

		//		//c.InvalidateScrollInfo();

		//		c.ViewPortSizeChanged?.Invoke(c, new((Size)e.OldValue, value));
		//	}
		//	else
		//	{
		//		//Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing by a very small amount, IGNORING. The old size: {previousValue}, new size: {value}.");
		//	}
		//}

		//public Size ViewPortSize
		//{
		//	get => (Size)GetValue(ViewPortSizeProperty);
		//	set
		//	{
		//		//if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
		//		//{
		//		//	_bitmapGrid.ViewPortSize = value;
		//		//	SetValue(ViewPortSizeProperty, value);
		//		//}

		//		SetCurrentValue(ViewPortSizeProperty, value);

		//	}
		//}

		//#endregion
	}
}
