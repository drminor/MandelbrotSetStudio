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
	/// Interaction logic for ColorBandEditorDialog.xaml
	/// </summary>
	public partial class ColorBandEditorDialog : Window
	{
		#region Constructor

		public ColorBandEditorDialog()
		{
			InitializeComponent();
			Loaded += ColorBandEditorDialog_Loaded;
		}

		#endregion

		#region Event Handlers

		private void ColorBandEditorDialog_Loaded(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("The ColorBandEditorDialog is now loaded");
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			TakeSelection();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TakeSelection()
		{
			DialogResult = true;
			Close();
		}

		#endregion
	}
}
