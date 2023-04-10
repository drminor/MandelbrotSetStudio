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

		private bool _canHScroll;
		private bool _canVScroll;

		#endregion

		static BitmapGridControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl),
			new FrameworkPropertyMetadata(typeof(BitmapGridControl)));
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

		public double HorizontalOffset => _offset.X;
		public double VerticalOffset => _offset.Y;

		public double ExtentWidth => _unscaledExtent.Width;
		public double ExtentHeight => _unscaledExtent.Height;

		public double ViewportWidth => _viewPortSize.Width;
		public double ViewportHeight => _viewPortSize.Height;

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
			double childHeight = _viewPortSize.Height;
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
			double childHeight = _viewPortSize.Height;
			SetVerticalOffset(VerticalOffset - childHeight);
		}

		public void SetHorizontalOffset(double offset)
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(SetHorizontalOffset)}.");
		}

		public void SetVerticalOffset(double offset)
		{
			if (offset < 0 || _viewPortSize.Height >= _unscaledExtent.Height)
			{
				offset = 0;
			}
			else
			{
				if (offset + _viewPortSize.Height >= _unscaledExtent.Height)
				{
					offset = _unscaledExtent.Height - _viewPortSize.Height;
				}
			}

			_offset.Y = offset;

			InvalidateScrollInfo();

			_contentOffsetTransform.Y = -offset;
			InvalidateMeasure();
		}

		public void SetVerticalOffset2(double offset)
		{
			_offset.Y = Math.Max(0, Math.Min(_unscaledExtent.Height - _viewPortSize.Height, Math.Max(0, offset)));

			InvalidateScrollInfo();

			_contentOffsetTransform.Y = -_offset.Y;
			InvalidateMeasure();
		}

		#endregion
	}
}
