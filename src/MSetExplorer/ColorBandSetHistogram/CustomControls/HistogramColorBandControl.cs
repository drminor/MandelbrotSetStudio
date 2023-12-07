using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

		private readonly bool _useDetailedDebug = true;

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

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Left)
			{
				
			}
			else if(e.Key == Key.Right)
			{

			}
			else
			{
				base.OnKeyDown(e);
			}
		}

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		public ListCollectionView? ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				if (_colorBandsView != null)
				{
					(_colorBandsView as INotifyCollectionChanged).CollectionChanged -= ColorBands_CollectionChanged;
				}

				_colorBandsView = value;

				if (_colorBandsView != null)
				{
					(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBands_CollectionChanged;
				}

				Debug.WriteLine($"The HistogramColorBandControl is calling DrawColorBands on ColorBandsView update.");

				RemoveSelectionLines();
				DrawColorBands(_colorBandsView);
				if (_mouseIsEntered)
				{
					Debug.WriteLine($"The HistogramColorBandControl is calling DrawSelectionLines on ColorBandsView update. (Have Mouse)");

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

					Debug.WriteLine($"The HistogramColorBandControl is calling DrawColorBands on ContentScale update.");

					RemoveSelectionLines();
					DrawColorBands(ColorBandsView);
					if (_mouseIsEntered)
					{
						Debug.WriteLine($"The HistogramColorBandControl is calling DrawSelectionLines on ContentScale update. (Have Mouse)");
						
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

				ClipAndOffset(previousVal, value);
			}
		}

		public ColorBand CurrentColorBand
		{
			get => (ColorBand)GetValue(CurrentColorBandProperty);
			set => SetCurrentValue(CurrentColorBandProperty, value);
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

		private void Handle_MouseLeave(object sender, MouseEventArgs e)
		{
			if (_colorBandIndexInDrag != null)
			{
				var cbsSelectionLine = _selectionLines[_colorBandIndexInDrag.Value];

				cbsSelectionLine.CancelDrag(raiseCancelEvent: false);
				RestoreColorBandRectangles(_colorBandIndexInDrag.Value);
				_colorBandIndexInDrag = null;
			}

			HideSelectionLines();
			_mouseIsEntered = false;
		}

		private void Handle_MouseEnter(object sender, MouseEventArgs e)
		{
			if (ColorBandsView == null) return;

			Debug.WriteLine($"The HistogramColorBandControl is calling DrawSelectionLines on Handle_MouseEnter.");

			if (_selectionLines.Count == 0)
			{
				DrawSelectionLines(_colorBandRectangles);
			}
			else
			{
				ShowSelectionLines();
			}

			_mouseIsEntered = true;
		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var cbsView = ColorBandsView;

			if (cbsView == null)
				return;
			
			var hitPoint = e.GetPosition(Canvas);
			var cbsSelectionLine = GetSelectionLine(hitPoint, _selectionLines, out var selectionLineIndex);

			if (cbsSelectionLine != null && selectionLineIndex != null)
			{
				var testColorBand = cbsView.GetItemAt(selectionLineIndex.Value);
				if (testColorBand is ColorBand cb)
				{
					cbsView.MoveCurrentTo(cb);

					CopyColorBandRectanglesOriginal(_colorBandRectangles, _colorBandRectanglesOriginal);
					cbsSelectionLine.SelectionLineMoved += HandleSelectionLineMoved;

					_colorBandIndexInDrag = cbsSelectionLine.ColorBandIndex;

					HilightColorBandRectangle(cbsSelectionLine.ColorBandIndex, Colors.Black, 200);

					Debug.WriteLine($"Starting Drag. ColorBandIndex = {_colorBandIndexInDrag.Value}. ContentScale: {ContentScale}. PosX: {hitPoint.X}. Original X: {cbsSelectionLine.SelectionLinePosition}.");

					cbsSelectionLine.StartDrag();
				}
				else
				{
					Debug.WriteLine("Could not set the CurrentColorBand while starting to Drag a SelectionLine.");
				}
			}
			else
			{
				var cbr = GetColorBandRectangle(hitPoint, _colorBandRectangles, out var cbrIndex);

				if (cbr != null && cbrIndex != null)
				{
					var testColorBand = cbsView.GetItemAt(cbrIndex.Value);
					if (testColorBand is ColorBand cb)
					{
						cbsView.MoveCurrentTo(cb);
					}
					else
					{
						Debug.WriteLine("Could not set the CurrentColorBand while starting to Drag a SelectionLine.");
					}
				}
			}
		}

		private void HandleSelectionLineMoved(object? sender, CbsSelectionLineMovedEventArgs e)
		{
			_colorBandIndexInDrag = null;

			if (sender is CbsSelectionLine selectionLine)
			{
				selectionLine.SelectionLineMoved -= HandleSelectionLineMoved;
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
			}
			else
			{
				HilightColorBandRectangle(colorBandIndex, Colors.Black, 200);

				var colorBandCutoff = e.NewXPosition / ContentScale.Width;

				var roundedColorBandCutoff = (int)Math.Round(colorBandCutoff);

				if (roundedColorBandCutoff == 0)
				{
					Debug.WriteLine($"WARNING: Setting the Cutoff to zero for ColorBandIndex: {colorBandIndex}.");
				}

				var currentColorBand = CurrentColorBand;

				if (currentColorBand != null)
				{
					currentColorBand.Cutoff = roundedColorBandCutoff;
				}
			}
		}

		private void ColorBands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");
		}

		#endregion

		#region Selection Line Support

		private CbsSelectionLine? GetSelectionLine(Point hitPoint, IList<CbsSelectionLine> cbsSelectionLines, [NotNullWhen(true)] out int? selectionLineIndex)
		{
			CbsSelectionLine? result = null;
			selectionLineIndex = null;

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
						selectionLineIndex = cbsLinePtr;
						result = cbsLine;
					}
				}
			}

			return result;
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

		#endregion

		#region ColorBandRectangle Support

		private GeometryDrawing? GetColorBandRectangle(Point hitPoint, IList<GeometryDrawing> colorBandRectangles, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		{

			for (var i = 0; i < colorBandRectangles.Count; i++)
			{
				var cbr = colorBandRectangles[i];

				if (cbr.Geometry.FillContains(hitPoint))
				{
					colorBandRectangleIndex = i;
					return cbr;
				}
			}

			colorBandRectangleIndex = null;
			return null;
		}

		//private GeometryDrawing? GetColorBandRectangle2(Point hitPoint, IList<GeometryDrawing> colorBandRectangles, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		//{
		//	if (ColorBandsView == null)
		//	{
		//		colorBandRectangleIndex = null;
		//		return null;
		//	}

		//	var posX = hitPoint.X / ContentScale.Width;

		//	_ = FindColorBand(ColorBandsView, posX, out colorBandRectangleIndex);

		//	if (colorBandRectangleIndex == null)
		//	{
		//		return null;
		//	}

		//	var cbr = _colorBandRectangles[colorBandRectangleIndex.Value];

		//	return cbr;
		//}

		//private GeometryDrawing? GetColorBandRectangle_OLD(Point hitPoint, IList<GeometryDrawing> colorBandRectangles, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		//{
		//	GeometryDrawing? result = null;
		//	colorBandRectangleIndex = null;

		//	var rectangleAtHitPoint = GetRectangleUnderMouse(hitPoint);

		//	if (rectangleAtHitPoint != null)
		//	{
		//		for (var cbrPtr = 0; cbrPtr < colorBandRectangles.Count; cbrPtr++)
		//		{
		//			var colorBandRectangle = colorBandRectangles[cbrPtr];

		//			if (colorBandRectangle.Geometry is RectangleGeometry rg)
		//			{
		//				var diffX = rg.Rect.Left - rectangleAtHitPoint.RenderedGeometry.Bounds.Left;

		//				if (ScreenTypeHelper.IsDoubleNearZero(diffX))
		//				{
		//					colorBandRectangleIndex = cbrPtr;
		//					result = colorBandRectangle;
		//				}
		//			}
		//		}
		//	}

		//	return result;
		//}

		//private Rectangle? GetRectangleUnderMouse(Point hitPoint)
		//{
		//	_hitList.Clear();

		//	var hitArea = new EllipseGeometry(hitPoint, 2.0, 2.0);
		//	VisualTreeHelper.HitTest(Canvas, null, HitTestCallBack2, new GeometryHitTestParameters(hitArea));

		//	foreach (Shape item in _hitList)
		//	{
		//		if (item is Rectangle rectangle)
		//		{
		//			var pos = rectangle.RenderedGeometry.Bounds.Left;
		//			var adjustedPos = pos / ContentScale.Width;
		//			Debug.WriteLine($"Got a hit for rectangle at position: {pos} / {adjustedPos}.");

		//			return rectangle;
		//		}
		//	}

		//	return null;
		//}

		//private HitTestResultBehavior HitTestCallBack2(HitTestResult result)
		//{
		//	if (result is GeometryHitTestResult hitTestResult)
		//	{
		//		switch (hitTestResult.IntersectionDetail)
		//		{
		//			case IntersectionDetail.NotCalculated:
		//				return HitTestResultBehavior.Stop;

		//			case IntersectionDetail.Empty:
		//				return HitTestResultBehavior.Stop;

		//			case IntersectionDetail.FullyInside:
		//				if (result.VisualHit is Shape s) _hitList.Add(s);
		//				return HitTestResultBehavior.Continue;

		//			case IntersectionDetail.FullyContains:
		//				if (result.VisualHit is Shape ss) _hitList.Add(ss);
		//				return HitTestResultBehavior.Continue;

		//			case IntersectionDetail.Intersects:
		//				if (result.VisualHit is Shape sss) _hitList.Add(sss);
		//				return HitTestResultBehavior.Continue;

		//			default:
		//				return HitTestResultBehavior.Stop;
		//		}
		//	}
		//	else
		//	{
		//		return HitTestResultBehavior.Stop;
		//	}
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
			var cbLeft = _colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;
			var cbRight = _colorBandRectangles[colorBandIndex + 1].Geometry as RectangleGeometry;

			if (cbLeft != null && cbRight != null)
			{
				var cbLeftOriginal = _colorBandRectanglesOriginal[colorBandIndex];
				var cbRightOriginal = _colorBandRectanglesOriginal[colorBandIndex + 1];

				if (cbLeftOriginal == null | cbRightOriginal == null)
				{
					throw new InvalidOperationException("Found cbLeft and cbRight, but could not find cbLeftOriginal or cbRightOrigianl.");
				}

				_colorBandRectangles[colorBandIndex].Geometry = cbLeftOriginal;
				_colorBandRectangles[colorBandIndex + 1].Geometry = cbRightOriginal;
			}
		}

		private void HilightColorBandRectangle(int colorBandIndex, Color penColor, int interval)
		{
			//var cbr = _colorBandRectangles[colorBandIndex];
			//cbr.Pen = new Pen(new SolidColorBrush(penColor), 1.25);

			//var timer = new DispatcherTimer(
			//	TimeSpan.FromMilliseconds(interval), 
			//	DispatcherPriority.Normal, 
			//	(s, e) =>
			//	{
			//		cbr.Pen = new Pen(Brushes.Transparent, 0);
			//	}, 
			//	Dispatcher);

			//timer.Start();
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
					var scaledAreaWithGap = DrawingHelper.Shorten(scaledArea, 1);
					cbr = DrawingHelper.BuildRectangle(scaledAreaWithGap, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);
				}
				else
				{
					cbr = DrawingHelper.BuildRectangle(scaledArea, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);
				}

				if (cbr.Geometry is RectangleGeometry rg)
				{
					if (rg.Rect.Right == 0)
					{
						Debug.WriteLine("Creating a rectangle with right = 0.");
					}
				}

				_colorBandRectangles.Add(cbr);
				_drawingGroup.Children.Add(cbr);

				curOffset += bandWidth;
			}
		}

		private void DrawSelectionLines(IList<GeometryDrawing> colorBandRectangles)
		{
			RemoveSelectionLines();

			for (var colorBandIndex = 0; colorBandIndex < colorBandRectangles.Count - 1; colorBandIndex++) 
			{
				var gLeft = colorBandRectangles[colorBandIndex].Geometry as RectangleGeometry;
				var gRight = colorBandRectangles[colorBandIndex + 1].Geometry as RectangleGeometry;

				if (gLeft == null || gRight == null)
				{
					throw new InvalidOperationException("DrawSelectionLines. Either the left, right or both ColorBandRectangle geometrys are new RectangleGeometrys");
				}

				// This corresponds to the ColorBands Cutoff
				var xPosition = gLeft.Rect.Right;
				if (gLeft.Rect.Width > 2)
				{
					xPosition += 1;
				}

				if (xPosition < 2)
				{
					Debug.WriteLine($"DrawSelectionLines found an xPosition with a value < 2.");
				}


				var sl = new CbsSelectionLine(_canvas, CB_ELEVATION, CB_HEIGHT, colorBandIndex, xPosition, gLeft, gRight/*, UpdateColorBandWidth*/);
				_selectionLines.Add(sl);
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

		private void HideSelectionLines()
		{
			foreach (var selectionLine in _selectionLines)
			{
				selectionLine.Hide();
			}
		}

		private void ShowSelectionLines()
		{
			foreach (var selectionLine in _selectionLines)
			{
				selectionLine.Show();
			}
		}

		private ColorBand? FindColorBand(ListCollectionView? listCollectionView, double pixelOffset, out int? colorBandIndex)
		{
			ColorBand? result = null;
			colorBandIndex = null;

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return result;
			}

			var prevOffset = 0;
			var curOffset = 0;

			var endPtr = listCollectionView.Count - 1;

			for (var i = 0; i <= endPtr; i++)
			{
				var colorBand = (ColorBand)listCollectionView.GetItemAt(i);
				var bandWidth = i == endPtr ? colorBand.BucketWidth : colorBand.BucketWidth + 1;
				curOffset += bandWidth;

				if (curOffset >= pixelOffset)
				{
					var cd = curOffset - pixelOffset;
					var pd = pixelOffset - prevOffset;

					colorBandIndex = pd > cd ? i - 1 : i;
					result = colorBand;
				}
			}

			return result;
		}

		#endregion

		#region Private Methods - Canvas

		private void ClipAndOffset(RectangleDbl previousValue, RectangleDbl newValue)
		{
			ReportTranslationTransformX(previousValue, newValue);
			_canvasTranslateTransform.X = newValue.Position.X * ContentScale.Width;
		}

		#endregion

		#region Dependency Property Declarations

		public static readonly DependencyProperty CurrentColorBandProperty =
		DependencyProperty.Register("CurrentColorBand", typeof(ColorBand), typeof(HistogramColorBandControl),
									new FrameworkPropertyMetadata(ColorBand.Empty, CurrentColorBandProperty_Changed));

		private static void CurrentColorBandProperty_Changed(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			HistogramColorBandControl c = (HistogramColorBandControl)o;

			var oldColorBand = (ColorBand?)e.OldValue;

			if (oldColorBand != null)
			{
				oldColorBand.PropertyChanged -= c.ColorBand_PropertyChanged;
			}

			var newColorBand = (ColorBand)e.NewValue;

			if (newColorBand != null)
			{
				newColorBand.PropertyChanged += c.ColorBand_PropertyChanged;
			}
		}

		private void ColorBand_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl:CurrentColorBand Prop: {e.PropertyName} is changing.");

				//var foundUpdate = false;

				if (e.PropertyName == nameof(ColorBand.StartColor))
				{
					//foundUpdate = true;
				}
				else if (e.PropertyName == nameof(ColorBand.Cutoff))
				{
					//foundUpdate = true;

					//if (TryGetColorBandIndex(ColorBandsView, cb, out var index))
					//{
					//	UpdateColorBandCutoff(index.Value, CurrentColorBand.Cutoff);
					//}
				}
				else if (e.PropertyName == nameof(ColorBand.BlendStyle))
				{
					//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
					//foundUpdate = true;
				}
				else
				{
					if (e.PropertyName == nameof(ColorBand.EndColor))
					{
						//foundUpdate = true;
					}
				}
			}
			else
			{
				Debug.WriteLine($"HistogramColorBandControl: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");

			}
		}

		private bool UpdateColorBandCutoff(int colorBandIndex, int newCutoff)
		{
			if (colorBandIndex < 0 || colorBandIndex > _colorBandRectangles.Count - 2)
			{
				throw new InvalidOperationException($"DrawColorBands. The ColorBandIndex must be between 0 and {_colorBandRectangles.Count - 1}, inclusive.");
			}

			var selectionLine = _selectionLines[colorBandIndex];

			var updated = selectionLine.UpdatePosition(newCutoff * ContentScale.Width);

			return updated;
		}


		private bool TryGetColorBandIndex(ListCollectionView? colorbandsView, ColorBand cb, [NotNullWhen(true)] out int? index)
		{
			var colorBandsList = colorbandsView as IList<ColorBand>;
			if (colorBandsList == null)
			{
				index = null;
				return false;
			}
			else
			{
				index = colorBandsList.IndexOf(cb);
				return true;
			}
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportTranslationTransformX(RectangleDbl previousValue, RectangleDbl newValue)
		{
			var previousXValue = previousValue.Position.X * ContentScale.Width;
			var newXValue = newValue.Position.X * ContentScale.Width;
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl's CanvasTranslationTransform is being set from {previousXValue} to {newXValue}.");
		}

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
