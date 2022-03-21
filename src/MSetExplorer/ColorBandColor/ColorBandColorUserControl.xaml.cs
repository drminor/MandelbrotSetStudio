using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandColorUserControl.xaml
	/// </summary>
	public partial class ColorBandColorUserControl : UserControl
	{
		private Canvas _canvas;
		private readonly DrawingGroup _drawingGroup;
		private GeometryDrawing _rectangle;

		#region Constructor

		public ColorBandColorUserControl()
		{
			_drawingGroup = new DrawingGroup();
			_rectangle = BuildRectangle(new SizeDbl(), new byte[] { 0, 0, 0 });
			_drawingGroup.Children.Add(_rectangle);

			Loaded += ColorPanelControl_Loaded;
			InitializeComponent();
		}

		private void ColorPanelControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorBandColorUserControl is being loaded.");
				return;
			}
			else
			{
				_canvas = MainCanvas;

				var rectImage = new Image { Source = new DrawingImage(_drawingGroup) };
				_ = _canvas.Children.Add(rectImage);
				rectImage.Focusable = true;

				RefreshTheView(new SizeDbl(ActualWidth, ActualHeight), ColorBandColor.ColorComps);

				SizeChanged += ColorPanelControl_SizeChanged;
				rectImage.MouseUp += RectImage_MouseUp;

				Debug.WriteLine("The ColorBandColorUserControl is now loaded.");
			}
		}

		#endregion

		#region Event Handlers

		private void ColorPanelControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshTheView(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize), ColorBandColor.ColorComps);
		}

		private void RectImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			MessageBox.Show("Hi");
		}

		#endregion

		#region Dependency Properties

		public static readonly DependencyProperty ColorBandColorProperty = DependencyProperty.Register(
			"ColorBandColor",
			typeof(ColorBandColor),
			typeof(ColorBandColorUserControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.White
			});

		public ColorBandColor ColorBandColor
		{
			get => (ColorBandColor)GetValue(ColorBandColorProperty);
			set => SetValue(ColorBandColorProperty, value);
		}

		private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandColor) e.OldValue;
			var newValue = (ColorBandColor) e.NewValue;

			if (oldValue != newValue)
			{
				if (d is ColorBandColorUserControl uc)
				{
					uc._rectangle.Brush = new SolidColorBrush(Color.FromRgb(newValue.ColorComps[0], newValue.ColorComps[1], newValue.ColorComps[2]));
				}
			}
		}

		#endregion

		#region Private Methods

		private void RefreshTheView(SizeDbl size, byte[] colorComps)
		{
			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Deflate(4);

				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				//var rectangleDrawing = BuildRectangle(size, startColorRgbComps);
				//_drawingGroup.Children.Add(rectangleDrawing);

				_rectangle.Brush = new SolidColorBrush(Color.FromRgb(colorComps[0], colorComps[1], colorComps[2]));
				_rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
			}
			else
			{
				_rectangle.Brush = Brushes.Transparent;
			}
		}

		private GeometryDrawing BuildRectangle(SizeDbl size, byte[] startColorRgbComps)
		{
			var startColor = Color.FromRgb(startColorRgbComps[0], startColorRgbComps[1], startColorRgbComps[2]);

			var result = new GeometryDrawing
				(
				new SolidColorBrush(startColor),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
				);

			return result;
		}

		#endregion
	}
}
