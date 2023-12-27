using MSS.Types;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static ScottPlot.Plottable.PopulationPlot;

namespace MSetExplorer
{
	internal class CbsRectangle
	{
		#region Private Fields

		private static readonly Pen DEFAULT_PEN = new Pen(new SolidColorBrush(Colors.Transparent), 0);
		private static readonly Pen IS_CURRENT_PEN = new Pen(new SolidColorBrush(Colors.DarkRed), 1);
		private static readonly Pen IS_SELECTED_PEN = new Pen(new SolidColorBrush(Colors.DarkCyan), 2.5);

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		//private double _xPosition;
		//private double _yPosition;

		//private double _width;
		//private double _height;

		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;
		//private bool _blend;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		//private SizeDbl _scaleSize;
		private IsSelectedChanged _isSelectedChanged;

		private bool _isCurrent;
		private bool _isSelected;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		//				var cbsRectangle = new CbsRectangle(i, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel, _canvas, CbRectangleIsSelectedChanged);


		public CbsRectangle(int colorBandIndex, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend,
			ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChanged isSelectedChanged)
		{
			_isCurrent = false;
			_isSelected = false;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_canvas = canvas;
			ColorBandIndex = colorBandIndex;

			//_xPosition = xPosition;
			//_yPosition = yPosition;

			//_width = width;
			//_height = height;

			//_startColor = startColor;
			//_endColor = endColor;

			//_scaleSize = scaleSize;

			_isSelectedChanged = isSelectedChanged;

			var yPosition = _colorBandLayoutViewModel.CbrElevation;
			var height = _colorBandLayoutViewModel.CbrHeight;
			var contentScale = _colorBandLayoutViewModel.ContentScale;

			_geometry = BuildRectangleGeometry(xPosition, yPosition, width, height, contentScale);
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);

			_rectanglePath.MouseUp += _rectanglePath_MouseUp;

			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);
		}

		private void _rectanglePath_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			_isSelectedChanged(ColorBandIndex, !IsSelected, shiftKeyPressed, controlKeyPressed);

			//IsSelected = !IsSelected;
		}

		#endregion

		#region Public Properties

		public Shape Rectangle => _rectanglePath; 
		public RectangleGeometry RectangleGeometry => _geometry;

		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (value != _isCurrent)
				{
					_isCurrent = value;
					SetRectangleStroke();
				}
			}
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					SetRectangleStroke();
				}
			}
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
			var area = new RectangleDbl(new PointDbl(xPosition, elevation + 1), new SizeDbl(width, height - 2));
			var scaledArea = area.Scale(scaleSize);

			//var scaledAreaWithGap = scaledArea.Width > 2 ? DrawingHelper.Shorten(scaledArea, 1) : scaledArea;
			//var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(scaledAreaWithGap));

			var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(scaledArea));


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
				//Focusable = true,
				IsHitTestVisible = true
			};

			return result;
		}

		private void SetRectangleStroke()
		{
			if (_isCurrent)
			{
				if (_isSelected)
				{
					_rectanglePath.Stroke = IS_SELECTED_PEN.Brush;
					_rectanglePath.StrokeThickness = IS_SELECTED_PEN.Thickness;
				}
				else
				{
					_rectanglePath.Stroke = IS_CURRENT_PEN.Brush;
					_rectanglePath.StrokeThickness = IS_CURRENT_PEN.Thickness;
				}
			}
			else
			{
				if (_isSelected)
				{
					_rectanglePath.Stroke = IS_SELECTED_PEN.Brush;
					_rectanglePath.StrokeThickness = IS_SELECTED_PEN.Thickness;
				}
				else
				{
					_rectanglePath.Stroke = DEFAULT_PEN.Brush;
					_rectanglePath.StrokeThickness = DEFAULT_PEN.Thickness;
				}
			}
		}

		#endregion

		#region Diag

		#endregion
	}
}
