using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public partial class BitmapGridControl : ContentControl
	{
		#region Private Properties

		private DebounceDispatcher _viewPortSizeDispatcher;
		private readonly SizeInt _blockSize;
		private ScrollViewer? _scrollOwner;

		private FrameworkElement? _content;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _containerSize;
		private SizeDbl _viewPortSizeInternal;

		//private Point _contentRenderTransformOrigin;
		//private TranslateTransform _contentOffsetTransform;
		//private TransformGroup _transformGroup;

		private BitmapGrid _bitmapGrid;

		#endregion

		#region Constructor

		public BitmapGridControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_blockSize = RMapConstants.BLOCK_SIZE;
			_scrollOwner = null;

			_content = null;
			_canvas = new Canvas();
			_image = new Image();
			_canvas.Children.Add(_image);

			var ourSize = new SizeDbl(ActualWidth, ActualHeight);

			_bitmapGrid = new BitmapGrid(_image, ourSize, _blockSize, OurDisposeMapSectionImplementation);

			_containerSize = new SizeDbl();
			_viewPortSizeInternal = new SizeDbl();

			//_contentRenderTransformOrigin = new Point(0, 0);
			//_transformGroup = new TransformGroup();

			//_contentOffsetTransform = new TranslateTransform(0, 0);
			//_transformGroup.Children.Add(_contentOffsetTransform);
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("Image SizeChanged");
			SetImageOffset(ImageOffset);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;
		public event EventHandler? ContentOffsetXChanged;
		public event EventHandler? ContentOffsetYChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas
		{
			get => _canvas;
			set => _canvas = value;
		}

		public Image Image
		{
			get => _image;
			set
			{
				if (_image != value)
				{
					_image.SizeChanged -= Image_SizeChanged;

					_image = value;
					_image.SizeChanged += Image_SizeChanged;
					
					_bitmapGrid.Image = value;
					_bitmapGrid.ViewPortSize = ViewPortSize;

					SetImageOffset(ImageOffset);
				}
			}
		}

		public IBitmapGrid BitmapGrid => _bitmapGrid;

		private SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;
				
				InvalidateScrollInfo();
				
				ViewPortSizeInternal = value;
			}
		}

		private SizeDbl ViewPortSizeInternal
		{
			get => _viewPortSizeInternal;
			set
			{
				if (value.Width > 1 && value.Height > 1 && _viewPortSizeInternal != value)
				{
					var previousValue = _viewPortSizeInternal;
					_viewPortSizeInternal = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");

					var newViewPortSize = value;

					if (previousValue.Width < 25 || previousValue.Height < 25)
					{
						// Update the 'real' value immediately
						Debug.WriteLine($"Updating the ViewPortSize immediately. Previous Size: {previousValue}, New Size: {value}.");
						ViewPortSize = newViewPortSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
						var tempOffset = GetTempImageOffset(ImageOffset, ViewPortSize, newViewPortSize);
						_ = SetImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLine($"Updating the ViewPortSize after debounce. Previous Size: {ViewPortSize}, New Size: {newViewPortSize}.");
								ViewPortSize = newViewPortSize;
							}
						);
					}
				}
				else
				{
					Debug.WriteLine($"Skipping the update of the ViewPortSize, the new value is the same as the old value. {_viewPortSizeInternal} vs {value}.");
				}
			}
		}

		//public SizeDbl ViewPortSize
		//{
		//	get => (SizeDbl)GetValue(ViewPortSizeProperty);
		//	set
		//	{
		//		if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
		//		{
		//			_bitmapGrid.ViewPortSize = value;
		//			SetValue(ViewPortSizeProperty, value);
		//		}
		//	}
		//}

		private SizeDbl _viewPortSize;
		public SizeDbl ViewPortSize
		{
			get => _viewPortSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
				{
					var previousValue = ViewPortSize;

					_bitmapGrid.ViewPortSize = value;
					_viewPortSize = value;
					ViewPortSizeChanged?.Invoke(this, (ViewPortSize, value));
				}
			}
		}

		public VectorDbl ImageOffset
		{
			get => (VectorDbl)GetValue(ImageOffsetProperty);
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(ImageOffset, value))
				{
					SetValue(ImageOffsetProperty, value);
				}
			}
		}

		public ObservableCollection<MapSection> MapSections
		{
			get => (ObservableCollection<MapSection>)GetValue(MapSectionsProperty);
			set => SetValue(MapSectionsProperty, value);
		}

		public ColorBandSet ColorBandSet
		{
			get => (ColorBandSet)GetValue(ColorBandSetProperty);
			set => SetValue(ColorBandSetProperty, value);
		}

		public bool UseEscapeVelocities
		{
			get => (bool)GetValue(UseEscapeVelocitiesProperty);
			set => SetValue(UseEscapeVelocitiesProperty, value);
		}

		public bool HighlightSelectedColorBand
		{
			get => (bool)GetValue(HighlightSelectedColorBandProperty);
			set => SetValue(HighlightSelectedColorBandProperty, value);
		}

		public Action<MapSection>? DisposeMapSection { get; set; }

		#endregion

		#region Private ContentControl Methods

		protected override Size MeasureOverride(Size constraint)
		{
			Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Size childSize = base.MeasureOverride(infiniteSize);

			//if (_containerSize != childSize)
			//{
			//	// Use the size of the child as the un-scaled extent content.
			//	_containerSize = childSize;

			//	InvalidateScrollInfo();
			//}

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

			return new Size(width, height);
		}

		protected override Size ArrangeOverride(Size finalSizeRaw)
		{
			//Size size = base.ArrangeOverride(finalSize);

			var finalSize = ForceSize(finalSizeRaw);

			var finalSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(finalSize);

			if (ContainerSize != finalSizeDbl)
			{
				if (_content != null)
				{
					_content.Arrange(new Rect(finalSize));
				}

				ContainerSize = finalSizeDbl;
			}

			return finalSize;
		}

		private Size ForceSize(Size finalSize)
		{
			if (finalSize.Width > 1020 && finalSize.Width < 1030 && finalSize.Height > 1020 && finalSize.Height < 1030)
			{
				return new Size(1024, 1024);
			}
			else
			{
				return finalSize;
			}
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = Template.FindName("BitmapGridControl_Content", this) as FrameworkElement;
			if (_content != null)
			{
				Debug.WriteLine($"Found the BitmapGridControl_Content template.");

				// Setup the transform on the content so that we can position the Bitmap to "pull" it left and up so that the
				// portion of the bitmap that is visible corresponds with the requested map coordinates.

				//_content.RenderTransformOrigin = _contentRenderTransformOrigin;
				//_content.RenderTransform = _transformGroup;

				(Canvas, Image) = BuildContentModel(_content);
			}
			else
			{
				Debug.WriteLine($"WARNING: Did not find the BitmapGridControl_Content template.");
			}
		}

		private (Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					//return ca;
					if (ca.Children[0] is Image im)
					{
						return (ca, im);
					}
				}
			}

			throw new InvalidOperationException("Cannot find the bmgcImage element on the BitmapGridControl_Content.");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateScrollInfo()
		{
			if (_scrollOwner != null && !_disableScrollOffsetSync)
			{
				_scrollOwner.InvalidateScrollInfo();
			}
		}

		private void OurDisposeMapSectionImplementation(MapSection mapSection)
		{
			DisposeMapSection?.Invoke(mapSection);
		}

		#endregion

		#region Dependency Properties

		//#region ViewPortSize Dependency Property

		//private static SizeDbl DEFAULT_VIEWPORT_SIZE = new SizeDbl(200, 200);

		//public static readonly DependencyProperty ViewPortSizeProperty = DependencyProperty.Register(
		//			"ViewPortSize", typeof(SizeDbl), typeof(BitmapGridControl),
		//			new FrameworkPropertyMetadata(DEFAULT_VIEWPORT_SIZE, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ViewPortSize_PropertyChanged));

		//private static void ViewPortSize_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	BitmapGridControl c = (BitmapGridControl)o;
		//	var previousValue = (SizeDbl)e.OldValue;
		//	var value = (SizeDbl)e.NewValue;

		//	if (ScreenTypeHelper.IsSizeDblChanged(previousValue, value))
		//	{
		//		//Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing. The old size: {previousValue}, new size: {value}.");

		//		//var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, c._blockSize, KEEP_DISPLAY_SQUARE);
		//		//c._bitmapGrid.CanvasSizeInBlocks = sizeInWholeBlocks;

		//		//c.InvalidateScrollInfo();
		//		c.ViewPortSizeChanged?.Invoke(c, new(previousValue, value));
		//	}
		//	else
		//	{
		//		//Debug.WriteLine($"BitmapGridControl: ViewPortSize is changing by a very small amount, IGNORING. The old size: {previousValue}, new size: {value}.");
		//	}
		//}

		//#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			if (c.SetImageOffset(newValue))
			{
				c.InvalidateScrollInfo();
			}
		}

		private bool SetImageOffset(VectorDbl newValue)
		{
			//Debug.Assert(offset.X >= 0 && offset.Y >= 0, "Setting offset to negative value.");

			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)Image.GetValue(Canvas.LeftProperty), 
				(double)Image.GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				Image.SetValue(Canvas.LeftProperty, invertedValue.X);
				Image.SetValue(Canvas.BottomProperty, invertedValue.Y);

				return true;
			}
			else
			{
				return false;
			}
		}

		private VectorDbl GetTempImageOffset(VectorDbl originalOffset, SizeDbl originalSize, SizeDbl newSize)
		{
			var diff = newSize.Diff(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		#endregion

		#region MapSections Dependency Property

		private static readonly ObservableCollection<MapSection> DEFAULT_MAPSECTIONS_VALUE = new ObservableCollection<MapSection>();

		public static readonly DependencyProperty MapSectionsProperty = DependencyProperty.Register(
					"MapSections", typeof(ObservableCollection<MapSection>), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(DEFAULT_MAPSECTIONS_VALUE, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, MapSections_PropertyChanged));

		private static void MapSections_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var previousValue = (ObservableCollection<MapSection>)e.OldValue;
			var value = (ObservableCollection<MapSection>)e.NewValue;

			if (value != previousValue)
			{
				//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");

				c._bitmapGrid.MapSections = value;

			}
		}

		#endregion

		#region ColorBandSet Dependency Property

		private static readonly ColorBandSet DEFAULT_COLOR_BAND_SET_VALUE = new ColorBandSet();

		public static readonly DependencyProperty ColorBandSetProperty = DependencyProperty.Register(
					"ColorBandSet", typeof(ColorBandSet), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(DEFAULT_COLOR_BAND_SET_VALUE, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColorBandSet_PropertyChanged));

		private static void ColorBandSet_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var previousValue = (ColorBandSet)e.OldValue;
			var value = (ColorBandSet)e.NewValue;

			if (value != previousValue)
			{
				c._bitmapGrid.ColorBandSet = value;
			}
		}

		#endregion

		#region UseEscapeVelocities Dependency Property

		public static readonly DependencyProperty UseEscapeVelocitiesProperty = DependencyProperty.Register(
					"UseEscapeVelocities", typeof(bool), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ColorBandSet_PropertyChanged));

		private static void UseEscapeVelocities_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var value = (bool)e.NewValue;

			c._bitmapGrid.UseEscapeVelocities = value;
		}

		#endregion

		#region HighlightSelectedColorBand Dependency Property

		public static readonly DependencyProperty HighlightSelectedColorBandProperty = DependencyProperty.Register(
					"HighlightSelectedColorBand", typeof(bool), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, HighlightSelectedColorBand_PropertyChanged));

		private static void HighlightSelectedColorBand_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			var value = (bool)e.NewValue;

			c._bitmapGrid.HighlightSelectedColorBand = value;
		}

		#endregion

		#endregion Dependency Properties

		#region Scroll Support

		#region Private Fields - Scroll Support

		///// <summary>
		///// The transform that is applied to the content to scale it by 'ContentScale'.
		///// </summary>
		//private ScaleTransform contentScaleTransform = null;

		///// <summary>
		///// The transform that is applied to the content to offset it by 'ContentOffsetX' and 'ContentOffsetY'.
		///// </summary>
		//private TranslateTransform contentOffsetTransform = null;

		/// <summary>
		/// Enable the update of the content offset as the content scale changes.
		/// This enabled for zooming about a point (google-maps style zooming) and zooming to a rect.
		/// </summary>
		//private bool _enableContentOffsetUpdateFromScale = false;

		/// <summary>
		/// Used to disable syncronization between IScrollInfo interface and ContentOffsetX/ContentOffsetY.
		/// </summary>
		private bool _disableScrollOffsetSync = false;

		/// <summary>
		/// Normally when content offsets changes the content focus is automatically updated.
		/// This syncronization is disabled when 'disableContentFocusSync' is set to 'true'.
		/// When we are zooming in or out we 'disableContentFocusSync' is set to 'true' because 
		/// we are zooming in or out relative to the content focus we don't want to update the focus.
		/// </summary>
		private bool _disableContentFocusSync = false;

		/// <summary>
		/// The width of the viewport in content coordinates, clamped to the width of the content.
		/// </summary>
		private double _constrainedContentViewportWidth = 0.0;

		/// <summary>
		/// The height of the viewport in content coordinates, clamped to the height of the content.
		/// </summary>
		private double _constrainedContentViewportHeight = 0.0;

		#endregion

		#region Private Methods - Scroll Support

		private void UpdateTranslationX()
		{
		}

		private void UpdateContentZoomFocusX()
		{
		}

		private void UpdateTranslationY()
		{
		}

		private void UpdateContentZoomFocusY()
		{
		}

		#endregion

		#region ContentOffsetY Dependency Property

		public static readonly DependencyProperty ContentOffsetYProperty =
				DependencyProperty.Register("ContentOffsetY", typeof(double), typeof(BitmapGridControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetY_PropertyChanged, ContentOffsetY_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetY' property has changed value.
		/// </summary>
		private static void ContentOffsetY_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;

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
			BitmapGridControl c = (BitmapGridControl)d;
			double value = (double)baseValue;
			double minOffsetY = 0.0;
			double maxOffsetY = Math.Max(0.0, c.ContainerSize.Height - c._constrainedContentViewportHeight);
			value = Math.Min(Math.Max(value, minOffsetY), maxOffsetY);
			return value;

			//return baseValue;
		}

		/// <summary>
		/// Get/set the Y offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetY
		{
			get => (double)GetValue(ContentOffsetYProperty);
			set => SetValue(ContentOffsetYProperty, value);
		}

		#endregion

		#region ContentOffsetX Dependency Property

		public static readonly DependencyProperty ContentOffsetXProperty =
				DependencyProperty.Register("ContentOffsetX", typeof(double), typeof(BitmapGridControl),
											new FrameworkPropertyMetadata(0.0, ContentOffsetX_PropertyChanged, ContentOffsetX_Coerce));

		/// <summary>
		/// Event raised when the 'ContentOffsetX' property has changed value.
		/// </summary>
		private static void ContentOffsetX_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;

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
			BitmapGridControl c = (BitmapGridControl)d;
			double value = (double)baseValue;
			double minOffsetX = 0.0;
			double maxOffsetX = Math.Max(0.0, c.ContainerSize.Width - c._constrainedContentViewportWidth);
			value = Math.Min(Math.Max(value, minOffsetX), maxOffsetX);
			return value;

			//return baseValue;
		}

		/// <summary>
		/// Get/set the X offset (in content coordinates) of the view on the content.
		/// </summary>
		public double ContentOffsetX
		{
			get => (double)GetValue(ContentOffsetXProperty);
			set => SetValue(ContentOffsetXProperty, value);
		}



		#endregion

		#endregion Scroll Support

		#region Unused SetCanvasTransform / ReportSizes / CheckImageSize

		private void SetCanvasTransform(PointDbl scale)
		{
			_canvas.RenderTransformOrigin = new Point(0.5, 0.5);
			_canvas.RenderTransform = new ScaleTransform(scale.X, scale.Y);
		}

		public void ReportSizes(string label)
		{
			var controlSize = new SizeInt(ActualWidth, ActualHeight);
			var canvasSize = new SizeInt(_canvas.ActualWidth, _canvas.ActualHeight);
			var imageSize = new Size(Image.ActualWidth, Image.ActualHeight);

			var bitmapSize = new Size(_bitmapGrid.Bitmap.Width, _bitmapGrid.Bitmap.Height);

			var canvasSizeInBlocks = _bitmapGrid.CanvasSizeInBlocks;

			Debug.WriteLine($"At {label}, Control: {controlSize}, Canvas: {canvasSize}, Image: {imageSize}, Bitmap: {bitmapSize}, CanvasSizeInBlocks: {canvasSizeInBlocks}.");
		}

		private bool IsCanvasSizeInWBsReasonable(SizeInt canvasSizeInWholeBlocks)
		{
			var result = !(canvasSizeInWholeBlocks.Width > 50 || canvasSizeInWholeBlocks.Height > 50);
			return result;
		}

		/// <summary>
		/// The position of the canvas' origin relative to the Image Block Data
		/// </summary>
		private void SetCanvasOffset(VectorInt value, double displayZoom)
		{
			//if (value != _offset || Math.Abs(displayZoom - _offsetZoom) > 0.001)
			//{
			//	//Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}. The ScreenCollection Index is {_vm.ScreenCollectionIndex}");
			//	Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}.");
			//	Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

			//	_offset = value;
			//	_offsetZoom = displayZoom;

			//	// For a postive offset, we "pull" the image down and to the left.
			//	var invertedOffset = value.Invert();

			//	var roundedZoom = RoundZoomToOne(displayZoom);
			//	var scaledInvertedOffset = invertedOffset.Scale(1 / roundedZoom);

			//	_image.SetValue(Canvas.LeftProperty, (double)scaledInvertedOffset.X);
			//	_image.SetValue(Canvas.BottomProperty, (double)scaledInvertedOffset.Y);

			//	ReportSizes("SetCanvasOffset");
			//}
		}

		private double RoundZoomToOne(double scale)
		{
			var zoomIsOne = Math.Abs(scale - 1) < 0.001;

			if (!zoomIsOne)
			{
				Debug.WriteLine($"WARNING: MapSectionDisplayControl: Display Zoom is not one.");
			}

			return zoomIsOne ? 1d : scale;
		}
		#endregion
	}
}
