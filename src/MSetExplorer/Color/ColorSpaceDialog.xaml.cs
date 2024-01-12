using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorSpaceDialog.xaml
	/// </summary>
	public partial class ColorSpaceDialog : Window
	{
		#region Constructor

		private ColorBandColor _colorBandColor;

		public ColorSpaceDialog(ColorBandColor colorBandColor)
		{
			_colorBandColor = colorBandColor;
			Loaded += ColorSpaceDialog_Loaded; 
			InitializeComponent();
		}

		private void ColorSpaceDialog_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ColorSpaceDialog_Loaded;

			//	clrPicker.Color.RGB_R = _colorBandColor.ColorComps[0];
			//	clrPicker.Color.RGB_G = _colorBandColor.ColorComps[1];
			//	clrPicker.Color.RGB_B = _colorBandColor.ColorComps[2];

			//	//clrPicker.Color = ScreenTypeHelper.ConvertToColor(_colorBandColor);

			//	Debug.WriteLine("The ColorPickerDialog is now loaded");

		}

		#endregion

		#region Public Properties

		//public ColorBandColor SelectedColorBandColor => new(new byte[] { clrPicker.SelectedColor.R, clrPicker.SelectedColor.G, clrPicker.SelectedColor.B });
		public ColorBandColor SelectedColorBandColor => new(new byte[] { 0xcc, 0x1a, 0xFF });

		#endregion

		#region Event Handlers

		//private void ColorPickerDialog_Loaded(object sender, RoutedEventArgs e)
		//{
		//	clrPicker.Color.RGB_R = _colorBandColor.ColorComps[0];
		//	clrPicker.Color.RGB_G = _colorBandColor.ColorComps[1];
		//	clrPicker.Color.RGB_B = _colorBandColor.ColorComps[2];

		//	//clrPicker.Color = ScreenTypeHelper.ConvertToColor(_colorBandColor);

		//	Debug.WriteLine("The ColorPickerDialog is now loaded");
		//}

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
