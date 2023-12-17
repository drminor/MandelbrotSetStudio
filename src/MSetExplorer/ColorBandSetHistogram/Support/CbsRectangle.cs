using MSS.Types;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static ScottPlot.Plottable.PopulationPlot;

namespace MSetExplorer
{
	internal class CbsRectangle
	{
		#region Private Fields

		private Canvas _canvas;

		private readonly Rectangle _rectangle;

		//private double _originalXPosition;
		//private double _originalWidth;

		private double _xPosition;
		private double _width;

		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;
		//private bool _blend;

		private double _cbElevation;
		private double _cbHeight;

		private SizeDbl _scaleSize;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend, Canvas canvas, double elevation, double height, SizeDbl scaleSize)
		{
			_canvas = canvas;
			//ColorBandIndex = colorBandIndex;

			//_originalXPosition = xPosition;
			_xPosition = xPosition;
			
			//_originalWidth = width;
			_width = width;

			//_startColor = startColor;
			//_endColor = endColor;

			_cbElevation = elevation;
			_cbHeight = height;

			_scaleSize = scaleSize;

			RectangleGeometry = BuildRectangleGeometry(xPosition, elevation, width, height, scaleSize);

			_rectangle = BuildRectangle(RectangleGeometry, startColor, endColor, blend);

			_canvas.Children.Add(_rectangle);
			_rectangle.SetValue(Canvas.LeftProperty, RectangleGeometry.Rect.Left);
			_rectangle.SetValue(Canvas.TopProperty, RectangleGeometry.Rect.Top);
			_rectangle.SetValue(Panel.ZIndexProperty, 20);
		}

		#endregion

		#region Public Properties

		public Rectangle Rectangle => _rectangle;

		public RectangleGeometry RectangleGeometry { get; set; }

		//public RectangleGeometry RectangleGeometry => _rectangle.RenderedGeometry as RectangleGeometry ?? new RectangleGeometry();

		public Brush Stroke
		{
			get => _rectangle.Stroke;
			set => _rectangle.Stroke = value;
		}

		public double StrokeThickness
		{
			get => _rectangle.StrokeThickness;
			set => _rectangle.StrokeThickness = value;
		}

		//public int ColorBandIndex { get; init; }

		//public double XPosition
		//{
		//	get => _xPosition;
		//	set
		//	{
		//		if (value != _xPosition)
		//		{
		//			_xPosition = value;
		//			_rectangle.SetValue(Canvas.LeftProperty, value);
		//		}
		//	}
		//}

		//public double Width
		//{
		//	get => _width;
		//	set
		//	{
		//		if (value != _width)
		//		{
		//			_width = value;
		//			_rectangle.Width = _width;
		//		}
		//	}
		//}

		//public double CbElevation
		//{
		//	get => _cbElevation;
		//	set
		//	{
		//		if (value != _cbElevation)
		//		{
		//			_cbElevation = value;

		//			_rectangle.Height = _cbElevation + CbHeight;
		//			_rectangle.SetValue(Canvas.TopProperty, _cbElevation);
		//		}
		//	}
		//}

		//public double CbHeight
		//{
		//	get => _cbHeight;
		//	set
		//	{
		//		if (value != _cbHeight)
		//		{
		//			_cbHeight = value;
		//			_rectangle.Height = _cbHeight;
		//		}
		//	}
		//}

		//public SizeDbl ScaleSize
		//{
		//	get => _scaleSize;
		//	set
		//	{
		//		if (value != _scaleSize)
		//		{
		//			_scaleSize = value;
		//			// TODO: Update the position and size.
		//		}
		//	}
		//}

		#endregion

		#region Public Methods

		//public void TearDown()
		//{
		//	try
		//	{
		//		if (_canvas != null)
		//		{
		//			_canvas.Children.Remove(_rectangle);
		//		}
		//	}
		//	catch
		//	{
		//		Debug.WriteLine("CbsSelectionLine encountered an exception in TearDown.");
		//	}
		//}

		//public void Hide()
		//{
		//	try
		//	{
		//		if (_canvas != null)
		//		{
		//			_rectangle.Fill.Opacity = 0;
		//		}
		//	}
		//	catch
		//	{
		//		Debug.WriteLine("CbsSelectionLine encountered an exception in Hide.");
		//	}
		//}

		//public void Show()
		//{
		//	try
		//	{
		//		if (_canvas != null)
		//		{
		//			_rectangle.Stroke.Opacity = 1;
		//		}
		//	}
		//	catch
		//	{
		//		Debug.WriteLine("CbsSelectionLine encountered an exception in Show.");
		//	}
		//}

		#endregion

		#region Event Handlers

		#endregion

		#region Private Methods

		private RectangleGeometry BuildRectangleGeometry(double xPosition, double elevation, double width, double height, SizeDbl scaleSize)
		{
			var area = new RectangleDbl(new PointDbl(xPosition, elevation), new SizeDbl(width, height));
			var scaledArea = area.Scale(scaleSize);

			var scaledAreaWithGap = scaledArea.Width > 2 ? DrawingHelper.Shorten(scaledArea, 1) : scaledArea;

			var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(scaledAreaWithGap));

			if (cbRectangle.Rect.Right == 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, "Creating a rectangle with right = 0.");
			}

			return cbRectangle;
		}

		private Rectangle BuildRectangle(RectangleGeometry area, ColorBandColor startColor, ColorBandColor endColor, bool blend)
		{
			var result = new Rectangle()
			{
				Fill = DrawingHelper.BuildBrush(startColor, endColor, blend),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Focusable = true,
				Width = area.Rect.Width,
				Height = area.Rect.Height,
				IsHitTestVisible = true,
			};

			return result;
		}

		#endregion

		#region Diag

		#endregion
	}
}
