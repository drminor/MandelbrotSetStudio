using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	internal class CbColorBlock
	{
		#region Private Fields

		//private static readonly Brush IS_CURRENT_BACKGROUND = new SolidColorBrush(Colors.PowderBlue);

		private const double DEFAULT_STROKE_THICKNESS = 2.0;
		private const double SELECTED_STROKE_THICKNESS = 2.0;

		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush DARKISH_GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9));
		private static readonly Brush VERY_LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xe5, 0xf3, 0xff));
		private static readonly Brush MIDDLIN_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xcc, 0xe8, 0xff));
		private static readonly Brush LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0x99, 0xd1, 0xff));

		private static readonly Brush DEFAULT_BACKGROUND = TRANSPARENT_BRUSH;
		//private static readonly Brush DEFAULT_STROKE = TRANSPARENT_BRUSH;
		private static readonly Brush DEFAULT_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_SELECTED_BACKGROUND = MIDDLIN_BLUE_BRUSH;
		private static readonly Brush IS_SELECTED_STROKE = MIDDLIN_BLUE_BRUSH;

		private static readonly Brush IS_SELECTED_INACTIVE_BACKGROUND = DARKISH_GRAY_BRUSH;
		private static readonly Brush IS_SELECTED_INACTIVE_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_HOVERED_BACKGROUND = VERY_LIGHT_BLUE_BRUSH;
		private static readonly Brush IS_HOVERED_STROKE = VERY_LIGHT_BLUE_BRUSH;

		//private static readonly Brush IS_CURRENT_STROKE = LIGHT_BLUE_BRUSH;
		private static readonly Brush IS_CURRENT_BACKGROUND = LIGHT_BLUE_BRUSH;

		// For diagnostics
		//private static readonly Brush IS_HOVERED_AND_IS_SELECTED_BACKGROUND = new SolidColorBrush(Colors.SeaGreen);

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;

		private SizeDbl _contentScale;

		private double _xPosition;
		private double _width;
		private double _cutoff;

		private ColorBandColor _startColor;
		private ColorBandColor _endColor;
		private bool _blend;

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		private RectangleGeometry _startGeometry;
		private readonly Shape _startColorBlockPath;

		private RectangleGeometry _endGeometry;
		private readonly Shape _endColorBlockPath;

		private bool _isSelected;
		private bool _isUnderMouse;

		private bool _parentIsFocused;

		#endregion

		#region Constructor

		public CbColorBlock(int colorBandIndex, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend, ColorBandLayoutViewModel colorBandLayoutViewModel)
		{
			_isSelected = false;
			_isUnderMouse = false;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;
			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;

			_canvas = _colorBandLayoutViewModel.Canvas;
			_xPosition = xPosition;
			_width = width;
			_cutoff = _xPosition + _width;

			_startColor = startColor;
			_endColor = endColor;
			_blend = blend;

			var isHighLighted = GetIsHighlighted(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);

			_geometry = new RectangleGeometry(BuildRectangle(_xPosition, width, isHighLighted, _colorBandLayoutViewModel));
			_rectanglePath = BuildRectanglePath(_geometry);
			_rectanglePath.MouseUp += Handle_MouseUp;
			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);

			_startGeometry = new RectangleGeometry(BuildColorBlockStart(_geometry.Rect, _colorBandLayoutViewModel));
			_startColorBlockPath = BuildStartColorBlockPath(_startGeometry, _startColor);
			//_rectanglePath.MouseUp += Handle_MouseUp;
			_canvas.Children.Add(_startColorBlockPath);
			_startColorBlockPath.SetValue(Panel.ZIndexProperty, 20);

			_endGeometry = new RectangleGeometry(BuildColorBlockEnd(_geometry.Rect, _startGeometry.Rect));
			_endColorBlockPath = BuildEndColorBlockPath(_endGeometry, _endColor);
			//_rectanglePath.MouseUp += Handle_MouseUp;
			_canvas.Children.Add(_endColorBlockPath);
			_endColorBlockPath.SetValue(Panel.ZIndexProperty, 20);
		}

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public RectangleGeometry RectangleGeometry => _geometry;

		public ColorBandColor StartColor
		{
			get => _startColor;
			set
			{
				if (value != _startColor)
				{
					_startColor = value;
					_startColorBlockPath.Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(_startColor));
				}
			}
		}

		public ColorBandColor EndColor
		{
			get => _endColor;
			set
			{
				if (value != _endColor)
				{
					_endColor = value;
					_endColorBlockPath.Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(_endColor));
				}
			}
		}

		public bool HorizontalBlend
		{
			get => _blend;
			set
			{
				if (value != _blend)
				{
					_blend = value;
				}
			}
		}

		#endregion

		#region Public Properies - Layout Width

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;
					Resize(_xPosition, Width, _isSelected, _isUnderMouse, _colorBandLayoutViewModel);
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
					_width = _cutoff - _xPosition;
					Resize(_xPosition, Width, _isSelected, _isUnderMouse, _colorBandLayoutViewModel);
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
					_cutoff = _xPosition + _width;
					Resize(_xPosition, Width, _isSelected, _isUnderMouse, _colorBandLayoutViewModel);
				}
			}
		}

		#endregion

		#region Private Properties IsSelected

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					UpdateSelectionBackground(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);
				}
			}
		}

		public bool IsUnderMouse
		{
			get => _isUnderMouse;
			set
			{
				if (value != _isUnderMouse)
				{
					_isUnderMouse = value;
					UpdateSelectionBackground(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);
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
					UpdateSelectionBackground(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);
				}
			}
		}

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
					_canvas.Children.Remove(_rectanglePath);
					_canvas.Children.Remove(_startColorBlockPath);
					_canvas.Children.Remove(_endColorBlockPath);
				}
			}
			catch
			{
				Debug.WriteLine("CbSectionLine encountered an exception in TearDown.");
			}
		}

		public bool ContainsPoint(Point hitPoint)
		{
			var result = _geometry.FillContains(hitPoint);
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
				Resize(_xPosition, _width, _isSelected, _isUnderMouse, _colorBandLayoutViewModel);
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
				Debug.WriteLine($"CbRectangle. Handling MouseUp for Index: {ColorBandIndex}.");
				NotifySelectionChange();
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_colorBandLayoutViewModel.RequestContextMenuShown(ColorBandIndex, ColorBandSetEditMode.Colors);
					e.Handled = true;
				}
			}
		}

		#endregion

		#region Private Methods - Layout

		private Shape BuildRectanglePath(RectangleGeometry area)
		{
			var result = new Path()
			{
				Fill = DEFAULT_BACKGROUND,
				Stroke = DEFAULT_STROKE,
				StrokeThickness = DEFAULT_STROKE_THICKNESS,
				Data = area,
				IsHitTestVisible = true
			};

			return result;
		}

		private Shape BuildStartColorBlockPath(RectangleGeometry area, ColorBandColor startColor)
		{
			var result = new Path()
			{
				Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(startColor)),
				Stroke = DEFAULT_STROKE,
				StrokeThickness = 1,
				Data = area,
				IsHitTestVisible = true
			};

			result.Visibility = area.Rect.Width > 0 ? Visibility.Visible : Visibility.Collapsed;

			return result;
		}

		private Shape BuildEndColorBlockPath(RectangleGeometry area, ColorBandColor endColor)
		{
			var result = new Path()
			{
				Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(endColor)),
				Stroke = DEFAULT_STROKE,
				StrokeThickness = 1,
				Data = area,
				IsHitTestVisible = true
			};

			result.Visibility = area.Rect.Width > 0 ? Visibility.Visible : Visibility.Collapsed;

			return result;
		}

		private void Resize(double xPosition, double width, bool isSelected, bool isUnderMouse, ColorBandLayoutViewModel layout)
		{
			var isHighLighted = GetIsHighlighted(isSelected, isUnderMouse, layout.ParentIsFocused);

			_geometry.Rect = BuildRectangle(xPosition, width, isHighLighted, layout);

			_startColorBlockPath.Visibility = _geometry.Rect.Width > 2 ? Visibility.Visible : Visibility.Collapsed;
			_startGeometry.Rect = BuildColorBlockStart(_geometry.Rect, layout);

			_endColorBlockPath.Visibility = _geometry.Rect.Width > 10 ? Visibility.Visible : Visibility.Collapsed;
			_endGeometry.Rect = BuildColorBlockEnd(_geometry.Rect, _startGeometry.Rect);
		}

		private Rect BuildRectangle(double xPosition, double width, bool isHighLighted, ColorBandLayoutViewModel layout)
		{
			var yPosition = layout.ColorBlocksElevation;
			var height = layout.ColorBlocksHeight;
			var rect = BuildRect(xPosition, yPosition, width, height, layout.ContentScale);

			Rect result;

			if (isHighLighted && rect.Width > 3)
			{
				// Reduce the height to accomodate the outside border. The outside border is 1 pixel thick.
				// Reduce the width to accomodate the section line and the selection background rectangle (SelRectangle).
				// The section line is 2 pixels thick, but we only need to get out of the way of 1/2 this distance.
				// The SelRectangle has a border of 2 pixels. 1 + 2 = 3. Must do this for the right and left side for a total of 6
				// 
				// Decrease the width by 4, if the width > 5. Decrease the height by 2
				//result = Rect.Inflate(rect, -3, -1);
				result = Rect.Inflate(rect, -1, -1);
			}
			else
			{
				result = Rect.Inflate(rect, 0, -1);
			}

			return result;
		}

		private Rect BuildColorBlockStart(Rect container, ColorBandLayoutViewModel layout)
		{
			var yPosition = layout.ColorBlocksElevation + 3;
			var height = layout.ColorBlocksHeight - 6;

			var (left, width) = GetBlock1Pos(container.X, container.Width);

			var result = new Rect(left, yPosition, width, height);
			return result;
		}

		private Rect BuildColorBlockEnd(Rect container, Rect block1Rect)
		{
			var yPosition = block1Rect.Y;
			var height = block1Rect.Height;

			var (left, width) = GetBlock2Pos(container.Width, block1Rect);

			var result = new Rect(left, yPosition, width, height);
			return result;
		}

		private (double left, double width) GetBlock1Pos(double containerLeft, double containerWidth)
		{
			double left;
			double width;


			if (containerWidth > 33)
			{
				left = containerLeft + 5;
				var ts = containerWidth - 9;    // 5 before, 2 between and 2 or more after
				width = ts / 2;                 // Divide remaining between both blocks
				width = Math.Min(width, 15);    // Each block should be no more than 15 pixels wide
			}
			else if (containerWidth > 27)
			{
				left = containerLeft + 2;
				var ts = containerWidth - 6;    // 2 before, 2 between and 2 after
				width = ts / 2;                 // Divide remaining between both blocks
				width = Math.Min(width, 15);    // Each block should be no more than 15 pixels wide
			}
			else if (containerWidth > 10)
			{
				left = containerLeft + 1;
				width = 4;
			}
			else if (containerWidth > 4)
			{
				left = containerLeft + 1;
				width = containerWidth - 2;
			}
			else if (containerWidth > 2)
			{
				left = containerLeft + 0.5;
				width = containerWidth - 1;
			}
			else
			{
				left = containerLeft;
				width = 0;
			}

			return (left, width);
		}

		private (double left, double width) GetBlock2Pos(double containerWidth, Rect block1Rect)
		{
			double left;
			double width;

			if (containerWidth > 27)
			{
				left = block1Rect.Right + 2;
				width = block1Rect.Width;
			}
			else if (containerWidth > 10)
			{
				left = block1Rect.Right + 1;
				width = block1Rect.Width;
			}
			else
			{
				left = block1Rect.Left;
				width = 0;
			}

			return (left, width);
		}

		private Rect BuildRect(double xPosition, double yPosition, double width, double height, SizeDbl contentScale)
		{
			var result = new Rect(new Point(xPosition, yPosition), new Size(width, height));
			result.Scale(contentScale.Width, contentScale.Height);

			return result;
		}

		#endregion

		#region Private Methods - IsCurrent / IsSelected State

		private void NotifySelectionChange()
		{
			_colorBandLayoutViewModel.IsSelectedChangedCallback(ColorBandIndex, ColorBandSetEditMode.Colors);
		}

		private void UpdateSelectionBackground(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			var isHighLighted = GetIsHighlighted(isSelected, isUnderMouse, parentIsFocused);

			_geometry.Rect = BuildRectangle(XPosition, Width, isHighLighted, _colorBandLayoutViewModel);

			_rectanglePath.Stroke = GetRectangleStroke(isSelected, isUnderMouse, parentIsFocused);
		}

		private Brush GetRectangleStroke(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			Brush result;

			if (parentIsFocused)
			{
				if (isSelected)
				{
					result = IS_SELECTED_STROKE;
				}
				else
				{
					result = isUnderMouse ? IS_HOVERED_STROKE : DEFAULT_STROKE;
				}
			}
			else
			{
				result = isSelected ? IS_SELECTED_INACTIVE_STROKE : DEFAULT_STROKE;
			}

			return result;
		}

		private bool GetIsHighlighted(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			//var result = _isSelected || (ParentIsFocused && (_isCurrent || _isUnderMouse));
			var result = isSelected || (parentIsFocused && isUnderMouse);

			return result;
		}

		#endregion

	}
}
