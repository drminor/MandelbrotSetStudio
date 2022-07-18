using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobTreeControl.xaml
	/// </summary>
	public partial class JobTreeControl : UserControl
	{
		//private IJobTreeViewModel _vm;

		#region Constructor 

		public JobTreeControl()
		{
			//_vm = (IJobTreeViewModel)DataContext;
			//Loaded += JobTreeControl_Loaded;
			InitializeComponent();
		}

		//private void JobTreeControl_Loaded(object sender, RoutedEventArgs e)
		//{
		//	if (DataContext is null)
		//	{
		//		Debug.WriteLine("The DataContext is null as the JobTree UserControl is being loaded.");
		//		return;
		//	}
		//	else
		//	{
		//		//lvColorBandsHdr.Width = lvColorBands.ActualWidth - 25;

		//		//_vm = (IJobTreeViewModel)DataContext;
		//		//_vm.PropertyChanged += ViewModel_PropertyChanged;
				

		//		//Debug.WriteLine("The JobTree UserControl is now loaded");
		//	}
		//}

		//private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(IJobTreeViewModel.CurrentProject))
		//	{
		//		CommandManager.InvalidateRequerySuggested();
		//	}

		//	//else if (e.PropertyName == nameof(IJobTreeViewModel.JobItems))
		//	//{
		//	//	trvJobs.ItemsSource = _vm.JobItems;
		//	//}
		//}

		#endregion

	}
}
