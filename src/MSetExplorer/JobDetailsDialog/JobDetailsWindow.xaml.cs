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

				lvJobDetails.ItemsSource = _vm.JobInfos;
				lvJobDetails.SelectionChanged += LvJobDetails_SelectionChanged;

				lvJobDetails.MouseDoubleClick += LvJobDetails_MouseDoubleClick;


				Debug.WriteLine("The JobDetails Window is now loaded");
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
			//btnSave.IsEnabled = _vm.SelectedName != null;
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

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			//var selectedName = _vm.SelectedName;

			//_ = _vm.DeleteSelected(out var numberOfMapSectionsDeleted)
			//	? MessageBox.Show($"The poster: {selectedName} has been deleted. {numberOfMapSectionsDeleted} map sections were deleted.")
			//	: MessageBox.Show($"Could not delete the selected poster: {selectedName}.");
		}

		private void TrimMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			//var numberOfMapSectionsDeleted = _vm.TrimSelected(agressive: false);

			//_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.");
		}

		private void DeleteMapSectionsButton_Click(object sender, RoutedEventArgs e)
		{
			//var numberOfMapSectionsDeleted = _vm.TrimSelected(agressive: true);
			//_ = MessageBox.Show($"{numberOfMapSectionsDeleted} map sections were deleted.");

			//if (mapSectionsDeletedUnsavedJobs > 0 && mapSectionsDeletedUnusedJobs > 0)
			//{
			//	_ = MessageBox.Show($"{introMessage}{mapSectionsDeletedUnsavedJobs} map sections belonging to jobs not saved and {mapSectionsDeletedUnusedJobs} map sections belonging to non-current jobs were deleted.");
			//}
			//else
			//{
			//	if (mapSectionsDeletedUnsavedJobs > 0)
			//	{
			//		_ = MessageBox.Show($"{introMessage}{mapSectionsDeletedUnsavedJobs} map sections belonging to jobs not saved were deleted.");
			//	}
			//	if (mapSectionsDeletedUnusedJobs > 0)
			//	{
			//		_ = MessageBox.Show($"{introMessage}{mapSectionsDeletedUnusedJobs} map sections belonging to non-current jobs were deleted.");
			//	}
			//}
		}


		#endregion

	}
}
