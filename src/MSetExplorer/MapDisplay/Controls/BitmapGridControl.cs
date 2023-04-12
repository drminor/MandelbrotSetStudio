﻿using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Streaming.Adaptive;

namespace MSetExplorer
{
	public partial class BitmapGridControl : ContentControl
	{
		#region Private Properties

		private static readonly bool KEEP_DISPLAY_SQUARE = true;

		private readonly SizeInt _blockSize;

		private ScrollViewer? _scrollOwner;
		private FrameworkElement? _content;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _containerSize;
		private SizeDbl _viewPortSizeInternal;

		//private Size _viewPortSize;
		//private SizeInt _viewPortSizeInBlocks;
		//private VectorDbl _canvasControlOffset;

		private VectorDbl _imageOffsetInternal;

		private Point _offset;
		private Size _unscaledExtent;

		private Point _contentRenderTransformOrigin;
		private TranslateTransform _contentOffsetTransform;
		private TransformGroup _transformGroup;

		private DebounceDispatcher _viewPortSizeDispatcher;

		#endregion

		#region Constructor

		public BitmapGridControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_blockSize = RMapConstants.BLOCK_SIZE;
			_scrollOwner = null;

			_content = null; 
			_canvas = new Canvas();
			_image = new Image();
			_containerSize = new SizeDbl();
			_viewPortSizeInternal = new SizeDbl();

			//_viewPortSize = new Size();
			//_viewPortSizeInBlocks = new SizeInt();
			//_canvasControlOffset = new VectorDbl();

			_imageOffsetInternal = new VectorDbl();

			_offset = new Point(0, 0);
			_unscaledExtent = new Size(0, 0);

			_contentRenderTransformOrigin = new Point(0, 0);
			_transformGroup = new TransformGroup();

			_contentOffsetTransform = new TranslateTransform(0, 0);
			_transformGroup.Children.Add(_contentOffsetTransform);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;
		public event EventHandler<ValueTuple<SizeInt, SizeInt>>? ViewPortSizeInBlocksChanged;

		public event EventHandler? ImageOffsetChanged;

		#endregion

		#region Regular Properties -- TODO: Convert to Dependency Properties

		public Canvas Canvas => _canvas;

		public Image Image => _image;

