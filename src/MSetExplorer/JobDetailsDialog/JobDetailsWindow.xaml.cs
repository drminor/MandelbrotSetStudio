using MongoDB.Bson;
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
using System.Xml.Linq;

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
				borderTop.DataContext = DataContext;

				lvJobDetails.ItemsSource = _vm.JobInfos;
				lvJobDetails.SelectionChanged += LvJobDetails_SelectionChanged;

				lvJobDetails.MouseDoubleClick += LvJobDetails_MouseDoubleClick;


				Debug.WriteLine("The JobDetails Window is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void JobDetailsWindow_ContentRendered(object? sender, System.EventArgs e)
		{
			if (lvJobDetails.ItemContainerGenerator.ContainerFromItem(lvJobDetails.Items[0]) is ListViewItem item)
			{
				lvJobDetails.SelectedIndex = 0;
				_ = item.Focus();
			}
		}

		private void LvJobDetails_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//TakeSelection();
		}


		private void LvJobDetails_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
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

		private void TrimMapSectionsHeavyButton_Click(object sender, RoutedEventArgs e)
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
