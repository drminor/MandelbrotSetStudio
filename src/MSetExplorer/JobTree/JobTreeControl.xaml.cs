using MongoDB.Bson;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSS.Common;

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

				//Debug.WriteLine("The JobTree UserControl is now loaded");
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
				_ = _vm.RestoreBranch(jobId);
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
				var numberDeleted = _vm.DeleteBranch(jobId);
				_ = MessageBox.Show($"{numberDeleted} jobs were deleted.");
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
			if (e.Parameter is ObjectId jobId)
			{
				_ = MessageBox.Show(_vm.GetDetails(jobId));
			}
		}

		#endregion
	}
}
