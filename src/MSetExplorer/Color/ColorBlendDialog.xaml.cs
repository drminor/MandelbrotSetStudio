using MSS.Types;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//using Color = System.Windows.Media.Color;

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
		private ColorBandBlendMethod _blendMethod;

		private WriteableBitmap _gradientBitmap;

		private byte[] _backBuffer;

		public ColorBlendDialog(ColorBandColor startingColor, ColorBandColor endingColor, ColorBandBlendMethod blendMethod)
		{
			_startingColor = startingColor;
			_endingColor = endingColor;
			_blendMethod = blendMethod;

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

			if (_blendMethod == ColorBandBlendMethod.Rgb)
			{
				chkBoxBlendMethodIsHsb.IsChecked = false;
				chkBoxBlendDirIsReversed.IsChecked = false;
			}
			else if (_blendMethod == ColorBandBlendMethod.HsbCw)
			{
				chkBoxBlendMethodIsHsb.IsChecked = true;
				chkBoxBlendDirIsReversed.IsChecked = false;
			}
			else
			{
				chkBoxBlendMethodIsHsb.IsChecked = true;
				chkBoxBlendDirIsReversed.IsChecked = true;
			}


			//var blendMethod = GetBlendMethod(chkBoxBlendMethodIsHsb.IsChecked == true, chkBoxBlendDirIsReversed.IsChecked == true);

			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, _blendMethod);

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

			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
		}

		private void ClrPicker2_ColorChanged(object sender, RoutedEventArgs e)
		{
			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
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

		public ColorBandBlendMethod BlendMethod => GetBlendMethod(chkBoxBlendMethodIsHsb.IsChecked == true, chkBoxBlendDirIsReversed.IsChecked == true);


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

		private void PaintTheBitmap(Color s, Color e, ColorBandBlendMethod blendMethod)
		{
			int errors;

			var c1 = ScreenTypeHelper.ConvertToColorBandColor(s);
			var c2 = ScreenTypeHelper.ConvertToColorBandColor(e);

			if (blendMethod == ColorBandBlendMethod.Rgb)
			{
				errors = PaintTheBitmap(c1, c2);
			}
			else
			{
				var startingHsl = ColorHelper.GetHSB(c1.ColorComps);
				var endingHsl = ColorHelper.GetHSB(c2.ColorComps);

				errors = PaintTheBitmap(startingHsl, endingHsl, blendMethod);
			}

			if (errors > 0)
			{
				Debug.WriteLine($"Got {errors} errors.");
			}

			_gradientBitmap.WritePixels(new Int32Rect(0, 0, BLEND_WIDTH, RECT_HEIGHT), _backBuffer, BLEND_WIDTH * BYTES_PER_PIXEL, 0, 0);
		}

		private int PaintTheBitmap(ColorBandColor startingColor, ColorBandColor endingColor)
		{
			var errorCnt = 0;

			var bv = new BlendVals(startingColor.ColorComps, endingColor.ColorComps);

			var resultRowPtr = 0;
			var resultRowPtrIncrement = BLEND_WIDTH * BYTES_PER_PIXEL;

			for (var j = 0; j < RECT_HEIGHT; j++)
			{
				var resultPtr = resultRowPtr;

				for (var i = 0; i < BLEND_WIDTH; i++)
				{
					var destination = new Span<byte>(_backBuffer, resultPtr, BYTES_PER_PIXEL);
					var stepFactor = i / (double)BLEND_WIDTH;

					var errors = bv.BlendAndPlace(stepFactor, destination);
					errorCnt += errors;

					resultPtr += BYTES_PER_PIXEL;
				}

				resultRowPtr += resultRowPtrIncrement;
			}

			return errorCnt;
		}

		private int PaintTheBitmap(double[] startingHsl, double[] endingHsl, ColorBandBlendMethod blendMethod)
		{
			var errorCnt = 0;

			var direction = blendMethod == ColorBandBlendMethod.HsbCw ? HsbBlendDirection.Clockwise : HsbBlendDirection.CounterClockwise;
			var bv = new BlendValsHSB(startingHsl, endingHsl, direction);
			//var bv = new BlendVals(c1.ColorComps, c2.ColorComps);

			var resultRowPtr = 0;
			var resultRowPtrIncrement = BLEND_WIDTH * BYTES_PER_PIXEL;

			for (var j = 0; j < RECT_HEIGHT; j++)
			{
				var resultPtr = resultRowPtr;

				for (var i = 0; i < BLEND_WIDTH; i++)
				{
					var destination = new Span<byte>(_backBuffer, resultPtr, BYTES_PER_PIXEL);
					var stepFactor = i / (double)BLEND_WIDTH;

					var hsb = bv.Blend(stepFactor, out var errors);
					ColorHelper.PlaceHsb(hsb, destination);
					errorCnt += errors;

					resultPtr += BYTES_PER_PIXEL;
				}

				resultRowPtr += resultRowPtrIncrement;
			}

			return errorCnt;
		}

		private void RaisePropertyChanged(string property)
		{
			if (property != null) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}


		#endregion

		private void handleChkBox_CheckedEvents(object sender, RoutedEventArgs e)
		{
			//var blendMethod = GetBlendMethod(chkBoxBlendMethodIsHsb.IsChecked == true, chkBoxBlendDirIsReversed.IsChecked == true);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
		}

		private ColorBandBlendMethod GetBlendMethod(bool useHsb, bool isReversed)
		{
			var result = useHsb 
				? isReversed 
					? ColorBandBlendMethod.HsbCcw 
					: ColorBandBlendMethod.HsbCw
				: ColorBandBlendMethod.Rgb;

			return result;
		}
	}
}
