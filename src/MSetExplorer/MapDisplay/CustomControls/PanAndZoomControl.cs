using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	public partial class PanAndZoomControl : ContentControl, IScrollInfo, IContentScaleInfo
	{
		#region Private Fields

		public static readonly double DefaultContentScale = 1.0;
		public static readonly double DefaultMinContentScale = 0.015625;
		public static readonly double DefaultMaxContentScale = 1.0;

		private bool _canHScroll = true;
		private bool _canVScroll = false;
		private bool _canZoom = true;

		private ScrollViewer? _scrollOwner;
		private ZoomSlider? _zoomSlider;

		private SizeDbl _viewportSize;

		//private ScaleTransform _contentScaleTransform;

		private bool _enableContentOffsetUpdateFromScale = true;

		private bool _disableScrollOffsetSync = false;
		private bool _disableContentFocusSync = false;
		private bool _disableContentOffsetChangeEvents = false;

		private SizeDbl _constrainedContentViewportSize = new SizeDbl();
		private SizeDbl _maxContentOffset = new SizeDbl();

		ScrollBarVisibility _originalVerticalScrollBarVisibility;
		ScrollBarVisibility _originalHorizontalScrollBarVisibility;

		private readonly bool _useDetailedDebug;

		#endregion

		#region Constructor

		public PanAndZoomControl()
		{
			ContentBeingZoomed = null;

			_scrollOwner = null;
			_zoomSlider = null;

			_viewportSize = new SizeDbl();
			//_contentScaleTransform = new ScaleTransform(DefaultContentScale, DefaultContentScale);

			ContentViewportSize = new SizeDbl();

			IsMouseWheelScrollingEnabled = false;

			_originalVerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			_originalHorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

			_useDetailedDebug = true;
		}

		#endregion

		#region Events

		public event EventHandler<ScaledImageViewInfo>? ViewportChanged;

		public event EventHandler? ContentOffsetXChanged;
		public event EventHandler? ContentOffsetYChanged;
		public event EventHandler? ContentScaleChanged;

		public event EventHandler? ScrollbarVisibilityChanged;

		#endregion

		#region Public Properties

		public FrameworkElement? ContentBeingZoomed { get; set; }

		public SizeDbl ViewportSize
		{
			get
			{
				CheckViewportSize(_viewportSize);
				return _viewportSize;
			}
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					_viewportSize = value;
					if (_contentScaler != null)
					{
						_contentScaler.ContentViewportSize = value;
					}
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
			set => SetCurrentValue(ContentScaleProperty, value);
		}

		public double MinContentScale
		{
			get => (double)GetValue(MinContentScaleProperty);
			set => SetCurrentValue(MinContentScaleProperty, value);
		}

		public double MaxContentScale
		{
			get => (double)GetValue(MaxContentScaleProperty);
			set => SetCurrentValue(MaxContentScaleProperty, value);
		}

		private SizeDbl _contentViewportSize;

		public SizeDbl ContentViewportSize
		{ 
			get
			{
				return _contentViewportSize;
			}

			set
			{
				_contentViewportSize = value;
				if (_contentScaler != null)
				{
					_contentScaler.ContentViewportSize = value;
				}
			}
		}

		public PointDbl ContentZoomFocus { get; set; }

		public PointDbl ViewportZoomFocus { get; set; }

		public double ContentOffsetX
		{
			get => (double)GetValue(ContentOffsetXProperty);
			set => SetCurrentValue(ContentOffsetXProperty, value);
		}

		public double ContentOffsetY
		{
			get => (double)GetValue(ContentOffsetYProperty);
			set => SetCurrentValue(ContentOffsetYProperty, value);
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

			//UpdateViewportSize(ScreenTypeHelper.ConvertToSizeDbl(availableSize));

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

			Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoom Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			// TODO: Figure out when its best to call UpdateViewportSize.
			// ANSWER: Don't call Update during Measure, only during Arrange.
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

				if (childSize != finalSize)
				{
					Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize} vs. {finalSize}.");
				}
				
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
				Debug.WriteLineIf(_useDetailedDebug, $"Found the PanAndZoomControl_Content template. The ContentBeingZoomed is {ContentBeingZoomed} /w type: {ContentBeingZoomed.GetType()}.");

				if (ContentBeingZoomed is ContentPresenter cp)
				{
					if (cp.Content is IContentScaler contentScaler)
					{
						_contentScaler = contentScaler;
					}
					else
					{
						_contentScaler = new ContentScaler(cp);
					}

					_contentScaler.ScaleTransform.ScaleX = ContentScale;
					_contentScaler.ScaleTransform.ScaleY = ContentScale;

					UpdateTranslationX();
					UpdateTranslationY();
				}
				else
				{
					throw new InvalidOperationException("Expecting the PanAndZoomControl's content to be a ContentPresentor.");
				}
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
				try
				{
					c._disableContentOffsetChangeEvents = true;

					c.ContentOffsetX = 0;
					c.ContentOffsetY = 0;


					if (c.ContentScale != 1.0)
					{
						c.ContentScale = 1.0;
					}
					else
					{
						c.UpdateContentViewportSize();
					}

					c.InvalidateScrollInfo();
				}
				finally
				{
					c._disableContentOffsetChangeEvents = false;
				}

				c.ScrollbarVisibilityChanged?.Invoke(c, new EventArgs());
			}
		}

		public void SetPositionAndZoom(VectorDbl displayPosition, double contentScale) 
		{
			try
			{
				_disableContentOffsetChangeEvents = true;

				ContentScale = contentScale;

				ContentOffsetX = displayPosition.X;
				ContentOffsetY = displayPosition.Y;

				UpdateContentViewportSize();

				InvalidateScrollInfo();
			}
			finally
			{
				_disableContentOffsetChangeEvents = false;
			}

			ScrollbarVisibilityChanged?.Invoke(this, new EventArgs());
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
			PanAndZoomControl c = (PanAndZoomControl)o;

			Debug.WriteLineIf(c._useDetailedDebug, $"BitmapGridControl: ContentScale is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");

			var newValue = (double)e.NewValue;

			Debug.Assert(newValue == c.ContentScale, "The NewValue for Content Scale does not match the ContentScale.");

			if (!c.UpdateScale(newValue))
			{
				// The new value is the same as the both the ScaleX and ScaleY value on the _contentScaleTransform property.
				return;
			}

			try
			{
				c._disableContentOffsetChangeEvents = true;

				c.UpdateContentViewportSize();
				c.UpdateContentOffsetsFromScale();
			}
			finally
			{
				c._disableContentOffsetChangeEvents = false;
			}

			c.ZoomSliderOwner?.ContentScaleWasUpdated(c.ContentScale);
			c.InvalidateScrollInfo();

			c.ContentScaleChanged?.Invoke(c, EventArgs.Empty);

			//if (c._contentScaler != null) c._contentScaler.ContentViewportSize = c.ContentViewportSize;

			var scaledImageViewInfo = new ScaledImageViewInfo(c.ContentViewportSize, new VectorDbl(c.ContentOffsetX, c.ContentOffsetY), c.ContentScale);
			c.ViewportChanged?.Invoke(c, scaledImageViewInfo);

			//c.InvalidateVisual(); // Is this really necessary?
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
				//c.UpdateContentZoomFocusX();
				c.UpdateContentZoomFocus();
			}

			if (!c._disableContentOffsetChangeEvents)
			{
				// Raise an event to let users of the control know that the content offset has changed.
				c.ContentOffsetXChanged?.Invoke(c, EventArgs.Empty);
			}

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
			//double minOffsetX = 0.0;
			double maxOffsetX = c._maxContentOffset.Width;

			double v1 = value >= 0 ? value : 0;

			double maxOffsetXTest = maxOffsetX + 50;

			//value = Math.Min(v1, maxOffsetX);
			value = Math.Min(v1, maxOffsetXTest);


			//if (gap || gap2)
			//{
			//	Debug.WriteLine($"CoerceOffsetX got: {baseValue} and returned {value}. UnscaledExtent.Width: {c.UnscaledExtent.Width}, MaxContentOffsetWidth: {c._maxContentOffset.Width}. " +
			//		$"ViewportWidth: {c.ViewportWidth} ContentViewportWidth: {c.ContentViewportSize.Width}. " +
			//		$"Gaps: {gap}, {gap2a}, {gap2}");
			//}

			Debug.WriteLineIf(c._useDetailedDebug, $"CoerceOffsetX got: {baseValue} and returned {value}.");

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
				c.UpdateContentZoomFocus();
			}

			if (!c._disableContentOffsetChangeEvents)
			{ 
				// Raise an event to let users of the control know that the content offset has changed.
				c.ContentOffsetYChanged?.Invoke(c, EventArgs.Empty);
			}

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
			UpdateContentZoomFocus();

			// Reset the viewport zoom focus to the center of the viewport.
			ResetViewportZoomFocus();

			try
			{
				_disableContentOffsetChangeEvents = true;
				
				// Update content offset from itself when the size of the viewport changes.
				// This ensures that the content offset remains properly clamped to its valid range.
				ContentOffsetX = ContentOffsetX;
				ContentOffsetY = ContentOffsetY;
			}
			finally
			{
				_disableContentOffsetChangeEvents = false;
			}

			InvalidateScrollInfo();

			//if (_contentScaler != null) _contentScaler.ContentViewportSize = ContentViewportSize;

			var scaledImageViewInfo = new ScaledImageViewInfo(ContentViewportSize, new VectorDbl(ContentOffsetX, ContentOffsetY), ContentScale);
			ViewportChanged?.Invoke(this, scaledImageViewInfo);
		}

		private void UpdateContentViewportSize()
		{
			if (_contentScaler == null)
			{
				Debug.WriteLine($"WARNING: The PanAndZoomControl is updating the ContentViewportSize, however the _contentScaler is null.");
			}

			if (_contentScaler?.ScaleTransform.ScaleX != ContentScale)
			{
				Debug.WriteLine($"WARNING: Not using the current value for ContentScale.");
			}
			
			ContentViewportSize = ViewportSize.Divide(ContentScale);


			// The position of the (scaled) Content View cannot be any larger than the (unscaled) extent

			// Usually the track position can vary over the entire ContentViewportSize,
			// however if the unscaled extents are less than the ContentViewportSize, no adjustment of the track position is possible.
			_constrainedContentViewportSize = UnscaledExtent.Min(ContentViewportSize);

			// If we want to avoid having the content shifted beyond the canvas boundary (thus leaving part of the canvas blank before/after the content),
			// the maximum value for the offsets is size of the ContentViewportSize subtracted from the the unscaled extents. 
			_maxContentOffset = UnscaledExtent.Sub(_constrainedContentViewportSize).Max(0);

			Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoomControl: UpdateContentViewportSize: {ContentViewportSize}, ViewportSize: {ViewportSize}, ConstrainedViewportSize: {_constrainedContentViewportSize}, ContentScale: {ContentScale}. MaxContentOffset: {_maxContentOffset}");

			SetVerticalScrollBarVisibility(_maxContentOffset.Height > 0);
			SetHorizontalScrollBarVisibility(_maxContentOffset.Width > 0);

			UpdateTranslationX();
			UpdateTranslationY();
		}

		private void UpdateTranslationX()
		{
			if (_contentScaler != null)
			{
				double result;
				var scaledContentWidth = UnscaledExtent.Width * ContentScale;

				if (scaledContentWidth < ViewportWidth)
				{
					// When the content can fit entirely within the viewport, center it.
					result = (ContentViewportSize.Width - UnscaledExtent.Width) / 2;
				}
				else
				{
					result = -ContentOffsetX;
				}

				Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoomControl would be setting the TranslateTransform.X to {result}.");
				//_contentScaler.TranslateTransform.X = result;
			}
		}

		private void UpdateTranslationY()
		{
			if (_contentScaler != null)
			{
				double result;
				var scaledContentHeight = UnscaledExtent.Height * ContentScale;

				if (scaledContentHeight < ViewportHeight)
				{
					// When the content can fit entirely within the viewport, center it.
					result = (ContentViewportSize.Height - UnscaledExtent.Height) / 2;
				}
				else
				{
					result = -ContentOffsetY;
				}

				Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoomControl would be setting the TranslateTransform.Y to {result}.");
				//_contentScaler.TranslateTransform.Y = result;
			}
		}

		private void UpdateContentZoomFocus()
		{
			var contentOffset = new PointDbl(ContentOffsetX, ContentOffsetY);

			ContentZoomFocus = contentOffset.Translate(_constrainedContentViewportSize.Divide(2));
		}

		// For this implementation the ViewportZoomFocus is always at the center
		// so we don't need to keep track of this value.
		private void ResetViewportZoomFocus()
		{
			//ViewportZoomFocusX = ViewportWidth / 2;
			//ViewportZoomFocusY = ViewportHeight / 2;
		}

		private bool UpdateScale(double contentScale)
		{
			var result = false;

			if (_contentScaler != null)
			{
				if (_contentScaler.ScaleTransform.ScaleX != contentScale)
				{
					_contentScaler.ScaleTransform.ScaleX = contentScale;
					result = true;
				}

				if (_contentScaler.ScaleTransform.ScaleY != contentScale)
				{
					_contentScaler.ScaleTransform.ScaleY = contentScale;
					result = true;
				}
			}

			return result;
		}

		private void UpdateContentOffsetsFromScale()
		{
			if (_enableContentOffsetUpdateFromScale)
			{
				try
				{
					_disableContentFocusSync = true;

					// Since the ViewportZoomFocus is always centered, 
					// we can simply examine the ContentZoomFocus.

					//var viewportOffset = ViewportZoomFocus.Sub(ViewportSize.Divide(2));
					//var contentOffset = viewportOffset.Divide(ContentScale);

					var contentOffset = ContentZoomFocus.Sub(ContentViewportSize.Divide(2)); //.Diff(contentOffset);

					ContentOffsetX = contentOffset.X;
					ContentOffsetY = contentOffset.Y;
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
			Debug.WriteLineIf(_useDetailedDebug, $"At {label}, Control: {controlSize}.");
		}

		[Conditional("DEBUG2")]
		private void CheckViewportSize(SizeDbl viewportSize)
		{
			var controlSize = new SizeDbl(ActualWidth, ActualHeight);

			if (ScreenTypeHelper.IsSizeDblChanged(viewportSize, controlSize, threshold: 0.05))
			{
				Debug.WriteLine($"WARNING: The viewportSize: {viewportSize} is not the same size as the control: {controlSize}.");
			}
		}

		#endregion
	}
}
