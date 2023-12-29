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

		//private static readonly Pen DEFAULT_PEN = new Pen(new SolidColorBrush(Colors.Transparent), 1);
		//private static readonly Pen IS_CURRENT_PEN = new Pen(new SolidColorBrush(Colors.DarkRed), 1);
		//private static readonly Pen IS_SELECTED_PEN = new Pen(new SolidColorBrush(Colors.DarkCyan), 2.5);

		private static readonly Brush DEFAULT_BACKGROUND = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush IS_CURRENT_BACKGROUND = new SolidColorBrush(Colors.LightGreen);
		private static readonly Brush IS_SELECTED_BACKGROUND = new SolidColorBrush(Colors.LightBlue);
		private static readonly Brush IS_CURRENT_AND_IS_SELECTED_BACKGROUND = new SolidColorBrush(Colors.SeaGreen);

		private static readonly Brush IS_CURRENT_STROKE = new SolidColorBrush(Colors.DeepSkyBlue);
		private static readonly Brush DEFAULT_STROKE = new SolidColorBrush(Colors.Transparent);
		private const double SEL_RECTANGLE_STROKE_THICKNESS = 3.0;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _controlHeight;
		private double _cbElevation;
		private double _cbHeight;
		private SizeDbl _contentScale;
		private IsSelectedChanged _isSelectedChanged;

		private double _selectionLinePosition;
		private double _width;
		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;
		//private bool _blend;

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		private RectangleGeometry _selGeometry;
		private readonly Shape _selRectanglePath;

		private bool _isCurrent;
		private bool _isSelected;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, bool isCurrent, bool isSelected, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend,
			ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChanged isSelectedChanged)
		{
			_isCurrent = isCurrent;
			_isSelected = isSelected;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_canvas = canvas;
			_controlHeight = _colorBandLayoutViewModel.ControlHeight;
			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_isSelectedChanged = isSelectedChanged;

			//var yPosition = _colorBandLayoutViewModel.CbrElevation;
			//var height = _colorBandLayoutViewModel.CbrHeight;
			//var contentScale = _colorBandLayoutViewModel.ContentScale;

			_selectionLinePosition = xPosition;
			//_width = width;
			//_startColor = startColor;
			//_endColor = endColor;
			//_blend = blend;

			_geometry = BuildRectangleGeometry(_selectionLinePosition, _cbElevation, width, _cbHeight, _contentScale);
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);

			_rectanglePath.MouseUp += Handle_MouseUp;

			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);

			var top = 0;
			_selGeometry = BuildSelRectangleGeometry(_selectionLinePosition, top, width, _controlHeight, _contentScale);
			_selRectanglePath = BuildSelRectanglePath(_selGeometry, _isCurrent, _isSelected, SEL_RECTANGLE_STROKE_THICKNESS);

			_canvas.Children.Add(_selRectanglePath);
			_selRectanglePath.SetValue(Panel.ZIndexProperty, 1);
		}

		private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
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

		public Shape SelRectangle => _selRectanglePath;
		public RectangleGeometry SelRectangleGeometry => _selGeometry;

		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (value != _isCurrent)
				{
					_isCurrent = value;
					UpdateSelectionBackground();
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
					UpdateSelectionBackground();
				}
			}
		}

		public int ColorBandIndex { get; init; }

		public double SelectionLinePosition
		{
			get => _selectionLinePosition;
			set
			{
				if (value != _selectionLinePosition)
				{
					_selectionLinePosition = value;
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
					Rectangle.Width = _width;
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
					_canvas.Children.Remove(Rectangle);
					_canvas.Children.Remove(SelRectangle);
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

		private RectangleGeometry BuildSelRectangleGeometry(double xPosition, double yPosition, double width, double height, SizeDbl scaleSize)
		{
			var area = new RectangleDbl(new PointDbl(xPosition, yPosition), new SizeDbl(width, height));
			var scaledArea = area.Scale(scaleSize);
			var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(scaledArea));

			return cbRectangle;
		}

		private Shape BuildSelRectanglePath(RectangleGeometry area, bool isCurrent, bool isSelected, double strokeThickness)
		{
			var result = new Path()
			{
				Fill = GetSelBackGround(isCurrent, isSelected),
				Stroke = isCurrent ? IS_CURRENT_STROKE : DEFAULT_STROKE,
				StrokeThickness = strokeThickness,
				Data = area,
				Focusable=false,
				IsHitTestVisible = true
			};

			return result;
		}

		//private void SetRectangleStroke()
		//{
		//	if (_isCurrent)
		//	{
		//		if (_isSelected)
		//		{
		//			_rectanglePath.Stroke = IS_SELECTED_PEN.Brush;
		//			_rectanglePath.StrokeThickness = IS_SELECTED_PEN.Thickness;
		//		}
		//		else
		//		{
		//			_rectanglePath.Stroke = IS_CURRENT_PEN.Brush;
		//			_rectanglePath.StrokeThickness = IS_CURRENT_PEN.Thickness;
		//		}
		//	}
		//	else
		//	{
		//		if (_isSelected)
		//		{
		//			_rectanglePath.Stroke = IS_SELECTED_PEN.Brush;
		//			_rectanglePath.StrokeThickness = IS_SELECTED_PEN.Thickness;
		//		}
		//		else
		//		{
		//			_rectanglePath.Stroke = DEFAULT_PEN.Brush;
		//			_rectanglePath.StrokeThickness = DEFAULT_PEN.Thickness;
		//		}
		//	}
		//}

		private void UpdateSelectionBackground()
		{
			_selRectanglePath.Stroke = _isCurrent ? IS_CURRENT_STROKE : DEFAULT_STROKE;

			_selRectanglePath.Fill = GetSelBackGround(_isCurrent, _isSelected);
		}

		private Brush GetSelBackGround(bool isCurrent, bool isSelected)
		{
			var result = isCurrent
				? isSelected
					? IS_CURRENT_AND_IS_SELECTED_BACKGROUND
					: IS_CURRENT_BACKGROUND
				: isSelected
					? IS_SELECTED_BACKGROUND
					: DEFAULT_BACKGROUND;

			return result;
		}

		#endregion

		#region Diag

		#endregion
	}
}
