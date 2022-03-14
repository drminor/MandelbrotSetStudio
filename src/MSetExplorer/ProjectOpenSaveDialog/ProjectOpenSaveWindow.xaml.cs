using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ProjectOpenSaveWindow.xaml
	/// </summary>
	public partial class ProjectOpenSaveWindow : Window
	{
		private IProjectOpenSaveViewModel _vm;

		#region Constructor 

		public ProjectOpenSaveWindow()
		{
			InitializeComponent();
			Loaded += ProjectOpenSaveWindow_Loaded;
		}

		private void ProjectOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IProjectOpenSaveViewModel)DataContext;
				borderTop.DataContext = DataContext;

				btnSave.Content = _vm.DialogType == DialogType.Open ? "Open" : "Save";
				Title = _vm.DialogType == DialogType.Open ? "Open Project" : "Save Project";

				lvProjects.ItemsSource = _vm.ProjectInfos;
				lvProjects.SelectionChanged += LvProjects_SelectionChanged;
				txtName.LostFocus += TxtName_LostFocus;

				_ = txtName.Focus();
				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ProjectOpenSaveWindow is now loaded");
			}
		}

		private void TxtName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				_vm.SelectedName = _vm.SelectedProject?.Name;
				_vm.UserIsSettingTheName = false;
			}
			else
			{
				if (txtName.Text == _vm.SelectedProject?.Name)
				{
					_vm.UserIsSettingTheName = false;
					_vm.SelectedName = _vm.SelectedProject?.Name;
				}
				else
				{
					_vm.UserIsSettingTheName = true;
				}
			}

			btnSave.IsEnabled = _vm.SelectedName != null;
		}


		private void LvProjects_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			btnSave.IsEnabled = _vm.SelectedName != null;
		}

		#endregion

		#region Public Properties

		public string ProjectName => _vm.SelectedName;
		public string ProjectDescription => _vm.SelectedDescription;

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.DialogType == DialogType.Save)
			{
				if (_vm.IsNameTaken(ProjectName))
				{
					var res = MessageBox.Show("A project already exists with this name. Do you want to overwrite?", "Overwrite Existing Project", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);
					
					if (res == MessageBoxResult.No)
					{
						return;
					}
				}
			}

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
