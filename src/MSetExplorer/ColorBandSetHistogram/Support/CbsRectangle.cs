using MSS.Types;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbsRectangle
	{
		#region Private Fields

		private static readonly Brush _selectionLineBrush = DrawingHelper.BuildSelectionDrawingBrush();

		private Canvas _canvas;

		private readonly Rectangle _rectangle;

		//private double _originalXPosition;
		//private double _originalWidth;

		private double _xPosition;
		private double _width;

		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;

		private double _cbElevation;
		private double _cbHeight;

		//private double _scaleSize;

		//private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, Canvas canvas, double elevation, double height, SizeDbl scaleSize)
		{
			_canvas = canvas;
			ColorBandIndex = colorBandIndex;

			//_originalXPosition = xPosition;
			_xPosition = xPosition;
			
			//_originalWidth = width;
			_width = width;

			//_startColor = startColor;
			//_endColor = endColor;

			_cbElevation = elevation;
			_cbHeight = height;

			var area = new RectangleDbl(new PointDbl(xPosition, elevation), new SizeDbl(width, height));
			var scaledArea = area.Scale(scaleSize);

			_rectangle = BuildRectangle(scaledArea, startColor, endColor);

			_canvas.Children.Add(_rectangle);
			_rectangle.SetValue(Canvas.LeftProperty, scaledArea.X1);
			_rectangle.SetValue(Canvas.BottomProperty, scaledArea.Y1);
			_rectangle.SetValue(Panel.ZIndexProperty, 20);

			RectangleGeometry = BuildRectangleGeometry(scaledArea);
		}

		private Rectangle BuildRectangle(RectangleDbl scaledArea, ColorBandColor startColor, ColorBandColor endColor)
		{
			// Reduce the width by a single pixel to 'high-light' the boundary.
			var widthWithGap = scaledArea.Width > 2 ? scaledArea.Width - 1: scaledArea.Width;

			var result = new Rectangle()
			{
				Fill = DrawingHelper.BuildBrush(startColor, endColor, true),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Focusable = true,
				Width = widthWithGap,
				Height = scaledArea.Height,
				IsHitTestVisible = true,
			};

			return result;
		}

		#endregion

		#region Public Properties

		public Rectangle Rectangle => _rectangle;

		public RectangleGeometry RectangleGeometry;

		public Brush Stroke
		{
			get => _rectangle.Stroke;
			set => _rectangle.Stroke = value;
		}

		public int ColorBandIndex { get; init; }

		public double XPosition
		{
			get => _xPosition;
			set
			{
				if (value != _xPosition)
				{
					_xPosition = value;
					_rectangle.SetValue(Canvas.LeftProperty, value);
				}
			}
		}

		public double Width
		{
			get => _width;
			set
			{
				if (value != _width)
				{
					_width = value;
					_rectangle.Width = _width;
				}
			}
		}

		public double CbElevation
		{
			get => _cbElevation;
			set
			{
				if (value != _cbElevation)
				{
					_cbElevation = value;

					_rectangle.Height = CbElevation + CbHeight;
					_rectangle.SetValue(Canvas.BottomProperty, CbElevation);
				}
			}
		}

		public double CbHeight
		{
			get => _cbHeight;
			set
			{
				if (value != _cbHeight)
				{
					_cbHeight = value;
					_rectangle.Height = CbHeight;
				}
			}
		}

		#endregion

		#region Public Methods

		public void TearDown()
		{
			try
			{
				if (_canvas != null)
				{
					_canvas.Children.Remove(_rectangle);
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in TearDown.");
			}
		}

		public void Hide()
		{
			try
			{
				if (_canvas != null)
				{
					_rectangle.Fill.Opacity = 0;
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in Hide.");
			}
		}

		public void Show()
		{
			try
			{
				if (_canvas != null)
				{
					_rectangle.Stroke.Opacity = 1;
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in Show.");
			}
		}

		#endregion

		#region Event Handlers

		#endregion

		#region Private Methods

		private RectangleGeometry BuildRectangleGeometry(RectangleDbl scaledArea)
		{
			var scaledAreaWithGap = scaledArea.Width > 2 ? DrawingHelper.Shorten(scaledArea, 1) : scaledArea;

			var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(scaledAreaWithGap));

			if (cbRectangle.Rect.Right == 0)
			{
				Debug.WriteLine("Creating a rectangle with right = 0.");
			}

			return cbRectangle;
		}

		#endregion

		#region Diag

		#endregion
	}

}
