using MongoDB.Bson;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
				//_vm.PropertyChanged += ViewModel_PropertyChanged;

				//Debug.WriteLine("The JobTree UserControl is now loaded");
			}
		}

		//private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(IJobTreeViewModel.CurrentJob))
		//	{
		//		var currentJob = _vm.CurrentJob;
		//		if (currentJob is null)
		//		{
		//			return;
		//		}

		//		var collectionSource = trvJobs.ItemsSource;

		//		if (collectionSource is CollectionViewSource a)
		//		{
		//			if (a.View.MoveCurrentTo(currentJob))
		//			{
		//				var sItem = trvJobs.SelectedItem;
		//				if (sItem is TreeViewItem tvi)
		//				{
		//					tvi.BringIntoView();
		//				}
		//			}
		//			else
		//			{
		//				Debug.WriteLine($"Could not Move the Tree View's Current Item to {currentJob.Id}.");
		//			}
		//		}
		//	}
		//}

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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn)
			{
				if (btn.Tag is string strJobId)
				{
					var jobId = new ObjectId(strJobId);
					_vm.RaiseNavigateToJobRequested(jobId);
				}
			}
		}

		private void ButtonShowOriginal_Click(object sender, RoutedEventArgs e)
		{
			_vm.ShowOriginalVersion();
		}

		private void ButtonRollupPans_Click(object sender, RoutedEventArgs e)
		{
			_vm.RollupPans();
		}

		private void ButtonRollupSingles_Click(object sender, RoutedEventArgs e)
		{
			_vm.RollupSingles();
		}


		#endregion


	}
}
