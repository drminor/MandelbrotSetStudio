using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
		private WriteableBitmap _bitmap;


		private Point _offset;
		private Size _unscaledExtent;
		private Size _viewport;

		private Point _contentRenderTransformOrigin;
		private TranslateTransform _contentOffsetTransform;
		private TransformGroup _transformGroup;

		private SizeDbl _containerSize;

		private SizeInt _viewPortInBlocks;
		private SizeInt _allocatedBlocks;
		private int _maxYPtr;

		//private VectorInt _canvasControlOffset;

		#endregion

		#region Constructor

		public BitmapGridControl()
		{
			_blockSize = RMapConstants.BLOCK_SIZE;
			_scrollOwner = null;

			_content = null; 
			_canvas = new Canvas();
			_image = new Image();
			_bitmap = CreateBitmap(new SizeInt(10));

			_offset = new Point(0, 0);
			_unscaledExtent = new Size(0, 0);
			_viewport = new Size(0, 0);

			_contentRenderTransformOrigin = new Point(0, 0);
			_transformGroup = new TransformGroup();

			_contentOffsetTransform = new TranslateTransform(0, 0);
			_transformGroup.Children.Add(_contentOffsetTransform);

			HandleContainerSizeUpdates = true;
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<Size, Size>>? ViewPortSizeChanged;
		public event EventHandler<ValueTuple<SizeInt, SizeInt>>? ViewPortSizeInBlocksChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas => _canvas;

		public Size UnscaledExtent
		{
			get => _unscaledExtent;
			set
			{
				_unscaledExtent = value;
			}
		}

		public Size ViewPort
		{
			get => _viewport;
			set
			{
				if (_viewport != value)
				{
					var previousValue = ViewPort;
					_viewport = value;

					Debug.WriteLine($"The BitmapGridControl ViewPort is now {value}.");
					InvalidateScrollInfo();
					ViewPortSizeChanged?.Invoke(this, new (previousValue, ViewPort));
				}
			}
		}

		public bool HandleContainerSizeUpdates { get; private set; }

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				if (HandleContainerSizeUpdates)
				{
					_containerSize = value;

					var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, _blockSize, KEEP_DISPLAY_SQUARE);

					ViewPortInBlocks = sizeInWholeBlocks;
				}
				else
				{
					//Debug.WriteLine($"Not handling the ContainerSize update. The value is {value}.");
				}
			}
		}

		public SizeInt ViewPortInBlocks
		{
			get => _viewPortInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_viewPortInBlocks != value)
				{
					// Calculate new size of bitmap in block-sized units
					var newAllocatedBlocks = value.Inflate(2);
					Debug.WriteLine($"Resizing the BitmapGridControl Writeable Bitmap. Old size: {_allocatedBlocks}, new size: {newAllocatedBlocks}.");

					ViewPortSizeInBlocksChanged?.Invoke(this, new (_allocatedBlocks, newAllocatedBlocks));

					_allocatedBlocks = newAllocatedBlocks;
					_maxYPtr = _allocatedBlocks.Height - 1;
					_viewPortInBlocks = value;

					// Create a new Writeable bitmap instance
					var newSize = newAllocatedBlocks.Scale(_blockSize);
					_bitmap = CreateBitmap(newSize);
					_image.Source = _bitmap;

					var newViewPortSize = ViewPortInBlocks.Scale(_blockSize);
					ViewPort = ScreenTypeHelper.ConvertToSize(newViewPortSize);
				}
			}
		}

		//public VectorInt CanvasControlOffset
		//{
		//	get => _canvasControlOffset;
		//	set
		//	{
		//		if (value != _canvasControlOffset)
		//		{
		//			_canvasControlOffset = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		#endregion

		#region Public Methods



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
				_image.Source = _bitmap;

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

		#region Bitmap Methods

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		#endregion
	}
}
