using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaceDialog.xaml
	/// </summary>
	public partial class ColorBlendDialog : Window
	{
		#region Constructor

		private ColorBandColor _startingColor;
		private ColorBandColor _endingColor;

		public ColorBlendDialog(ColorBandColor startingColor, ColorBandColor endingColor)
		{
			_startingColor = startingColor;
			_endingColor = endingColor;
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
		}

		private void ClrPicker2_ColorChanged(object sender, RoutedEventArgs e)
		{
			UpdateTheBlendRectangle(clrPicker1.SelectedColor, clrPicker2.SelectedColor);
		}

		#endregion

		#region Public Properties

		public ColorBandColor SelectedColorBandColor1 => new(new byte[] { clrPicker1.SelectedColor.R, clrPicker1.SelectedColor.G, clrPicker1.SelectedColor.B });
		public ColorBandColor SelectedColorBandColor2 => new(new byte[] { clrPicker2.SelectedColor.R, clrPicker2.SelectedColor.G, clrPicker2.SelectedColor.B });

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

		#endregion
	}
}
