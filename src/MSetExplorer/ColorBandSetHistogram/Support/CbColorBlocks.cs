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
	internal class CbColorBlocks
	{
		#region Private Static Fields

		private const double DEFAULT_STROKE_THICKNESS = 2.0;
		private const double SUB_COLOR_BLOCK_STROKE_THICKNESS = 1.0;

		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush DARKISH_GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9));

		private static readonly Brush VERY_LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xe5, 0xf3, 0xff));
		private static readonly Brush LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0x99, 0xd1, 0xff));
		private static readonly Brush MIDDLIN_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xcc, 0xe8, 0xff));
		private static readonly Brush SKY_BLUE = new SolidColorBrush(Colors.SkyBlue);

		private static readonly Brush DEFAULT_BACKGROUND = TRANSPARENT_BRUSH;
		private static readonly Brush DEFAULT_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_SELECTED_STROKE = LIGHT_BLUE_BRUSH; // MIDDLIN_BLUE_BRUSH;

		private static readonly Brush IS_SELECTED_INACTIVE_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_HOVERED_STROKE = SKY_BLUE; // LIGHT_BLUE_BRUSH; // VERY_LIGHT_BLUE_BRUSH;

		#endregion

		#region Private Fields

		private readonly Canvas _canvas;
		private readonly ColorBandLayoutViewModel _colorBandLayoutViewModel;

		private SizeDbl _contentScale;
		private bool _parentIsFocused;

		private Rect _colorBlocksArea;
		private double _xPosition;
		private double _width;

		private ColorBandColor _startColor;
		private ColorBandColor _endColor;
		private bool _blend;
		private double _opacity;

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		//private RectangleGeometry _startGeometry;
		//private readonly Shape _startColorBlockPath;

		//private RectangleGeometry _endGeometry;
		//private readonly Shape _endColorBlockPath;

		private readonly CbColorPair _cbColorPair;

		private bool _isSelected;
		private bool _isUnderMouse;

		#endregion

		#region Constructor

		public CbColorBlocks(int colorBandIndex, Rect area, ColorBandColor startColor, ColorBandColor endColor, bool blend, ColorBandLayoutViewModel colorBandLayoutViewModel)
		{
			_isSelected = false;
			_isUnderMouse = false;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;
			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;

			_canvas = _colorBandLayoutViewModel.Canvas;

			_colorBlocksArea = area;

			_xPosition = area.Right;
			_width = area.Width;
			_opacity = 1.0;

			_startColor = startColor;
			_endColor = endColor;
			_blend = blend;

			var isHighLighted = GetIsHighlighted(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);

			_geometry = new RectangleGeometry(BuildRectangle(_colorBlocksArea, isHighLighted, ContentScale));
			_rectanglePath = BuildRectanglePath(_geometry);
			_rectanglePath.MouseUp += Handle_MouseUp;
			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);


			//_startGeometry = new RectangleGeometry(BuildColorBlockStart(_geometry.Rect, _colorBlocksArea));
			//_startColorBlockPath = BuildStartColorBlockPath(_startGeometry, _startColor);
			////_rectanglePath.MouseUp += Handle_MouseUp;
			//_canvas.Children.Add(_startColorBlockPath);
			//_startColorBlockPath.SetValue(Panel.ZIndexProperty, 20);

			//_endGeometry = new RectangleGeometry(BuildColorBlockEnd(_geometry.Rect, _startGeometry.Rect));
			//_endColorBlockPath = BuildEndColorBlockPath(_endGeometry, _endColor);
			////_rectanglePath.MouseUp += Handle_MouseUp;
			//_canvas.Children.Add(_endColorBlockPath);
			//_endColorBlockPath.SetValue(Panel.ZIndexProperty, 20);

			_cbColorPair = new CbColorPair(colorBandIndex, _geometry.Rect, startColor, endColor, _canvas);
		}

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public RectangleGeometry RectangleGeometry => _geometry;

		public RectangleGeometry StartColorGeometry => _cbColorPair.StartColorGeometry;
		public RectangleGeometry EndColorGeometry => _cbColorPair.EndColorGeometry;

		public Path ColorBlocksRectangle => (Path)_rectanglePath;

		public ColorBandColor StartColor
		{
			get => _cbColorPair.StartColor;
			set => _cbColorPair.StartColor = value;
		}

		public ColorBandColor EndColor
		{
			get => _cbColorPair.EndColor;
			set => _cbColorPair.EndColor = value;
		}

		public bool Blend
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

		#region Public Properies - Layout

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;
					ResizeColorBlocks(ColorBlocksArea, _isSelected, _isUnderMouse, ParentIsFocused, ContentScale);
				}
			}
		}

		public Rect ColorBlocksArea
		{
			get => _colorBlocksArea;
			set
			{
				if (value != _colorBlocksArea)
				{
					_colorBlocksArea = value;
					_width = _colorBlocksArea.Width;
					_xPosition = _colorBlocksArea.X;

					ResizeColorBlocks(ColorBlocksArea, _isSelected, _isUnderMouse, ParentIsFocused, ContentScale);
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
					ColorBlocksArea = new Rect(value, _colorBlocksArea.Y, Width, _colorBlocksArea.Height);
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
					ColorBlocksArea = new Rect(_colorBlocksArea.X, _colorBlocksArea.Y, value, _colorBlocksArea.Height);
				}
			}
		}

		public double Opacity
		{
			get => _opacity;
			set
			{
				if (value != _opacity)
				{
					_opacity = value;

					_rectanglePath.Opacity = value;
					SubBlockOpacity = value;
				}
			}
		}

		public double SubBlockOpacity
		{
			get => _cbColorPair.Opacity;
			set => _cbColorPair.Opacity = value;
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
				}

				_cbColorPair.TearDown();
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
			//else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ControlHeight))
			//{
			//	ResizeColorBlocks(_xPosition, _width, _isSelected, _isUnderMouse, _colorBandLayoutViewModel);
			//}
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
				IsHitTestVisible = true,
				Name = "ColorBlocksRectangle"
			};

			return result;
		}

		private Shape BuildStartColorBlockPath(RectangleGeometry area, ColorBandColor startColor)
		{
			var result = new Path()
			{
				Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(startColor)),
				Stroke = DEFAULT_STROKE,
				StrokeThickness = SUB_COLOR_BLOCK_STROKE_THICKNESS,
				Data = area,
				IsHitTestVisible = true
			};

			result.Visibility = area.Rect.Width > 0 && area.Rect.Height > 0 ? Visibility.Visible : Visibility.Collapsed;

			return result;
		}

		private Shape BuildEndColorBlockPath(RectangleGeometry area, ColorBandColor endColor)
		{
			var result = new Path()
			{
				Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(endColor)),
				Stroke = DEFAULT_STROKE,
				StrokeThickness = SUB_COLOR_BLOCK_STROKE_THICKNESS,
				Data = area,
				IsHitTestVisible = true
			};

			result.Visibility = area.Rect.Width > 0 && area.Rect.Height > 0 ? Visibility.Visible : Visibility.Collapsed;

			return result;
		}

		private void ResizeColorBlocks(Rect colorBlocksArea, bool isSelected, bool isUnderMouse, bool parentIsFocused, SizeDbl contentScale)
		{
			var isHighLighted = GetIsHighlighted(isSelected, isUnderMouse, parentIsFocused);

			_geometry.Rect = BuildRectangle(colorBlocksArea, isHighLighted, contentScale);

			_cbColorPair.Container = _geometry.Rect;
		}

		private Rect BuildRectangle(Rect colorBlocksArea, bool isHighLighted, SizeDbl contentScale)
		{
			var rect = BuildRect(colorBlocksArea, contentScale);

			Rect result = Rect.Inflate(rect, -1, -1);

			//if (isHighLighted && rect.Width > 3)
			//{
			//	// Reduce the height to accommodate the outside border. The outside border is 1 pixel thick.

			//	// Reduce the width to accommodate the section line.
			//	// The section line is 1 pixel thick, but we only need to get out of the way of 1/2 this distance.

			//	result = Rect.Inflate(rect, -1, -1);
			//}
			//else
			//{
			//	result = Rect.Inflate(rect, 0, 0);
			//}

			Debug.WriteLine($"ColorBlocks just built rectangle with top: {result.Top} and height: {result.Height} and left: {result.Left} and width: {result.Width}.");

			return result;
		}

		private Rect BuildRect(Rect r, SizeDbl contentScale)
		{
			var result = new Rect(r.Location, r.Size);
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
			_geometry.Rect = BuildRectangle(ColorBlocksArea, isHighLighted, ContentScale);
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
