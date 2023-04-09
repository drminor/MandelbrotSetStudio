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

		private Point _offset;
		private Size _unscaledExtent;
		private Size _viewPort;

		private Point _contentRenderTransformOrigin;
		private TranslateTransform _contentOffsetTransform;
		private TransformGroup _transformGroup;

		private SizeDbl _containerSize;

		private SizeInt _viewPortInBlocks;

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

			_offset = new Point(0, 0);
			_unscaledExtent = new Size(0, 0);
			_viewPort = new Size(0, 0);

			_contentRenderTransformOrigin = new Point(0, 0);
			_transformGroup = new TransformGroup();

			_contentOffsetTransform = new TranslateTransform(0, 0);
			_transformGroup.Children.Add(_contentOffsetTransform);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<Size, Size>>? ViewPortSizeChanged;

		public event EventHandler<ValueTuple<SizeInt, SizeInt>>? ViewPortSizeInBlocksChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas => _canvas;

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			private set
			{
				_containerSize = value;
				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(ContainerSize, _blockSize, KEEP_DISPLAY_SQUARE);
				ViewPortInBlocks = sizeInWholeBlocks;

				ViewPort = ScreenTypeHelper.ConvertToSize(ContainerSize);
			}
		}

		public SizeInt ViewPortInBlocks
		{
			get => _viewPortInBlocks;
			private set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_viewPortInBlocks != value)
				{
					var previousValue = _viewPortInBlocks;
					_viewPortInBlocks = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPortInBlocks is changing: Old size: {previousValue}, new size: {_viewPortInBlocks}.");

					//ViewPortSizeInBlocksChanged?.Invoke(this, new (previousValue, _viewPortInBlocks));

					if (ViewPortSizeInBlocksChanged != null)
					{
						RaiseViewPortSizeInBlocksChanged(new(previousValue, _viewPortInBlocks));
					}
				}
			}
		}

		public Size ViewPort
		{
			get => _viewPort;
			private set
			{
				if (_viewPort != value)
				{
					var previousValue = _viewPort;
					_viewPort = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");
					
					InvalidateScrollInfo();

					if (ViewPortSizeChanged != null)
					{
						RaiseViewPortSizeChanged(new(previousValue, _viewPort));
					}
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

				_canvas = BuildContentModel(_content);

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

		private Canvas BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					return ca;
					//if (ca.Children[0] is Image im)
					//{
					//	return (ca, im);
					//}
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
	}
}
