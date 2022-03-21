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
		//private IColorBandViewModel _vm;
		private Canvas _canvas;
		private readonly DrawingGroup _drawingGroup;
		private GeometryDrawing _rectangle;

		#region Constructor

		public ColorBandUserControl()
		{
			_drawingGroup = new DrawingGroup();
			_rectangle = BuildRectangle(new SizeDbl(), new byte[] { 0, 0, 0 }, new byte[] { 0, 0, 0 });
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
				RefreshTheView(new SizeDbl(ActualWidth, ActualHeight), StartColor.ColorComps, EndColor.ColorComps);
				SizeChanged += ColorPanelControl_SizeChanged;

				Debug.WriteLine("The ColorBandUserControl is now loaded.");
			}
		}

		#endregion

		#region Event Handlers

		private void ColorPanelControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshTheView(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize), StartColor.ColorComps, EndColor.ColorComps);
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
					var startColor = Color.FromRgb(newValue.ColorComps[0], newValue.ColorComps[1], newValue.ColorComps[2]);
					var endColor = Color.FromRgb(uc.EndColor.ColorComps[0], uc.EndColor.ColorComps[1], uc.EndColor.ColorComps[2]);

					uc._rectangle.Brush = uc.BuildBrush(startColor, endColor);
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
					var startColor = Color.FromRgb(uc.StartColor.ColorComps[0], uc.StartColor.ColorComps[1], uc.StartColor.ColorComps[2]);
					var endColor = Color.FromRgb(newValue.ColorComps[0], newValue.ColorComps[1], newValue.ColorComps[2]);

					uc._rectangle.Brush = uc.BuildBrush(startColor, endColor);
				}
			}
		}
		#endregion

		#region Private Methods

		private void RefreshTheView(SizeDbl size, byte[] startColorComps, byte[] endColorComps)
		{
			//_drawingGroup.Children.Clear();

			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Deflate(4);
				
				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				//var rectangleDrawing = BuildRectangle(size, startColorRgbComps, endColorRgbComps);
				//_drawingGroup.Children.Add(rectangleDrawing);

				var startColor = Color.FromRgb(startColorComps[0], startColorComps[1], startColorComps[2]);
				var endColor = Color.FromRgb(endColorComps[0], endColorComps[1], endColorComps[2]);

				_rectangle.Brush = BuildBrush(startColor, endColor);
				_rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
			}
			else
			{
				_rectangle.Brush = Brushes.Transparent;
			}
		}

		private GeometryDrawing BuildRectangle(SizeDbl size, byte[] startColorComps, byte[] endColorComps)
		{
			var startColor = Color.FromRgb(startColorComps[0], startColorComps[1], startColorComps[2]);
			var endColor = Color.FromRgb(endColorComps[0], endColorComps[1], endColorComps[2]);

			var result = new GeometryDrawing
				(
				BuildBrush(startColor, endColor),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
				); ;

			return result;
		}

		private Brush BuildBrush(Color startColor, Color endColor)
		{
			var result = new LinearGradientBrush
				(
				new GradientStopCollection
				{
					new GradientStop(startColor, 0.0),
					new GradientStop(startColor, 0.15),
					new GradientStop(endColor, 0.85),
					new GradientStop(endColor, 1.0),

				},
				new Point(0.5, 0),
				new Point(0.5, 1)
				);

			return result;
		}


		#endregion
	}
}
