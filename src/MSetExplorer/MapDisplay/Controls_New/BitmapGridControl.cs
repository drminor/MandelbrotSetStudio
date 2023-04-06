using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;



namespace MSetExplorer
{
	public partial class BitmapGridControl : ContentControl
	{
		#region Private Properties

		private ScrollViewer? _scrollOwner;
		private FrameworkElement? _content;

		private Point _offset;
		private Size _unscaledExtent;
		private Size _viewport;

		#endregion

		#region Constructor

		static BitmapGridControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl),
			new FrameworkPropertyMetadata(typeof(BitmapGridControl)));
		}

		public BitmapGridControl()
		{
			_scrollOwner = null;
			_content = null;
			_offset = new Point(0, 0);
			_unscaledExtent = new Size(0, 0);
			_viewport = new Size(0, 0);
		}

		#endregion

		#region Public Properties



		#endregion

		#region Public Methods



		#endregion

		#region Private Methods

		private void UpdateViewportSize(Size newSize)
		{
			if (_viewport == newSize)
			{
				return;
			}

			_viewport = newSize;

			// Update the viewport size in content coordiates.
			//UpdateContentViewportSize();

			// Initialise the content zoom focus point.
			//UpdateContentZoomFocusX();
			//UpdateContentZoomFocusY();

			// Reset the viewport zoom focus to the center of the viewport.
			//ResetViewportZoomFocus();

			// Update content offset from itself when the size of the viewport changes.
			// This ensures that the content offset remains properly clamped to its valid range.
			//this.ContentOffsetX = this.ContentOffsetX;
			//this.ContentOffsetY = this.ContentOffsetY;

			InvalidateScrollInfo();
		}

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

			// Update the size of the viewport using the availableSize.
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

			//UpdateTranslationX();
			//UpdateTranslationY();

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
			UpdateViewportSize(finalSize);

			return size;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = Template.FindName("BitmapGridControl_Content", this) as FrameworkElement;
			if (_content != null)
			{
				Debug.WriteLine($"Found the BitmapGridControl_Content template.");

				//// Setup the transform on the content so that we can scale it by 'ContentScale'.
				//this.contentScaleTransform = new ScaleTransform(this.ContentScale, this.ContentScale);

				//// Setup the transform on the content so that we can translate it by 'ContentOffsetX' and 'ContentOffsetY'.
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
			else
			{
				Debug.WriteLine($"WARNING: Did not find the BitmapGridControl_Content template.");

			}
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
	}
}
