using MSS.Types.MSet;
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

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ProjectDetailsEditorWindow.xaml
	/// </summary>
	public partial class ProjectDetailsEditorWindow : Window
	{
		private ProjectDetailsViewModel _vm;

		#region Constructor 

		public ProjectDetailsEditorWindow()
		{
			_vm = (ProjectDetailsViewModel)DataContext;

			Loaded += ProjectDetailsEditorWindow_Loaded;
			ContentRendered += ProjetDetailsEditorWindow_ContentRendered;
			InitializeComponent();
		}

		private void ProjectDetailsEditorWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ProjectDetailsEditor Window is being loaded.");
				return;
			}
			else
			{
				_vm = (ProjectDetailsViewModel)DataContext;
				//borderDetails.DataContext = DataContext;

				_vm.PropertyChanged += ProjectDetailsViewModel_PropertyChanged;


				Debug.WriteLine("The ProjectDetailsEditor Window is now loaded");
			}
		}

		private void ProjectDetailsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{

		}

		#endregion

		#region Event Handlers

		private void ProjetDetailsEditorWindow_ContentRendered(object? sender, EventArgs e)
		{

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

		#endregion

	}
}
