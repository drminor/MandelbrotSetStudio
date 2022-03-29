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
	/// Interaction logic for ColorBandSetOpenSaveWindow.xaml
	/// </summary>
	public partial class ColorBandSetOpenSaveWindow : Window
	{
		private IColorBandSetOpenSaveViewModel _vm;

		#region Constructor

		public ColorBandSetOpenSaveWindow()
		{
			InitializeComponent();
			Loaded += ColorBandSetOpenSaveWindow_Loaded;
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
				_vm = (IColorBandSetOpenSaveViewModel)DataContext;
				//borderTop.DataContext = DataContext;

				//btnSave.Content = _vm.DialogType == DialogType.Open ? "Open" : "Save";
				//Title = _vm.DialogType == DialogType.Open ? "Open Project" : "Save Project";

				//lvProjects.ItemsSource = _vm.ProjectInfos;
				//lvProjects.SelectionChanged += LvProjects_SelectionChanged;

				//lvProjects.MouseDoubleClick += LvProjects_MouseDoubleClick;

				//txtName.LostFocus += TxtName_LostFocus;

				//_ = txtName.Focus();
				//btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ProjectOpenSaveWindow is now loaded");
			}
		}

		#endregion


		#region Event Handlers

		//private void LvProjects_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		//{
		//	TakeSelection();
		//}

		//private void TxtName_LostFocus(object sender, RoutedEventArgs e)
		//{
		//	if (string.IsNullOrWhiteSpace(txtName.Text))
		//	{
		//		_vm.SelectedName = _vm.SelectedProject?.Name;
		//		_vm.UserIsSettingTheName = false;
		//	}
		//	else
		//	{
		//		if (txtName.Text == _vm.SelectedProject?.Name)
		//		{
		//			_vm.UserIsSettingTheName = false;
		//			_vm.SelectedName = _vm.SelectedProject?.Name;
		//		}
		//		else
		//		{
		//			_vm.UserIsSettingTheName = true;
		//		}
		//	}

		//	btnSave.IsEnabled = _vm.SelectedName != null;
		//}


		//private void LvProjects_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		//{
		//	btnSave.IsEnabled = _vm.SelectedName != null;
		//}

		#endregion

		#region Public Properties

		public string ColorBandSetName => _vm.SelectedName;
		public string ColorBandSetDescription => _vm.SelectedDescription;
		public int ColorBandSetVersionNumber => _vm.SelectedVersionNumber;

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
			if (_vm.DialogType == DialogType.Save)
			{
				if (_vm.IsNameTaken(ColorBandSetName))
				{
					var res = MessageBox.Show("A ColorBandSet already exists with this name. Do you want to overwrite?", "Overwrite Existing Project", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

					if (res == MessageBoxResult.No)
					{
						return;
					}
				}
			}

			DialogResult = true;
			Close();
		}

		#endregion
	}
}
