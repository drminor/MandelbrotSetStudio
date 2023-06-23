﻿using MSS.Types;
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
				_originalHorizontalScrollBarVisibility = _scrollOwner.HorizontalScrollBarVisibility;
			}
		}

		public bool CanHorizontallyScroll
		{
			get => _canHScroll;
			set
			{
				_canHScroll = value;
			}
		}

		public bool CanVerticallyScroll
		{
			get => _canVScroll;
			set
			{
				_canVScroll = value;
			}
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
				var w = ViewportSize.Width;
				return w;
			}
		}

		public double ViewportHeight
		{
			get
			{
				var h = ViewportSize.Height;
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
			if (_scrollOwner != null && _scrollOwner.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
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

		private void SetHorizontalScrollBarVisibility(bool show)
		{
			if (_scrollOwner != null && _scrollOwner.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
			{
				if (show && _scrollOwner.HorizontalScrollBarVisibility != ScrollBarVisibility.Visible)
				{
					_scrollOwner.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
				}
				else
				{
					if (_scrollOwner.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible && !show)
					{
						_scrollOwner.HorizontalScrollBarVisibility = _originalHorizontalScrollBarVisibility;
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

		#region IContentScaler Support

		private IContentScaler? _contentScaler;

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

	}
}
