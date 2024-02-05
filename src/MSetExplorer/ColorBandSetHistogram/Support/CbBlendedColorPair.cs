using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbBlendedColorPair : ICloneable
	{
		#region Private Fields

		private readonly Canvas _canvas;

		private Rect _container;

		private ColorBandColor _startColor;
		private ColorBandColor _endColor;
		private bool _blend;

		private RectangleGeometry _containerGeometry;
		private Shape _containerPath;

		#endregion

		#region Constructor

		public CbBlendedColorPair(Rect container, ColorBandColor startColor, ColorBandColor endColor, bool blend, Canvas canvas)
		{
			//ColorBandIndex = colorBandIndex;

			_canvas = canvas;

			_container = container;

			_startColor = startColor;
			_endColor = endColor;
			_blend = blend;

			_containerGeometry = new RectangleGeometry(container);
			_containerPath = BuildRectanglePath(_containerGeometry, startColor, endColor, blend);
			_canvas.Children.Add(_containerPath);
			_containerPath.SetValue(Panel.ZIndexProperty, 20);
		}

		#endregion

		#region Public Properties

		//public int ColorBandIndex { get; set; }

		public RectangleGeometry ContainerGeometry => _containerGeometry;

		public Path ContainerPath
		{
			get => (Path)_containerPath;
			set => _containerPath = value;
		}

		public ColorBandColor StartColor
		{
			get => _startColor;
			set
			{
				if (value != _startColor)
				{
					_startColor = value;
					_containerPath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);

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
					_containerPath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);
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
					_containerPath.Fill = GetBlendedBrush(_startColor, _endColor, _blend);
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
					_containerGeometry.Rect = value;
				}
			}
		}

		public double Opacity
		{
			get => _containerPath.Opacity;
			set
			{
				_containerPath.Opacity = value;
			}
		}

		public Visibility Visibility
		{
			get => _containerPath.Visibility;

			set
			{
				_containerPath.Visibility = value;
			}
		}

		#endregion

		#region Public Methods

		object ICloneable.Clone()
		{
			return Clone();
		}

		public CbBlendedColorPair Clone()
		{
			var result = new CbBlendedColorPair(Container, StartColor, EndColor, Blend, _canvas);
			return result;
		}

		public void TearDown()
		{
			try
			{
				if (_canvas != null)
				{
					_canvas.Children.Remove(_containerPath);
				}
			}
			catch
			{
				Debug.WriteLine("CbSectionLine encountered an exception in TearDown.");
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
	}
}
