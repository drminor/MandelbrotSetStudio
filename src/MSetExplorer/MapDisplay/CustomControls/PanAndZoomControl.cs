using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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

	public partial class PanAndZoomControl : ContentControl, IScrollInfo, IZoomInfo, IDisposable
	{
		#region Private Fields

		public static readonly double DefaultContentScale = 1.0;
		public static readonly double DefaultMinContentScale = 0.015625;
		public static readonly double DefaultMaxContentScale = 1.0;

		private ScrollViewer? _scrollOwner;
		private ZoomSlider? _zoomOwner;

		private SizeDbl _unscaledViewportSize;

		private bool _enableContentOffsetUpdateFromScale = false; // true;

		private bool _disableScrollOffsetSync = false;
		private bool _disableContentFocusSync = false;
		private bool _disableContentOffsetChangeEvents = false;

		private SizeDbl _constrainedContentViewportSize = new SizeDbl();
		private SizeDbl _maxContentOffset = new SizeDbl();

		ScrollBarVisibility _originalVerticalScrollBarVisibility;
		ScrollBarVisibility _originalHorizontalScrollBarVisibility;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static PanAndZoomControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PanAndZoomControl), new FrameworkPropertyMetadata(typeof(PanAndZoomControl)));
		}

		public PanAndZoomControl()
		{
			CanHorizontallyScroll = true;
			CanVerticallyScroll = false;
			CanZoom = true;
			ContentBeingZoomed = null;

			_scrollOwner = null;
			_zoomOwner = null;

			_unscaledViewportSize = new SizeDbl();

			ContentViewportSize = new SizeDbl();

			IsMouseWheelScrollingEnabled = false;

			_originalVerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
			_originalHorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
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

		public SizeDbl UnscaledViewportSize
		{
			get
			{
				CheckViewportSize(_unscaledViewportSize);
				return _unscaledViewportSize;
			}
			set
			{
				//if (ScreenTypeHelper.IsSizeDblChanged(_unscaledViewportSize, value))
				//{
				//	_unscaledViewportSize = value;
				//}

				if (value != _unscaledViewportSize)
				{
					_unscaledViewportSize = value;	
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
			get => _contentViewportSize;

			set
			{
				_contentViewportSize = value;


				if (_contentScaler != null/* && !_disableMinMaxContentScaleChecks*/)
				{
					Debug.WriteLine("The PanAndZoom control is settting the ContentScaler's ContentViewportSize.");
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

		public VectorDbl ContentOffset => new VectorDbl((double)GetValue(ContentOffsetXProperty), (double)GetValue(ContentOffsetYProperty));

		//public double ContentPositionX
		//{
		//	get => (double)GetValue(ContentPositionXProperty);
		//	set => SetCurrentValue(ContentPositionXProperty, value);
		//}

		//public double ContentPositionY
		//{
		//	get => (double)GetValue(ContentPositionYProperty);
		//	set => SetCurrentValue(ContentPositionYProperty, value);
		//}

		public ScaledImageViewInfo ViewportSizeOffsetAndScale
		{
			get => (ScaledImageViewInfo)GetValue(ViewportSizeOffsetAndScaleProperty);
			set => SetCurrentValue(ViewportSizeOffsetAndScaleProperty, value);
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

			//Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoom Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			// TODO: Figure out when its best to call UpdateViewportSize.
			// ANSWER: Don't call Update during Measure, only during Arrange.
			//UpdateViewportSize(childSize);
			//UpdateViewportSize(result);

			//UpdateTranslationX();
			//UpdateTranslationY();
			UpdateTranslation();

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
				//Debug.WriteLineIf(_useDetailedDebug, $"Found the PanAndZoomControl_Content template. The ContentBeingZoomed is {ContentBeingZoomed} /w type: {ContentBeingZoomed.GetType()}.");

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

					//_contentScaler.ScaleTransform.ScaleX = ContentScale;
					//_contentScaler.ScaleTransform.ScaleY = ContentScale;

					_contentScaler.ContentScale = new SizeDbl(ContentScale, ContentScale);

					//UpdateTranslationX();
					//UpdateTranslationY();
					UpdateTranslation();
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
			var value = (SizeDbl)e.NewValue;

			if (value.IsNearZero())
			{
				return;
			}

			PanAndZoomControl c = (PanAndZoomControl)o;

			try
			{
				c._disableContentOffsetChangeEvents = true;

				c.UpdateContentViewportSize();
				c.InvalidateScrollInfo();
			}
			finally
			{
				c._disableContentOffsetChangeEvents = false;
			}

			// TODO: CheckEvent
			c.ScrollbarVisibilityChanged?.Invoke(c, new EventArgs());

			//c.ViewportSizeOffsetAndScale = new ScaledImageViewInfo(c.ContentViewportSize, new VectorDbl(c.ContentOffsetX, c.ContentOffsetY), c.ContentScale);
			var sivi = new ScaledImageViewInfo(c.ContentViewportSize, new VectorDbl(c.ContentOffsetX, c.ContentOffsetY), c.ContentScale);
			c.ViewportChanged?.Invoke(c, sivi);
		}

		public void ResetExtentWithPositionAndScale(VectorDbl displayPosition, double contentScale, double minContentScale, double maxContentScale)
		{
			UnscaledExtent = new SizeDbl();

			if (_contentScaler != null)
			{
				_contentScaler.TranslationAndClipSize = null;
			}

			_maxContentOffset = new SizeDbl(displayPosition).Inflate(100); // Temporary to allow the updates to ContentOffset not get caught by the Coercers.

			ContentOffsetX = displayPosition.X;
			ContentOffsetY = displayPosition.Y;

			MinContentScale = minContentScale;
			MaxContentScale = maxContentScale;

			ContentScale = contentScale;
			ZoomOwner?.InvalidateScaleContentInfo();
		}

		#endregion

		#region ContentScale Dependency Property

		public static readonly DependencyProperty ContentScaleProperty =
				DependencyProperty.Register("ContentScale", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(
												DefaultContentScale,
												FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
												ContentScale_PropertyChanged, 
												ContentScale_Coerce,
												false,
												UpdateSourceTrigger.Explicit
												)
					);


		/// <summary>
		/// Event raised when the 'ContentScale' property has changed value.
		/// </summary>
		private static void ContentScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;

			//Debug.WriteLineIf(c._useDetailedDebug, $"PanAndZoomControl: ContentScale is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");
			Debug.WriteLine($"PanAndZoomControl: ContentScale is changing. The old size: {e.OldValue}, new size: {e.NewValue}.");

			var newValue = (double)e.NewValue;

			if (!c.UpdateScale(newValue))
			{
				// The new value is the same as the both the ScaleX and ScaleY value on the _contentScaleTransform property.
				return;
			}

			if (c.UnscaledExtent.IsNearZero())
			{
				return;
			}

			try
			{
				c._disableContentOffsetChangeEvents = true;

				c.UpdateContentViewportSize();

				if (c._enableContentOffsetUpdateFromScale)
				{
					c.UpdateContentOffsetsFromScale();
				}
			}
			finally
			{
				c._disableContentOffsetChangeEvents = false;
			}

			c.ZoomOwner?.ContentScaleWasUpdated(c.ContentScale);
			c.InvalidateScrollInfo();

			// TODO: CheckEvent
			c.ContentScaleChanged?.Invoke(c, EventArgs.Empty);

			//c.ViewportSizeOffsetAndScale = new ScaledImageViewInfo(c.ContentViewportSize, new VectorDbl(c.ContentOffsetX, c.ContentOffsetY), c.ContentScale);
			var sivi = new ScaledImageViewInfo(c.ContentViewportSize, new VectorDbl(c.ContentOffsetX, c.ContentOffsetY), c.ContentScale);
			c.ViewportChanged?.Invoke(c, sivi);
		}

		/// <summary>
		/// Method called to clamp the 'ContentScale' value to its valid range.
		/// </summary>
		private static object ContentScale_Coerce(DependencyObject d, object baseValue)
		{
			PanAndZoomControl c = (PanAndZoomControl)d;
			var bVal = (double)baseValue;
			//double value = (double)baseValue;
			var value = Math.Min(Math.Max(bVal, c.MinContentScale), c.MaxContentScale);

			//if (value != bVal)
			//{
			//	Debug.WriteLine($"ContentScale is being coerced from {bVal} to {value}.");
			//}
			//else
			//{
			//	Debug.WriteLine("....*******.........");
			//}

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

			if (!c.UnscaledExtent.IsNearZero())
			{
				c.ContentScale = Math.Min(Math.Max(c.ContentScale, c.MinContentScale), c.MaxContentScale);
				c.ZoomOwner?.InvalidateScaleContentInfo();
			}
		}

		#endregion

		#region ContentOffsetX Dependency Property

		public static readonly DependencyProperty ContentOffsetXProperty =
				DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(
												0.0,
												FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
												ContentOffsetX_PropertyChanged, 
												ContentOffsetX_Coerce,
												false, 
												UpdateSourceTrigger.PropertyChanged
												)
					);

		//public static readonly DependencyProperty ContentPositionXProperty =
		//	DependencyProperty.Register("ContentPositionX", typeof(double), typeof(PanAndZoomControl),
		//		new FrameworkPropertyMetadata(0.0));

		/// <summary>
		/// Event raised when the 'ContentOffsetX' property has changed value.
		/// </summary>
		private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;

			if (c.UnscaledExtent.IsNearZero())
			{
				return;
			}

			c.UpdateTranslation();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocus();
			}

			if (!c._disableContentOffsetChangeEvents)
			{
				//c.ContentPositionX = (double)e.NewValue;

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

			double value = (double)baseValue;

			if (!c.UnscaledExtent.IsNearZero())
			{
				double maxOffsetX = c._maxContentOffset.Width;
				double v1 = value >= 0 ? value : 0;
				value = Math.Min(v1, maxOffsetX);
			}

			Debug.WriteLineIf(c._useDetailedDebug, $"CoerceOffsetX got: {baseValue} and returned {value}. The maximum is {c._maxContentOffset.Width}.");

			return value;
		}

		#endregion

		#region ContentOffsetY Dependency Property

		public static readonly DependencyProperty ContentOffsetYProperty =
				DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(
												0.0,
												FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
												ContentOffsetY_PropertyChanged,
												ContentOffsetY_Coerce,
												false,
												UpdateSourceTrigger.PropertyChanged
												)
					);

		//public static readonly DependencyProperty ContentPositionYProperty = 
		//	DependencyProperty.Register("ContentPositionY", typeof(double), typeof(PanAndZoomControl),
		//		new FrameworkPropertyMetadata(0.0));

		/// <summary>
		/// Event raised when the 'ContentOffsetY' property has changed value.
		/// </summary>
		private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			PanAndZoomControl c = (PanAndZoomControl)o;

			if (c.UnscaledExtent.IsNearZero())
			{
				return;
			}

			c.UpdateTranslation();

			if (!c._disableContentFocusSync)
			{
				// Don't update the ZoomFocus if zooming is in progress.
				c.UpdateContentZoomFocus();
			}

			if (!c._disableContentOffsetChangeEvents)
			{
				//c.ContentOffsetX = (double)e.NewValue; // c.ContentOffsetY;

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

			if (!c.UnscaledExtent.IsNearZero())
			{
				double minOffsetY = 0.0;
				double maxOffsetY = c._maxContentOffset.Height;
				value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
			}

			Debug.WriteLine($"CoerceOffsetY got: {baseValue} and returned {value}. The maximum is {c._maxContentOffset.Height}.");

			return value;
		}

		#endregion

		#region ViewportSizeOffsetAndScale Dependency Property

		public static readonly DependencyProperty ViewportSizeOffsetAndScaleProperty =
				DependencyProperty.Register("ViewportSizeOffsetAndScale", typeof(ScaledImageViewInfo), typeof(PanAndZoomControl),
											new FrameworkPropertyMetadata(ScaledImageViewInfo.Zero, FrameworkPropertyMetadataOptions.None, ViewportSizeOffsetAndScale_PropertyChanged));

		/// <summary>
		/// Event raised when the 'ViewportSizeOffsetAndScale' property has changed value.
		/// </summary>
		private static void ViewportSizeOffsetAndScale_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region Private Methods - Scroll Support

		private void UpdateViewportSize(SizeDbl newValue)
		{
			if (UnscaledViewportSize == newValue )
			{
				return;
			}

			UnscaledViewportSize = newValue;

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

			ViewportSizeOffsetAndScale = new ScaledImageViewInfo(ContentViewportSize, new VectorDbl(ContentOffsetX, ContentOffsetY), ContentScale);
			ViewportChanged?.Invoke(this, ViewportSizeOffsetAndScale);
		}

		private void UpdateContentViewportSize()
		{
			if (_contentScaler == null)
			{
				Debug.WriteLine($"WARNING: The PanAndZoomControl is updating the ContentViewportSize, however the _contentScaler is null.");
			}

			if (_contentScaler?.ContentScale.Width != ContentScale)
			{
				Debug.WriteLine($"WARNING: Not using the current value for ContentScale.");
			}

			ContentViewportSize = UnscaledViewportSize.Divide(ContentScale);

			// The position of the (scaled) Content View cannot be any larger than the (unscaled) extent

			// Usually the track position can vary over the entire ContentViewportSize,
			// however if the unscaled extents are less than the ContentViewportSize, no adjustment of the track position is possible.
			_constrainedContentViewportSize = UnscaledExtent.Min(ContentViewportSize);

			// If we want to avoid having the content shifted beyond the canvas boundary (thus leaving part of the canvas blank before/after the content),
			// the maximum value for the offsets is size of the ContentViewportSize subtracted from the the unscaled extents. 
			_maxContentOffset = UnscaledExtent.Sub(_constrainedContentViewportSize).Max(0);

			Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoomControl: UpdateContentViewportSize: {ContentViewportSize}, UnscaledViewportSize: {UnscaledViewportSize}, ConstrainedViewportSize: {_constrainedContentViewportSize}, ContentScale: {ContentScale}. MaxContentOffset: {_maxContentOffset}");

			SetVerticalScrollBarVisibility(_maxContentOffset.Height > 0);
			SetHorizontalScrollBarVisibility(_maxContentOffset.Width > 0);

			UpdateTranslation();
		}

		private void UpdateTranslation()
		{
			if (UnscaledExtent.Width == 0 || UnscaledExtent.Height == 0)
			{
				return;
			}

			if (_contentScaler != null)
			{
				SizeDbl contentScale2D = new SizeDbl(ContentScale, ContentScale);

				var scaledExtent = UnscaledExtent.Scale(contentScale2D);

				double offsetX;
				double clipWidth;
				if (scaledExtent.Width < UnscaledViewportSize.Width)
				{
					// When the content can fit entirely within the viewport, center it.
					//resultWidth = (ContentViewportSize.Width - UnscaledExtent.Width) / 2;
					offsetX = (UnscaledViewportSize.Width - scaledExtent.Width) / 2;
					clipWidth = scaledExtent.Width;
				}
				else
				{
					offsetX = 0; // -ContentOffsetX;
					clipWidth = UnscaledViewportSize.Width;
				}

				double offsetY;
				double	clipHeight;
				if (scaledExtent.Height < UnscaledViewportSize.Height)
				{
					// When the content can fit entirely within the viewport, center it.
					//resultHeight = (ContentViewportSize.Height - UnscaledExtent.Height) / 2;
					offsetY = (UnscaledViewportSize.Height - scaledExtent.Height) / 2;
					clipHeight = scaledExtent.Height;
				}
				else
				{
					offsetY = 0; // -ContentOffsetY;
					clipHeight = UnscaledViewportSize.Height;
				}

				RectangleDbl? contentClip = offsetX > 0 | offsetY > 0 
					? new RectangleDbl(new PointDbl(offsetX, offsetY), new SizeDbl(clipWidth, clipHeight)) 
					: null;


				Debug.WriteLineIf(_useDetailedDebug, $"PanAndZoomControl is setting the ContentScaler's {nameof(IContentScaler.TranslationAndClipSize)} to {RectangleDbl.FormatNully(contentClip)}.");
				_contentScaler.TranslationAndClipSize = contentClip;
			}
		}

		private bool UpdateScale(double contentScale)
		{
			var result = false;

			if (_contentScaler != null)
			{
				var contentScale2D = new SizeDbl(contentScale, contentScale);

				if (ScreenTypeHelper.IsSizeDblChanged(contentScale2D, _contentScaler.ContentScale, RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF))
				{
					Debug.WriteLine($"The PanAndZoom control is setting the ContentScaler's ContentScale to {contentScale2D}. Update was successful.");

					_contentScaler.ContentScale = contentScale2D;
					result = true;
				}
				else
				{
					Debug.WriteLine($"The PanAndZoom control is setting the ContentScaler's ContentScale. Update was NOT made, value already: {contentScale}.");
				}
			}

			return result;
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

			ViewportZoomFocus = new PointDbl(UnscaledViewportSize.Divide(2));
		}

		private void UpdateContentOffsetsFromScale()
		{
			try
			{
				_disableContentFocusSync = true;

				// Since the ViewportZoomFocus is always centered, 
				// we can simply examine the ContentZoomFocus.

				var viewportOffset = ViewportZoomFocus.Sub(UnscaledViewportSize.Divide(2));
				var contentOffset = viewportOffset.Divide(ContentScale);

				//var contentOffset = ContentZoomFocus.Sub(ContentViewportSize.Divide(2)); //.Diff(contentOffset);

				Debug.WriteLine($"WARNING: ContentOffsets are being updated after updating the scale. NewValue: {contentOffset}.");

				ContentOffsetX = contentOffset.X;
				ContentOffsetY = contentOffset.Y;
			}
			finally
			{
				_disableContentFocusSync = false;
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

		#region Public Properties - IScrollInfo

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

		public bool CanHorizontallyScroll { get; set; }

		public bool CanVerticallyScroll { get; set; }

		public double ExtentWidth => Math.Max(UnscaledExtent.Width, ViewportWidth) * ContentScale;

		public double ExtentHeight => Math.Max(UnscaledExtent.Height, ViewportHeight) * ContentScale;

		public double ViewportWidth => UnscaledViewportSize.Width;

		public double ViewportHeight => UnscaledViewportSize.Height;

		public double HorizontalOffset => ContentOffsetX * ContentScale;

		public double VerticalOffset => ContentOffsetY * ContentScale;

		#endregion

		#region Line / Page / MouseWheel - IScrollInfo

		public void LineDown()
		{
			ContentOffsetY += ContentViewportSize.Height / 10;
		}

		public void LineUp() => ContentOffsetY -= ContentViewportSize.Height / 10;

		public void LineLeft() => ContentOffsetX -= ContentViewportSize.Width / 10;

		public void LineRight() => ContentOffsetX += ContentViewportSize.Width / 10;

		public void PageUp()
		{
			Debug.WriteLine("\n\nUser is Paging Up.");
			ContentOffsetY -= ContentViewportSize.Height;
		}

		public void PageDown()
		{
			Debug.WriteLine("\n\nUser is Paging Down.");
			ContentOffsetY += ContentViewportSize.Height;
		}

		public void PageLeft()
		{
			Debug.WriteLine("\n\nUser is Paging Left.");
			ContentOffsetX -= ContentViewportSize.Width;
		}

		public void PageRight()
		{
			Debug.WriteLine("\n\nUser is Paging Right.");
			ContentOffsetX += ContentViewportSize.Width;
		}

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
					if (_scrollOwner.VerticalScrollBarVisibility == ScrollBarVisibility.Visible && !show)
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

		public ZoomSlider? ZoomOwner
		{
			get => _zoomOwner;
			set => _zoomOwner = value;
		}

		public bool CanZoom { get; set; }

		public double Scale => ContentScale;

		public double MinScale => MinContentScale;

		public double MaxScale => MaxContentScale;

		public void SetScale(double contentScale)
		{
			var currentValue = ContentScale;

			if (contentScale != currentValue)
			{
				var transformScaleBefore = _contentScaler?.ContentScale ?? new SizeDbl();
				ContentScale = contentScale;
				var transformScaleAfter = _contentScaler?.ContentScale ?? new SizeDbl();

				if (transformScaleAfter == transformScaleBefore)
				{
					Debug.WriteLine($"WARNING: The PanAndZoom Control is updating the ContentScale from {currentValue} to {contentScale} as SetScale is being called, however the actual display control's scale did not change.");
				}
				else
				{
					Debug.WriteLine($"The PanAndZoom Control is updating the ContentScale from {currentValue} to {contentScale} as SetScale is being called.");
				}
			}
			else
			{
				Debug.WriteLine($"The PanAndZoom Control found that the ContentScale is already {currentValue} as SetScale is being called.");
			}

		}

		#endregion

		#region IContentScaler Support

		private IContentScaler? _contentScaler;

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			if (ZoomOwner != null)
			{
				((IDisposable)ZoomOwner).Dispose();
			}
		}

		#endregion

	}
}
