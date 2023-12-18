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

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		//private double _xPosition;
		//private double _yPosition;

		//private double _width;
		//private double _height;

		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;
		//private bool _blend;

		private Canvas _canvas;
		//private SizeDbl _scaleSize;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, double xPosition, double yPosition, double width, double height, ColorBandColor startColor, ColorBandColor endColor, bool blend, Canvas canvas, SizeDbl scaleSize)
		{
			_canvas = canvas;
			ColorBandIndex = colorBandIndex;

			//_xPosition = xPosition;
			//_yPosition = yPosition;

			//_width = width;
			//_height = height;

			//_startColor = startColor;
			//_endColor = endColor;

			//_scaleSize = scaleSize;

			_geometry = BuildRectangleGeometry(xPosition, yPosition, width, height, scaleSize);
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);

			_canvas.Children.Add(_rectanglePath);
			//_rectanglePath.SetValue(Canvas.LeftProperty, _geometry.Rect.Left);
			//_rectanglePath.SetValue(Canvas.TopProperty, _geometry.Rect.Top);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);
		}

		#endregion

		#region Public Properties

		public Shape Rectangle => _rectanglePath; 
		public RectangleGeometry RectangleGeometry => _geometry;

		public Brush Stroke
		{
			get => _rectanglePath.Stroke;
			set => _rectanglePath.Stroke = value;
		}

		public double StrokeThickness
		{
			get => _rectanglePath.StrokeThickness;
			set => _rectanglePath.StrokeThickness = value;
		}

		public int ColorBandIndex { get; init; }

		//public double XPosition
		//{
		//	get => _xPosition;
		//	set
		//	{
		//		if (value != _xPosition)
		//		{
		//			_xPosition = value;
		//			Rectangle.SetValue(Canvas.LeftProperty, value);
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
		//			Rectangle.Width = _width;
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

		//			Rectangle.Height = _cbElevation + CbHeight;
		//			Rectangle.SetValue(Canvas.TopProperty, _cbElevation);
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
		//			Rectangle.Height = _cbHeight;
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

		public void TearDown()
		{
			try
			{
				if (_canvas != null)
				{
					_canvas.Children.Remove(Rectangle);
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in TearDown.");
			}
		}

		//public void Hide()
		//{
		//	try
		//	{
		//		if (_canvas != null)
		//		{
		//			Rectangle.Fill.Opacity = 0;
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
		//			Rectangle.Stroke.Opacity = 1;
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

		private Shape BuildRectanglePath(RectangleGeometry area, ColorBandColor startColor, ColorBandColor endColor, bool blend)
		{
			var result = new Path()
			{
				Fill = DrawingHelper.BuildBrush(startColor, endColor, blend),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Data = area,
				Focusable = true,
				IsHitTestVisible = true
			};

			return result;
		}

		#endregion

		#region Diag

		#endregion
	}
}
