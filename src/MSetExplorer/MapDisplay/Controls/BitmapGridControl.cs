using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
		private Size _viewPortSize;
		private SizeInt _viewPortSizeInBlocks;
		private VectorInt _canvasControlOffset;

		private Point _offset;
		private Size _unscaledExtent;

		private Point _contentRenderTransformOrigin;
		private TranslateTransform _contentOffsetTransform;
		private TransformGroup _transformGroup;

		private DebounceDispatcher _vpSizeThrottle;
		private DebounceDispatcher _vpSizeInBlocksThrottle;

		#endregion

		#region Constructor

		public BitmapGridControl()
		{
			_vpSizeThrottle = new DebounceDispatcher();
			_vpSizeInBlocksThrottle = new DebounceDispatcher();

			_blockSize = RMapConstants.BLOCK_SIZE;
			_scrollOwner = null;

			_content = null; 
			_canvas = new Canvas();
			_image = new Image();
			_containerSize = new SizeDbl();
			_viewPortSize = new Size(0, 0);
			_viewPortSizeInBlocks = new SizeInt();
			_canvasControlOffset = new VectorInt();

			_offset = new Point(0, 0);
			_unscaledExtent = new Size(0, 0);

			_contentRenderTransformOrigin = new Point(0, 0);
			_transformGroup = new TransformGroup();

			_contentOffsetTransform = new TranslateTransform(0, 0);
			_transformGroup.Children.Add(_contentOffsetTransform);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<Size, Size>>? ViewPortSizeChanged;
		public event EventHandler<ValueTuple<SizeInt, SizeInt>>? ViewPortSizeInBlocksChanged;

		//public event EventHandler ContentOffsetXChanged;
		//public event EventHandler ContentOffsetYChanged;

		#endregion

		#region Regular Properties -- TODO: Convert to Dependency Properties

		public Canvas Canvas => _canvas;

		public Image Image => _image;

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			private set
			{
				_containerSize = value;
				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, _blockSize, KEEP_DISPLAY_SQUARE);
				ViewPortSizeInBlocks = sizeInWholeBlocks;

				ViewPortSize = ScreenTypeHelper.ConvertToSize(ContainerSize);
			}
		}

		public SizeInt ViewPortSizeInBlocks
		{
			get => _viewPortSizeInBlocks;
			private set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_viewPortSizeInBlocks != value)
				{
					var previousValue = _viewPortSizeInBlocks;
					_viewPortSizeInBlocks = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPortInBlocks is changing: Old size: {previousValue}, new size: {_viewPortInBlocks}.");

					if (ViewPortSizeInBlocksChanged != null)
					{
						RaiseViewPortSizeInBlocksChanged(new(previousValue, _viewPortSizeInBlocks));
					}
				}
			}
		}

		public Size ViewPortSize
		{
			get => _viewPortSize;
			private set
			{
				if (_viewPortSize != value)
				{
					var previousValue = _viewPortSize;
					_viewPortSize = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");
					
					InvalidateScrollInfo();

					if (ViewPortSizeChanged != null)
					{
						RaiseViewPortSizeChanged(new(previousValue, _viewPortSize));
					}
				}
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				if (value != _canvasControlOffset)
				{
					Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_canvasControlOffset = value;

					//OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasControlOffset));

					SetCanvasOffset(value);
				}
			}
		}

		//public Size UnscaledExtent
		//{
		//	get => _unscaledExtent;
		//	set
		//	{
		//		_unscaledExtent = value;
		//	}
		//}

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

		private void RaiseViewPortSizeChanged(ValueTuple<Size, Size> e)
		{
			_vpSizeThrottle.Throttle(
				interval: 150,
				action: parm =>
				{
					ViewPortSizeChanged?.Invoke(this, e);
				}
			);
		}

		private void RaiseViewPortSizeInBlocksChanged(ValueTuple<SizeInt, SizeInt> e)
		{
			_vpSizeInBlocksThrottle.Throttle(
				interval: 150,
				action: parm =>
				{
					ViewPortSizeInBlocksChanged?.Invoke(this, e);
				}
			);
		}

		#endregion

		#region Private Method -- Regular Property Support

		public void SetCanvasOffset(VectorInt offset)
		{
			Debug.Assert(offset.X >= 0 && offset.Y >= 0, "Setting offset to negative value.");

			// For a postive offset, we "pull" the image down and to the left.
			var invertedOffset = offset.Invert();

			_image.SetValue(Canvas.LeftProperty, (double)invertedOffset.X);
			_image.SetValue(Canvas.BottomProperty, (double)invertedOffset.Y);
		}

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

		//#region Dependency Properties

		///// <summary>
		///// Get/set the X offset (in content coordinates) of the view on the content.
		///// </summary>
		//public double ContentOffsetX
		//{
		//	get => (double)GetValue(ContentOffsetXProperty);
		//	set => SetValue(ContentOffsetXProperty, value);
		//}


		///// <summary>
		///// Get/set the Y offset (in content coordinates) of the view on the content.
		///// </summary>
		//public double ContentOffsetY
		//{
		//	get => (double) GetValue(ContentOffsetYProperty);
		//	set => 	SetValue(ContentOffsetYProperty, value);
		//}



		//public static readonly DependencyProperty ContentOffsetXProperty =
		//		DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		//public static readonly DependencyProperty ContentOffsetYProperty =
		//		DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));


		///// <summary>
		///// Event raised when the 'ContentOffsetX' property has changed value.
		///// </summary>
		//private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//	TestContentControl c = (TestContentControl)o;

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
		//	//TestContentControl c = (TestContentControl)d;
		//	//double value = (double)baseValue;
		//	//double minOffsetX = 0.0;
		//	//double maxOffsetX = Math.Max(0.0, c.unScaledExtent.Width - c.constrainedContentViewportWidth);
		//	//value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);
		//	//return value;

		//	return baseValue;
		//}

		///// <summary>
		///// Event raised when the 'ContentOffsetY' property has changed value.
		///// </summary>
		//private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//TestContentControl c = (TestContentControl)o;

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
		//	//TestContentControl c = (TestContentControl)d;
		//	//double value = (double)baseValue;
		//	//double minOffsetY = 0.0;
		//	//double maxOffsetY = Math.Max(0.0, c.unScaledExtent.Height - c.constrainedContentViewportHeight);
		//	//value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
		//	//return value;

		//	return baseValue;
		//}


		//#endregion
	}
}
