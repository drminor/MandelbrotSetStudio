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
using System.Xml.Linq;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetRenameDialog.xaml
	/// </summary>
	public partial class ColorBandSetRenameWindow : Window
	{

		private ColorBandSetRenameViewModel _vm;

		#region Constructor

		public ColorBandSetRenameWindow()
		{
			_vm = (ColorBandSetRenameViewModel)DataContext;
			Loaded += ColorBandSetOpenSaveWindow_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (ColorBandSetRenameViewModel)DataContext;
				TopGrid.DataContext = DataContext;

				//txtName.LostFocus += TxtName_LostFocus;

				_ = txtNameNew.Focus();
				btnSave.IsEnabled = _vm.SelectedNameSource != null;

				Debug.WriteLine("The ColorBandSetOpenSaveWindow is now loaded");
			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			//if (ColorBandSetName != null)
			//{
			//	TakeSelection(ColorBandSetName);
			//}

			if (_vm.AreNamesOk())
			{

			}

		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TakeSelection(string selectedName)
		{
			if (_vm.IsNameTaken(selectedName))
			{
				var msg = "A ColorBandSet already exists with this name. Do you want to overwrite?";
				var res = MessageBox.Show(msg, "Overwrite Existing ColorBandSet", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

				if (res == MessageBoxResult.No)
				{
					return;
				}
			}

			if (_vm.IsNameTaken(selectedName))
			{
				var msg = $"Opening the selected ColorBandSet will result in a new ColorBandSet being created with Target Iterations = {_vm.TargetIterations}. " +
					$"A ColorBandSet already exists with this name with the Target Iterations = {_vm.TargetIterations}. Do you want to overwrite?";

				var res = MessageBox.Show(msg, "Overwrite Existing ColorBandSet", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

				if (res == MessageBoxResult.No)
				{
					return;
				}
			}

			DialogResult = true;
			Close();
		}

		#endregion

	}
}
