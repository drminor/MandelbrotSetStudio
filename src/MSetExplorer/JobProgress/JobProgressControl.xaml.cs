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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for JobProgressControl.xaml
	/// </summary>
	public partial class JobProgressControl : UserControl
	{
		private JobProgressViewModel _vm;

		#region Constructor 

		public JobProgressControl()
		{
			_vm = (JobProgressViewModel)DataContext;

			Loaded += JobProgressControl_Loaded;
			InitializeComponent();
		}

		private void JobProgressControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the obProgress Control is being loaded.");
				return;
			}
			else
			{
				_vm = (JobProgressViewModel)DataContext;
				borderTop.DataContext = DataContext;

				lvJobProgressEntries.ItemsSource = _vm.JobProgressRecords;
				//lvJobProgressEntries.SelectionChanged += LvJobProgressEntries_SelectionChanged;
				//lvJobProgressEntries.MouseDoubleClick += LvJobProgressEntries_MouseDoubleClick;

				Debug.WriteLine("The JobProgress Control is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void LvJobProgressEntries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void LvJobProgressEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			throw new NotImplementedException();
		}

		#endregion


	}
}
