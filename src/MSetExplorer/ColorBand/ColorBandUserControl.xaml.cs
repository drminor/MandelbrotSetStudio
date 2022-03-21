using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorPanelControl.xaml
	/// </summary>
	public partial class ColorBandUserControl : UserControl
	{
		private Canvas _canvas;
		private readonly DrawingGroup _drawingGroup;
		private GeometryDrawing _rectangle;

		#region Constructor

		public ColorBandUserControl()
		{
			_drawingGroup = new DrawingGroup();
			_rectangle = BuildRectangle(new SizeDbl(), ColorBandColor.White, ColorBandColor.White);
			_drawingGroup.Children.Add(_rectangle);

			Loaded += ColorPanelControl_Loaded;
			InitializeComponent();
		}

		private void ColorPanelControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandUserControl is being loaded.");
				return;
			}
			else
			{
				_canvas = MainCanvas;

				var rectImage = new Image { Source = new DrawingImage(_drawingGroup) };
				_ = _canvas.Children.Add(rectImage);
				RefreshTheView(new SizeDbl(ActualWidth, ActualHeight), StartColor, EndColor);
				SizeChanged += ColorPanelControl_SizeChanged;

				Debug.WriteLine("The ColorBandUserControl is now loaded.");
			}
		}

		#endregion

		#region Event Handlers

		private void ColorPanelControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshTheView(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize), StartColor, EndColor);
		}

		#endregion

		#region Dependency Properties

		public static readonly DependencyProperty StartColorProperty = DependencyProperty.Register(
			"StartColor",
			typeof(ColorBandColor),
			typeof(ColorBandUserControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnStartColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.White
			});

		public ColorBandColor StartColor
		{
			get => (ColorBandColor)GetValue(StartColorProperty);
			set => SetValue(StartColorProperty, value);
		}

		private static void OnStartColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandColor)e.OldValue;
			var newValue = (ColorBandColor)e.NewValue;

			if (oldValue != newValue)
			{
				if (d is ColorBandUserControl uc)
				{
					uc._rectangle.Brush = uc.BuildBrush(uc.StartColor, uc.EndColor);
				}
			}
		}

		public static readonly DependencyProperty EndColorProperty = DependencyProperty.Register(
			"EndColor",
			typeof(ColorBandColor),
			typeof(ColorBandUserControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnEndColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.Black
			});

		public ColorBandColor EndColor
		{
			get => (ColorBandColor)GetValue(EndColorProperty);
			set => SetValue(EndColorProperty, value);
		}

		private static void OnEndColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandColor)e.OldValue;
			var newValue = (ColorBandColor)e.NewValue;

			if (oldValue != newValue)
			{
				if (d is ColorBandUserControl uc)
				{
					uc._rectangle.Brush = uc.BuildBrush(uc.StartColor, uc.EndColor);
				}
			}
		}
		#endregion

		#region Private Methods

		private void RefreshTheView(SizeDbl size, ColorBandColor startColor, ColorBandColor endColor)
		{
			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Deflate(4);
				
				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				_rectangle.Brush = BuildBrush(startColor, endColor);
				_rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
			}
			else
			{
				_rectangle.Brush = Brushes.Transparent;
			}
		}

		private GeometryDrawing BuildRectangle(SizeDbl size, ColorBandColor startColor, ColorBandColor endColor)
		{
			var result = new GeometryDrawing
				(
				BuildBrush(startColor, endColor),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
				); ;

			return result;
		}

		private Brush BuildBrush(ColorBandColor startColor, ColorBandColor endColor)
		{
			var startC = Color.FromRgb(startColor.ColorComps[0], startColor.ColorComps[1], startColor.ColorComps[2]);
			var endC = Color.FromRgb(endColor.ColorComps[0], endColor.ColorComps[1], endColor.ColorComps[2]);

			var result = new LinearGradientBrush
				(
				new GradientStopCollection
				{
					new GradientStop(startC, 0.0),
					new GradientStop(startC, 0.15),
					new GradientStop(endC, 0.85),
					new GradientStop(endC, 1.0),

				},
				new Point(0.5, 0),
				new Point(0.5, 1)
				);

			return result;
		}


		#endregion
	}
}
