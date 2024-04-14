using MSS.Types;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaceDialog.xaml
	/// </summary>
	public partial class ColorBlendDialog : Window, INotifyPropertyChanged
	{
		#region Constructor

		private const int BYTES_PER_PIXEL = 4;

		private const int BLEND_WIDTH = 300;
		private const int RECT_HEIGHT = 60;


		private ColorBandColor _startingColor;
		private ColorBandColor _endingColor;

		private WriteableBitmap _gradientBitmap;

		private byte[] _backBuffer;

		public ColorBlendDialog(ColorBandColor startingColor, ColorBandColor endingColor)
		{
			_startingColor = startingColor;
			_endingColor = endingColor;

			_backBuffer = ArrayPool<byte>.Shared.Rent(BLEND_WIDTH * RECT_HEIGHT * BYTES_PER_PIXEL);

			_gradientBitmap = new WriteableBitmap(BLEND_WIDTH, RECT_HEIGHT, 96, 96, PixelFormats.Pbgra32, null);

			Loaded += ColorBlendDialog_Loaded;
			Unloaded += ColorBlendDialog_Unloaded;
			InitializeComponent();
		}

		#endregion

		#region Event Handlers

		private void ColorBlendDialog_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ColorBlendDialog_Loaded;

			//clrPicker.Color = ScreenTypeHelper.ConvertToColor(_colorBandColor);

			//clrPicker1.Color.RGB_R = _startingColor.ColorComps[0];
			//clrPicker1.Color.RGB_G = _startingColor.ColorComps[1];
			//clrPicker1.Color.RGB_B = _startingColor.ColorComps[2];
			clrPicker1.SelectedColor = ScreenTypeHelper.ConvertToColor(_startingColor);

			//clrPicker2.Color.RGB_R = _endingColor.ColorComps[0];
			//clrPicker2.Color.RGB_G = _endingColor.ColorComps[1];
			//clrPicker2.Color.RGB_B = _endingColor.ColorComps[2];

			clrPicker2.SelectedColor = ScreenTypeHelper.ConvertToColor(_endingColor);

			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor);

			clrPicker1.ColorChanged += ClrPicker1_ColorChanged;
			clrPicker2.ColorChanged += ClrPicker2_ColorChanged;

			Debug.WriteLine("The ColorBlendDialog is now loaded");
		}

		private void ColorBlendDialog_Unloaded(object sender, RoutedEventArgs e)
		{
			clrPicker1.ColorChanged -= ClrPicker1_ColorChanged;
			clrPicker2.ColorChanged -= ClrPicker2_ColorChanged;
		}

		private void ClrPicker1_ColorChanged(object sender, RoutedEventArgs e)
		{
			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
		}

		private void ClrPicker2_ColorChanged(object sender, RoutedEventArgs e)
		{
			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
		}

		#endregion

		#region Public Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion

		#region Public Properties

		public ColorBandColor SelectedColorBandColor1 => new(new byte[] { clrPicker1.SelectedColor.R, clrPicker1.SelectedColor.G, clrPicker1.SelectedColor.B });
		public ColorBandColor SelectedColorBandColor2 => new(new byte[] { clrPicker2.SelectedColor.R, clrPicker2.SelectedColor.G, clrPicker2.SelectedColor.B });

		public WriteableBitmap GradientBitmap
		{
			get => _gradientBitmap;
			set
			{
				_gradientBitmap = value;
				RaisePropertyChanged(nameof(GradientBitmap));
			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion

		#region Private Methods

		private void UpdateTheBlendRectangle(Color s, Color e)
		{
			var lGBrush = linearGradientBrush1;

			//lGBrush.GradientStops.Clear();
			//lGBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0.0));
			//lGBrush.GradientStops.Add(new GradientStop(Colors.Beige, 1.0));

			lGBrush.GradientStops[0].Color = s;
			lGBrush.GradientStops[1].Color = e;
		}

		private void PaintTheBitmap(Color s, Color e)
		{
			var errorCnt = 0;

			var c1 = ScreenTypeHelper.ConvertToColorBandColor(s);
			var c2 = ScreenTypeHelper.ConvertToColorBandColor(e);

			//var hue = Color.Get

			var bv = new BlendVals(c1.ColorComps, c2.ColorComps);

			var resultRowPtr = 0;
			var resultRowPtrIncrement = BLEND_WIDTH * BYTES_PER_PIXEL;

			for (var j = 0; j < RECT_HEIGHT; j++)
			{
				var resultPtr = resultRowPtr;

				for (var i = 0; i < BLEND_WIDTH; i++)
				{
					var destination = new Span<byte>(_backBuffer, resultPtr, BYTES_PER_PIXEL);
					var stepFactor = i / (double)BLEND_WIDTH;
					errorCnt += bv.BlendAndPlace(stepFactor, destination);

					resultPtr += BYTES_PER_PIXEL;
				}

				resultRowPtr += resultRowPtrIncrement;
			}

			_gradientBitmap.WritePixels(new Int32Rect(0, 0, BLEND_WIDTH, RECT_HEIGHT), _backBuffer, BLEND_WIDTH * BYTES_PER_PIXEL, 0, 0);
		}

		private void RaisePropertyChanged(string property)
		{
			if (property != null) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		#endregion
	}
}
