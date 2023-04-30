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

	public partial class BitmapGridControl2 : ContentControl, IScrollInfo
	{
		#region Scroll Info Fields

		private const double VERTICAL_SCROLL_BAR_WIDTH = 17;
		private const double HORIZONTAL_SCROLL_BAR_WIDTH = 17;
		private double _contentScale = 1.0d;

		private bool _canHScroll = true;
		private bool _canVScroll = true;

		#endregion

		static BitmapGridControl2()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl2), new FrameworkPropertyMetadata(typeof(BitmapGridControl2)));
		}

		#region Public Properties

		public ScrollViewer ScrollOwner
		{
			get => _scrollOwner ?? new ScrollViewer();
			set => _scrollOwner = value;
		}

		public bool CanHorizontallyScroll
		{
			get => _canHScroll;
			set => _canHScroll = value;
		}

		public bool CanVerticallyScroll
		{
			get => _canVScroll;
			set => _canVScroll = value;
		}

		public double ExtentWidth
		{
			get
			{
				//var nrmExtentWidth = UnscaledExtent.IsEmpty ? ViewportWidth : Math.Min(UnscaledExtent.Width, ViewportWidth);
				var nrmExtentWidth = UnscaledExtent.IsEmpty ? ViewportWidth : UnscaledExtent.Width;

				//if (nrmExtentWidth < 10)
				//{
				//	Debug.WriteLine("WARNING: ExtentWidth < 10.");
				//}

				return nrmExtentWidth * _contentScale;
			}
		}

		public double ExtentHeight
		{
			get
			{
				//var nrmExtentHeight = UnscaledExtent.IsEmpty ? ViewportHeight : Math.Min(UnscaledExtent.Height, ViewportHeight);
				var nrmExtentHeight = UnscaledExtent.IsEmpty ? ViewportHeight : UnscaledExtent.Height;

				//if (nrmExtentHeight < 10)
				//{
				//	Debug.WriteLine("WARNING: ExtentHeight < 10.");
				//}

				return nrmExtentHeight * _contentScale;
			}
		}

		public double ViewportWidth
		{
			get
			{	
				//Debug.WriteLine($"Vpw: {ViewPortSize.Width}. Ew: {ExtentWidth}.");
				return _viewPortSize.Width;
			}
		}

		public double ViewportHeight
		{
			get
			{
				//Debug.WriteLine($"Vph: {ViewPortSize.Height}. Eh: {ExtentHeight}");
				return _viewPortSize.Height;
			}
		}

		public double HorizontalOffset => ContentOffsetX * _contentScale;
		public double VerticalOffset => ContentOffsetY * _contentScale;

		/// <summary>
		/// Called when the offset of the horizontal scrollbar has been set.
		/// </summary>
		public void SetHorizontalOffset(double offset)
		{
			if (_disableScrollOffsetSync)
			{
				return;
			}

			try
			{
				_disableScrollOffsetSync = true;

				ContentOffsetX = offset / _contentScale;
			}
			finally
			{
				_disableScrollOffsetSync = false;
			}
		}

		/// <summary>
		/// Called when the offset of the vertical scrollbar has been set.
		/// </summary>
		public void SetVerticalOffset(double offset)
		{
			if (_disableScrollOffsetSync)
			{
				return;
			}

			try
			{
				_disableScrollOffsetSync = true;

				ContentOffsetY = offset / _contentScale;
			}
			finally
			{
				_disableScrollOffsetSync = false;
			}
		}

		#endregion

		#region Line / Page / MouseWheel 

		public void LineDown() => ContentOffsetY += ContentViewportHeight / 10;

		public void LineUp() => ContentOffsetY -= ContentViewportHeight / 10;

		public void LineLeft() => ContentOffsetX -= ContentViewportWidth / 10;

		public void LineRight() => ContentOffsetX += ContentViewportWidth / 10;

		public void PageUp() => ContentOffsetY -= ContentViewportHeight;

		public void PageDown() => ContentOffsetY += ContentViewportHeight;

		public void PageLeft() => ContentOffsetX -= ContentViewportWidth;

		public void PageRight() => ContentOffsetX += ContentViewportWidth;

		public void MouseWheelDown() { if (IsMouseWheelScrollingEnabled) LineDown(); }

		public void MouseWheelLeft() { if (IsMouseWheelScrollingEnabled) LineLeft(); }

		public void MouseWheelRight() { if (IsMouseWheelScrollingEnabled) LineRight(); }

		public void MouseWheelUp() { if (IsMouseWheelScrollingEnabled) LineUp(); }

		#endregion

		#region MakeVisible 

		/// <summary>
		/// Bring the specified rectangle to view.
		/// </summary>
		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			if (_content == null)
			{
				Debug.WriteLine("MakeVisible is being called, however _content = null. Returning.");
				return rectangle;
			}

			if (_content.IsAncestorOf(visual))
			{
				Rect transformedRect = visual.TransformToAncestor(_content).TransformBounds(rectangle);
				Rect viewportRect = new Rect(ContentOffsetX, ContentOffsetY, ContentViewportWidth, ContentViewportHeight);
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
