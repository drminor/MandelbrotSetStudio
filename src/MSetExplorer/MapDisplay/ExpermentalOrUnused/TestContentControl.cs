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

namespace MSetExplorer.MapDisplay.ExpermentalOrUnused
{
	public partial class TestContentControl : ContentControl, IScrollInfo
	{
		#region Private Properties

		private FrameworkElement? _content = null;

		#endregion

		#region Constructor

		public TestContentControl()
		{
			_offset = new Point(0, 0);

			_extent = new Size(0, 0);
			_viewport = new Size(0, 0);

			RenderTransform = _trans;
		}

		#endregion

		#region Public Properties



		#endregion

		#region Public Methods



		#endregion

		#region Private Methods

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size constraint)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Size childSize = base.MeasureOverride(infiniteSize);

			if (childSize != _extent)
			{
				//
				// Use the size of the child as the un-scaled extent content.
				//
				_extent = childSize;

				if (ScrollOwner != null)
				{
					ScrollOwner.InvalidateScrollInfo();
				}
			}

			//
			// Update the size of the viewport onto the content based on the passed in 'constraint'.
			//
			//UpdateViewportSize(constraint);

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

			//UpdateTranslationX();
			//UpdateTranslationY();

			return new Size(width, height);
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			Size size = base.ArrangeOverride(DesiredSize);

			if (_content != null && _content.DesiredSize != _extent)
			{
				//
				// Use the size of the child as the un-scaled extent content.
				//
				_extent = _content.DesiredSize;

				if (_owner != null)
				{
					_owner.InvalidateScrollInfo();
				}
			}

			//
			// Update the size of the viewport onto the content based on the passed in 'arrangeBounds'.
			//
			//UpdateViewportSize(arrangeBounds);

			return size;
		}


		#endregion
	}
}
