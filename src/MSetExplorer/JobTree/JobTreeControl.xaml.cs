using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn)
			{
				if (btn.Tag is string jobId)
				{
					_vm.NavigateToJob(jobId);
				}
			}
		}

		#endregion
	}
}
