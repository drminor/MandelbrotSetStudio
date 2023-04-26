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

	public partial class BitmapGridControl : ContentControl, IScrollInfo
	{
		#region Scroll Info Properties

		private Point _offset = new Point();
		private SizeInt _unscaledExtent = new SizeInt();

		private bool _canHScroll = false;
		private bool _canVScroll = false;

		#endregion

		static BitmapGridControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl),
			new FrameworkPropertyMetadata(typeof(BitmapGridControl)));
		}

		#region Public Properties

		public SizeInt PosterSize
		{
			get => _unscaledExtent;
			set
			{
				_unscaledExtent = value;
				SetHorizontalOffset(0);
				SetVerticalOffset(0);
				InvalidateScrollInfo();
			}
		}

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

		public double HorizontalOffset => _offset.X;
		public double VerticalOffset => _offset.Y;

		public double ExtentWidth => Math.Max(_unscaledExtent.Width, _containerSize.Width);
		public double ExtentHeight => Math.Max(_unscaledExtent.Height, _containerSize.Height);

		public double ViewportWidth
		{
			get
			{	
				Debug.WriteLine($"Vpw: {ViewPortSize.Width}.");
				return ViewPortSize.Width;
			}
		}

		public double ViewportHeight
		{
			get
			{
				Debug.WriteLine($"Vph: {ViewPortSize.Height}.");
				return ViewPortSize.Height;
			}
		}

		#endregion

		#region Public Methods

		public void LineDown()
		{
			SetVerticalOffset(VerticalOffset + 1);
		}

		public void LineLeft()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(LineLeft)}.");
		}

		public void LineRight()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(LineRight)}.");
		}

		public void LineUp()
		{
			SetVerticalOffset(VerticalOffset - 1);
		}

		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			//for (int i = 0; i < InternalChildren.Count; i++)
			//{
			//	if (InternalChildren[i] == visual)
			//	{
			//		// We found the visual! Let's scroll it into view.
			//		// First we need to know how big each child is.
			//		Size finalSize = RenderSize;

			//		Size childSize = new Size(
			//			finalSize.Width,
			//			finalSize.Height * 2 / InternalChildren.Count
			//			);

			//		// now we can calculate the vertical offset that we need and set it
			//		SetVerticalOffset(childSize.Height * i);

			//		// child size is always smaller than viewport, because that is what makes the Panel
			//		// an AnnoyingPanel.
			//		return rectangle;
			//	}
			//}

			//throw new ArgumentException("Given visual is not in this Panel");

			return rectangle;
		}

		public void MouseWheelDown()
		{
			SetVerticalOffset(VerticalOffset + 10);
		}

		public void MouseWheelLeft()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(MouseWheelLeft)}.");
		}

		public void MouseWheelRight()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(MouseWheelRight)}.");
		}

		public void MouseWheelUp()
		{
			SetVerticalOffset(this.VerticalOffset - 10);
		}

		public void PageDown()
		{
			double childHeight = ViewPortSize.Height;
			SetVerticalOffset(VerticalOffset + childHeight);
		}

		public void PageLeft()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(PageLeft)}.");
		}

		public void PageRight()
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(PageRight)}.");
		}

		public void PageUp()
		{
			double childHeight = ViewPortSize.Height;
			SetVerticalOffset(VerticalOffset - childHeight);
		}

		public void SetHorizontalOffset(double offset)
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(SetHorizontalOffset)}.");
		}

		public void SetVerticalOffset(double offset)
		{
			if (offset < 0 || ViewPortSize.Height >= _containerSize.Height)
			{
				offset = 0;
			}
			else
			{
				if (offset + ViewPortSize.Height >= _containerSize.Height)
				{
					offset = _containerSize.Height - ViewPortSize.Height;
				}
			}

			_offset.Y = offset;

			InvalidateScrollInfo();

			//_contentOffsetTransform.Y = -offset;
			InvalidateMeasure();
		}

		public void SetVerticalOffset2(double offset)
		{
			_offset.Y = Math.Max(0, Math.Min(_containerSize.Height - ViewPortSize.Height, Math.Max(0, offset)));

			InvalidateScrollInfo();

			//_contentOffsetTransform.Y = -_offset.Y;
			InvalidateMeasure();
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

		*/
	}
}
