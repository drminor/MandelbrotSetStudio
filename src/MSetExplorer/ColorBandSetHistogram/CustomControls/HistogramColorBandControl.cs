using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MSetExplorer
{
	public class HistogramColorBandControl : ContentControl, IContentScaler
	{
		#region Private Fields 

		private const int CB_ELEVATION = 0; // Distance from the top of the Grid Row containing the Color Band Rectangles and the top of each Color Band Rectangle
		private const int CB_HEIGHT = 58;   // Height of each Color Band Rectangle

		private readonly static bool CLIP_IMAGE_BLOCKS = false;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Image _image;

		private ListCollectionView? _colorBandsView;

		private ImageSource _drawingimageSource;
		private readonly DrawingGroup _drawingGroup;
		private readonly IList<GeometryDrawing> _colorBandRectangles;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;

		private SizeDbl _viewportSize;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		static HistogramColorBandControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramColorBandControl), new FrameworkPropertyMetadata(typeof(HistogramColorBandControl)));
		}

		public HistogramColorBandControl()
		{
			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_image = new Image();

			_canvas.MouseWheel += MouseWheelHandler;

			_colorBandRectangles = new List<GeometryDrawing>();
			_drawingGroup = new DrawingGroup();
			_drawingimageSource = new DrawingImage(_drawingGroup);

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_contentScale = new SizeDbl(1);
			_translationAndClipSize = new RectangleDbl();

			_viewportSize = new SizeDbl();
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		public ListCollectionView? ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				_colorBandsView = value;
				DrawColorBands(_colorBandsView);
			}
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas.MouseWheel -= MouseWheelHandler;
				_canvas = value;
				_canvas.MouseWheel += MouseWheelHandler;

				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.RenderTransform = _canvasRenderTransform;
			}
		}

		public Image Image
		{
			get => _image;
			set
			{
				if (_image != value)
				{
					_image = value;
					_image.Source = _drawingimageSource;

					//_image.Source = HistogramImageSource;
					_image.SetValue(Panel.ZIndexProperty, 20);

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The HistogramColorBandControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
				}
			}
		}

		//public ImageSource HistogramImageSource
		//{
		//	get => (ImageSource)GetValue(HistogramImageSourceProperty);
		//	set => SetCurrentValue(HistogramImageSourceProperty, value);
		//}

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;
					DrawColorBands(ColorBandsView);
				}
			}
		}

		public RectangleDbl TranslationAndClipSize
		{
			get => _translationAndClipSize; 
			set
			{
				var previousVal = _translationAndClipSize;
				_translationAndClipSize = value;

				//LogicalViewportSize = ClipAndOffset(previousVal, value);
				ClipAndOffset(previousVal, value);
			}
		}

		//public SizeDbl LogicalViewportSize
		//{
		//	get => _logicalViewportSize;
		//	set
		//	{
		//		if (ScreenTypeHelper.IsSizeDblChanged(value, _logicalViewportSize))
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}.");
		//			_logicalViewportSize = value;
		//		}
		//	}
		//}

		#endregion

		#region Private Methods - Control

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_ourContent.Measure(availableSize);

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

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			return result;
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = base.ArrangeOverride(finalSize);

			if (childSize != finalSize) Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;

			if (canvas.ActualWidth != finalSize.Width)
			{
				canvas.Width = finalSize.Width;
			}

			if (canvas.ActualHeight != finalSize.Height)
			{
				canvas.Height = finalSize.Height;
			}

			ViewportSize = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			return finalSize;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();
				(Canvas, Image) = BuildContentModel(_ourContent);
			}
			else
			{
				throw new InvalidOperationException("Did not find the HistogramColorBandControl_Content template.");
			}
		}

		private (Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					if (ca.Children[0] is Image im)
					{
						return (ca, im);
					}
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the HistogramColorBandControl's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region HistogramImageSource Dependency Property

		//public static readonly DependencyProperty HistogramImageSourceProperty = DependencyProperty.Register(
		//			"HistogramImageSource", typeof(ImageSource), typeof(HistogramColorBandControl),
		//			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, HistogramImageSource_PropertyChanged));

		//private static void HistogramImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//var c = (HistogramColorBandControl)o;
		//	//var previousValue = (ImageSource)e.OldValue;
		//	//var value = (ImageSource)e.NewValue;

		//	//if (value != previousValue)
		//	//{
		//	//	c.Image.Source = value;
		//	//}
		//}

		#endregion

		#region Event Handlers
		private void MouseWheelHandler(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			if (e.Delta < 0)
			{
				UpdateColorBandWidth(9, -5);
			}
			else
			{
				UpdateColorBandWidth(9, 5);
			}
		}

		#endregion

		#region Private Methods - ColorBandSet View

		private void DrawColorBands(ListCollectionView? listCollectionView)
		{
			RemoveColorBandRectangles();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			var scaleSize = new SizeDbl(ContentScale.Width, 1);

			Debug.WriteLine($"The scale is {scaleSize} on DrawColorBands.");

			var curOffset = 0;
			int bandWidth;

			var endPtr = listCollectionView.Count - 1;

			for (var i = 0; i <= endPtr; i++)
			{
				var colorBand = (ColorBand) listCollectionView.GetItemAt(i);

				bandWidth = i == endPtr ? colorBand.BucketWidth : colorBand.BucketWidth + 1;

				bandWidth -= 1; // Leave a gap

				var area = new RectangleDbl(new PointDbl(curOffset, CB_ELEVATION), new SizeDbl(bandWidth, CB_HEIGHT));
				var scaledArea = area.Scale(scaleSize);

				var r = DrawingHelper.BuildRectangle(scaledArea, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);

				_colorBandRectangles.Add(r);

				_drawingGroup.Children.Add(r);

				curOffset += bandWidth + 1; // Cover the gap.
			}
		}

		//private int GetExtent(ColorBandSet colorBandSet, out int endPtr)
		//{
		//	var result = colorBandSet.Count < 2 ? 0 : colorBandSet.HighCutoff;
		//	endPtr = colorBandSet.Count < 2 ? 0 : colorBandSet.Count - 1;
		//	return result;
		//}

		private void RemoveColorBandRectangles()
		{
			foreach (var colorBandRectangle in _colorBandRectangles)
			{
				_drawingGroup.Children.Remove(colorBandRectangle);
			}

			_colorBandRectangles.Clear();
		}

		private void UpdateColorBandWidth(int colorBandIndex, double newValue)
		{
			if (colorBandIndex > 0)
			{
				var cbLeft = _colorBandRectangles[colorBandIndex - 1].Geometry as RectangleGeometry;
				var cbRight = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;

				if (cbLeft != null && cbRight != null)
				{
					if (newValue < 0)
					{
						var currentRect = cbLeft.Rect;
						cbLeft.Rect = new Rect(currentRect.Location, new Size(currentRect.Width - 1, currentRect.Height));

						currentRect = cbRight.Rect;
						cbRight.Rect = new Rect(currentRect.Location, new Size(currentRect.Width + 1, currentRect.Height));
					}
					else
					{
						var currentRect = cbLeft.Rect;
						cbLeft.Rect = new Rect(currentRect.Location, new Size(currentRect.Width + 1, currentRect.Height));

						currentRect = cbRight.Rect;
						cbRight.Rect = new Rect(currentRect.Location, new Size(currentRect.Width - 1, currentRect.Height));
					}
				}
			}
		}


		#endregion

		#region Private Methods - Canvas

		private void ClipAndOffset(RectangleDbl previousValue, RectangleDbl newValue)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl's {nameof(TranslationAndClipSize)} is being set from {previousValue} to {newValue}.");

			ReportTranslationTransformX(previousValue, newValue);
			_canvasTranslateTransform.X = newValue.Position.X * ContentScale.Width;
		}

		[Conditional("DEBUG2")]
		private void ReportTranslationTransformX(RectangleDbl previousValue, RectangleDbl newValue)
		{
			var previousXValue = previousValue.Position.X * ContentScale.Width;
			var newXValue = newValue.Position.X* ContentScale.Width;
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl's CanvasTranslationTransform is being set from {previousXValue} to {newXValue}.");
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void CheckThatImageIsAChildOfCanvas(Image image, Canvas canvas)
		{
			foreach (var v in canvas.Children)
			{
				if (v == image)
				{
					return;
				}
			}

			throw new InvalidOperationException("The image is not a child of the canvas.");
		}

		#endregion
	}
}
