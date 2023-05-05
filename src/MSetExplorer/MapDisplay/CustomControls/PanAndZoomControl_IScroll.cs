using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <remarks>
	/// 
	/// This code is based on the CodeProject: A WPF Custom Control for Zooming and Panning
	/// https://www.codeproject.com/Articles/85603/A-WPF-custom-control-for-zooming-and-panning
	/// Written by Ashley Davis
	/// 
	/// </remarks>

	public partial class PanAndZoomControl : ContentControl, IScrollInfo, IContentScaleInfo
	{
		static PanAndZoomControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PanAndZoomControl), new FrameworkPropertyMetadata(typeof(PanAndZoomControl)));
		}

		#region Public Properties

		public ScrollViewer ScrollOwner
		{
			get => _scrollOwner ?? (_scrollOwner = new ScrollViewer());
			set
			{
				_scrollOwner = value;
				_originalVerticalScrollBarVisibility = _scrollOwner.VerticalScrollBarVisibility;
				ScrollBarDisplacement = GetScrollBarDisplacement();
			}
		}

		public bool CanHorizontallyScroll
		{
			get => _canHScroll;
			set
			{
				_canHScroll = value;
				//ScrollBarDisplacement = new SizeDbl(value ? VERTICAL_SCROLL_BAR_WIDTH : 0, ScrollBarDisplacement.Height);
			}
		}

		public bool CanVerticallyScroll
		{
			get => _canVScroll;
			set
			{
				_canVScroll = value;
				//ScrollBarDisplacement = new SizeDbl(ScrollBarDisplacement.Width, value ? VERTICAL_SCROLL_BAR_WIDTH : 0);
			}
		}

		public SizeDbl ScrollBarDisplacement
		{
			get => _scrollBarDisplacement;
			set => _scrollBarDisplacement = value;
		}

		public double ExtentWidth
		{
			get
			{
				var x = Math.Max(UnscaledExtent.Width, ViewportWidth) * ContentScale;
				return x;
			}
		}

		public double ExtentHeight
		{
			get
			{
				var y = Math.Max(UnscaledExtent.Height, ViewportHeight) * ContentScale;
				return y;
			}
		}

		public double ViewportWidth
		{
			get
			{
				var w = ViewPortSize.Width;
				return w;
			}
		}

		public double ViewportHeight
		{
			get
			{
				var h = ViewPortSize.Height;
				return h;
			}
		}

		public double HorizontalOffset
		{
			get
			{
				var hOffset = ContentOffsetX * ContentScale;
				return hOffset;
			}
		}

		public double VerticalOffset
		{
			get
			{
				var vOffset = ContentOffsetY * ContentScale;
				return vOffset;
			}
		}

		#endregion

		#region Line / Page / MouseWheel 

		public void LineDown() => ContentOffsetY += ContentViewportSize.Height / 10;

		public void LineUp() => ContentOffsetY -= ContentViewportSize.Height / 10;

		public void LineLeft() => ContentOffsetX -= ContentViewportSize.Width / 10;

		public void LineRight() => ContentOffsetX += ContentViewportSize.Width / 10;

		public void PageUp() => ContentOffsetY -= ContentViewportSize.Height;

		public void PageDown() => ContentOffsetY += ContentViewportSize.Height;

		public void PageLeft() => ContentOffsetX -= ContentViewportSize.Width;

		public void PageRight() => ContentOffsetX += ContentViewportSize.Width;

		public void MouseWheelDown() { if (IsMouseWheelScrollingEnabled) LineDown(); }

		public void MouseWheelLeft() { if (IsMouseWheelScrollingEnabled) LineLeft(); }

		public void MouseWheelRight() { if (IsMouseWheelScrollingEnabled) LineRight(); }

		public void MouseWheelUp() { if (IsMouseWheelScrollingEnabled) LineUp(); }

		#endregion

		#region SetHorizontalOffset, SetVerticalOffset and MakeVisible 

		public void SetHorizontalOffset(double offset)
		{
			if (_disableScrollOffsetSync)
			{
				return;
			}

			try
			{
				_disableScrollOffsetSync = true;

				ContentOffsetX = offset / ContentScale;
			}
			finally
			{
				_disableScrollOffsetSync = false;
			}
		}

		public void SetVerticalOffset(double offset)
		{
			if (_disableScrollOffsetSync)
			{
				return;
			}

			try
			{
				_disableScrollOffsetSync = true;

				ContentOffsetY = offset / ContentScale;
			}
			finally
			{
				_disableScrollOffsetSync = false;
			}
		}


		/// <summary>
		/// Bring the specified rectangle to view.
		/// </summary>
		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			if (ContentBeingZoomed == null)
			{
				Debug.WriteLine("MakeVisible is being called, however ContentBeingZoomed = null. Returning.");
				return rectangle;
			}

			if (ContentBeingZoomed.IsAncestorOf(visual))
			{
				Rect transformedRect = visual.TransformToAncestor(ContentBeingZoomed).TransformBounds(rectangle);
				Rect viewportRect = new Rect(new Point(ContentOffsetX, ContentOffsetY), ScreenTypeHelper.ConvertToSize(ContentViewportSize));

				if (!transformedRect.Contains(viewportRect))
				{
					double horizOffset = 0;
					double vertOffset = 0;

					if (transformedRect.Left < viewportRect.Left)
					{
						//
						// Want to move viewport left.
						//
						horizOffset = transformedRect.Left - viewportRect.Left;
					}
					else if (transformedRect.Right > viewportRect.Right)
					{
						//
						// Want to move viewport right.
						//
						horizOffset = transformedRect.Right - viewportRect.Right;
					}

					if (transformedRect.Top < viewportRect.Top)
					{
						//
						// Want to move viewport up.
						//
						vertOffset = transformedRect.Top - viewportRect.Top;
					}
					else if (transformedRect.Bottom > viewportRect.Bottom)
					{
						//
						// Want to move viewport down.
						//
						vertOffset = transformedRect.Bottom - viewportRect.Bottom;
					}

					SnapContentOffsetTo(new Point(ContentOffsetX + horizOffset, ContentOffsetY + vertOffset));
				}
			}
			return rectangle;
		}

		/// <summary>
		/// Instantly center the view on the specified point (in content coordinates).
		/// </summary>
		public void SnapContentOffsetTo(Point contentOffset)
		{
			//AnimationHelper.CancelAnimation(this, ContentOffsetXProperty);
			//AnimationHelper.CancelAnimation(this, ContentOffsetYProperty);

			ContentOffsetX = contentOffset.X;
			ContentOffsetY = contentOffset.Y;
		}

		private void SetVerticalScrollBarVisibility(bool show)
		{
			//if (_scrollOwner != null && _scrollOwner.VerticalScrollBarVisibility != value)
			//{
			//	_scrollOwner.VerticalScrollBarVisibility = value;
			//}

			if (_scrollOwner != null)
			{
				if (show && _scrollOwner.VerticalScrollBarVisibility != ScrollBarVisibility.Visible)
				{
					_scrollOwner.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
				}
				else
				{
					if(_scrollOwner.VerticalScrollBarVisibility == ScrollBarVisibility.Visible && !show)
					{
						_scrollOwner.VerticalScrollBarVisibility = _originalVerticalScrollBarVisibility;
					}
				}
			}
		}

		#endregion

		#region IContentScaleInfo Support

		public ZoomSlider? ZoomSliderOwner
		{
			get => _zoomSlider;
			set => _zoomSlider = value;
		}

		public bool CanZoom
		{
			get => _canZoom;
			set => _canZoom = value;
		}

		public double Scale => ContentScale;

		public double MinScale => MinContentScale;

		public double MaxScale => MaxContentScale;

		public void SetScale(double contentScale) => SetValue(ContentScaleProperty, contentScale);

		#endregion

		/* Sample MeasureOverrride and ArrangeOverride implementations.

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size constraint)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Size childSize = base.MeasureOverride(infiniteSize);

			if (childSize != unScaledExtent)
			{
				//
				// Use the size of the child as the un-scaled extent content.
				//
				unScaledExtent = childSize;

				if (scrollOwner != null)
				{
					scrollOwner.InvalidateScrollInfo();
				}
			}

			//
			// Update the size of the viewport onto the content based on the passed in 'constraint'.
			//
			UpdateViewportSize(constraint);

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

			UpdateTranslationX();
			UpdateTranslationY();

			return new Size(width, height);
		}

        /// <summary>
        /// Arrange the control and it's children.
        /// </summary>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Size size = base.ArrangeOverride(this.DesiredSize);

            if (content.DesiredSize != unScaledExtent)
            {
                //
                // Use the size of the child as the un-scaled extent content.
                //
                unScaledExtent = content.DesiredSize;

                if (scrollOwner != null)
                {
                    scrollOwner.InvalidateScrollInfo();
                }
            }

            //
            // Update the size of the viewport onto the content based on the passed in 'arrangeBounds'.
            //
            UpdateViewportSize(arrangeBounds);

            return size;
        }

		PREVIOUS LOGIC -- NOW Replaced by UpdateViewportSize

			//if (ContainerSize != finalSizeDbl)
			//{
			//	if (_content != null)
			//	{
			//		_content.Arrange(new Rect(finalSize));
			//	}

			//	ContainerSize = finalSizeDbl;
			//}
		*/

		/* Sample MeasureOverride - Mark2

		//protected override Size MeasureOverride(Size availableSize)
		//{
		//	//var childSize = new SizeDbl(
		//	//	availableSize.Width,
		//	//	availableSize.Height / 2);

		//	//var extent = new SizeDbl(
		//	//	availableSize.Width,
		//	//	childSize.Height);

		//	//if (extent != _unScaledExtent)
		//	//{
		//	//	_unScaledExtent = extent;

		//	//	InvalidateScrollInfo();
		//	//}

		//	//_unScaledExtent = new SizeDbl(4096, 4096);

		//	var ourSize = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

		//	if (_viewPortSize != ourSize)
		//	{
		//		_viewPortSize = ourSize;

		//		InvalidateScrollInfo();
		//	}

		//	// If we had visual children, here is where we would call Measure for each.

		//	return availableSize;
		//}


		//protected override Size ArrangeOverride(Size finalSize)
		//{
		//	//Size childSize = new Size(
		//	//  finalSize.Width,
		//	//  (finalSize.Height * 2) / this.InternalChildren.Count);
		//	//Size extent = new Size(
		//	//  finalSize.Width,
		//	//  childSize.Height * this.InternalChildren.Count);

		//	//if (extent != _extent)
		//	//{
		//	//	_extent = extent;
		//	//	if (_owner != null)
		//	//		_owner.InvalidateScrollInfo();
		//	//}

		//	//if (finalSize != _viewport)
		//	//{
		//	//	_viewport = finalSize;
		//	//	if (_owner != null)
		//	//		_owner.InvalidateScrollInfo();
		//	//}

		//	var ourSize = ScreenTypeHelper.ConvertToSizeDbl(finalSize);

		//	if (_viewPortSize != ourSize)
		//	{
		//		_viewPortSize = ourSize;

		//		InvalidateScrollInfo();
		//	}

		//	//for (int i = 0; i < this.InternalChildren.Count; i++)
		//	//{
		//	//	this.InternalChildren[i].Arrange(new Rect(0, childSize.Height * i, childSize.Width, childSize.Height));
		//	//}

		//	return finalSize;
		//}


		*/
	}
}
