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
			_rectangle = BuildRectangle(new SizeDbl(), ColorBandColor.White);
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

				RefreshTheView(new SizeDbl(ActualWidth, ActualHeight), ColorBandColor);

				SizeChanged += ColorPanelControl_SizeChanged;
				rectImage.MouseUp += RectImage_MouseUp;

				Debug.WriteLine("The ColorBandColorUserControl is now loaded.");
			}
		}

		#endregion

		#region Event Handlers

		private void ColorPanelControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshTheView(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize), ColorBandColor);
		}

		private void RectImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (ShowColorPicker(ColorBandColor, out var selectedColor))
			{
				ColorBandColor = selectedColor;
			}
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
					uc._rectangle.Brush = uc.BuildBrush(newValue);
				}
			}
		}

		#endregion

		#region Private Methods

		private bool ShowColorPicker(ColorBandColor initalColor, out ColorBandColor selectedColor)
		{
			var colorPickerDialalog = new ColorPickerDialog(initalColor);

			if (colorPickerDialalog.ShowDialog() == true)
			{
				selectedColor = colorPickerDialalog.SelectedColorBandColor;
				return true;
			}
			else
			{
				selectedColor = ColorBandColor.Black;
				return false;
			}
		}

		private void RefreshTheView(SizeDbl size, ColorBandColor color)
		{
			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Deflate(4);

				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				_rectangle.Brush = BuildBrush(color);
				_rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
			}
			else
			{
				_rectangle.Brush = Brushes.Transparent;
			}
		}

		private GeometryDrawing BuildRectangle(SizeDbl size, ColorBandColor color)
		{
			var result = new GeometryDrawing
				(
				BuildBrush(color),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
				);

			return result;
		}

		private Brush BuildBrush(ColorBandColor color)
		{
			var result = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(color));

			return result;
		}

		#endregion
	}
}
