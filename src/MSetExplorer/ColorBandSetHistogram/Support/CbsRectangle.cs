using MSS.Types;
using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static ScottPlot.Plottable.PopulationPlot;

namespace MSetExplorer
{
	internal class CbsRectangle
	{
		#region Private Fields

		private static readonly Brush DEFAULT_BACKGROUND = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush IS_CURRENT_BACKGROUND = new SolidColorBrush(Colors.PowderBlue);
		private static readonly Brush IS_SELECTED_BACKGROUND = new SolidColorBrush(Colors.LightBlue);
		private static readonly Brush IS_SELECTED_INACTIVE_BACKGROUND = new SolidColorBrush(Colors.LightSlateGray);

		private static readonly Brush IS_CURRENT_AND_IS_SELECTED_BACKGROUND = IS_SELECTED_BACKGROUND; // new SolidColorBrush(Colors.SeaGreen);

		private static readonly Brush DEFAULT_STROKE = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush IS_CURRENT_STROKE = new SolidColorBrush(Colors.DarkBlue);
		private static readonly Brush IS_SELECTED_STROKE = new SolidColorBrush(Colors.LightBlue);
		private static readonly Brush IS_CURRENT_AND_IS_SELECTED_STROKE = IS_SELECTED_STROKE; // new SolidColorBrush(Colors.SeaGreen);


		private static readonly Brush INACTIVE_STROKE = new SolidColorBrush(Colors.Transparent);

		private const double SEL_RECTANGLE_STROKE_THICKNESS = 2.0;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _controlHeight;
		private double _cbElevation;
		private double _cbHeight;
		private SizeDbl _contentScale;
		private IsSelectedChanged _isSelectedChanged;
		private Action<int, ColorBandSelectionType> _requestContextMenuShown;

		private double _xPosition;
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
		private bool _parentIsFocused;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, bool isCurrent, bool isSelected, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend,
			ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChanged isSelectedChanged, Action<int, ColorBandSelectionType> requestContextMenuShown)
		{
			_isCurrent = isCurrent;
			_isSelected = isSelected;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;

			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;

			_canvas = canvas;
			_controlHeight = _colorBandLayoutViewModel.ControlHeight;
			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			_isSelectedChanged = isSelectedChanged;
			_requestContextMenuShown = requestContextMenuShown;

			_xPosition = xPosition;
			_width = width;
			//_startColor = startColor;
			//_endColor = endColor;
			//_blend = blend;

			var isHighLigted = _parentIsFocused && (_isCurrent || _isSelected);
			_geometry = new RectangleGeometry(BuildRectangle(_xPosition, width, isHighLigted, _colorBandLayoutViewModel));
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);

