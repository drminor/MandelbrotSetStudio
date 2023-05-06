using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MSetExplorer
{
	public partial class PanAndZoomControl : ContentControl, IScrollInfo, IContentScaleInfo
	{
		#region Private Fields

		private const double VERTICAL_SCROLL_BAR_WIDTH = 17;
		private const double HORIZONTAL_SCROLL_BAR_WIDTH = 17;

		public static readonly double DefaultContentScale = 1.0;
		public static readonly double DefaultMinContentScale = 0.0625;
		public static readonly double DefaultMaxContentScale = 1.0;

		private bool _canHScroll = true;
		private bool _canVScroll = false;
		private bool _canZoom = true;

		private SizeDbl _scrollBarDisplacement = new SizeDbl();

		private ScrollViewer? _scrollOwner;
		private ZoomSlider? _zoomSlider;

		private SizeDbl _viewportSize;

		private TranslateTransform? _contentOffsetTransform = null;
		private ScaleTransform _contentScaleTransform = new ScaleTransform();

		private bool _enableContentOffsetUpdateFromScale = false;
		private bool _disableScrollOffsetSync = false;
		private bool _disableContentFocusSync = false;

		//private double _constrainedContentViewportWidth = 0.0;
		//private double _constrainedContentViewportHeight = 0.0;

		private SizeDbl _maxContentOffset = new SizeDbl();

		ScrollBarVisibility _originalVerticalScrollBarVisibility;

		#endregion

		#region Constructor

		public PanAndZoomControl()
		{
			ContentBeingZoomed = null;

			_scrollOwner = null;
			_zoomSlider = null;

			_viewportSize = new SizeDbl();

			ContentViewportSize = new SizeDbl();

			IsMouseWheelScrollingEnabled = false;

			_originalVerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		}

		#endregion

		#region Events

		//public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		public event EventHandler<ScaledImageViewInfo>? ViewportChanged;

		public event EventHandler? ContentOffsetXChanged;
		public event EventHandler? ContentOffsetYChanged;
		public event EventHandler? ContentScaleChanged;

		#endregion

		#region Public Properties

		public FrameworkElement? ContentBeingZoomed { get; set; }

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					//Debug.WriteLine($"The PanAndZoomControl is having its ViewportSize updated to {value}, the current value is {_viewPortSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					//ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					//Debug.WriteLine($"The PanAndZoomControl is having its ViewportSize updated to {value}, the current value is already: {_viewPortSize}; not raising the ViewportSizeChanged event.");
				}
			}
		}

		public SizeDbl UnscaledExtent
		{
			get => (SizeDbl)GetValue(UnscaledExtentProperty);
			set => SetCurrentValue(UnscaledExtentProperty, value);
		}

		public double ContentScale
		{
			get => (double)GetValue(ContentScaleProperty);
			set => SetValue(ContentScaleProperty, value);
		}

		public double MinContentScale
		{
			get => (double)GetValue(MinContentScaleProperty);
			set => SetValue(MinContentScaleProperty, value);
		}

		public double MaxContentScale
		{
			get => (double)GetValue(MaxContentScaleProperty);
			set => SetValue(MaxContentScaleProperty, value);
		}

		public SizeDbl ContentViewportSize { get; set; }

		public double ContentOffsetX
		{
			get => (double)GetValue(ContentOffsetXProperty);
			set => SetValue(ContentOffsetXProperty, value);
		}

		public double ContentOffsetY
		{
			get => (double)GetValue(ContentOffsetYProperty);
			set => SetValue(ContentOffsetYProperty, value);
		}



		public bool IsMouseWheelScrollingEnabled { get; set; }

		#endregion

		#region Private Methods - Control

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			ContentBeingZoomed?.Measure(availableSize);

			UpdateViewportSize(ScreenTypeHelper.ConvertToSizeDbl(availableSize));

			double width = availableSize.Width;
			double height = availableSize.Height;

			if (double.IsInfinity(width))
			{
				width = childSize.Width;
			}

			if (double.IsInfinity(height))
			{
				height = childSize.Height;
			}

			var result = new Size(width, height);

			Debug.WriteLine($"PanAndZoom Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			// TODO: Figure out when its best to call UpdateViewportSize.
			//UpdateViewportSize(childSize);
			//UpdateViewportSize(result);

			UpdateTranslationX();
			UpdateTranslationY();

			return result;
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = base.ArrangeOverride(finalSize);

			if (ContentBeingZoomed != null)
			{
				ContentBeingZoomed.Arrange(new Rect(finalSize));

				if (childSize != finalSize) Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");

				UpdateViewportSize(ScreenTypeHelper.ConvertToSizeDbl(childSize));
			}

			return finalSize;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			ContentBeingZoomed = Template.FindName("PART_Content", this) as FrameworkElement;
			if (ContentBeingZoomed != null)
			{
				Debug.WriteLine($"Found the PanAndZoomControl_Content template. The ContentBeingZoomed is {ContentBeingZoomed} /w type: {ContentBeingZoomed.GetType()}.");

				_contentScaleTransform = new ScaleTransform(ContentScale, ContentScale);

				UpdateTranslationX();
				UpdateTranslationY();

				ContentBeingZoomed.RenderTransform = _contentScaleTransform;

				//var fe = Content as FrameworkElement;
				//Debug.Assert(fe!.RenderTransform == ContentBeingZoomed.RenderTransform, "RenderTransform Mismatch.");
			}
			else
			{
				//Debug.WriteLine($"WARNING: Did not find the PanAndZoomControl_Content template.");
				throw new InvalidOperationException("Did not find the PanAndZoomControl_Content template.");
			}
		}

		#endregion

		#region UnscaledExtent Dependency Property

		public static readonly DependencyProperty UnscaledExtentProperty = DependencyProperty.Register(
					"UnscaledExtent", typeof(SizeDbl), typeof(PanAndZoomControl),
					new FrameworkPropertyMetadata(SizeDbl.Zero, FrameworkPropertyMetadataOptions.None, UnscaledExtent_PropertyChanged));

		private static void UnscaledExtent_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;
			var previousValue = (SizeDbl)e.OldValue;
			var value = (SizeDbl)e.NewValue;

			if (!value.Diff(previousValue).IsNearZero())
			{
				c.ContentOffsetX = 0;
				c.ContentOffsetY = 0;

				c.UpdateContentViewportSize();
			}

			//c.InvalidateMeasure();
		}

		#endregion

		#region ContentScale Dependency Property

		public static readonly DependencyProperty ContentScaleProperty =
				DependencyProperty.Register("ContentScale", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(DefaultContentScale, ContentScale_PropertyChanged, ContentScale_Coerce));

		/// <summary>
		/// Event raised when the 'ContentScale' property has changed value.
		/// </summary>
		private static void ContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"BitmapGridControl: ContentScale is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");

			PanAndZoomControl c = (PanAndZoomControl)o;

			var newValue = (double)e.NewValue;

			Debug.Assert(newValue == c.ContentScale, "The NewValue for Content Scale does not match the ContentScale.");

			if (!c.UpdateScale(newValue))
			{
				// The new value is the same as the both the ScaleX and ScaleY value on the _contentScaleTransform property.
				return;
			}

			c.UpdateContentViewportSize();

			c.UpdateContentOffsetsFromScale();

			c.ContentScaleChanged?.Invoke(c, EventArgs.Empty);

			c.ZoomSliderOwner?.ContentScaleWasUpdated(c.ContentScale);

			c.InvalidateVisual(); // Is this really necessary?
		}

		/// <summary>
		/// Method called to clamp the 'ContentScale' value to its valid range.
		/// </summary>
		private static object ContentScale_Coerce(DependencyObject d, object baseValue)
		{
			PanAndZoomControl c = (PanAndZoomControl)d;
			double value = (double)baseValue;
			value = Math.Min(Math.Max(value, c.MinContentScale), c.MaxContentScale);
			return value;
		}

		#endregion

		#region MinContentScale and MaxContentScale Dependency Properties

		public static readonly DependencyProperty MinContentScaleProperty =
				DependencyProperty.Register("MinContentScale", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(DefaultMinContentScale, MinOrMaxContentScale_PropertyChanged));


		public static readonly DependencyProperty MaxContentScaleProperty =
				DependencyProperty.Register("MaxContentScale", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(DefaultMaxContentScale, MinOrMaxContentScale_PropertyChanged));

		/// <summary>
		/// Event raised 'MinContentScale' or 'MaxContentScale' has changed.
		/// </summary>
		private static void MinOrMaxContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;
			c.ContentScale = Math.Min(Math.Max(c.ContentScale, c.MinContentScale), c.MaxContentScale);

			c.ZoomSliderOwner?.InvalidateScaleContentInfo();
		}

		#endregion

		#region ContentOffsetX Dependency Property

		public static readonly DependencyProperty ContentOffsetXProperty =
				DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetX' property has changed value.
		/// </summary>
		private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;

			c.UpdateTranslationX();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocusX();
			}

			// Raise an event to let users of the control know that the content offset has changed.
			c.ContentOffsetXChanged?.Invoke(c, EventArgs.Empty);

			c.InvalidateScrollInfo();
		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetX' value to its valid range.
		/// </summary>
		private static object ContentOffsetX_Coerce(DependencyObject d, object baseValue)
		{
			PanAndZoomControl c = (PanAndZoomControl)d;

			//var gap = c.ContentViewportSize.Width != c.ViewportWidth;
			//var gap2a = Math.Min(c.ContentViewportSize.Width/* - HORIZONTAL_SCROLL_BAR_WIDTH*/, c.UnscaledExtent.Width);
			//var gap2 = c._maxContentOffset.Width != gap2a;

			double value = (double)baseValue;
			double minOffsetX = 0.0;
			double maxOffsetX = c._maxContentOffset.Width;
			value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);

			//if (gap || gap2)
			//{
			//	Debug.WriteLine($"CoerceOffsetX got: {baseValue} and returned {value}. UnscaledExtent.Width: {c.UnscaledExtent.Width}, MaxContentOffsetWidth: {c._maxContentOffset.Width}. " +
			//		$"ViewportWidth: {c.ViewportWidth} ContentViewportWidth: {c.ContentViewportSize.Width}. " +
			//		$"Gaps: {gap}, {gap2a}, {gap2}");
			//}

			//Debug.WriteLine($"CoerceOffsetX got: {baseValue} and returned {value}.");


			return value;
		}

		#endregion

		#region ContentOffsetY Dependency Property

		public static readonly DependencyProperty ContentOffsetYProperty =
				DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetY' property has changed value.
		/// </summary>
		private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;

			c.UpdateTranslationY();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocusY();
			}

			// Raise an event to let users of the control know that the content offset has changed.
			c.ContentOffsetYChanged?.Invoke(c, EventArgs.Empty);

			c.InvalidateScrollInfo();
		}

		/// <summary>
		/// Method called to clamp the 'ContentOffsetY' value to its valid range.
		/// </summary>
		private static object ContentOffsetY_Coerce(DependencyObject d, object baseValue)
		{
			PanAndZoomControl c = (PanAndZoomControl)d;
			double value = (double)baseValue;
			double minOffsetY = 0.0;
			double maxOffsetY = c._maxContentOffset.Height;
			value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);

			//Debug.WriteLine($"CoerceOffsetY got: {baseValue} and returned {value}.");

			return value;
		}

		#endregion

		#region Private Methods - Scroll Support

		private void ResetViewportZoomFocus()
		{
			//ViewportZoomFocusX = ViewportWidth / 2;
			//ViewportZoomFocusY = ViewportHeight / 2;
		}

		private void UpdateViewportSize(SizeDbl newValue)
		{
			if (ViewportSize == newValue)
			{
				return;
			}

			ViewportSize = newValue;

			// Update the viewport size in content coordiates.
			UpdateContentViewportSize();

			// Initialise the content zoom focus point.
			UpdateContentZoomFocusX();
			UpdateContentZoomFocusY();

			// Reset the viewport zoom focus to the center of the viewport.
			ResetViewportZoomFocus();

			// Update content offset from itself when the size of the viewport changes.
			// This ensures that the content offset remains properly clamped to its valid range.
			ContentOffsetX = ContentOffsetX;
			ContentOffsetY = ContentOffsetY;

			InvalidateScrollInfo();
		}

		private void UpdateContentViewportSize()
		{
			if (_contentScaleTransform.ScaleX != ContentScale)
			{
				Debug.WriteLine($"Not using the current value for ContentScale.");
			}

			var currentMaxContentOffset = _maxContentOffset;
			var currentScrollBarDisplacement = ScrollBarDisplacement;

			var viewPortSizeSansScrBarThicknesses = ViewportSize.Diff(ScrollBarDisplacement);

			ContentViewportSize = viewPortSizeSansScrBarThicknesses.Divide(ContentScale);

			// The position of the (scaled) Content View cannot be any larger than the (unscaled) extent

			// Usually the track position can vary over the entire ContentViewportSize,
			// however if the unscaled extents are less than the ContentViewportSize, no adjustment of the track position is possible.
			var maxTrackVariance = UnscaledExtent.Min(ContentViewportSize);

			// If we want to avoid having the content shifted beyond the canvas boundary (thus leaving part of the canvas blank before/after the content),
			// the maximum value for the offsets is size of the ContentViewportSize subtracted from the the unscaled extents. 
			_maxContentOffset = UnscaledExtent.Sub(maxTrackVariance).Max(0);
			
			SetVerticalScrollBarVisibility(_maxContentOffset.Height > 0);

			ScrollBarDisplacement = GetScrollBarDisplacement();

			UpdateTranslationX();
			UpdateTranslationY();

			ViewportChanged?.Invoke(this, new ScaledImageViewInfo(new VectorDbl(ContentOffsetX, ContentOffsetY), ContentViewportSize));

			InvalidateScrollInfo();
		}

		private SizeDbl GetScrollBarDisplacement(SizeDbl? currentValue = null)
		{
			var result = _scrollOwner == null
				? new SizeDbl()
				: new SizeDbl
					(
						_scrollOwner.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible
							? HORIZONTAL_SCROLL_BAR_WIDTH
							: 0,
						_scrollOwner.VerticalScrollBarVisibility == ScrollBarVisibility.Visible
							? VERTICAL_SCROLL_BAR_WIDTH
							: 0
					);

			if (currentValue != null && currentValue.Value.Height != result.Height)
			{
				Debug.WriteLine($"The vertical scrollbar visibility has been updated from {currentValue.Value.Height == 0} to {result.Height == 0}.");
			}

			return result;
		}

		private void UpdateTranslationX()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentWidth = UnscaledExtent.Width * ContentScale;
				if (scaledContentWidth < ViewportWidth)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.X = (ContentViewportSize.Width - UnscaledExtent.Width) / 2;
				}
				else
				{
					_contentOffsetTransform.X = -ContentOffsetX;
				}
			}
		}

		private void UpdateTranslationY()
		{
			if (_contentOffsetTransform != null)
			{
				double scaledContentHeight = UnscaledExtent.Height * ContentScale;
				if (scaledContentHeight < ViewportHeight)
				{
					//
					// When the content can fit entirely within the viewport, center it.
					//
					_contentOffsetTransform.Y = (ContentViewportSize.Height - UnscaledExtent.Height) / 2;
				}
				else
				{
					_contentOffsetTransform.Y = -ContentOffsetY;
				}
			}
		}

		private void UpdateContentZoomFocusX()
		{
			//ContentZoomFocusX = ContentOffsetX + (_constrainedContentViewportWidth / 2);
		}

		private void UpdateContentZoomFocusY()
		{
			//ContentZoomFocusY = ContentOffsetY + (_constrainedContentViewportHeight / 2);
		}

		private bool UpdateScale(double contentScale)
		{
			var result = false;

			if (_contentScaleTransform.ScaleX != contentScale)
			{
				_contentScaleTransform.ScaleX = contentScale;
				result = true;
			}

			if (_contentScaleTransform.ScaleY != contentScale)
			{
				_contentScaleTransform.ScaleY = contentScale;
				result = true;
			}

			return result;
		}

		private void UpdateContentOffsetsFromScale()
		{
			if (_enableContentOffsetUpdateFromScale)
			{
				try
				{
					// 
					// Disable content focus syncronization.  We are about to update content offset whilst zooming
					// to ensure that the viewport is focused on our desired content focus point.  Setting this
					// to 'true' stops the automatic update of the content focus when content offset changes.
					//
					_disableContentFocusSync = true;

					//
					// Whilst zooming in or out keep the content offset up-to-date so that the viewport is always
					// focused on the content focus point (and also so that the content focus is locked to the 
					// viewport focus point - this is how the google maps style zooming works).
					//
					//double viewportOffsetX = c.ViewportZoomFocusX - (c.ViewportWidth / 2);
					//double viewportOffsetY = c.ViewportZoomFocusY - (c.ViewportHeight / 2);

					//double contentOffsetX = viewportOffsetX / c.ContentScale;
					//double contentOffsetY = viewportOffsetY / c.ContentScale;

					//c.ContentOffsetX = (c.ContentZoomFocusX - (c.ContentViewportWidth / 2)) - contentOffsetX;
					//c.ContentOffsetY = (c.ContentZoomFocusY - (c.ContentViewportHeight / 2)) - contentOffsetY;
				}
				finally
				{
					_disableContentFocusSync = false;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateScrollInfo()
		{
			if (_scrollOwner != null && !_disableScrollOffsetSync)
			{
				_scrollOwner.InvalidateScrollInfo();
			}
		}

		public void ReportSizes(string label)
		{
			var controlSize = new SizeInt(ActualWidth, ActualHeight);
			Debug.WriteLine($"At {label}, Control: {controlSize}.");
		}

		#endregion
	}
}
