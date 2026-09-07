using MSS.Types;
using MSS.Types.PColor;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaceDialog.xaml
	/// </summary>
	public partial class ColorBlendDialog : Window, INotifyPropertyChanged
	{
		#region Constructor

		private const int BYTES_PER_PIXEL = 4;

		private const int BLEND_WIDTH = 450;
		private const int RECT_HEIGHT = 60;

		private ColorBandColor _startingColor;
		private ColorBandColor _endingColor;
		private ColorBandBlendMethod _blendMethod;

		private WriteableBitmap _gradientBitmap;
		private byte[] _backBuffer;

		private bool _handleClrPicker_ColorChangedEvents = true;

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

		private void ColorBlendDialog_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ColorBlendDialog_Loaded;

			clrPicker1.SelectedColor = ScreenTypeHelper.ConvertToColor(_startingColor);
			clrPicker2.SelectedColor = ScreenTypeHelper.ConvertToColor(_endingColor);

			UpdateRgb1(clrPicker1.SelectedColor);
			UpdateRgb2(clrPicker2.SelectedColor);

			if (_blendMethod == ColorBandBlendMethod.Lch)
			{
				chkBoxUseLCH.IsChecked = true;
				stPanLch1.Visibility = Visibility.Visible;
				stPanLch2.Visibility = Visibility.Visible;

				UpdateLch1(clrPicker1.SelectedColor);
				UpdateLch2(clrPicker2.SelectedColor);
			}
			else
			{
				chkBoxUseLCH.IsChecked = false;
				stPanLch1.Visibility = Visibility.Collapsed;
				stPanLch2.Visibility = Visibility.Collapsed;
			}

			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
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

		#endregion

		#region Public Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion

		#region Public Properties

		public ColorBandColor SelectedColor1 => new(clrPicker1.SelectedColor.R, clrPicker1.SelectedColor.G, clrPicker1.SelectedColor.B);
		public ColorBandColor SelectedColor2 => new(clrPicker2.SelectedColor.R, clrPicker2.SelectedColor.G, clrPicker2.SelectedColor.B);

		public WriteableBitmap GradientBitmap
		{
			get => _gradientBitmap;
			set
			{
				_gradientBitmap = value;
				RaisePropertyChanged(nameof(GradientBitmap));
			}
		}

		public ColorBandBlendMethod BlendMethod => chkBoxUseLCH.IsChecked == true ? ColorBandBlendMethod.Lch : ColorBandBlendMethod.Rgb;

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
			var c1 = ScreenTypeHelper.ConvertToColorBandColor(s);
			var c2 = ScreenTypeHelper.ConvertToColorBandColor(e);

			IColorMapEntry colorMapEntry;

			if (blendMethod == ColorBandBlendMethod.Lch)
			{
				colorMapEntry = new ColorMapEntryLCH(BLEND_WIDTH, c1, ColorBandBlendStyle.End, c2, 0, BLEND_WIDTH, false);
			}
			else
			{
				colorMapEntry = new ColorMapEntryRGB(BLEND_WIDTH, c1, ColorBandBlendStyle.End, c2, 0, BLEND_WIDTH, false);
			}

			var errors = PaintTheBitmap(colorMapEntry);

			if (errors > 0)
			{
				Debug.WriteLine($"Got {errors} errors.");
			}

			_gradientBitmap.WritePixels(new Int32Rect(0, 0, BLEND_WIDTH, RECT_HEIGHT), _backBuffer, BLEND_WIDTH * BYTES_PER_PIXEL, 0, 0);
		}

		private int PaintTheBitmap(IColorMapEntry colorMapEntry)
		{
			var errorCnt = 0;

			var resultRowPtr = 0;
			var resultRowPtrIncrement = BLEND_WIDTH * BYTES_PER_PIXEL;

			for (var j = 0; j < RECT_HEIGHT; j++)
			{
				var resultPtr = resultRowPtr;

				for (var i = 0; i < BLEND_WIDTH; i++)
				{
					var destination = new Span<byte>(_backBuffer, resultPtr, BYTES_PER_PIXEL);
					var stepFactor = i / (double)BLEND_WIDTH;

					var errors = colorMapEntry.BlendAndPlace(stepFactor, destination);
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

		private void UpdateRgb1(Color c)
		{
			txtRed1.Text = c.R.ToString();
			txtGreen1.Text = c.G.ToString();
			txtBlue1.Text = c.B.ToString();
		}

		private void UpdateLch1(Color c)
		{
			var lch = ColorHelper.GetLch(c.R, c.G, c.B);
			txtL1.Text = lch.L.ToString();
			txtC1.Text = lch.C.ToString();
			txtH1.Text = lch.H.ToString();
		}

		private void UpdateRgb2(Color c)
		{
			txtRed2.Text = c.R.ToString();
			txtGreen2.Text = c.G.ToString();
			txtBlue2.Text = c.B.ToString();
		}

		private void UpdateLch2(Color c)
		{
			var lch = ColorHelper.GetLch(c.R, c.G, c.B);
			txtL2.Text = lch.L.ToString();
			txtC2.Text = lch.C.ToString();
			txtH2.Text = lch.H.ToString();
		}

		#endregion

		#region Event Handlers

		private void ClrPicker1_ColorChanged(object sender, RoutedEventArgs e)
		{
			if (!_handleClrPicker_ColorChangedEvents) return;

			UpdateRgb1(clrPicker1.SelectedColor);

			if (BlendMethod == ColorBandBlendMethod.Lch)
			{
				UpdateLch1(clrPicker1.SelectedColor);
			}

			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
		}

		private void ClrPicker2_ColorChanged(object sender, RoutedEventArgs e)
		{
			if (!_handleClrPicker_ColorChangedEvents) return;

			UpdateRgb2(clrPicker2.SelectedColor);

			if (BlendMethod == ColorBandBlendMethod.Lch)
			{
				UpdateLch2(clrPicker2.SelectedColor);
			}

			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
		}

		private void chkBoxUseLCHIsUpdated(object sender, RoutedEventArgs e)
		{
			if (BlendMethod == ColorBandBlendMethod.Lch)
			{
				stPanLch1.Visibility = Visibility.Visible;
				stPanLch2.Visibility = Visibility.Visible;

				UpdateLch1(clrPicker1.SelectedColor);
				UpdateLch2(clrPicker2.SelectedColor);
			}
			else
			{
				stPanLch1.Visibility = Visibility.Collapsed;
				stPanLch2.Visibility = Visibility.Collapsed;
			}

			PaintTheBitmap(clrPicker1.SelectedColor, clrPicker2.SelectedColor, BlendMethod);
		}

		private void btnUpdateRGB1_Click(object sender, RoutedEventArgs e)
		{
			var rt = txtRed1.Text;
			var gt = txtGreen1.Text;
			var bt = txtBlue1.Text;

			var r = GetRgbByteFromString(rt);
			var g = GetRgbByteFromString(gt);
			var b = GetRgbByteFromString(bt);

			var c = Color.FromRgb(r, g, b);

			if (BlendMethod == ColorBandBlendMethod.Lch)
			{
				UpdateLch1(c);
			}

			_handleClrPicker_ColorChangedEvents = false;
			clrPicker1.SelectedColor = c;
			_handleClrPicker_ColorChangedEvents = true;
		}

		private void btnUpdateLCH1_Click(object sender, RoutedEventArgs e)
		{
			var lt = txtL1.Text;
			var ct = txtC1.Text;
			var ht = txtH1.Text;

			var l = GetDoubleFromString(lt);
			var c = GetDoubleFromString(ct);
			var h = GetDoubleFromString(ht);

			var lch = new Lch(l, c, h);
			var rgb = ColorHelper.GetRgb(lch);

			var byteVals = rgb.Get_sRGB_Vals();
			var nc = Color.FromRgb(byteVals[0], byteVals[1], byteVals[2]);

			Debug.Print($"The LCH vals are L = {lch.L}, C = {lch.C}, H = {lch.H}.");
			Debug.Print($"The new RGB vals are R:{byteVals[0]}, G:{byteVals[1]}, B: {byteVals[2]}.");
			Debug.Print($"The new color is {nc}.");

			UpdateRgb1(nc);

			_handleClrPicker_ColorChangedEvents = false;
			clrPicker1.SelectedColor = nc;
			_handleClrPicker_ColorChangedEvents = true;
		}

		private void btnUpdateRGB2_Click(object sender, RoutedEventArgs e)
		{
			var rt = txtRed2.Text;
			var gt = txtGreen2.Text;
			var bt = txtBlue2.Text;

			var r = GetRgbByteFromString(rt);
			var g = GetRgbByteFromString(gt);
			var b = GetRgbByteFromString(bt);

			var c = Color.FromRgb(r, g, b);

			if (BlendMethod == ColorBandBlendMethod.Lch)
			{
				UpdateLch2(c);
			}

			_handleClrPicker_ColorChangedEvents = false;
			clrPicker2.SelectedColor = c;
			_handleClrPicker_ColorChangedEvents = true;
		}

		private void btnUpdateLCH2_Click(object sender, RoutedEventArgs e)
		{
			var lt = txtL2.Text;
			var ct = txtC2.Text;
			var ht = txtH2.Text;

			var l = GetDoubleFromString(lt);
			var c = GetDoubleFromString(ct);
			var h = GetDoubleFromString(ht);

			var lch = new Lch(l, c, h);
			var rgb = ColorHelper.GetRgb(lch);

			var byteVals = rgb.Get_sRGB_Vals();

			var nc = Color.FromRgb(byteVals[0], byteVals[1], byteVals[2]);

			UpdateRgb2(nc);

			_handleClrPicker_ColorChangedEvents = false;
			clrPicker2.SelectedColor = nc;
			_handleClrPicker_ColorChangedEvents = true;
		}

		private byte GetRgbByteFromString(string v)
		{
			if (string.IsNullOrEmpty(v))
			{
				return 0;
			}

			try
			{
				var result = Convert.ToByte(v);
				return result;
			}
			catch
			{
				return 0;
			}
		}

		private double GetDoubleFromString(string v)
		{
			if (string.IsNullOrEmpty(v))
			{
				return 0;
			}

			try
			{
				var result = Convert.ToDouble(v);
				return result;
			}
			catch
			{
				return 0;
			}
		}

		#endregion

	}
}
