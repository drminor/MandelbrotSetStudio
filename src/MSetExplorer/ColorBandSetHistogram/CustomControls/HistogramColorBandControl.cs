using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSetExplorer
{
	public delegate bool ColorBandWidthUpdater(int colorBandIndex, double originalXPosition, double newXPosition);

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
		private readonly IList<RectangleGeometry> _colorBandRectanglesOriginal;

		private readonly IList<CbsSelectionLine> _selectionLines;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;

		private SizeDbl _viewportSize;

		private bool _mouseIsEntered;
		private List<Shape> _hitList;
		private int? _colorBandIndexInDrag;

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

			_canvas.MouseEnter += Handle_MouseEnter;
			_canvas.MouseLeave += Handle_MouseLeave;

			_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;


			_colorBandRectangles = new List<GeometryDrawing>();
			_colorBandRectanglesOriginal = new List<RectangleGeometry>();
			_selectionLines = new List<CbsSelectionLine>();

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
			_mouseIsEntered = false;
			_hitList = new List<Shape>();
			_colorBandIndexInDrag = null;
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;
		public event EventHandler<ValueTuple<int, int>>? ColorBandCutoffChanged;

		#endregion

		#region Public Properties

		public ListCollectionView? ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				_colorBandsView = value;

				DrawColorBands(_colorBandsView);
				if (_mouseIsEntered)
				{
					Debug.WriteLine($"The HistogramColorBandControl, because the mouse is on the canvas, is calling DrawSelectionLines on ColorBandsView update.");

					DrawSelectionLines(_colorBandRectangles);
				}
			}
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas.MouseEnter -= Handle_MouseEnter;
				_canvas.MouseLeave -= Handle_MouseLeave;
				_canvas.PreviewMouseLeftButtonDown -= Handle_PreviewMouseLeftButtonDown;

				_canvas = value;

				_canvas.MouseEnter += Handle_MouseEnter;
				_canvas.MouseLeave += Handle_MouseLeave;
				_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;

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

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;

					var extent = GetExtent(ColorBandsView);
					var scaledExtent = extent * ContentScale.Width;
					Canvas.Width = scaledExtent;

					DrawColorBands(ColorBandsView);
					if (_mouseIsEntered)
					{
						Debug.WriteLine($"The HistogramColorBandControl, because the mouse is on the canvas, is calling DrawSelectionLines on ContentScale update.");
						DrawSelectionLines(_colorBandRectangles);
					}
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

		#region Event Handlers

		private void Handle_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (_colorBandIndexInDrag != null)
			{
				var cbsSelectionLine = _selectionLines[_colorBandIndexInDrag.Value];

				cbsSelectionLine.CancelDrag(raiseCancelEvent: false);
				RestoreColorBandRectangles(_colorBandIndexInDrag.Value);
				_colorBandIndexInDrag = null;
			}

			RemoveSelectionLines();
			_mouseIsEntered = false;
		}

		private void Handle_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (ColorBandsView == null) return;

			DrawSelectionLines(_colorBandRectangles);
			_mouseIsEntered = true;
		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var hitPoint = e.GetPosition(Canvas);
			var cbsSelectionLine = GetSelectionLine(hitPoint, _selectionLines);

			if (cbsSelectionLine != null)
			{
				CopyColorBandRectanglesOriginal(_colorBandRectangles, _colorBandRectanglesOriginal);
				cbsSelectionLine.SelectionLineMoved += CbsSelectionLine_SelectionLineMoved;

				_colorBandIndexInDrag = cbsSelectionLine.ColorBandIndex;

				HilightColorBandRectangle(cbsSelectionLine.ColorBandIndex, Colors.Black, 200);

				Debug.WriteLine($"Starting Drag. ColorBandIndex = {_colorBandIndexInDrag.Value}. ContentScale: {ContentScale}. PosX: {hitPoint.X}. Original X: {cbsSelectionLine.SelectionLinePosition}.");

				cbsSelectionLine.StartDrag();
			}
		}

		private void CbsSelectionLine_SelectionLineMoved(object? sender, CbsSelectionLineMovedEventArgs e)
		{
			_colorBandIndexInDrag = null;

			if (sender is CbsSelectionLine selectionLine)
			{
				selectionLine.SelectionLineMoved -= CbsSelectionLine_SelectionLineMoved;
			}

			if (ColorBandsView == null)
			{
				Debug.WriteLine($"The ColorBandsView is null as The HistogramColorBandControl is handling the CbsSelectionLineMoved Event.");
				return;
			}

			var colorBandIndex = e.ColorBandIndex;

			if (colorBandIndex == -1)
			{
				Debug.WriteLine($"The cbIndex = -1 as The HistogramColorBandControl is handling the CbsSelectionLineMoved Event.");
				return;
			}

			if (e.IsPreviewBeingCancelled)
			{
				Debug.WriteLine($"The HistogramColorBandControl is handling the CbsSelectionLineMoved Event. The CbsSelectionMove is being cancelled.");

				RestoreColorBandRectangles(colorBandIndex);

				RemoveSelectionLines();
				DrawSelectionLines(_colorBandRectangles);
			}
			else
			{
				HilightColorBandRectangle(colorBandIndex, Colors.Black, 200);

				//var colorBandCutoff = GetCountValFromXPosition(e.NewXPosition);
				var colorBandCutoff = e.NewXPosition / ContentScale.Width;

				// This is handled by the CbshDisplayViewModel_ColorBandWidthChanged method on the ExplorerWindow class
				Debug.WriteLine($"About to raise the ColorBandCutoffChanged event. ColorBandIndex: {colorBandIndex}. NewXPosition: {e.NewXPosition}, CanvasTranslation: {_canvasTranslateTransform.X}, ScaleContent: {ContentScale.Width}. ColorBandCutoff: {colorBandCutoff}.");

				// Use the ColorBandIndex -- Not the previous index.
				//ColorBandWidthChanged?.Invoke(this, new(colorBandIndex - 1, (int)Math.Round(colorBandCutoff)));
				ColorBandCutoffChanged?.Invoke(this, new(colorBandIndex, (int)Math.Round(colorBandCutoff)));

				//Debug.WriteLine($"Here we will raise the ColorBandWidthChanged event. ColorBandIndex: {colorBandIndex}. ColorBandCutoff: {colorBandCutoff}.");
			}
		}

		#endregion

		#region Selection Line Support

		private CbsSelectionLine? GetSelectionLine(Point hitPoint, IList<CbsSelectionLine> cbsSelectionLines)
		{
			var lineAtHitPoint = GetLineUnderMouse(hitPoint);

			if (lineAtHitPoint != null)
			{
				for (var cbsLinePtr = 0; cbsLinePtr < cbsSelectionLines.Count; cbsLinePtr++)
				{
					var cbsLine = cbsSelectionLines[cbsLinePtr];

					var diffX = cbsLine.SelectionLinePosition - lineAtHitPoint.X1;

					if (ScreenTypeHelper.IsDoubleNearZero(diffX))
					{
						Debug.Assert(cbsLine.ColorBandIndex == cbsLinePtr, "CbsLine.ColorBandIndex Mismatch.");
						return cbsLine;
					}
				}
			}

			return null;
		}

		private Line? GetLineUnderMouse(Point hitPoint)
		{
			_hitList.Clear();

			var hitArea = new EllipseGeometry(hitPoint, 2.0, 2.0);
			VisualTreeHelper.HitTest(Canvas, null, HitTestCallBack, new GeometryHitTestParameters(hitArea));

			foreach (Shape item in _hitList)
			{
				if (item is Line line)
				{
					var adjustedPos = line.X1 / ContentScale.Width;
					Debug.WriteLine($"Got a hit for line at position: {line.X1} / {adjustedPos}.");

					return line;
				}
			}

			return null;
		}

		private HitTestResultBehavior HitTestCallBack(HitTestResult result)
		{
			if (result is GeometryHitTestResult hitTestResult)
			{
				switch (hitTestResult.IntersectionDetail)
				{
					case IntersectionDetail.NotCalculated:
						return HitTestResultBehavior.Stop;

					case IntersectionDetail.Empty:
						return HitTestResultBehavior.Stop;

					case IntersectionDetail.FullyInside:
						if (result.VisualHit is Shape s) _hitList.Add(s);
						return HitTestResultBehavior.Continue;

					case IntersectionDetail.FullyContains:
						if (result.VisualHit is Shape ss) _hitList.Add(ss);
						return HitTestResultBehavior.Continue;

					case IntersectionDetail.Intersects:
						if (result.VisualHit is Shape sss) _hitList.Add(sss);
						return HitTestResultBehavior.Continue;

					default:
						return HitTestResultBehavior.Stop;
				}
			}
			else
			{
				return HitTestResultBehavior.Stop;
			}
		}

		//private double GetCountValFromXPosition(double xPosition)
		//{
		//	//Debug.Assert(_canvasTranslateTransform.X <= 0, "The CanvasTranslateTransform is positive.");
		//	//var origin = _canvasTranslateTransform.X;
		//	var pixelOffset = xPosition; // origin + xPosition;
		//	var pixelOffsetScaled = pixelOffset / ContentScale.Width;

		//	return pixelOffsetScaled;
		//}

		private void CopyColorBandRectanglesOriginal(IList<GeometryDrawing> source, IList<RectangleGeometry> dest)
		{
			dest.Clear();

			foreach (var cbr in source)
			{
				if (cbr.Geometry is RectangleGeometry rg)
				{
					dest.Add(new RectangleGeometry(rg.Rect));
				}
			}
		}

		private void RestoreColorBandRectangles(int colorBandIndex)
		{
			//var cbLeft = _colorBandRectangles[colorBandIndex - 1].Geometry as RectangleGeometry;
			//var cbRight = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;

			var cbLeft = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;
			var cbRight = _colorBandRectangles[colorBandIndex + 1].Geometry as RectangleGeometry;


			if (cbLeft != null && cbRight != null)
			{
				//var cbLeftOriginal = _colorBandRectanglesOriginal[colorBandIndex - 1];
				//var cbRightOriginal = _colorBandRectanglesOriginal[colorBandIndex];

				var cbLeftOriginal = _colorBandRectanglesOriginal[colorBandIndex];
				var cbRightOriginal = _colorBandRectanglesOriginal[colorBandIndex + 1];

				if (cbLeftOriginal == null | cbRightOriginal == null)
				{
					throw new InvalidOperationException("Found cbLeft and cbRight, but could not find cbLeftOriginal or cbRightOrigianl.");
				}

				//_colorBandRectangles[colorBandIndex - 1].Geometry = cbLeftOriginal;
				//_colorBandRectangles[colorBandIndex].Geometry = cbRightOriginal;

				_colorBandRectangles[colorBandIndex].Geometry = cbLeftOriginal;
				_colorBandRectangles[colorBandIndex + 1].Geometry = cbRightOriginal;
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

			var endPtr = listCollectionView.Count - 1;

			for (var i = 0; i <= endPtr; i++)
			{
				var colorBand = (ColorBand) listCollectionView.GetItemAt(i);

				//var bandWidth = i == endPtr ? colorBand.BucketWidth : colorBand.BucketWidth + 1;
				var bandWidth = colorBand.BucketWidth;
				
				if (i < endPtr)
				{
					bandWidth += 1;
				}

				var area = new RectangleDbl(new PointDbl(curOffset, CB_ELEVATION), new SizeDbl(bandWidth, CB_HEIGHT));
				var scaledArea = area.Scale(scaleSize);

				GeometryDrawing cbr;

				// Reduce the width by a single pixel to 'high-light' the boundary.
				if (scaledArea.Width > 2)
				{
					var scaledAreaWithGap = Shorten(scaledArea, 1);
					cbr = DrawingHelper.BuildRectangle(scaledAreaWithGap, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);
				}
				else
				{
					cbr = DrawingHelper.BuildRectangle(scaledArea, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);
				}

				_colorBandRectangles.Add(cbr);
				_drawingGroup.Children.Add(cbr);

				curOffset += bandWidth;
			}
		}

		private void HilightColorBandRectangle(int colorBandIndex, Color penColor, int interval)
		{
			var cbr = _colorBandRectangles[colorBandIndex];
			cbr.Pen = new Pen(new SolidColorBrush(penColor), 0.75);

			var timer = new DispatcherTimer(
				TimeSpan.FromMilliseconds(interval), 
				DispatcherPriority.Normal, 
				(s, e) =>
				{
					cbr.Pen = new Pen(Brushes.Transparent, 0);
				}, 
				Dispatcher);

			timer.Start();
		}


		private void DrawSelectionLines(IList<GeometryDrawing> colorBandRectangles)
		{
			RemoveSelectionLines();

			for (var colorBandIndex = 0; colorBandIndex < colorBandRectangles.Count - 1; colorBandIndex++) 
			{
				var cbr = colorBandRectangles[colorBandIndex];
				var g = cbr.Geometry as RectangleGeometry;

				if (g != null)
				{
					//var xPosition =  g.Rect.Left;
					// Don't use the Starting Cutoff, use the Cutoff
					var xPosition = g.Rect.Right;

					var sl = new CbsSelectionLine(_canvas, CB_ELEVATION, CB_HEIGHT, colorBandIndex, xPosition, UpdateColorBandWidth);
					_selectionLines.Add(sl);
				}
			}
		}

		private int GetExtent(ListCollectionView? listCollectionView)
		{
			if (listCollectionView == null)
			{
				return 0;
			}

			var cnt = listCollectionView.Count;

			if (cnt < 2)
			{
				return 0;
			}

			var d = listCollectionView.GetItemAt(cnt - 1) as ColorBand;

			if (d != null)
			{
				return d.Cutoff;
			}
			else
			{
				return 0;
			}
		}

		private void RemoveColorBandRectangles()
		{
			foreach (var colorBandRectangle in _colorBandRectangles)
			{
				_drawingGroup.Children.Remove(colorBandRectangle);
			}

			_colorBandRectangles.Clear();
		}

		private void RemoveSelectionLines()
		{
			foreach (var selectionLine in _selectionLines)
			{
				selectionLine.TearDown();
				//_canvas.Children.Remove(selectionLine);
			}

			_selectionLines.Clear();
		}

		private bool UpdateColorBandWidth(int colorBandIndex, double originalXPosition, double newXPosition)
		{
			if (colorBandIndex < 0 || colorBandIndex > _colorBandRectangles.Count - 2)
			{
				throw new InvalidOperationException($"The ColorBandIndex must be between 0 and {_colorBandRectangles.Count - 1}, inclusive.");
			}

			var updated = false;

			//var cbLeft = _colorBandRectangles[colorBandIndex - 1].Geometry as RectangleGeometry;
			//var cbRight = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;

			var cbLeft = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;
			var cbRight = _colorBandRectangles[colorBandIndex + 1].Geometry as RectangleGeometry;

			if (cbLeft != null && cbRight != null)
			{
				//var cbLeftOriginal = _colorBandRectanglesOriginal[colorBandIndex - 1];
				//var cbRightOriginal = _colorBandRectanglesOriginal[colorBandIndex];

				var cbLeftOriginal = _colorBandRectanglesOriginal[colorBandIndex];
				var cbRightOriginal = _colorBandRectanglesOriginal[colorBandIndex + 1];

				if (cbLeftOriginal == null || cbRightOriginal == null)
				{
					throw new InvalidOperationException("cbLeftOriginal or cbRightOriginal is null");
				}

				var amount = newXPosition - originalXPosition;

				if (amount < 0)
				{
					amount = amount * -1;
					if (cbLeftOriginal.Rect.Width > amount && cbRightOriginal.Rect.X > amount)
					{
						cbLeft.Rect = Shorten(cbLeftOriginal.Rect, amount);
						cbRight.Rect = MoveRectLeft(cbRightOriginal.Rect, amount);
						updated = true;
					}
				}
				else
				{
					if (cbRightOriginal.Rect.Width > amount)
					{
						cbLeft.Rect = Lengthen(cbLeftOriginal.Rect, amount);
						cbRight.Rect = MoveRectRight(cbRightOriginal.Rect, amount);
						updated = true;
					}
				}
			}

			return updated;
		}

		Rect Shorten(Rect r, double amount)
		{
			var result = new Rect(r.Location, new Size(r.Width - amount, r.Height));
			return result;
		}

		Rect MoveRectLeft(Rect r, double amount)
		{
			var result = new Rect(new Point(r.X - amount, r.Y), new Size(r.Width + amount, r.Height));
			return result;
		}

		Rect Lengthen(Rect r, double amount)
		{
			var result = new Rect(r.Location, new Size(r.Width + amount, r.Height));
			return result;
		}

		Rect MoveRectRight(Rect r, double amount)
		{
			var result = new Rect(new Point(r.X + amount, r.Y), new Size(r.Width - amount, r.Height));
			return result;
		}

		RectangleDbl Shorten(RectangleDbl rd, double amount)
		{
			var result = new RectangleDbl(rd.Position, new SizeDbl(rd.Width - amount, rd.Height));
			return result;
		}

		//private int FindColorBand(ListCollectionView? listCollectionView, double pixelOffset)
		//{
		//	var result = -1;

		//	if (listCollectionView == null || listCollectionView.Count < 2)
		//	{
		//		return result;
		//	}

		//	var prevOffset = 0;
		//	var curOffset = 0;

		//	var endPtr = listCollectionView.Count - 1;

		//	for (var i = 0; i <= endPtr; i++)
		//	{
		//		var colorBand = (ColorBand)listCollectionView.GetItemAt(i);
		//		var bandWidth = i == endPtr ? colorBand.BucketWidth : colorBand.BucketWidth + 1;
		//		curOffset += bandWidth;

		//		if (curOffset >= pixelOffset)
		//		{
		//			var cd = curOffset - pixelOffset;
		//			var pd = pixelOffset - prevOffset;

		//			result = pd > cd ? i - 1 : i;

		//			return result;
		//		}
		//	}

		//	return endPtr;
		//}

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
