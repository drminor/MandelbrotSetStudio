using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MSetExplorer.MapDisplay.ExpermentalOrUnused
{
	public partial class TestContentControl : ContentControl, IScrollInfo
	{
		#region Scroll Info Properties

		private ScrollViewer? _owner;

		private bool _canHScroll;
		private bool _canVScroll;

		private Point _offset;

		private Size _extent;
		private Size _viewport;

		private TranslateTransform _trans = new TranslateTransform();

		#endregion

		/// <summary>
		/// Static constructor to define metadata for the control (and link it to the style in Generic.xaml).
		/// </summary>
		static TestContentControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TestContentControl), new FrameworkPropertyMetadata(typeof(TestContentControl)));
		}

		/// <summary>
		/// Called when a template has been applied to the control.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = this.Template.FindName("PART_Content", this) as FrameworkElement;
			if (_content != null)
			{
				////
				//// Setup the transform on the content so that we can scale it by 'ContentScale'.
				////
				//this.contentScaleTransform = new ScaleTransform(this.ContentScale, this.ContentScale);

				////
				//// Setup the transform on the content so that we can translate it by 'ContentOffsetX' and 'ContentOffsetY'.
				////
				//this.contentOffsetTransform = new TranslateTransform();
				//UpdateTranslationX();
				//UpdateTranslationY();

				////
				//// Setup a transform group to contain the translation and scale transforms, and then
				//// assign this to the content's 'RenderTransform'.
				////
				//TransformGroup transformGroup = new TransformGroup();
				//transformGroup.Children.Add(this.contentOffsetTransform);
				//transformGroup.Children.Add(this.contentScaleTransform);
				//content.RenderTransform = transformGroup;
			}
		}


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
			double childHeight = _viewport.Height;
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
			double childHeight = _viewport.Height;
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

		#endregion
	}
}
