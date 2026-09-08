using System.Diagnostics;
using System.Windows;

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
			Loaded += ColorBandSetRenameWindow_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetRenameWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ColorBandSetRename Window is being loaded.");
				return;
			}
			else
			{
				_vm = (ColorBandSetRenameViewModel)DataContext;
				TopGrid.DataContext = DataContext;

				//txtName.LostFocus += TxtName_LostFocus;

				_ = txtNameNew.Focus();
				btnSave.IsEnabled = _vm.SelectedNameSource != null;

				Debug.WriteLine("The ColorBandSetRename Window is now loaded");
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