		private SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;
				ViewPortSizeInternal = value;
			}
		}

		private SizeDbl ViewPortSizeInternal
		{
			get => _viewPortSizeInternal;
			set
			{
				if (_viewPortSizeInternal != value)
				{
					var previousValue = _viewPortSizeInternal;
					_viewPortSizeInternal = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");

					InvalidateScrollInfo();

					var newViewPortSize = value;

					if (previousValue.Width < 5 || previousValue.Height < 5)
					{
						// Update the 'real' value immediately
						ViewPortSize = newViewPortSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						var originalViewPortSize = ViewPortSize;
						ImageOffsetInternal = GetAdjustedCanvasControlOffset(originalViewPortSize, ImageOffset, _viewPortSizeInternal);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								ImageOffset = ImageOffsetInternal;
								ViewPortSize = newViewPortSize;
							}
						);
					}
				}
			}
		}

		private VectorDbl ImageOffsetInternal
		{
			get => _imageOffsetInternal;
			set
			{
				if (value != _imageOffsetInternal)
				{
					_imageOffsetInternal = value;
					//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
					SetImageOffset(value);
				}
			}
		}

		//public Size ViewPortSize
		//{
		//	get => _viewPortSize;
		//	private set
		//	{
		//		if (_viewPortSize != value)
		//		{
		//			var previousValue = _viewPortSize;
		//			_viewPortSize = value;

		//			Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing: Old size: {previousValue}, new size: {_viewPortSize}.");

		//			var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, _blockSize, KEEP_DISPLAY_SQUARE);
		//			ViewPortSizeInBlocks = sizeInWholeBlocks;

		//			InvalidateScrollInfo();

		//			ViewPortSizeChanged?.Invoke(this, new (previousValue, _viewPortSize));
		//		}
		//	}
		//}

		//public SizeInt ViewPortSizeInBlocks
		//{
		//	get => _viewPortSizeInBlocks;
		//	private set
		//	{
		//		if (value.Width < 0 || value.Height < 0)
		//		{
		//			return;
		//		}

		//		if (_viewPortSizeInBlocks != value)
		//		{
		//			var previousValue = _viewPortSizeInBlocks;
		//			_viewPortSizeInBlocks = value;

		//			//Debug.WriteLine($"BitmapGridControl: ViewPortInBlocks is changing: Old size: {previousValue}, new size: {_viewPortSizeInBlocks}.");
		//			ViewPortSizeInBlocksChanged?.Invoke(this, new(previousValue, _viewPortSizeInBlocks));
		//		}
		//	}
		//}


		public SizeDbl ViewPortSize
		{
			get => (SizeDbl)GetValue(ViewPortSizeProperty);
			set => SetValue(ViewPortSizeProperty, value);
		}

		public SizeInt ViewPortSizeInBlocks
		{
			get => (SizeInt)GetValue(ViewPortSizeInBlocksProperty);
			set => SetValue(ViewPortSizeInBlocksProperty, value);
		}

		public VectorDbl ImageOffset
		{
			get => (VectorDbl)GetValue(ImageOffsetProperty);
			set => SetValue(ImageOffsetProperty, value);
		}

		public ImageSource ImageSource
		{
			get => (ImageSource)GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		#endregion

		#region Private ContentControl Methods

		protected override Size MeasureOverride(Size constraint)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Size childSize = base.MeasureOverride(infiniteSize);

			//if (_unscaledExtent != childSize)
			//{
			//	// Use the size of the child as the un-scaled extent content.
			//	_unscaledExtent = childSize;

			//	InvalidateScrollInfo();
			//}

			double width = constraint.Width;
			double height = constraint.Height;

			if (double.IsInfinity(width))
			{
				//
				// Make sure we don't return infinity!
				//
				width = childSize.Width;
			}

			if (double.IsInfinity(height))
			{
				//
				// Make sure we don't return infinity!
				//
				height = childSize.Height;
			}

			return new Size(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Size size = base.ArrangeOverride(finalSize);

			if (_unscaledExtent != size)
			{
				// Use the size of the child as the un-scaled extent content.
				_unscaledExtent = size;

				if (_content != null)
				{
					_content.Arrange(new Rect(finalSize));
				}

				InvalidateScrollInfo();
			}

			// Update the size of the viewport using the final size.
			ContainerSize = ScreenTypeHelper.ConvertToSizeDbl(finalSize);

			return size;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = Template.FindName("BitmapGridControl_Content", this) as FrameworkElement;
			if (_content != null)
			{
				Debug.WriteLine($"Found the BitmapGridControl_Content template.");

				(_canvas, _image) = BuildContentModel(_content);

				// Setup the transform on the content so that we can position the Bitmap to "pull" it left and up so that the
				// portion of the bitmap that is visible corresponds with the requested map coordinates.

				_content.RenderTransformOrigin = _contentRenderTransformOrigin;
				_content.RenderTransform = _transformGroup;
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
			if (_scrollOwner != null)
			{
				_scrollOwner.InvalidateScrollInfo();
			}
		}

		#endregion

		#region ImageSource Dependency Property

		public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
					"ImageSource", typeof(ImageSource), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageSource_PropertyChanged));


		private static void ImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;

			if (e.NewValue == null)
			{
				return;
			}

			if (e.NewValue is WriteableBitmap wb)
			{
				c.Image.Source = wb;
				c.InvalidateScrollInfo();
			}
			else
			{
				Debug.WriteLine($"ImageSource is being assigned a value of type {e.NewValue.GetType()} No update made, only values of type WriteableBitmap are supported.");
			}
		}

		#endregion

		#region ViewPortSize Dependency Property

		private static SizeDbl DEFAULT_VIEWPORT_SIZE = new SizeDbl(10, 10);

		public static readonly DependencyProperty ViewPortSizeProperty = DependencyProperty.Register(
					"ViewPortSize", typeof(SizeDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(DEFAULT_VIEWPORT_SIZE, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ViewPortSize_PropertyChanged));

		private static void ViewPortSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var previousValue = (SizeDbl)e.OldValue;
			var value = (SizeDbl)e.NewValue;

			if (double.IsNaN(value.Width) || double.IsNaN(value.Height))
			{
				return;
			} 

			if (value.Width - previousValue.Width > 0.1 || value.Height - previousValue.Height > 0.1)
			{
				Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing. The old size: {previousValue}, new size: {value}.");

				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(c.ContainerSize, c._blockSize, KEEP_DISPLAY_SQUARE);
				c.ViewPortSizeInBlocks = sizeInWholeBlocks;
				
				c.InvalidateScrollInfo();
				c.ViewPortSizeChanged?.Invoke(c, new(previousValue, value));
			}
			else
			{
				Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing by a very small amount, IGNORING. The old size: {previousValue}, new size: {value}.");
			}
		}

		#endregion

		#region ViewPortSizeInBlocks Dependency Property

		public static readonly DependencyProperty ViewPortSizeInBlocksProperty = DependencyProperty.Register(
					"ViewPortSizeInBlocks", typeof(SizeInt), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(SizeInt.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ViewPortSizeInBlocks_PropertyChanged));

		private static void ViewPortSizeInBlocks_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var previousValue = (SizeInt)e.OldValue;
			var value = (SizeInt)e.NewValue;

			if (value.Width < 0 || value.Height < 0)
			{
				return;
			}

			if (previousValue != value)
			{
				Debug.WriteLine($"BitmapGridControl: ViewPortInBlocks is changing: Old size: {previousValue}, new size: {value}.");
				c.ViewPortSizeInBlocksChanged?.Invoke(c, new(previousValue, value));
			}
		}

		#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var previousValue = (VectorDbl)e.OldValue;
			var value = (VectorDbl)e.NewValue;

			if (value != previousValue)
			{
				//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");

				c.SetImageOffset(value);
				c.ImageOffsetChanged?.Invoke(c, EventArgs.Empty);
				c.InvalidateScrollInfo();
			}
		}

		private void SetImageOffset(VectorDbl offset)
		{
			//Debug.Assert(offset.X >= 0 && offset.Y >= 0, "Setting offset to negative value.");

			// For a postive offset, we "pull" the image down and to the left.
			var invertedOffset = offset.Invert();

			_image.SetValue(Canvas.LeftProperty, invertedOffset.X);
			_image.SetValue(Canvas.BottomProperty, invertedOffset.Y);
		}

		private VectorDbl GetAdjustedCanvasControlOffset(SizeDbl originalSize, VectorDbl originalOffset, SizeDbl newSize)
		{
			var diff = newSize.Diff(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		#endregion

		//#region ContentOffsetX Dependency Property

		//public static readonly DependencyProperty ContentOffsetXProperty =
		//		DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(BitmapGridControl),
		//									new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		///// <summary>
		///// Event raised when the 'ContentOffsetX' property has changed value.
		///// </summary>
		//private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//	BitmapGridControl c = (BitmapGridControl)o;

		//	//	c.UpdateTranslationX();

		//	//	if (!c.disableContentFocusSync)
		//	//	{
		//	//		//
		//	//		// Normally want to automatically update content focus when content offset changes.
		//	//		// Although this is disabled using 'disableContentFocusSync' when content offset changes due to in-progress zooming.
		//	//		//
		//	//		c.UpdateContentZoomFocusX();
		//	//	}

		//	//	if (c.ContentOffsetXChanged != null)
		//	//	{
		//	//		//
		//	//		// Raise an event to let users of the control know that the content offset has changed.
		//	//		//
		//	//		c.ContentOffsetXChanged(c, EventArgs.Empty);
		//	//	}

		//	//	if (!c.disableScrollOffsetSync && c.scrollOwner != null)
		//	//	{
		//	//		//
		//	//		// Notify the owning ScrollViewer that the scrollbar offsets should be updated.
		//	//		//
		//	//		c.scrollOwner.InvalidateScrollInfo();
		//	//	}
		//}

		///// <summary>
		///// Method called to clamp the 'ContentOffsetX' value to its valid range.
		///// </summary>
		//private static object ContentOffsetX_Coerce(DependencyObject d, object baseValue)
		//{
		//	//BitmapGridControl c = (BitmapGridControl)d;
		//	//double value = (double)baseValue;
		//	//double minOffsetX = 0.0;
		//	//double maxOffsetX = Math.Max(0.0, c.unScaledExtent.Width - c.constrainedContentViewportWidth);
		//	//value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);
		//	//return value;

		//	return baseValue;
		//}

		///// <summary>
		///// Get/set the X offset (in content coordinates) of the view on the content.
		///// </summary>
		//public double ContentOffsetX
		//{
		//	get => (double)GetValue(ContentOffsetXProperty);
		//	set => SetValue(ContentOffsetXProperty, value);
		//}


		//#endregion

		//#region ContentOffsetY Dependency Property

		//public static readonly DependencyProperty ContentOffsetYProperty =
		//		DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(BitmapGridControl),
		//									new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));


		///// <summary>
		///// Event raised when the 'ContentOffsetY' property has changed value.
		///// </summary>
		//private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//BitmapGridControl c = (BitmapGridControl)o;

		//	//c.UpdateTranslationY();

		//	//if (!c.disableContentFocusSync)
		//	//{
		//	//	//
		//	//	// Normally want to automatically update content focus when content offset changes.
		//	//	// Although this is disabled using 'disableContentFocusSync' when content offset changes due to in-progress zooming.
		//	//	//
		//	//	c.UpdateContentZoomFocusY();
		//	//}

		//	//if (c.ContentOffsetYChanged != null)
		//	//{
		//	//	//
		//	//	// Raise an event to let users of the control know that the content offset has changed.
		//	//	//
		//	//	c.ContentOffsetYChanged(c, EventArgs.Empty);
		//	//}

		//	//if (!c.disableScrollOffsetSync && c.scrollOwner != null)
		//	//{
		//	//	//
		//	//	// Notify the owning ScrollViewer that the scrollbar offsets should be updated.
		//	//	//
		//	//	c.scrollOwner.InvalidateScrollInfo();
		//	//}

		//}

		///// <summary>
		///// Method called to clamp the 'ContentOffsetY' value to its valid range.
		///// </summary>
		//private static object ContentOffsetY_Coerce(DependencyObject d, object baseValue)
		//{
		//	//BitmapGridControl c = (BitmapGridControl)d;
		//	//double value = (double)baseValue;
		//	//double minOffsetY = 0.0;
		//	//double maxOffsetY = Math.Max(0.0, c.unScaledExtent.Height - c.constrainedContentViewportHeight);
		//	//value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
		//	//return value;

		//	return baseValue;
		//}

		///// <summary>
		///// Get/set the Y offset (in content coordinates) of the view on the content.
		///// </summary>
		//public double ContentOffsetY
		//{
		//	get => (double) GetValue(ContentOffsetYProperty);
		//	set => 	SetValue(ContentOffsetYProperty, value);
		//}

		//#endregion

		#region Old SetImageOffset Code

		///// <summary>
		///// The position of the canvas' origin relative to the Image Block Data
		///// </summary>
		//private void SetCanvasOffset(VectorInt value, double displayZoom)
		//{
		//	if (value != _offset || Math.Abs(displayZoom - _offsetZoom) > 0.001)
		//	{
		//		//Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}. The ScreenCollection Index is {_vm.ScreenCollectionIndex}");
		//		Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}.");
		//		Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

		//		_offset = value;
		//		_offsetZoom = displayZoom;

		//		// For a postive offset, we "pull" the image down and to the left.
		//		var invertedOffset = value.Invert();

		//		var roundedZoom = RoundZoomToOne(displayZoom);
		//		var scaledInvertedOffset = invertedOffset.Scale(1 / roundedZoom);

		//		_image.SetValue(Canvas.LeftProperty, (double)scaledInvertedOffset.X);
		//		_image.SetValue(Canvas.BottomProperty, (double)scaledInvertedOffset.Y);

		//		ReportSizes("SetCanvasOffset");
		//	}
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

		#region Unused SetCanvasTransform / ReportSizes / CheckImageSize

		//private void SetCanvasTransform(PointDbl scale)
		//{
		//	_canvas.RenderTransformOrigin = new Point(0.5, 0.5);
		//	_canvas.RenderTransform = new ScaleTransform(scale.X, scale.Y);
		//}

		//private void ReportSizes(string label)
		//{
		//	var iSize = new Size(Image.ActualWidth, Image.ActualHeight);
		//	var bSize = new Size(Bitmap.Width, Bitmap.Height);

		//	Debug.WriteLine($"At {label}, the sizes are CanvasSizeInBlocks: {CanvasSizeInBlocks}, ImageSizeInBlocks: {ImageSizeInBlocks}, Image: {iSize}, Bitmap: {bSize}.");
		//}

		//private bool IsCanvasSizeInWBsReasonable(SizeInt canvasSizeInWholeBlocks)
		//{
		//	var result = !(canvasSizeInWholeBlocks.Width > 50 || canvasSizeInWholeBlocks.Height > 50);
		//	return result;
		//}

		#endregion
	}
}
