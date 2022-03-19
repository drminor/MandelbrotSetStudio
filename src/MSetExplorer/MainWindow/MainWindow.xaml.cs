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

				colorBandView1.DataContext = _vm.ColorBandViewModel;
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
				SetWindowTitle(_vm.MapProjectViewModel.CurrentProject.Name);
				CommandManager.InvalidateRequerySuggested();
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

			//if (e.PropertyName == nameof(IMainWindowViewModel.ColorMapEntries))
			//{
			//	//TODO: Update the CME View.
			//}
		}

		#endregion

		#region Window Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			_ = SaveChanges();
			Close();
		}

		#endregion

		#region Map Nav Button Handlers

		private void GoBack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoBack ?? false;
		}

		private void GoBack_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.MapProjectViewModel.GoBack();
		}

		private void GoForward_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoForward ?? false;
		}

		private void GoForward_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = _vm.MapProjectViewModel.GoForward();
		}

		#endregion

		#region Project Button Handlers

		// New
		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			_ = SaveChanges();
			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			_ = SaveChanges();

			var initialName = _vm.MapProjectViewModel.CurrentProjectName;
			if (ShowOpenSaveProjectWindow(DialogType.Open, initialName, out var selectedName, out var _))
			{
				Debug.WriteLine($"Opening project with name: {selectedName}.");
				_vm.MapProjectViewModel.ProjectOpen(selectedName);
			}
		}
		
		// Save
		private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CanSaveProject;
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.MapProjectViewModel.ProjectSave();
		}

		// Save As
		private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentJob != null;
		}

		private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var curProject = _vm.MapProjectViewModel.CurrentProject;

			var initialName = curProject.Name;
			var curColorBandSetIds = curProject.ColorBandSetSNs;
			var curColorBandSet = curProject.CurrentColorBandSet;

			if (ShowOpenSaveProjectWindow(DialogType.Save, initialName, out var selectedName, out var description))
			{
				Debug.WriteLine($"Saving project with name: {selectedName}.");
				_vm.MapProjectViewModel.ProjectSaveAs(selectedName, description, curColorBandSetIds, curColorBandSet);
			}
		}

		#endregion

		#region Pan Button Handlers

		private const int SHIFT_AMOUNT = 16;

		private void Pan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentJob != null;
		}

		private void PanLeft_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(new VectorInt(-1 * SHIFT_AMOUNT, 0));
		}

		private void PanUp_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(new VectorInt(0, SHIFT_AMOUNT));
		}

		private void PanRight_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(new VectorInt(SHIFT_AMOUNT, 0));
		}

		private void PanDown_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(new VectorInt(0, -1 * SHIFT_AMOUNT));
		}

		#endregion

		#region Private Methods

		private void LoadNewProject()
		{
			var maxIterations = 700;
			var mSetInfo = MapJobHelper.BuildInitialMSetInfo(maxIterations);

			var cbsId = new Guid("{187b379d-1515-479e-b928-b64728315b15}");
			var colorBandSet = _vm.MapProjectViewModel.GetColorBandSet(cbsId);

			if (colorBandSet == null)
			{
				colorBandSet = MapJobHelper.BuildInitialColorBandSet(maxIterations);
				colorBandSet = new ColorBandSet(cbsId, colorBandSet);
			}

			_vm.MapProjectViewModel.ProjectStartNew(mSetInfo, colorBandSet);
		}

		private bool SaveChanges()
		{
			bool result;

			if (_vm.MapProjectViewModel.CurrentProjectIsDirty)
			{
				if (UserSaysSaveChanges())
				{
					if (_vm.MapProjectViewModel.CanSaveProject)
					{
						_vm.MapProjectViewModel.ProjectSave();
						result = true;
					}
					else
					{
						_ = MessageBox.Show("Will Show SaveAs dialog here.");
						result = false;
					}
				}
				else
				{
					result = false;
				}
			}
			else
			{
				result = false;
			}

			return result;
		}

		private bool UserSaysSaveChanges()
		{
			var defaultResult = _vm.MapProjectViewModel.CanSaveProject ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show("Save Changes?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

			var result = res == MessageBoxResult.Yes;

			return result;
		}

		private bool ShowOpenSaveProjectWindow(DialogType dialogType, string initalName, out string selectedName, out string description)
		{
			var showOpenSaveVm = _vm.CreateAProjectOpenSaveViewModel(initalName, dialogType);
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

		private void Pan(VectorInt amount)
		{
			var newArea = new RectangleInt(new PointInt(amount), _vm.MapProjectViewModel.CanvasSize);
			_vm.MapProjectViewModel.UpdateMapView(TransformType.Pan, newArea);
		}

		private void SetWindowTitle(string projectName)
		{
			if (!string.IsNullOrWhiteSpace(projectName))
			{
				Title = $"MainWindow \u2014 {projectName}";
			}
			else
			{
				Title = "MainWindow";
			}
		}

		#endregion`
	}
}
