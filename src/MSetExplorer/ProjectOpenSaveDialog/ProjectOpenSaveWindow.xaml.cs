using MSS.Common;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
			_vm = (IProjectOpenSaveViewModel)DataContext;

			Loaded += ProjectOpenSaveWindow_Loaded;
			ContentRendered += ProjectOpenSaveWindow_ContentRendered;
			InitializeComponent();
		}

		#endregion

		#region Event Handlers

		private void ProjectOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ProjectOpenSaveWindow_Loaded;

			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ProjectOpenSave Window is being loaded.");
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

				lvProjects.MouseDoubleClick += LvProjects_MouseDoubleClick;
				lvProjects.Focusable = true;

				txtName.LostFocus += TxtName_LostFocus;

				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ProjectOpenSave Window is now loaded");
			}
		}

		private void ProjectOpenSaveWindow_ContentRendered(object? sender, System.EventArgs e)
		{
			ContentRendered -= ProjectOpenSaveWindow_ContentRendered;

			if (_vm.DialogType == DialogType.Save)
			{
				_ = txtName.Focus();
			}
			else
			{
				if (lvProjects.Items.Count > 0 && lvProjects.ItemContainerGenerator.ContainerFromItem(lvProjects.Items[0]) is ListViewItem item)
				{
					lvProjects.SelectedIndex = 0;
					_ = item.Focus();
				}
			}
		}

		private void LvProjects_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			TakeSelection();
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

		public string? ProjectName => _vm.SelectedName;
		public string? ProjectDescription => _vm.SelectedDescription;


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

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedName = _vm.SelectedName;

			var res = MessageBox.Show($"Delete project {selectedName}?", "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

			if (res == MessageBoxResult.Yes)
			{
				_ = _vm.DeleteSelected(out var numberOfMapSectionsDeleted)
					? MessageBox.Show($"The project: {selectedName} has been deleted. {numberOfMapSectionsDeleted} map sections were deleted.")
					: MessageBox.Show("Could not delete this Project.");
			}
		}

		private void ViewJobsButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedPoster = _vm.SelectedProject;

			if (selectedPoster != null)
			{
				OpenJobDetailsDialog(selectedPoster);
			}
		}

		private void TrimMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			var msg = "Delete all Map Sections used by this Project except those used by the current Job?";
			var resp = MessageBox.Show(msg, "Trim Map Sections", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

			if (resp == MessageBoxResult.Yes)
			{
				var numberOfMapSectionsDeleted = _vm.TrimSelected(agressive: false);

				_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.");
			}
		}

		private void TrimMapSectionsHeavyButton_Click(object sender, RoutedEventArgs e)
		{
			var msg = "Delete all Map Sections used by this Project except those used by the current Job and...\n";
			msg += "Delete all Map Sections used by the current Job except for those Map Sections required to create the Image.";
			var resp = MessageBox.Show(msg, "Trim Map Sections - Agressive", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

			if (resp == MessageBoxResult.Yes)
			{
				var numberOfMapSectionsDeleted = _vm.TrimSelected(agressive: true);
				_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.");
			}
		}

		private void TakeSelection()
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

		#endregion

		#region Jobs Dialog

		private void OpenJobDetailsDialog(IJobOwnerInfo jobOwnerInfo)
		{
			var jobDetailsViewModel = _vm.ViewModelFactory.CreateAJobDetailsViewModel(jobOwnerInfo);

			var jobDetailsDialog = new JobDetailsWindow
			{
				DataContext = jobDetailsViewModel
			};

			jobDetailsDialog.ShowDialog();
		}

		#endregion

		#region Sort Menu Click Handlers

		private void SortAToZ_Click(object sender, RoutedEventArgs e)
		{
			_vm.SortDescending = mnuItem_sortAz.IsChecked;
			mnuItem_sortZa.IsChecked = !mnuItem_sortAz.IsChecked;
		}

		private void SortZToA_Click(object sender, RoutedEventArgs e)
		{
			_vm.SortDescending = mnuItem_sortZa.IsChecked;
			mnuItem_sortAz.IsChecked = !mnuItem_sortZa.IsChecked;
		}

		private void SortByLastAccessed_Click(object sender, RoutedEventArgs e)
		{
			_vm.SortByFieldName = "LastAccessed";

			mnuItem_sortByDateCreated.IsChecked = false;
			mnuItem_sortByName.IsChecked = false;
		}

		private void SortByDateCreated_Click(object sender, RoutedEventArgs e)
		{
			_vm.SortByFieldName = "DateCreated";

			mnuItem_sortByLastAccessed.IsChecked = false;
			mnuItem_sortByName.IsChecked = false;
		}

		private void SortByName_Click(object sender, RoutedEventArgs e)
		{
			_vm.SortByFieldName = "Name";

			mnuItem_sortByLastAccessed.IsChecked = false;
			mnuItem_sortByDateCreated.IsChecked = false;
		}

		#endregion

	}
}
