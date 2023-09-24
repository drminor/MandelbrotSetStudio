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

		#region Dependency Property Definitions

		//
		// Definitions for dependency properties.
		//

		public static readonly DependencyProperty ContentScaleProperty =
				DependencyProperty.Register("ContentScale", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(1.0, ContentScale_PropertyChanged, ContentScale_Coerce));

		public static readonly DependencyProperty MinContentScaleProperty =
				DependencyProperty.Register("MinContentScale", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(0.01, MinOrMaxContentScale_PropertyChanged));

		public static readonly DependencyProperty MaxContentScaleProperty =
				DependencyProperty.Register("MaxContentScale", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(10.0, MinOrMaxContentScale_PropertyChanged));

		public static readonly DependencyProperty ContentOffsetXProperty =
				DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		public static readonly DependencyProperty ContentOffsetYProperty =
				DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));

		//public static readonly DependencyProperty AnimationDurationProperty =
		//		DependencyProperty.Register("AnimationDuration", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.4));

		//public static readonly DependencyProperty ContentZoomFocusXProperty =
		//		DependencyProperty.Register("ContentZoomFocusX", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0));

		//public static readonly DependencyProperty ContentZoomFocusYProperty =
		//		DependencyProperty.Register("ContentZoomFocusY", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0));

		//public static readonly DependencyProperty ViewportZoomFocusXProperty =
		//		DependencyProperty.Register("ViewportZoomFocusX", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0));

		//public static readonly DependencyProperty ViewportZoomFocusYProperty =
		//		DependencyProperty.Register("ViewportZoomFocusY", typeof(double), typeof(TestContentControl),
		//									new FrameworkPropertyMetadata(0.0));

		public static readonly DependencyProperty ContentViewportWidthProperty =
				DependencyProperty.Register("ContentViewportWidth", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(0.0));

		public static readonly DependencyProperty ContentViewportHeightProperty =
				DependencyProperty.Register("ContentViewportHeight", typeof(double), typeof(TestContentControl),
											new FrameworkPropertyMetadata(0.0));

		public static readonly DependencyProperty IsMouseWheelScrollingEnabledProperty =
				DependencyProperty.Register("IsMouseWheelScrollingEnabled", typeof(bool), typeof(TestContentControl),
											new FrameworkPropertyMetadata(false));

		#endregion


		#region Dependency Property Getters

		/// <summary>
		/// Event raised when the 'ContentScale' property has changed value.
		/// </summary>
		private static void ContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			//TestContentControl c = (TestContentControl)o;

			//if (c.contentScaleTransform != null)
			//{
			//	//
			//	// Update the content scale transform whenever 'ContentScale' changes.
			//	//
			//	c.contentScaleTransform.ScaleX = c.ContentScale;
			//	c.contentScaleTransform.ScaleY = c.ContentScale;
			//}

			////
			//// Update the size of the viewport in content coordinates.
			////
			//c.UpdateContentViewportSize();

			//if (c.enableContentOffsetUpdateFromScale)
			//{
			//	try
			//	{
			//		// 
			//		// Disable content focus syncronization.  We are about to update content offset whilst zooming
			//		// to ensure that the viewport is focused on our desired content focus point.  Setting this
			//		// to 'true' stops the automatic update of the content focus when content offset changes.
			//		//
			//		c.disableContentFocusSync = true;

			//		//
			//		// Whilst zooming in or out keep the content offset up-to-date so that the viewport is always
			//		// focused on the content focus point (and also so that the content focus is locked to the 
			//		// viewport focus point - this is how the google maps style zooming works).
			//		//
			//		double viewportOffsetX = c.ViewportZoomFocusX - (c.ViewportWidth / 2);
			//		double viewportOffsetY = c.ViewportZoomFocusY - (c.ViewportHeight / 2);
			//		double contentOffsetX = viewportOffsetX / c.ContentScale;
			//		double contentOffsetY = viewportOffsetY / c.ContentScale;
			//		c.ContentOffsetX = (c.ContentZoomFocusX - (c.ContentViewportWidth / 2)) - contentOffsetX;
			//		c.ContentOffsetY = (c.ContentZoomFocusY - (c.ContentViewportHeight / 2)) - contentOffsetY;
			//	}
			//	finally
			//	{
			//		c.disableContentFocusSync = false;
			//	}
			//}

			//if (c.ContentScaleChanged != null)
			//{
			//	c.ContentScaleChanged(c, EventArgs.Empty);
			//}

			//if (c.scrollOwner != null)
			//{
			//	c.scrollOwner.InvalidateScrollInfo();
			//}
		}

		/// <summary>
		/// Method called to clamp the 'ContentScale' value to its valid range.
		/// </summary>
		private static object ContentScale_Coerce(DependencyObject d, object baseValue)
		{
			TestContentControl c = (TestContentControl)d;
			double value = (double)baseValue;
			value = Math.Min(Math.Max(value, c.MinContentScale), c.MaxContentScale);
			return value;
		}

		/// <summary>
		/// Event raised 'MinContentScale' or 'MaxContentScale' has changed.
		/// </summary>
		private static void MinOrMaxContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
		//	TestContentControl c = (TestContentControl)o;
		//	c.ContentScale = Math.Min(Math.Max(c.ContentScale, c.MinContentScale), c.MaxContentScale);
		}

		/// <summary>
		/// Event raised when the 'ContentOffsetX' property has changed value.
		/// </summary>
		private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			//	TestContentControl c = (TestContentControl)o;

			//	c.UpdateTranslationX();

			//	if (!c.disableContentFocusSync)
			//	{
			//		//
			//		// Normally want to automatically update content focus when content offset changes.
			//		// Although this is disabled using 'disableContentFocusSync' when content offset changes due to in-progress zooming.
			//		//
			//		c.UpdateContentZoomFocusX();
			//	}

			//	if (c.ContentOffsetXChanged != null)
			//	{
			//		//
			//		// Raise an event to let users of the control know that the content offset has changed.
			//		//
			//		c.ContentOffsetXChanged(c, EventArgs.Empty);
			//	}

			//	if (!c.disableScrollOffsetSync && c.scrollOwner != null)
			//	{
			//		//
			//		// Notify the owning ScrollViewer that the scrollbar offsets should be updated.
			//		//
			//		c.scrollOwner.InvalidateScrollInfo();
			//	}
		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetX' value to its valid range.
		/// </summary>
		private static object ContentOffsetX_Coerce(DependencyObject d, object baseValue)
		{
			//TestContentControl c = (TestContentControl)d;
			//double value = (double)baseValue;
			//double minOffsetX = 0.0;
			//double maxOffsetX = Math.Max(0.0, c.unScaledExtent.Width - c.constrainedContentViewportWidth);
			//value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);
			//return value;

			return baseValue;
		}

		/// <summary>
		/// Event raised when the 'ContentOffsetY' property has changed value.
		/// </summary>
		private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			//TestContentControl c = (TestContentControl)o;

			//c.UpdateTranslationY();

			//if (!c.disableContentFocusSync)
			//{
			//	//
			//	// Normally want to automatically update content focus when content offset changes.
			//	// Although this is disabled using 'disableContentFocusSync' when content offset changes due to in-progress zooming.
			//	//
			//	c.UpdateContentZoomFocusY();
			//}

			//if (c.ContentOffsetYChanged != null)
			//{
			//	//
			//	// Raise an event to let users of the control know that the content offset has changed.
			//	//
			//	c.ContentOffsetYChanged(c, EventArgs.Empty);
			//}

			//if (!c.disableScrollOffsetSync && c.scrollOwner != null)
			//{
			//	//
			//	// Notify the owning ScrollViewer that the scrollbar offsets should be updated.
			//	//
			//	c.scrollOwner.InvalidateScrollInfo();
			//}

		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetY' value to its valid range.
		/// </summary>
		private static object ContentOffsetY_Coerce(DependencyObject d, object baseValue)
		{
			//TestContentControl c = (TestContentControl)d;
			//double value = (double)baseValue;
			//double minOffsetY = 0.0;
			//double maxOffsetY = Math.Max(0.0, c.unScaledExtent.Height - c.constrainedContentViewportHeight);
			//value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
			//return value;

			return baseValue;
		}

		#endregion

		/// <summary>
		/// Get/set the X offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetX
		{
			get
			{
				return (double)GetValue(ContentOffsetXProperty);
			}
			set
			{
				SetValue(ContentOffsetXProperty, value);
			}
		}

		///// <summary>
		///// Event raised when the ContentOffsetX property has changed.
		///// </summary>
		//public event EventHandler ContentOffsetXChanged;

		/// <summary>
		/// Get/set the Y offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetY
		{
			get
			{
				return (double)GetValue(ContentOffsetYProperty);
			}
			set
			{
				SetValue(ContentOffsetYProperty, value);
			}
		}

		///// <summary>
		///// Event raised when the ContentOffsetY property has changed.
		///// </summary>
		//public event EventHandler ContentOffsetYChanged;

		/// <summary>
		/// Get/set the current scale (or zoom factor) of the content.
		/// </summary>
		public double ContentScale
		{
			get
			{
				return (double)GetValue(ContentScaleProperty);
			}
			set
			{
				SetValue(ContentScaleProperty, value);
			}
		}

		///// <summary>
		///// Event raised when the ContentScale property has changed.
		///// </summary>
		//public event EventHandler ContentScaleChanged;

		/// <summary>
		/// Get/set the minimum value for 'ContentScale'.
		/// </summary>
		public double MinContentScale
		{
			get
			{
				return (double)GetValue(MinContentScaleProperty);
			}
			set
			{
				SetValue(MinContentScaleProperty, value);
			}
		}

		/// <summary>
		/// Get/set the maximum value for 'ContentScale'.
		/// </summary>
		public double MaxContentScale
		{
			get
			{
				return (double)GetValue(MaxContentScaleProperty);
			}
			set
			{
				SetValue(MaxContentScaleProperty, value);
			}
		}

		///// <summary>
		///// The X coordinate of the content focus, this is the point that we are focusing on when zooming.
		///// </summary>
		//public double ContentZoomFocusX
		//{
		//	get
		//	{
		//		return (double)GetValue(ContentZoomFocusXProperty);
		//	}
		//	set
		//	{
		//		SetValue(ContentZoomFocusXProperty, value);
		//	}
		//}

		///// <summary>
		///// The Y coordinate of the content focus, this is the point that we are focusing on when zooming.
		///// </summary>
		//public double ContentZoomFocusY
		//{
		//	get
		//	{
		//		return (double)GetValue(ContentZoomFocusYProperty);
		//	}
		//	set
		//	{
		//		SetValue(ContentZoomFocusYProperty, value);
		//	}
		//}

		///// <summary>
		///// The X coordinate of the viewport focus, this is the point in the viewport (in viewport coordinates) 
		///// that the content focus point is locked to while zooming in.
		///// </summary>
		//public double ViewportZoomFocusX
		//{
		//	get
		//	{
		//		return (double)GetValue(ViewportZoomFocusXProperty);
		//	}
		//	set
		//	{
		//		SetValue(ViewportZoomFocusXProperty, value);
		//	}
		//}

		///// <summary>
		///// The Y coordinate of the viewport focus, this is the point in the viewport (in viewport coordinates) 
		///// that the content focus point is locked to while zooming in.
		///// </summary>
		//public double ViewportZoomFocusY
		//{
		//	get
		//	{
		//		return (double)GetValue(ViewportZoomFocusYProperty);
		//	}
		//	set
		//	{
		//		SetValue(ViewportZoomFocusYProperty, value);
		//	}
		//}

		///// <summary>
		///// The duration of the animations (in seconds) started by calling AnimatedZoomTo and the other animation methods.
		///// </summary>
		//public double AnimationDuration
		//{
		//	get
		//	{
		//		return (double)GetValue(AnimationDurationProperty);
		//	}
		//	set
		//	{
		//		SetValue(AnimationDurationProperty, value);
		//	}
		//}

		/// <summary>
		/// Get the viewport width, in content coordinates.
		/// </summary>
		public double ContentViewportWidth
		{
			get
			{
				return (double)GetValue(ContentViewportWidthProperty);
			}
			set
			{
				SetValue(ContentViewportWidthProperty, value);
			}
		}

		/// <summary>
		/// Get the viewport height, in content coordinates.
		/// </summary>
		public double ContentViewportHeight
		{
			get
			{
				return (double)GetValue(ContentViewportHeightProperty);
			}
			set
			{
				SetValue(ContentViewportHeightProperty, value);
			}
		}

		/// <summary>
		/// Set to 'true' to enable the mouse wheel to scroll the zoom and pan control.
		/// This is set to 'false' by default.
		/// </summary>
		public bool IsMouseWheelScrollingEnabled
		{
			get
			{
				return (bool)GetValue(IsMouseWheelScrollingEnabledProperty);
			}
			set
			{
				SetValue(IsMouseWheelScrollingEnabledProperty, value);
			}
		}
	}
}
