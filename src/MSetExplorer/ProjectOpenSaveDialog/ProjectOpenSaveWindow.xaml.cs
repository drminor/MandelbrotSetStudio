using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ProjectOpenSaveWindow.xaml
	/// </summary>
	public partial class ProjectOpenSaveWindow : Window
	{
		private IProjectOpenSaveViewModel _vm;


		#region Constructor 

		public ProjectOpenSaveWindow()
		{
			InitializeComponent();
			Loaded += ProjectOpenSaveWindow_Loaded;
		}

		private void ProjectOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IProjectOpenSaveViewModel)DataContext;

				Debug.WriteLine("The ProjectOpenSaveWindow is now loaded");
			}
		}

		#endregion

		#region Public Properties

		public string ProjectName { get; private set; }

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			ProjectName = _vm.SelectedName;
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