			_rectanglePath.MouseUp += Handle_MouseUp;

			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);

			_selGeometry = new RectangleGeometry(BuildSelRectangle(_xPosition, width, _colorBandLayoutViewModel));
			_selRectanglePath = BuildSelRectanglePath(_selGeometry, _isCurrent, _isSelected, _parentIsFocused, SEL_RECTANGLE_STROKE_THICKNESS);

			_canvas.Children.Add(_selRectanglePath);
			_selRectanglePath.SetValue(Panel.ZIndexProperty, 1);
		}


		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public double ControlHeight
		{
			get => _controlHeight;
			set
			{
				if (value != _controlHeight)
				{
					_controlHeight = value;
				}
			}
		}

		public double XPosition
		{
			get => _xPosition;
			set
			{
				if (value != _xPosition)
				{
					_xPosition = value;
					Resize(_xPosition, Width, _colorBandLayoutViewModel);
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
					Resize(_xPosition, Width, _colorBandLayoutViewModel);
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
					Resize(_xPosition, Width, _colorBandLayoutViewModel);
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
					Resize(_xPosition, Width, _colorBandLayoutViewModel);

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
					Resize(_xPosition, Width, _colorBandLayoutViewModel);
				}
			}
		}

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

		public bool ParentIsFocused
		{
			get => _parentIsFocused;

			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					UpdateSelectionBackground();
				}
			}
		}

		public Shape Rectangle => _rectanglePath;
		public RectangleGeometry RectangleGeometry => _geometry;

		public Shape SelRectangle => _selRectanglePath;
		public RectangleGeometry SelRectangleGeometry => _selGeometry;

		#endregion

		#region Public Methods

		public void TearDown()
		{
			try
			{
				_colorBandLayoutViewModel.PropertyChanged -= ColorBandLayoutViewModel_PropertyChanged;
				_rectanglePath.MouseUp -= Handle_MouseUp;

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

		public bool ContainsPoint(Point hitPoint)
		{
			var result = SelRectangleGeometry.FillContains(hitPoint);
			return result;
		}

		#endregion

		#region Event Handlers

		private void ColorBandLayoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandLayoutViewModel.ContentScale))
			{
				ContentScale = _colorBandLayoutViewModel.ContentScale;
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ControlHeight))
			{
				ControlHeight = _colorBandLayoutViewModel.ControlHeight;
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.CbrHeight))
			{
				CbHeight = _colorBandLayoutViewModel.CbrHeight;
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ParentIsFocused))
			{
				ParentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			}
		}

		private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				Debug.WriteLine($"CbsRectangle. Handling MouseUp.");
				var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

				_isSelectedChanged(ColorBandIndex, !IsSelected, shiftKeyPressed, controlKeyPressed);
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_requestContextMenuShown(ColorBandIndex, ColorBandSelectionType.Color);
					e.Handled = true;
				}
			}
		}

		#endregion

		#region Private Methods

		private Shape BuildRectanglePath(RectangleGeometry area, ColorBandColor startColor, ColorBandColor endColor, bool blend)
		{
			var result = new Path()
			{
				Fill = DrawingHelper.BuildBrush(startColor, endColor, blend),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Data = area,
				IsHitTestVisible = true
			};

			return result;
		}

		private Shape BuildSelRectanglePath(RectangleGeometry area, bool isCurrent, bool isSelected, bool parentIsFocused, double strokeThickness)
		{
			var result = new Path()
			{
				Fill = GetSelBackGround(isCurrent, isSelected, parentIsFocused),
				Stroke = GetSelStroke(isCurrent, isSelected, parentIsFocused),
				StrokeThickness = strokeThickness,
				Data = area,
				IsHitTestVisible = true
			};

			return result;
		}

		private void Resize(double xPosition, double width, ColorBandLayoutViewModel layout)
		{
			var isHighLighted = ParentIsFocused && (IsCurrent || IsSelected);

			_geometry.Rect = BuildRectangle(xPosition, width, isHighLighted, layout);
			_selGeometry.Rect = BuildSelRectangle(xPosition, width, layout);
		}

		private Rect BuildRectangle(double xPosition, double width, bool isHighLighted, ColorBandLayoutViewModel layout)
		{
			var yPosition = layout.CbrElevation;
			var height = layout.CbrHeight;
			var rect = BuildRect(xPosition, yPosition, width, height, layout.ContentScale);

			//var result = rect.Width > 7 ? Rect.Inflate(rect, -3, 0) : rect; // Decrease the width by 2, if the width > 3. Decrease the height by 2

			Rect result;

			if (isHighLighted && rect.Width > 7)
			{
				result = Rect.Inflate(rect, -3, -1);    // Decrease the width by 4, if the width > 7. Decrease the height by 2
			}
			else
			{
				result = Rect.Inflate(rect, 0, -1);     // Decrease the height by 2
			}

			return result;
		}

		private Rect BuildSelRectangle(double xPosition, double width, ColorBandLayoutViewModel layout)
		{
			var yPosition = 0;
			var height = layout.ControlHeight;
			var rect = BuildRect(xPosition, yPosition, width, height, layout.ContentScale);

			var result = rect.Width > 7 ? Rect.Inflate(rect, -2, -1) : Rect.Inflate(rect, 0, -1); // Decrease the width by 2, if the width > 3. Decrease the height by 2

			return result;
		}

		private Rect BuildRect(double xPosition, double yPosition, double width, double height, SizeDbl scaleSize)
		{
			var area = new RectangleDbl(new PointDbl(xPosition, yPosition), new SizeDbl(width, height));
			var scaledArea = area.Scale(scaleSize);
			var result = ScreenTypeHelper.ConvertToRect(scaledArea);

			return result;
		}

		private void UpdateSelectionBackground()
		{
			var isHighLighted = ParentIsFocused && (IsCurrent || IsSelected);

			_geometry.Rect = BuildRectangle(XPosition, Width, isHighLighted, _colorBandLayoutViewModel);

			_selRectanglePath.Stroke = GetSelStroke(_isCurrent, _isSelected, _parentIsFocused);

			_selRectanglePath.Fill = GetSelBackGround(_isCurrent, _isSelected, _parentIsFocused);
		}

		private Brush GetSelBackGround(bool isCurrent, bool isSelected, bool parentIsFocused)
		{
			Brush result;

			if (parentIsFocused)
			{
				if (isCurrent)
				{
					result = IsSelected ? IS_CURRENT_AND_IS_SELECTED_BACKGROUND : IS_CURRENT_BACKGROUND;
				}
				else
				{
					result = IsSelected ? IS_SELECTED_BACKGROUND : DEFAULT_BACKGROUND;
				}
			}
			else
			{
				result = isSelected ? IS_SELECTED_INACTIVE_BACKGROUND : DEFAULT_BACKGROUND;
			}

			return result;
		}

		private Brush GetSelStroke(bool isCurrent, bool isSelected, bool parentIsFocused)
		{
			Brush result;

			if (parentIsFocused)
			{
				if (isCurrent)
				{
					result = IsSelected ? IS_CURRENT_AND_IS_SELECTED_STROKE : IS_CURRENT_STROKE;
				}
				else
				{
					result = IsSelected ? IS_SELECTED_STROKE : DEFAULT_STROKE;
				}
			}
			else
			{
				result = INACTIVE_STROKE;
			}

			return result;
		}

		#endregion

		#region Diag

		#endregion
	}
}
