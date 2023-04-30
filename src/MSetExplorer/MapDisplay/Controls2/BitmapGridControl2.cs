﻿using MSS.Types;
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

		private DebounceDispatcher _viewPortSizeDispatcher;
		private ScrollViewer? _scrollOwner;

		private FrameworkElement? _content;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _containerSize;
		private SizeDbl _viewPortSizeInternal;
		private SizeDbl _viewPortSize;

		#endregion

		#region Constructor

		public BitmapGridControl2()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_scrollOwner = null;

			_content = null;
			_canvas = new Canvas();
			_image = new Image();
			_canvas.Children.Add(_image);

			_containerSize = new SizeDbl();
			_viewPortSizeInternal = new SizeDbl();
			_viewPortSize = new SizeDbl();
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("Image SizeChanged");
			SetImageOffset(ImageOffset);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;
		public event EventHandler? ContentOffsetXChanged;
		public event EventHandler? ContentOffsetYChanged;

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

					SetImageOffset(ImageOffset);
				}
			}
		}

		private SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				var contentViewPortSize = new SizeDbl(ContentViewportWidth, ContentViewportHeight);
				Debug.WriteLine($"The BitmapGridControl is having its ContainerSize updated to {value}, the current value is {_containerSize}. The ContentViewPortSize is {contentViewPortSize}.");

				_containerSize = value;
				
				//InvalidateScrollInfo();
				
				ViewPortSizeInternal = value;
			}
		}

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
						_ = SetImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLine($"Updating the ViewPortSize after debounce. Previous Size: {ViewPortSize}, New Size: {newViewPortSize}.");
								ViewPortSize = newViewPortSize;
							}
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
					var copyOfNewValue = value;
					var contentViewPortSizeBefore = new SizeDbl(ContentViewportWidth, ContentViewportHeight);


					_viewPortSize = value;


					InvalidateScrollInfo();
					var copyOfNewValue2 = _viewPortSize;
					var contentViewPortSizeAfter = new SizeDbl(ContentViewportWidth, ContentViewportHeight);
					Debug.WriteLine($"The BitmapGridControl just called InvalidateScrollInfo. The ContentViewPortSize before: {contentViewPortSizeBefore}, after: {contentViewPortSizeAfter}. " +
						$"The value of ViewPortSize before raising the event is {copyOfNewValue}, the value after is {_viewPortSize}.");


					ViewPortSizeChanged?.Invoke(this, (ViewPortSize, value));

					var contentViewPortSize2 = new SizeDbl(ContentViewportWidth, ContentViewportHeight);

					Debug.WriteLine($"The BitmapGridControl just raised the ViewPortSizeChanged event. The ContentViewPortSize before: {contentViewPortSizeAfter}, after: {contentViewPortSize2}. " +
						$"The value of ViewPortSize before raising the event is {copyOfNewValue2}, the value after is {_viewPortSize}.");
				}
				else
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewPortSize updated to {value}, the current value is already: {_viewPortSize}; not raising the ViewPortSizeChanged event.");
				}
			}
		}

		public VectorDbl ImageOffset
		{
			get => (VectorDbl)GetValue(ImageOffsetProperty);
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(ImageOffset, value))
				{
					SetValue(ImageOffsetProperty, value);
				}
			}
		}

		#endregion

		#region Private ContentControl Methods

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

		#region Dependency Properties

		//#region ViewPortSize Dependency Property

		//private static SizeDbl DEFAULT_VIEWPORT_SIZE = new SizeDbl(200, 200);

		//public static readonly DependencyProperty ViewPortSizeProperty = DependencyProperty.Register(
		//			"ViewPortSize", typeof(SizeDbl), typeof(BitmapGridControl2),
		//			new FrameworkPropertyMetadata(DEFAULT_VIEWPORT_SIZE, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ViewPortSize_PropertyChanged));

		//private static void ViewPortSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	BitmapGridControl c = (BitmapGridControl)o;
		//	var previousValue = (SizeDbl)e.OldValue;
		//	var value = (SizeDbl)e.NewValue;

		//	if (ScreenTypeHelper.IsSizeDblChanged(previousValue, value))
		//	{
		//		//Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing. The old size: {previousValue}, new size: {value}.");

		//		//var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, c._blockSize, KEEP_DISPLAY_SQUARE);
		//		//c._bitmapGrid.CanvasSizeInBlocks = sizeInWholeBlocks;

		//		//c.InvalidateScrollInfo();
		//		c.ViewPortSizeChanged?.Invoke(c, new(previousValue, value));
		//	}
		//	else
		//	{
		//		//Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing by a very small amount, IGNORING. The old size: {previousValue}, new size: {value}.");
		//	}
		//}

		//public SizeDbl ViewPortSize
		//{
		//	get => (SizeDbl)GetValue(ViewPortSizeProperty);
		//	set
		//	{
		//		if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
		//		{
		//			_bitmapGrid.ViewPortSize = value;
		//			SetValue(ViewPortSizeProperty, value);
		//		}
		//	}
		//}

		//#endregion

		#region ImageSource Dependency Property

		public static readonly DependencyProperty BitmapGridImageSourceProperty = DependencyProperty.Register(
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl2),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BitmapGridImageSource_PropertyChanged));

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
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl2 c = (BitmapGridControl2)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			if (c.SetImageOffset(newValue))
			{
				c.InvalidateScrollInfo();
			}
		}

		private bool SetImageOffset(VectorDbl newValue)
		{
			//Debug.Assert(offset.X >= 0 && offset.Y >= 0, "Setting offset to negative value.");

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
					new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, UnscaledExtent_PropertyChanged));

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
			set => SetValue(UnscaledExtentProperty, value);
		}

		#endregion

		#endregion Dependency Properties

		#region Scroll Support

		#region Private Fields - Scroll Support

		/// <summary>
		/// The transform that is applied to the content to offset it by 'ContentOffsetX' and 'ContentOffsetY'.
		/// </summary>
		private TranslateTransform? _contentOffsetTransform = null;

		///// <summary>
		///// The transform that is applied to the content to scale it by 'ContentScale'.
		///// </summary>
		//private ScaleTransform? _contentScaleTransform = null;

		///// <summary>
		///// Enable the update of the content offset as the content scale changes.
		///// This enabled for zooming about a point (google-maps style zooming) and zooming to a rect.
		///// </summary>
		//private bool _enableContentOffsetUpdateFromScale = false;

		/// <summary>
		/// Used to disable syncronization between IScrollInfo interface and ContentOffsetX/ContentOffsetY.
		/// </summary>
		private bool _disableScrollOffsetSync = false;

		/// <summary>
		/// Normally when content offsets changes the content focus is automatically updated.
		/// This syncronization is disabled when 'disableContentFocusSync' is set to 'true'.
		/// When we are zooming in or out we 'disableContentFocusSync' is set to 'true' because 
		/// we are zooming in or out relative to the content focus we don't want to update the focus.
		/// </summary>
		private bool _disableContentFocusSync = false;

		/// <summary>
		/// The width of the viewport in content coordinates, clamped to the width of the content.
		/// </summary>
		private double _constrainedContentViewportWidth = 0.0;

		/// <summary>
		/// The height of the viewport in content coordinates, clamped to the height of the content.
		/// </summary>
		private double _constrainedContentViewportHeight = 0.0;

		#endregion

		#region Private Methods - Scroll Support

		/// <summary>
		/// Reset the viewport zoom focus to the center of the viewport.
		/// </summary>
		private void ResetViewportZoomFocus()
		{
			//ViewportZoomFocusX = ViewportWidth / 2;
			//ViewportZoomFocusY = ViewportHeight / 2;
		}

		/// <summary>
		/// Update the viewport size from the specified size.
		/// </summary>
		private void UpdateViewportSize(Size newSize)
		{
			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(newSize);

			if (ContainerSize == newSizeDbl)
			{
				// The viewport is already the specified size.
				return;
			}

			if (_content != null)
			{
				_content.Arrange(new Rect(newSize));
			}

			ContainerSize = newSizeDbl;

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

		/// <summary>
		/// Update the size of the viewport in content coordinates after the viewport size or 'ContentScale' has changed.
		/// </summary>
		private void UpdateContentViewportSize()
		{
			ContentViewportWidth = ViewportWidth / _contentScale;
			ContentViewportHeight = ViewportHeight / _contentScale;

			_constrainedContentViewportWidth = Math.Min(ContentViewportWidth - HORIZONTAL_SCROLL_BAR_WIDTH, UnscaledExtent.Width);
			_constrainedContentViewportHeight = Math.Min(ContentViewportHeight - VERTICAL_SCROLL_BAR_WIDTH, UnscaledExtent.Height);

			UpdateTranslationX();
			UpdateTranslationY();
		}

		/// <summary>
		/// Update the X coordinate of the translation transformation.
		/// </summary>
		private void UpdateTranslationX()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentWidth = UnscaledExtent.Width * _contentScale;
				if (scaledContentWidth < ViewportWidth)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.X = (ContentViewportWidth - UnscaledExtent.Width) / 2;
				}
				else
				{
					_contentOffsetTransform.X = -ContentOffsetX;
				}
			}
		}

		/// <summary>
		/// Update the Y coordinate of the translation transformation.
		/// </summary>
		private void UpdateTranslationY()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentHeight = UnscaledExtent.Height * _contentScale;
				if (scaledContentHeight < ViewportHeight)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.Y = (ContentViewportHeight - UnscaledExtent.Height) / 2;
				}
				else
				{
					_contentOffsetTransform.Y = -ContentOffsetY;
				}
			}
		}

		/// <summary>
		/// Update the X coordinate of the zoom focus point in content coordinates.
		/// </summary>
		private void UpdateContentZoomFocusX()
		{
			//ContentZoomFocusX = ContentOffsetX + (_constrainedContentViewportWidth / 2);
		}

		/// <summary>
		/// Update the Y coordinate of the zoom focus point in content coordinates.
		/// </summary>
		private void UpdateContentZoomFocusY()
		{
			//ContentZoomFocusY = ContentOffsetY + (_constrainedContentViewportHeight / 2);
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
			double value = (double)baseValue;
			double minOffsetX = 0.0;
			double maxOffsetX = c.UnscaledExtent.IsEmpty ? 0.0 : Math.Max(0.0, c.UnscaledExtent.Width - c._constrainedContentViewportWidth);
			value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);

			Debug.WriteLine($"CoerceOffsetX got: {baseValue} and returned {value}. UnscaledExtent.Width: {c.UnscaledExtent.Width}, ContrainedContentViewportWidth: {c._constrainedContentViewportWidth}. ViewportWidth: {c.ViewportWidth} ContentViewPortWidth: {c.ContentViewportWidth}.");

			return value;
		}

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
		
		#region ContentViewportWidth Dependency Property

		public static readonly DependencyProperty ContentViewportWidthProperty =
				DependencyProperty.Register("ContentViewportWidth", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(0.0));

		/// <summary>
		/// Get the viewport width, in content coordinates.
		/// </summary>
		public double ContentViewportWidth
		{
			get
			{
				return (double)GetValue(ContentViewportWidthProperty);
			}
			set
			{
				SetValue(ContentViewportWidthProperty, value);
			}
		}

		#endregion

		#region ContentViewportHeight Dependency Property

		public static readonly DependencyProperty ContentViewportHeightProperty =
				DependencyProperty.Register("ContentViewportHeight", typeof(double), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(0.0));

		/// <summary>
		/// Get the viewport height, in content coordinates.
		/// </summary>
		public double ContentViewportHeight
		{
			get
			{
				return (double)GetValue(ContentViewportHeightProperty);
			}
			set
			{
				SetValue(ContentViewportHeightProperty, value);
			}
		}

		#endregion

		#region IsMouseWheelScrollingEnabled Dependency Property

		public static readonly DependencyProperty IsMouseWheelScrollingEnabledProperty =
				DependencyProperty.Register("IsMouseWheelScrollingEnabled", typeof(bool), typeof(BitmapGridControl2),
											new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Set to 'true' to enable the mouse wheel to scroll the zoom and pan control.
		/// This is set to 'false' by default.
		/// </summary>
		public bool IsMouseWheelScrollingEnabled
		{
			get
			{
				return (bool)GetValue(IsMouseWheelScrollingEnabledProperty);
			}
			set
			{
				SetValue(IsMouseWheelScrollingEnabledProperty, value);
			}
		}


		#endregion

		#endregion Scroll Support

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
	}
}
