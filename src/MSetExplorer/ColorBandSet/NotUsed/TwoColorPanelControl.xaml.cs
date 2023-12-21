using MSS.Types;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorPanelControl.xaml
	/// </summary>
	public partial class TwoColorPanelControl : UserControl
	{
		private Canvas _canvas;
		private readonly DrawingGroup _drawingGroup;
		private GeometryDrawing _rectangle;

		#region Constructor

		//static TwoColorPanelControl()
		//{
		//	DefaultStyleKeyProperty.OverrideMetadata(typeof(TwoColorPanelControl), new FrameworkPropertyMetadata(typeof(TwoColorPanelControl)));
		//}

		public TwoColorPanelControl()
		{
			_canvas = new Canvas();
			_drawingGroup = new DrawingGroup();
			_rectangle = DrawingHelper.BuildRectangle(new RectangleDbl(), ColorBandColor.White, ColorBandColor.White, horizBlend:false);
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

				//Debug.WriteLine("The ColorBandUserControl is now loaded.");
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
			typeof(TwoColorPanelControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.White
			});

		public ColorBandColor StartColor
		{
			get => (ColorBandColor)GetValue(StartColorProperty);
			set => SetValue(StartColorProperty, value);
		}

		public static readonly DependencyProperty EndColorProperty = DependencyProperty.Register(
			"EndColor",
			typeof(ColorBandColor),
			typeof(TwoColorPanelControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.Black
			});

		public ColorBandColor EndColor
		{
			get => (ColorBandColor)GetValue(EndColorProperty);
			set => SetValue(EndColorProperty, value);
		}

		private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandColor)e.OldValue;
			var newValue = (ColorBandColor)e.NewValue;

			if (oldValue != newValue)
			{
				if (d is TwoColorPanelControl uc)
				{
					uc._rectangle.Brush = DrawingHelper.BuildBrush(uc.StartColor, uc.EndColor, horizBlend:false);
				}
			}
		}

		#endregion

		#region Private Methods

		private void RefreshTheView(SizeDbl size, ColorBandColor startColor, ColorBandColor endColor)
		{
			size = size.Deflate(4);
			DrawingHelper.UpdateRectangleDrawing(_rectangle, size, startColor, endColor, horizBlend:false);

			if (size.Width > 0 && size.Height > 0)
			{
				_canvas.Width = size.Width;
				_canvas.Height = size.Height;
			}
			else
			{
				_canvas.Width = 1;
				_canvas.Height = 1;
			}
		}

		//private void RefreshTheViewOld(SizeDbl size, ColorBandColor startColor, ColorBandColor endColor)
		//{
		//	if (size.Width > 5 && size.Height > 5)
		//	{
		//		size = size.Deflate(4);

		//		_canvas.Width = size.Width;
		//		_canvas.Height = size.Height;

		//		_rectangle.Brush = DrawingHelper.BuildBrush(startColor, endColor);
		//		_rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
		//	}
		//	else
		//	{
		//		_rectangle.Brush = Brushes.Transparent;
		//	}
		//}
		
		//private GeometryDrawing BuildRectangle(SizeDbl size, ColorBandColor startColor, ColorBandColor endColor)
		//{
		//	var result = new GeometryDrawing
		//		(
		//		BuildBrush(startColor, endColor),
		//		new Pen(Brushes.Transparent, 0),
		//		new RectangleGeometry(ScreenTypeHelper.CreateRect(size))
		//		);

		//	return result;
		//}

		//private Brush BuildBrush(ColorBandColor startColor, ColorBandColor endColor)
		//{
		//	var startC = ScreenTypeHelper.ConvertToColor(startColor);
		//	var endC = ScreenTypeHelper.ConvertToColor(endColor);

		//	var result = new LinearGradientBrush
		//		(
		//		new GradientStopCollection
		//		{
		//			new GradientStop(startC, 0.0),
		//			new GradientStop(startC, 0.15),
		//			new GradientStop(endC, 0.85),
		//			new GradientStop(endC, 1.0),

		//		},
		//		new Point(0.5, 0),
		//		new Point(0.5, 1)
		//		);

		//	return result;
		//}


		#endregion
	}
}
