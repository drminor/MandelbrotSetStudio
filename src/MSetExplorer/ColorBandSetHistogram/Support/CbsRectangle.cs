﻿using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbsRectangle
	{
		#region Private Fields

		//private static readonly Brush IS_CURRENT_BACKGROUND = new SolidColorBrush(Colors.PowderBlue);

		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush DARKISH_GRAY_BRUSH = new SolidColorBrush(Color.FromRgb(0xd9, 0xd9, 0xd9));
		private static readonly Brush VERY_LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xe5, 0xf3, 0xff));
		private static readonly Brush MIDDLIN_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0xcc, 0xe8, 0xff));
		private static readonly Brush LIGHT_BLUE_BRUSH = new SolidColorBrush(Color.FromRgb(0x99, 0xd1, 0xff));


		private static readonly Brush DEFAULT_BACKGROUND = TRANSPARENT_BRUSH;
		private static readonly Brush DEFAULT_STROKE = TRANSPARENT_BRUSH;

		private static readonly Brush IS_SELECTED_BACKGROUND = MIDDLIN_BLUE_BRUSH;
		private static readonly Brush IS_SELECTED_STROKE = MIDDLIN_BLUE_BRUSH;

		private static readonly Brush IS_SELECTED_INACTIVE_BACKGROUND = DARKISH_GRAY_BRUSH;
		private static readonly Brush IS_SELECTED_INACTIVE_STROKE = DARKISH_GRAY_BRUSH;

		private static readonly Brush IS_HOVERED_BACKGROUND = VERY_LIGHT_BLUE_BRUSH;
		private static readonly Brush IS_HOVERED_STROKE = VERY_LIGHT_BLUE_BRUSH;

		private static readonly Brush IS_CURRENT_STROKE = LIGHT_BLUE_BRUSH;

		// For diagnostics
		//private static readonly Brush IS_HOVERED_AND_IS_SELECTED_BACKGROUND = new SolidColorBrush(Colors.SeaGreen);

		private const double SEL_RECTANGLE_STROKE_THICKNESS = 2.0;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _controlHeight;
		private double _cbElevation;
		private double _cbHeight;
		private SizeDbl _contentScale;
		private IsSelectedChangedCallback _isSelectedChangedCallback;
		private Action<int, ColorBandSetEditMode> _requestContextMenuShown;

		private double _xPosition;
		private double _width;
		private double _cutoff;

		//private ColorBandColor _startColor;
		//private ColorBandColor _endColor;
		//private bool _blend;

		private RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		private RectangleGeometry _selGeometry;
		private readonly Shape _selRectanglePath;

		private bool _isCurrent;
		private bool _isSelected;
		private bool _isUnderMouse;

		private bool _parentIsFocused;

		#endregion

		#region Constructor

		public CbsRectangle(int colorBandIndex, bool isCurrent, bool isSelected, double xPosition, double width, ColorBandColor startColor, ColorBandColor endColor, bool blend,
			ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChangedCallback isSelectedChangedCallBack, Action<int, ColorBandSetEditMode> requestContextMenuShown)
		{
			_isCurrent = isCurrent;
			_isSelected = isSelected;
			_isUnderMouse = false;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;

			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;

			_canvas = canvas;
			_controlHeight = _colorBandLayoutViewModel.ControlHeight;
			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_contentScale = _colorBandLayoutViewModel.ContentScale;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			_isSelectedChangedCallback = isSelectedChangedCallBack;
			_requestContextMenuShown = requestContextMenuShown;

			_xPosition = xPosition;
			_width = width;
			_cutoff = _xPosition + _width;

			//_startColor = startColor;
			//_endColor = endColor;
			//_blend = blend;


			var isHighLighted = _isSelected || (ParentIsFocused && (_isCurrent || _isUnderMouse));


			_geometry = new RectangleGeometry(BuildRectangle(_xPosition, width, isHighLighted, _colorBandLayoutViewModel));
			_rectanglePath = BuildRectanglePath(_geometry, startColor, endColor, blend);

			_rectanglePath.MouseUp += Handle_MouseUp;

			_canvas.Children.Add(_rectanglePath);
			_rectanglePath.SetValue(Panel.ZIndexProperty, 20);

			_selGeometry = new RectangleGeometry(BuildSelRectangle(_xPosition, width, _colorBandLayoutViewModel));
			_selRectanglePath = BuildSelRectanglePath(_selGeometry, _isCurrent, _isSelected, _isUnderMouse, _parentIsFocused, SEL_RECTANGLE_STROKE_THICKNESS);

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
					_width = Cutoff - _xPosition;
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

		public double Cutoff
		{
			get => _cutoff;
			set
			{
				if (value != _cutoff)
				{
					_cutoff = value;
					Width = _cutoff - XPosition;
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

		public bool IsUnderMouse
		{
			get => _isUnderMouse;
			set
			{
				if (value != _isUnderMouse)
				{
					_isUnderMouse = value;
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
				Debug.WriteLine("CbsSectionLine encountered an exception in TearDown.");
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
				Debug.WriteLine($"CbsRectangle. Handling MouseUp for Index: {ColorBandIndex}.");
				NotifySelectionChange();
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_requestContextMenuShown(ColorBandIndex, ColorBandSetEditMode.Colors);
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
				Fill = DrawingHelper.BuildBrush(startColor, endColor, blend),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Data = area,
				IsHitTestVisible = true
			};

			return result;
		}

		private Shape BuildSelRectanglePath(RectangleGeometry area, bool isCurrent, bool isSelected, bool isUnderMouse, bool parentIsFocused, double strokeThickness)
		{
			var result = new Path()
			{
				Fill = GetSelBackGround(isSelected, isUnderMouse, parentIsFocused),
				Stroke = GetSelStroke(isCurrent, isSelected, isUnderMouse, parentIsFocused),
				StrokeThickness = strokeThickness,
				Data = area,
				IsHitTestVisible = true
			};

			return result;
		}

		private void Resize(double xPosition, double width, ColorBandLayoutViewModel layout)
		{
			//var isCurrent = _isCurrent || _isUnderMouse;

			var isHighLighted = _isSelected || (ParentIsFocused && (_isCurrent || _isUnderMouse));

			_geometry.Rect = BuildRectangle(xPosition, width, isHighLighted, layout);
			_selGeometry.Rect = BuildSelRectangle(xPosition, width, layout);
		}

		private Rect BuildRectangle(double xPosition, double width, bool isHighLighted, ColorBandLayoutViewModel layout)
		{
			var yPosition = layout.CbrElevation;
			var height = layout.CbrHeight;
			var rect = BuildRect(xPosition, yPosition, width, height, layout.ContentScale);

			Rect result;

			if (isHighLighted && rect.Width > 7)
			{
				// Reduce the height to accomodate the outside border. The outside border is 1 pixel thick.
				// Reduce the width to accomodate the section line and the selection background rectangle (SelRectangle).
				// The section line is 2 pixels thick, but we only need to get out of the way of 1/2 this distance.
				// The SelRectangle has a border of 2 pixels. 1 + 2 = 3. Must do this for the right and left side for a total of 6
				// 
				// Decrease the width by 6, if the width > 7. Decrease the height by 2
				result = Rect.Inflate(rect, -3, -1);    
			}
			else
			{
				result = Rect.Inflate(rect, 0, -1);
			}

			return result;
		}

		private Rect BuildSelRectangle(double xPosition, double width, ColorBandLayoutViewModel layout)
		{
			var yPosition = 0;
			var height = layout.ControlHeight;
			var rect = BuildRect(xPosition, yPosition, width, height, layout.ContentScale);

			Rect result;

			// The Stroke width increases the size of the rectangle,
			// subtract 2 pixels on both the left and right so that the border fits between the section lines.
			//
			// Decrease the width by 4, if the width > 7, Decrease the height by 2 
			if (rect.Width > 7)
			{
				result = Rect.Inflate(rect, -2, -1);
			}
			else
			{
				result = Rect.Inflate(rect, 0, -1);
			}

			return result;
		}

		private Rect BuildRect(double xPosition, double yPosition, double width, double height, SizeDbl contentScale)
		{
			//var area = new RectangleDbl(new PointDbl(xPosition, yPosition), new SizeDbl(width, height));
			//var scaledArea = area.Scale(scaleSize);
			//var result = ScreenTypeHelper.ConvertToRect(scaledArea);

			var result = new Rect(new Point(xPosition, yPosition), new Size(width, height));
			result.Scale(contentScale.Width, contentScale.Height);

			return result;
		}

		#endregion

		#region Private Methods - IsCurrent / IsSelected State

		private void NotifySelectionChange()
		{
			_isSelectedChangedCallback(ColorBandIndex, ColorBandSetEditMode.Bands);
		}

		private void UpdateSelectionBackground()
		{
			//var isCurrent = _isCurrent || _isUnderMouse;

			var isHighLighted = _isSelected || (ParentIsFocused && (_isCurrent || _isUnderMouse));

			_geometry.Rect = BuildRectangle(XPosition, Width, isHighLighted, _colorBandLayoutViewModel);

			_selRectanglePath.Stroke = GetSelStroke(_isCurrent, _isSelected, _isUnderMouse, _parentIsFocused);

			_selRectanglePath.Fill = GetSelBackGround(_isSelected, _isUnderMouse, _parentIsFocused);

			Debug.Assert(isHighLighted == (_selRectanglePath.Stroke != DEFAULT_STROKE), "isHighlighted / SelRectangle's stroke mismatch.");
		}

		private Brush GetSelBackGround(bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			Brush result;

			if (parentIsFocused)
			{
				if (isSelected)
				{
					result = IS_SELECTED_BACKGROUND;
				}
				else
				{
					if (isUnderMouse)
					{
						result = IS_HOVERED_BACKGROUND;
					}
					else
					{
						result = DEFAULT_BACKGROUND;
					}
				}
			}
			else
			{
				result = isSelected ? IS_SELECTED_INACTIVE_BACKGROUND : DEFAULT_BACKGROUND;
			}

			return result;
		}

		private Brush GetSelStroke(bool isCurrent, bool isSelected, bool isUnderMouse, bool parentIsFocused)
		{
			Brush result;

			if (parentIsFocused)
			{
				if (isCurrent)
				{
					result = IS_CURRENT_STROKE;
				}
				else
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
			}
			else
			{
				result = isSelected ? IS_SELECTED_INACTIVE_STROKE : DEFAULT_STROKE;
			}

			return result;
		}

		#endregion

		#region Diag

		#endregion
	}
}
