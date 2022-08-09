using MongoDB.Bson;
using MSS.Common;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobTreeControl.xaml
	/// </summary>
	public partial class JobTreeControl : UserControl
	{
		private IJobTreeViewModel _vm;

		#region Constructor 

		public JobTreeControl()
		{
			_vm = (IJobTreeViewModel)DataContext;
			Loaded += JobTreeControl_Loaded;
			InitializeComponent();
		}

		private void JobTreeControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the JobTree UserControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IJobTreeViewModel)DataContext;

				JobTreeItem.IsSelectedColor = GetColorString(Colors.Tan);
				JobTreeItem.IsParentSelectedColor = GetColorString(Colors.LightPink);
				JobTreeItem.IsSiblingSelectedColor = GetColorString(Colors.LightGoldenrodYellow);
				JobTreeItem.IsChildSelectedColor = GetColorString(Colors.LightCyan);

				trvJobs.SelectedItemChanged += TrvJobs_SelectedItemChanged;

				//Debug.WriteLine("The JobTree UserControl is now loaded");
			}
		}

		private string GetColorString(Color color)
		{
			var colorStringWithOpacity = color.ToString(CultureInfo.InvariantCulture);
			var result = "#" + new string(colorStringWithOpacity.ToCharArray(), 3, 6);
			return result;
		}

		private void TrvJobs_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (trvJobs.SelectedItem is JobTreeItem selectedItem)
			{
				_vm.SelectedViewItem = selectedItem;
			}
		}

		#endregion

		#region Button Handlers 

		private void JobTreeItem_KeyUp(object sender, KeyEventArgs e)
		{
			if (sender is StackPanel sp)
			{
				if (e.Key == Key.C && (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl)))
				{
					if (sp.Tag is string strJobId)
					{
						Clipboard.SetText(strJobId);
					}
				}
			}
		}

		#endregion

		#region Command Binding Handlers

		// MoveTo CanExecute
		private void MoveToCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.CurrentProject != null;
		}

		// MoveTo
		private void MoveToCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: OJ -- Need the TransformType to find Alternate Display Jobs
			if (e.Parameter is ObjectId jobId)
			{
				if (_vm.TryGetJob(jobId, out var job))
				{
					_vm.CurrentJob = job;
				}
			}
		}

		// Restore Current Branch CanExecute
		private void RestoreBranchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (e.Parameter is ObjectId jobId)
			{
				var currentPath = _vm.GetPath(jobId);
				e.CanExecute = currentPath?.Count > 1;
			}
		}

		// Restore Current Branch
		private void RestoreBranchCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Parameter is ObjectId jobId)
			{
				try
				{
					_ = _vm.RestoreBranch(jobId);
				}
				catch (System.InvalidOperationException ioe)
				{
					MessageBox.Show($"Restore failed. Error = {ioe.Message}.");
				}
			}
		}

		// Delete CanExecute
		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.CurrentProject != null;
		}

		// Delete
		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Parameter is ObjectId jobId)
			{
				var numberJobsDeleted = _vm.DeleteBranch(jobId, out var numberOfMapSectionsDeleted);

				_ = MessageBox.Show($"{numberJobsDeleted} jobs and {numberOfMapSectionsDeleted} map sections were deleted.");
			}
		}

		// Show Details  CanExecute
		private void ShowDetailsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.CurrentProject != null;
		}

		// Show Details
		private void ShowDetailsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: OJ -- Need the TransformType to find Alternate Display Jobs
			if (e.Parameter is ObjectId jobId)
			{
				var details = _vm.GetDetails(jobId) + "\n\nCopy To Clipboard?";

				var resp = MessageBox.Show(details, jobId.ToString(), MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No);
				if (resp == MessageBoxResult.Yes)
				{
					Clipboard.SetText(details);
				}
			}
		}

		#endregion
	}
}
