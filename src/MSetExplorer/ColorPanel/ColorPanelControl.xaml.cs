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
	public partial class ColorPanelControl : UserControl
	{
		//private IColorBandViewModel _vm;
		private Canvas _canvas;
		private readonly DrawingGroup _drawingGroup;

		#region Constructor

		public ColorPanelControl()
		{
			_drawingGroup = new DrawingGroup();
			Loaded += ColorPanelControl_Loaded;
			InitializeComponent();
		}

		private void ColorPanelControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the ColorPanelControl is being loaded.");
				return;
			}
			else
			{
				_canvas = MainCanvas;

				var rectImage = new Image { Source = new DrawingImage(_drawingGroup) };
				_ = _canvas.Children.Add(rectImage);
				RefreshTheView(new SizeDbl(ActualWidth, ActualHeight), StartColor.ColorComps, EndColor.ColorComps);
				SizeChanged += ColorPanelControl_SizeChanged;

				Debug.WriteLine("The ColorPanelControl is now loaded.");
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
			typeof(ColorPanelControl),
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
			if (e.OldValue != e.NewValue)
			{
				// code to be executed on value update
			}
		}

		public static readonly DependencyProperty EndColorProperty = DependencyProperty.Register(
			"EndColor",
			typeof(ColorBandColor),
			typeof(ColorPanelControl),
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
			if (e.OldValue != e.NewValue)
			{
				// code to be executed on value update
			}
		}
		#endregion

		#region Private Methods

		private void RefreshTheView(SizeDbl size, byte[] startColorRgbComps, byte[] endColorRgbComps)
		{
			_drawingGroup.Children.Clear();

			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Inflate(-4);
				
				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				var rectangleDrawing = BuildRectangle(size, startColorRgbComps, endColorRgbComps);
				_drawingGroup.Children.Add(rectangleDrawing);
			}
		}

		private GeometryDrawing BuildRectangle(SizeDbl size, byte[] startColorRgbComps, byte[] endColorRgbComps)
		{
			var startColor = Color.FromRgb(startColorRgbComps[0], startColorRgbComps[1], startColorRgbComps[2]);
			var endColor = Color.FromRgb(endColorRgbComps[0], endColorRgbComps[1], endColorRgbComps[2]);

			var result = new GeometryDrawing
				(
				new LinearGradientBrush
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
					),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
				);

			return result;
		}


		#endregion
	}
}
