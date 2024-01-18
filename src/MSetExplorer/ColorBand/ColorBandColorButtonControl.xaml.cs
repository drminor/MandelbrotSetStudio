using MSS.Types;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static MongoDB.Driver.WriteConcern;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandColorUserControl.xaml
	/// </summary>
	public partial class ColorBandColorButtonControl : UserControl
	{
		private Canvas _canvas;

		private readonly RectangleGeometry _geometry;
		private readonly Shape _rectanglePath;

		public readonly object objectLock = new object();

		#region Constructor

		public ColorBandColorButtonControl()
		{
			_canvas = new Canvas();

			Loaded += ColorBandColorButtonControl_Loaded;
			Unloaded += ColorBandColorButtonControl_Unloaded;
			InitializeComponent();

			_geometry = BuildGeometry(new SizeDbl());
			_rectanglePath = BuildRectanglePath(_geometry, new ColorBandColor(new byte[] {255, 255, 255}));
			_rectanglePath.Focusable = true;
		}

		private void ColorBandColorButtonControl_Loaded(object sender, RoutedEventArgs e)
		{
			_canvas = MainCanvas;
			_canvas.Children.Add(_rectanglePath);
			RefreshTheView(new SizeDbl(ActualWidth, ActualHeight));

			_rectanglePath.Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(ColorBandColor));

			SizeChanged += ColorPanelControl_SizeChanged;
		}

		private void ColorBandColorButtonControl_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= ColorPanelControl_SizeChanged;

			Loaded -= ColorBandColorButtonControl_Loaded;
			Unloaded -= ColorBandColorButtonControl_Unloaded;
		}

		#endregion

		public event MouseButtonEventHandler CustomMouseUp
		{
			add { lock (objectLock) { _rectanglePath.MouseUp += value; } }
			remove { lock (objectLock) { _rectanglePath.MouseUp -= value; } }
		}

		#region Public Properties

		public ColorBandColor ColorBandColor
		{
			get => (ColorBandColor)GetValue(ColorBandColorProperty);
			set => SetValue(ColorBandColorProperty, value);
		}

		public Canvas Canvas => _canvas;

		#endregion

		#region Event Handlers

		private void ColorPanelControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RefreshTheView(new SizeDbl(ActualWidth, ActualHeight));
		}

		#endregion

		#region Dependency Properties

		public static readonly DependencyProperty ColorBandColorProperty = DependencyProperty.Register(
			"ColorBandColor",
			typeof(ColorBandColor),
			typeof(ColorBandColorButtonControl),
			new FrameworkPropertyMetadata()
			{
				PropertyChangedCallback = OnStartColorChanged,
				BindsTwoWayByDefault = true,
				DefaultValue = ColorBandColor.White
			});

		private static void OnStartColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = (ColorBandColor) e.OldValue;
			var newValue = (ColorBandColor) e.NewValue;

			if (oldValue != newValue)
			{
				if (d is ColorBandColorButtonControl uc)
				{
					uc._rectanglePath.Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(newValue));
				}
			}
		}

		#endregion

		#region Private Methods - Layout

		private void RefreshTheView(SizeDbl size)
		{
			if (size.Width > 5 && size.Height > 5)
			{
				size = size.Deflate(4);

				_canvas.Width = size.Width;
				_canvas.Height = size.Height;

				_geometry.Rect = new Rect(0, 0, size.Width, size.Height);

				_rectanglePath.Visibility = Visibility.Visible;
			}
			else
			{
				_rectanglePath.Visibility = Visibility.Collapsed;
			}
		}

		private Shape BuildRectanglePath(RectangleGeometry rectangleGeometry, ColorBandColor color)
		{
			var result = new Path()
			{
				Fill = new SolidColorBrush(ScreenTypeHelper.ConvertToColor(color)),
				Stroke = Brushes.Transparent,
				StrokeThickness = 0,
				Data = rectangleGeometry,
				Visibility = Visibility.Collapsed
			};

			return result;
		}

		private RectangleGeometry BuildGeometry(SizeDbl size)
		{
			var rect = new Rect(0, 0, size.Width, size.Height);

			var rectGeometry = new RectangleGeometry(rect);
			return rectGeometry;
		}

		#endregion
	}
}
