using MSS.Common;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobDetailsWindow.xaml
	/// </summary>
	public partial class JobDetailsWindow : Window
	{
		private JobDetailsViewModel _vm;

		#region Constructor 

		public JobDetailsWindow()
		{
			_vm = (JobDetailsViewModel)DataContext;

			Loaded += JobDetailsWindow_Loaded;
			ContentRendered += JobDetailsWindow_ContentRendered;
			InitializeComponent();
		}

		private void JobDetailsWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the JobDetails Window is being loaded.");
				return;
			}
			else
			{
				_vm = (JobDetailsViewModel)DataContext;
				//borderDetails.DataContext = DataContext;

				_vm.PropertyChanged += JobDetailsViewModel_PropertyChanged;

				lvJobDetails.ItemsSource = _vm.JobInfos;
				lvJobDetails.SelectionChanged += LvJobDetails_SelectionChanged;

				lvJobDetails.MouseDoubleClick += LvJobDetails_MouseDoubleClick;

				Debug.WriteLine("The JobDetails Window is now loaded");
			}
		}

		private void JobDetailsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "JobInfos")
			{
				lvJobDetails.ItemsSource = _vm.JobInfos;
			}
		}

		#endregion

		#region Event Handlers

		private void JobDetailsWindow_ContentRendered(object? sender, EventArgs e)
		{
			if (lvJobDetails.ItemContainerGenerator.ContainerFromItem(lvJobDetails.Items[0]) is ListViewItem item)
			{
				lvJobDetails.SelectedIndex = 0;
				_ = item.Focus();
			}
		}

		private void LvJobDetails_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//TakeSelection();
		}


		private void LvJobDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_vm.SelectedJobInfo = (IJobInfo) lvJobDetails.SelectedItem;
			btnDelete.IsEnabled = !_vm.TheCurrentJobIsSelected;
		}

		#endregion

		#region Public Properties

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

		private void DeleteJobButton_Click(object sender, RoutedEventArgs e)
		{
			string msg;
			
			if (_vm.DeleteSelectedJob(out var numberOfMapSectionsDeleted))
			{
				msg = $"The job has been deleted. {numberOfMapSectionsDeleted} map sections were deleted.";
			}
			else
			{
				msg = $"Could not delete the selected job.";
			}

			_ = MessageBox.Show(msg, "Delete Job", MessageBoxButton.OK);
		}

		private void DeleteMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.TheCurrentJobIsSelected)
			{
				if (MessageBoxResult.Yes != MessageBox.Show("This will delete all Map Sections for the current job. Are you sure?", "Delete All Map Sections", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.Yes))
				{
					return;
				}
			}

			var numberOfMapSectionsDeleted = _vm.DeleteAllMapSectionsForSelectedJob();
			_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.", "Delete All Map Sections");
		}

		private void TrimMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			var numberOfMapSectionsDeleted = _vm.TrimMapSectionsForSelectedJob();
			_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.", "Trim Map Sections");
		}

		#endregion

	}
}
