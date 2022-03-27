using System.Diagnostics;
using System.Windows;

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
