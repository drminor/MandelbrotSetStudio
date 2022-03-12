using MSS.Types;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IMainWindowViewModel _vm;

		#region Constructor

		public MainWindow()
		{
			Loaded += MainWindow_Loaded;
			ContentRendered += MainWindow_ContentRendered;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IMainWindowViewModel)DataContext;
				_vm.PropertyChanged += MainWindowViewModel_PropertyChanged;

				_vm.MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

				mapDisplay1.DataContext = _vm.MapDisplayViewModel;

				txtIterations.LostFocus += TxtIterations_LostFocus;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void MainWindow_ContentRendered(object sender, EventArgs e)
		{
			Debug.WriteLine("The MainWindow is handling ContentRendered");
			LoadNewProject();
		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapProjectViewModel.CanGoBack))
			{
				btnGoBack.IsEnabled = _vm.MapProjectViewModel.CanGoBack;
				return;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CanGoForward))
			{
				btnGoForward.IsEnabled = _vm.MapProjectViewModel.CanGoForward;
				return;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProject))
			{
				Title = $"MainWindow \u2014 {_vm.MapProjectViewModel.CurrentProject.Name}";
			}
		}

		private void TxtIterations_LostFocus(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(txtIterations.Text, out var newValue))
			{
				_vm.TargetIterations = newValue;
			}

			// TODO: Respond to changes in the CME View and update the MainWindowViewModel
		}

		private void MainWindowViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMainWindowViewModel.TargetIterations))
			{
				txtIterations.Text = _vm.TargetIterations.ToString(CultureInfo.InvariantCulture);
			}

			if (e.PropertyName == nameof(IMainWindowViewModel.ColorMapEntries))
			{
				//TODO: Update the CME View.
			}
		}

		#endregion

		#region Window Buttons

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion

		#region Map Nav Buttons

		//private void GoBackButton_Click(object sender, RoutedEventArgs e)
		//{
		//	if (_vm.MapProjectViewModel.CanGoBack)
		//	{
		//		_ =_vm.MapProjectViewModel.GoBack();
		//	}
		//}

		private void GoBack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoBack ?? false;
		}

		private void GoBack_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.MapProjectViewModel.GoBack();
		}

		//private void GoForwardButton_Click(object sender, RoutedEventArgs e)
		//{
		//	if (_vm.MapProjectViewModel.CanGoForward)
		//	{
		//		_ =_vm.MapProjectViewModel.GoForward();
		//	}
		//}

		private void GoForward_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoForward ?? false;
		}

		private void GoForward_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.MapProjectViewModel.GoForward();
		}

		#endregion

		#region Project Button Handers

		// New
		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var initialName = "BlueBerry";
			if (ShowOpenSaveProjectWindow(isOpenDialog: true, initialName, out var selectedName, out var description))
			{
				Debug.WriteLine($"Opening project with name: {selectedName}.");
				_vm.MapProjectViewModel.LoadProject(selectedName);
			}
		}
		
		// Save
		private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CanSaveProject;
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.MapProjectViewModel.SaveLoadedProject();
		}

		// Save As
		private void SaveAsButton_Click(object sender, RoutedEventArgs e)
		{
			var initialName = "Test3";
			if (ShowOpenSaveProjectWindow(isOpenDialog: false, initialName, out var selectedName, out var description))
			{
				Debug.WriteLine($"Saving project with name: {selectedName}.");
				_vm.MapProjectViewModel.SaveProject(selectedName, description);
			}
		}

		#endregion

		#region Pan Button Handlers

		private const int SHIFT_AMOUNT = 16;

		private void GoLeftButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(-1 * SHIFT_AMOUNT, 0));
		}

		private void GoUpButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(0, SHIFT_AMOUNT));
		}

		private void GoRightButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(SHIFT_AMOUNT, 0));
		}

		private void GoDownButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(0, -1 * SHIFT_AMOUNT));
		}

		private void Pan(VectorInt amount)
		{
			var newArea = new RectangleInt(new PointInt(amount), _vm.MapProjectViewModel.CanvasSize);
			_vm.MapProjectViewModel.UpdateMapView(TransformType.Pan, newArea);
		}

		#endregion

		#region Private Methods

		private void LoadNewProject()
		{
			var mSetInfo = MapJobHelper.BuildInitialMSetInfo(maxIterations: 700);
			_vm.MapProjectViewModel.StartNewProject(mSetInfo);
		}

		private bool ShowOpenSaveProjectWindow(bool isOpenDialog, string initialName, out string selectedName, out string description)
		{
			var showOpenSaveVm = new ProjectOpenSaveViewModel(initialName, isOpenDialog);
			var showOpenSaveWindow = new ProjectOpenSaveWindow
			{
				DataContext = showOpenSaveVm
			};

			if (showOpenSaveWindow.ShowDialog() == true)
			{
				selectedName = showOpenSaveWindow.ProjectName;
				description = showOpenSaveWindow.ProjectDescription;
				return true;
			}
			else
			{
				selectedName = null;
				description = null;
				return false;
			}
		}

		#endregion`
	}
}
