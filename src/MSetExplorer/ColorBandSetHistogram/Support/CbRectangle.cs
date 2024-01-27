using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbRectangle
	{
		#region Private Const Fields

		private const double DEFAULT_STROKE_THICKNESS = 1.0;
		private const double SELECTED_STROKE_THICKNESS = 2.0;

		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush DARKISH_GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9));

		private static readonly Brush VERY_LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xe5, 0xf3, 0xff));
		private static readonly Brush LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0x99, 0xd1, 0xff));
		private static readonly Brush MIDDLIN_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xcc, 0xe8, 0xff));
		private static readonly Brush SKY_BLUE = new SolidColorBrush(Colors.SkyBlue);
		private static readonly Brush LIGHT_GRAY_BRUSH = new SolidColorBrush(Colors.LightGray);
		//private static readonly Brush PINK = new SolidColorBrush(Colors.DeepPink);


		private static readonly Brush DEFAULT_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_SELECTED_STROKE = LIGHT_BLUE_BRUSH;
		private static readonly Brush IS_SELECTED_INACTIVE_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_HOVERED_STROKE = SKY_BLUE; // LIGHT_BLUE_BRUSH; // VERY_LIGHT_BLUE_BRUSH;

		private static readonly Brush IS_CURRENT_BACKGROUND = LIGHT_BLUE_BRUSH;
		private static readonly Brush IS_NOT_CURRENT_BACKGROUND = TRANSPARENT_BRUSH; // LIGHT_GRAY_BRUSH;

		#endregion

		#region Private Fields

		private readonly Canvas _canvas;
		private readonly ColorBandLayoutViewModel _colorBandLayoutViewModel;

		private SizeDbl _contentScale;
		private bool _parentIsFocused;

		private Rect _blendRectangleArea;
		private Rect _isCurrentArea;
		private double _xPosition;
		private double _width;

		private ColorBandColor _startColor;
		private ColorBandColor _endColor;
		private bool _blend;
		private double _opacity;

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		private RectangleGeometry _curGeometry;
		private readonly Shape _curRectanglePath;

		private bool _isCurrent;
		private bool _isSelected;
		private bool _isUnderMouse;

		#endregion

		#region Constructor

		public CbRectangle(int colorBandIndex, Rect blendArea, Rect isCurrentArea, ColorBandColor startColor, ColorBandColor endColor, bool blend, ColorBandLayoutViewModel colorBandLayoutViewModel)
		{
			_isCurrent = false;
			_isSelected = false;
			_isUnderMouse = false;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;
			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;

			_canvas = colorBandLayoutViewModel.Canvas;

			_blendRectangleArea = blendArea;
			_isCurrentArea = isCurrentArea;
			_xPosition = blendArea.Left;
			_width = blendArea.Width;

			_startColor = startColor;
			_endColor = endColor;
			_blend = blend;
			_opacity = 1.0;

			var isHighLighted = GetIsHighlighted(_isSelected, _isUnderMouse, _colorBandLayoutViewModel.ParentIsFocused);

			_geometry = new RectangleGeometry(BuildRectangle(_blendRectangleArea, isHighLighted, ContentScale));
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);
			_rectanglePath.MouseUp += Handle_MouseUp;
			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);

			_curGeometry = new RectangleGeometry(BuildCurRectangle(_isCurrentArea, ContentScale));
			_curRectanglePath = BuildCurRectanglePath(_curGeometry, _isCurrent);
			_canvas.Children.Add(_curRectanglePath);
			_curRectanglePath.SetValue(Panel.ZIndexProperty, 1);
		}

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public RectangleGeometry RectangleGeometry => _geometry;

		public Path BlendedBandRectangle => (Path)_rectanglePath;

		public ColorBandColor StartColor
		{
			get => _startColor;
			set
			{
				if (value != _startColor)
				{
					_startColor = value;
					_rectanglePath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);

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
					_rectanglePath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);
				}
			}
		}

		public bool Blend
		{
			get => _blend;
			set
			{
				if (value != _blend)
				{
					_blend = value;
					_rectanglePath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);
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
					ResizeBlendRectangle(BlendRectangleArea, _isSelected, _isUnderMouse, ParentIsFocused, ContentScale);
					ResizeIsCurrentRectangle(IsCurrentArea, ContentScale);
				}
			}
		}

		public Rect BlendRectangleArea
		{
			get => _blendRectangleArea;
			set
			{
				if (value != _blendRectangleArea)
				{
					_blendRectangleArea = value;
					_width = _blendRectangleArea.Width;
					_xPosition = _blendRectangleArea.X;

					ResizeBlendRectangle(BlendRectangleArea, _isSelected, _isUnderMouse, ParentIsFocused, ContentScale);
				}
			}
		}

		public Rect IsCurrentArea
		{
			get => _isCurrentArea;
			set
			{
				if (value != _isCurrentArea)
				{
					_isCurrentArea = value;

					ResizeIsCurrentRectangle(IsCurrentArea, ContentScale);
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
					BlendRectangleArea = new Rect(value, _blendRectangleArea.Y, Width, _blendRectangleArea.Height);
					IsCurrentArea = new Rect(value, _isCurrentArea.Y, Width, _isCurrentArea.Height);
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
					BlendRectangleArea = new Rect(_blendRectangleArea.X, _blendRectangleArea.Y, value, _blendRectangleArea.Height);
					IsCurrentArea = new Rect(_isCurrentArea.X, _isCurrentArea.Y, Width, _isCurrentArea.Height);
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

					_curRectanglePath.Opacity = value;
					_rectanglePath.Opacity = value;
				}
			}
		}

		#endregion

		#region Public Propeties IsCurrent / IsSelected

		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (value != _isCurrent)
				{
					_isCurrent = value;
					UpdateCurBackground(IsCurrent);
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
					_canvas.Children.Remove(_curRectanglePath);
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
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ParentIsFocused))
			{
				ParentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			}
		}

		private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				Debug.WriteLine($"CbRectangle. Handling MouseUp for Index: {ColorBandIndex}. IsHandled = {e.Handled}.");
				NotifySelectionChange();
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_colorBandLayoutViewModel.RequestContextMenuShown(ColorBandIndex, ColorBandSetEditMode.Bands);
					e.Handled = true;
				}
			}
		}

		#endregion

		#region Private Methods - Layout

		private Shape BuildRectanglePath(RectangleGeometry area, ColorBandColor startColor, ColorBandColor endColor, bool blend)
		{
			var result = new Path()
			{
				Fill = GetBlendedBrush(startColor, endColor, blend),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Data = area,
				IsHitTestVisible = true,
				Opacity = 1.0
			};

			return result;
		}

		private Shape BuildCurRectanglePath(RectangleGeometry area, bool isCurrent)
		{
			var result = new Path()
			{
				Fill = isCurrent ? IS_CURRENT_BACKGROUND : IS_NOT_CURRENT_BACKGROUND,
				Stroke = new SolidColorBrush(Colors.Transparent),
				StrokeThickness = 0,
				Data = area,
				IsHitTestVisible = true,
				Opacity= 1.0
			};

			return result;
		}

		private void ResizeBlendRectangle(Rect blendRectangleArea, bool isSelected, bool isUnderMouse, bool parentIsFocused, SizeDbl contentScale)
		{
			var isHighLighted = GetIsHighlighted(isSelected, isUnderMouse, parentIsFocused);

			_geometry.Rect = BuildRectangle(blendRectangleArea, isHighLighted, contentScale);
		}

		private Rect BuildRectangle(Rect blendRectangleArea, bool isHighLighted, SizeDbl contentScale)
		{
			var rect = BuildRect(blendRectangleArea, contentScale);

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
				//result = Rect.Inflate(rect, 0, -1);
				result = Rect.Inflate(rect, 0, 0);
			}

			return result;
		}

		private void ResizeIsCurrentRectangle(Rect isCurrentArea, SizeDbl contentScale)
		{
			_curGeometry.Rect = BuildCurRectangle(isCurrentArea, contentScale);
		}

		private Rect BuildCurRectangle(Rect isCurrentArea, SizeDbl contentScale)
		{
			var result = BuildRect(isCurrentArea, contentScale);

			return result;
		}

		private Rect BuildRect(Rect r, SizeDbl contentScale)
		{
			var result = new Rect(r.Location, r.Size);
			result.Scale(contentScale.Width, contentScale.Height);

			return result;
		}

		private Brush GetBlendedBrush(ColorBandColor startColor, ColorBandColor endColor, bool blend)
		{
			Brush result;

			if (blend)
			{
				result = DrawingHelper.BuildBrush(startColor, endColor, blend);
			}
			else
			{
				result = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(startColor));
			}

			return result;
		}

		#endregion

		#region Private Methods - IsCurrent / IsSelected State

		private void NotifySelectionChange()
		{
			_colorBandLayoutViewModel.IsSelectedChangedCallback(ColorBandIndex, ColorBandSetEditMode.Bands);
		}

		private void UpdateCurBackground(bool isCurrent)
		{
			_curRectanglePath.Fill = isCurrent ? IS_CURRENT_BACKGROUND : IS_NOT_CURRENT_BACKGROUND;
		}

		private void UpdateSelectionBackground(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			var isHighLighted = GetIsHighlighted(isSelected, isUnderMouse, parentIsFocused);

			_geometry.Rect = BuildRectangle(BlendRectangleArea, isHighLighted, ContentScale);

			_rectanglePath.Stroke = GetRectangleStroke(isSelected, isUnderMouse, parentIsFocused);
			_rectanglePath.StrokeThickness = GetRectangleStrokeThickness(isHighLighted);
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

		private double GetRectangleStrokeThickness(bool isHighlighted)
		{
			var result = isHighlighted ? SELECTED_STROKE_THICKNESS : DEFAULT_STROKE_THICKNESS;
			return result;
		}

		private bool GetIsHighlighted(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			var result = isSelected || (parentIsFocused && isUnderMouse);

			return result;
		}

		#endregion
	}
}
