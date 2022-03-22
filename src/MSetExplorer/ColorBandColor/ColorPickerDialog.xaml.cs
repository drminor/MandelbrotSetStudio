using MSS.Types;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorPickerDialog.xaml
	/// </summary>
	public partial class ColorPickerDialog : Window
	{
		#region Constructor

		private ColorBandColor _colorBandColor;

		public ColorPickerDialog(ColorBandColor colorBandColor)
		{
			_colorBandColor = colorBandColor;
			InitializeComponent();
			Loaded += ColorPickerDialog_Loaded;
		}

		#endregion

		#region Public Properties

		public ColorBandColor SelectedColorBandColor => new(new byte[] { clrPicker.SelectedColor.R, clrPicker.SelectedColor.G, clrPicker.SelectedColor.B });

		#endregion

		#region Event Handlers

		private void ColorPickerDialog_Loaded(object sender, RoutedEventArgs e)
		{
			clrPicker.Color.RGB_R = _colorBandColor.ColorComps[0];
			clrPicker.Color.RGB_G = _colorBandColor.ColorComps[1];
			clrPicker.Color.RGB_B = _colorBandColor.ColorComps[2];

			//clrPicker.Color = ScreenTypeHelper.ConvertToColor(_colorBandColor);

			Debug.WriteLine("The ColorPickerDialog is now loaded");
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
	}
}
