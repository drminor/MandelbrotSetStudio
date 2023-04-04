using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MSetExplorer
{
	public class TestPanel : Panel, IScrollInfo
	{
		#region Private Properties

		private ScrollViewer? _owner;

		private bool _canHScroll;
		private bool _canVScroll;

		private Point _offset;

		private Size _extent;
		private Size _viewport;

		private TranslateTransform _trans = new TranslateTransform();

		#endregion

		#region Constructor

		public TestPanel()
		{
			_offset = new Point(0, 40);

			_extent = new Size(0, 0);
			_viewport = new Size(0, 0);

			RenderTransform = _trans;
		}

		#endregion

		#region Public Properties

		public ScrollViewer ScrollOwner
		{
			get => _owner ?? new ScrollViewer();
			set => _owner = value;
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

		public double ExtentWidth => _extent.Width;
		public double ExtentHeight => _extent.Height;

		public double ViewportWidth => _viewport.Width;
		public double ViewportHeight => _viewport.Height;

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
			double childHeight = (_viewport.Height * 2) / InternalChildren.Count;
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
			double childHeight = (_viewport.Height * 2) / InternalChildren.Count;
			SetVerticalOffset(VerticalOffset - childHeight);
		}

		public void SetHorizontalOffset(double offset)
		{
			//throw new NotImplementedException();
			Debug.WriteLine($"WARNING: Not Handling {nameof(SetHorizontalOffset)}.");
		}

		public void SetVerticalOffset(double offset)
		{
			if (offset < 0 || _viewport.Height >= _extent.Height)
			{
				offset = 0;
			}
			else
			{
				if (offset + _viewport.Height >= _extent.Height)
				{
					offset = _extent.Height - _viewport.Height;
				}
			}

			_offset.Y = offset;

			if (_owner != null)
			{
				_owner.InvalidateScrollInfo();
			}

			_trans.Y = -offset;
		}


		public void SetVerticalOffset2(double offset)
		{
			_offset.Y = Math.Max(0, Math.Min(_extent.Height - _viewport.Height, Math.Max(0, offset)));

			if (_owner != null)
			{
				_owner.InvalidateScrollInfo();
			}
			
			_trans.Y = -_offset.Y; 

			InvalidateMeasure();
		}

		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			for (int i = 0; i < InternalChildren.Count; i++)
			{
				if (InternalChildren[i] == visual)
				{
					// We found the visual! Let's scroll it into view.
					// First we need to know how big each child is.
					Size finalSize = RenderSize;

					Size childSize = new Size(
						finalSize.Width,
						finalSize.Height * 2 / InternalChildren.Count
						);

					// now we can calculate the vertical offset that we need and set it
					SetVerticalOffset(childSize.Height * i);

					// child size is always smaller than viewport, because that is what makes the Panel
					// an AnnoyingPanel.
					return rectangle;
				}
			}

			throw new ArgumentException("Given visual is not in this Panel");
		}


		#endregion

		#region Private Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = new Size(
				availableSize.Width,
				(availableSize.Height * 2) / InternalChildren.Count
				);

			Size extent = new Size(
				availableSize.Width,
				childSize.Height * InternalChildren.Count);

			if (extent != _extent)
			{
				_extent = extent;
				if (_owner != null)
				{
					_owner.InvalidateScrollInfo();
				}
			}

			if (availableSize != _viewport)
			{
				_viewport = availableSize;
				if (_owner != null)
				{
					_owner.InvalidateScrollInfo();
				}
			}

			foreach (UIElement child in InternalChildren)
			{
				child.Measure(childSize);
			}

			return availableSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = new Size(
				finalSize.Width,
				(finalSize.Height * 2) / InternalChildren.Count);

			Size extent = new Size(
				finalSize.Width,
				childSize.Height * InternalChildren.Count);

			if (extent != _extent)
			{
				_extent = extent;
				if (_owner != null)
				{
					_owner.InvalidateScrollInfo();
				}
			}

			if (finalSize != _viewport)
			{
				_viewport = finalSize;

				if (_owner != null)
				{
					_owner.InvalidateScrollInfo();
				}
			}

			for (int i = 0; i < InternalChildren.Count; i++)
			{
				InternalChildren[i].Arrange(new Rect(0, childSize.Height * i, childSize.Width, childSize.Height));
			}

			return finalSize;
		}

		#endregion
	}
}
