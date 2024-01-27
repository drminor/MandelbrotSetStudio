using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbColorPair
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

		private Rect _container;

		private ColorBandColor _startColor;
		private ColorBandColor _endColor;
		private double _opacity;

		private RectangleGeometry _startGeometry;
		private readonly Shape _startColorBlockPath;

		private RectangleGeometry _endGeometry;
		private readonly Shape _endColorBlockPath;

		#endregion

		#region Constructor

		public CbColorPair(int colorBandIndex, Rect containerArea, ColorBandColor startColor, ColorBandColor endColor, Canvas canvas)
		{
			ColorBandIndex = colorBandIndex;

			_canvas = canvas;

			_container = containerArea;

			_startColor = startColor;
			_endColor = endColor;

			_startGeometry = new RectangleGeometry(BuildColorBlockStart(_container));
			_startColorBlockPath = BuildStartColorBlockPath(_startGeometry, _startColor);
			_canvas.Children.Add(_startColorBlockPath);
			_startColorBlockPath.SetValue(Panel.ZIndexProperty, 20);

			_endGeometry = new RectangleGeometry(BuildColorBlockEnd(_container, _startGeometry.Rect));
			_endColorBlockPath = BuildEndColorBlockPath(_endGeometry, _endColor);
			_canvas.Children.Add(_endColorBlockPath);
			_endColorBlockPath.SetValue(Panel.ZIndexProperty, 20);
		}

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public RectangleGeometry StartColorGeometry => _startGeometry;
		public RectangleGeometry EndColorGeometry => _endGeometry;

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

		#endregion

		#region Public Properies - Layout

		public Rect Container
		{
			get => _container;
			set
			{
				if (value != _container)
				{
					_container = value;

					ResizeColorBlocks(_container);
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
					_startColorBlockPath.Opacity = value;
					_endColorBlockPath.Opacity = value;
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
					_canvas.Children.Remove(_startColorBlockPath);
					_canvas.Children.Remove(_endColorBlockPath);
				}
			}
			catch
			{
				Debug.WriteLine("CbSectionLine encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Private Methods - Layout

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

		private void ResizeColorBlocks(Rect container)
		{
			_startColorBlockPath.Visibility = container.Width > 2 ? Visibility.Visible : Visibility.Collapsed;
			_startGeometry.Rect = BuildColorBlockStart(container);

			_endColorBlockPath.Visibility = container.Width > 10 ? Visibility.Visible : Visibility.Collapsed;
			_endGeometry.Rect = BuildColorBlockEnd(container, _startGeometry.Rect);
		}

		private Rect BuildColorBlockStart(Rect container)
		{
			if (container.Height < 7 || container.Width < 3)
			{
				return new Rect();
			}

			//var yPosition = colorBlocksArea.Y + 3;
			//var height = colorBlocksArea.Height - 6;

			var yPosition = container.Y + 3;
			var height = container.Height - 6;


			var (left, width) = GetBlock1Pos(container.X, container.Width);

			var result = new Rect(left, yPosition, width, height);
			return result;
		}

		private Rect BuildColorBlockEnd(Rect container, Rect block1Rect)
		{
			if (container.Height < 7 || container.Width < 11)
			{
				return new Rect();
			}

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

		private Rect BuildRect(Rect r, SizeDbl contentScale)
		{
			var result = new Rect(r.Location, r.Size);
			result.Scale(contentScale.Width, contentScale.Height);

			return result;
		}

		#endregion
	}
}
