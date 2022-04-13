using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics.CodeAnalysis;
using MSS.Types.MSet;

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
			_vm = (IMainWindowViewModel)DataContext;
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
				_vm.MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;
				mapDisplay1.DataContext = _vm.MapDisplayViewModel;

				_vm.ColorBandSetViewModel.PropertyChanged += ColorBandSetViewModel_PropertyChanged;
				colorBandView1.DataContext = _vm.ColorBandSetViewModel;

				mapCalcSettingsView1.DataContext = _vm.MSetInfoViewModel;
				mapCoordsView1.DataContext = _vm.MSetInfoViewModel;

				((MainWindowViewModel)_vm).TestDiv();

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void MainWindow_ContentRendered(object? sender, EventArgs e)
		{
			Debug.WriteLine("The MainWindow is handling ContentRendered");
			//LoadNewProject();
		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
				SetWindowTitle(_vm.MapProjectViewModel.CurrentProject?.Name);
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private void ColorBandSetViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.IsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		#endregion

		#region Window Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			var triResult = ProjectSaveChanges();
			if (triResult == true)
			{
				_ = MessageBox.Show("Changes Saved");
			}

			if (!triResult.HasValue)
			{
				// user cancelled.
				return;
			}

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
			var triResult = ProjectSaveChanges();
			if (triResult == true)
			{
				_ = MessageBox.Show("Changes Saved");
			}

			if (!triResult.HasValue)
			{
				// user cancelled.
				return;
			}

			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var triResult = ProjectSaveChanges();
			if (triResult == true)
			{
				_ = MessageBox.Show("Changes Saved");
			}

			if (!triResult.HasValue)
			{
				// user cancelled.
				return;
			} 

			var initialName = _vm.MapProjectViewModel.CurrentProjectName;
			if (ProjectShowOpenSaveWindow(DialogType.Open, initialName, out var selectedName, out _))
			{
				if (selectedName != null)
				{
					Debug.WriteLine($"Opening project with name: {selectedName}.");
					_ = _vm.MapProjectViewModel.ProjectOpen(selectedName);
				}
				else
				{
					Debug.WriteLine($"Cannot open project with name: {selectedName}.");
				}
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

			if (curProject == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			_ = SaveProjectInteractive(curProject);
		}

		#endregion

		#region Colors Button Handlers

		// Colors Import
		private void ColorsOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CurrentProject != null;
		}

		private void ColorsOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var initialName = _vm.MapProjectViewModel.CurrentColorBandSet?.Name;
			if (ColorsShowOpenWindow(initialName, out var colorBandSet))
			{
				Debug.WriteLine($"Importing ColorBandSet with Id: {colorBandSet.Id}, name: {colorBandSet.Name}.");
				_vm.MapProjectViewModel.CurrentColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLine($"User decliend to import a ColorBandSet.");
				if (_vm.MapDisplayViewModel.ColorBandSet != _vm.MapProjectViewModel.CurrentColorBandSet)
				{
					_vm.MapDisplayViewModel.ColorBandSet = _vm.MapProjectViewModel.CurrentColorBandSet;
				}
			}
		}

		// Colors Export
		private void ColorsSaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CurrentProject != null;
		}

		private void ColorsSaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var curColorBandSet = _vm.MapProjectViewModel.CurrentColorBandSet;

			ColorsShowSaveWindow(curColorBandSet);
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

		#region Disp Size Button Handlers

		private void W_Up_Button_Click(object sender, RoutedEventArgs e)
		{
			Width += 128;
			//_vm.BumpDispWidth(increase: true);
		}

		private void W_Down_Button_Click(object sender, RoutedEventArgs e)
		{
			Width -= 128;
			//_vm.BumpDispWidth(increase: false);
		}

		private void H_Up_Button_Click(object sender, RoutedEventArgs e)
		{
			Height += 128;
			//_vm.BumpDispHeight(increase: true);
		}

		private void H_Down_Button_Click(object sender, RoutedEventArgs e)
		{
			Height -= 128;
			//_vm.BumpDispHeight(increase: false);
		}

		#endregion

		#region Private Methods

		private void LoadNewProject()
		{
			var maxIterations = 700;
			var mSetInfo = MapJobHelper.BuildInitialMSetInfo(maxIterations);
			var colorBandSet = MapJobHelper.BuildInitialColorBandSet(maxIterations);
			_vm.MapProjectViewModel.ProjectStartNew(mSetInfo, colorBandSet);
		}

		private bool? ProjectSaveChanges()
		{
			var curProject = _vm.MapProjectViewModel.CurrentProject;

			if (curProject == null)
			{
				return false;
			}

			if (!_vm.MapProjectViewModel.CurrentProjectIsDirty)
			{
				return false;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return null;
			}

			var triResult = ProjectUserSaysSaveChanges();

			if (triResult == true)
			{
				if (_vm.MapProjectViewModel.CanSaveProject)
				{
					// The Project is on-file, just save the pending changes.
					_vm.MapProjectViewModel.ProjectSave();
					triResult = true;
				}
				else
				{
					// The Project is not on-file, must ask user for the name and optional description.
					triResult = SaveProjectInteractive(curProject);
				}
			}

			return triResult;
		}

		private bool? SaveProjectInteractive(Project curProject)
		{
			bool? result;

			var initialName = curProject.Name;
			var curJobId = curProject.CurrentJobId;
			var curColorBandSetId = curProject.CurrentColorBandSetId;

			if (ProjectShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description))
			{
				if (selectedName != null)
				{
					Debug.WriteLine($"Saving project with name: {selectedName}.");
					_vm.MapProjectViewModel.ProjectSaveAs(selectedName, description, curJobId, curColorBandSetId);
					result = true;
				}
				else
				{
					Debug.WriteLine($"Cannot save project with name: {selectedName}.");
					result = null;
				}
			}
			else
			{
				result = null;
			}

			return result;
		}

		private bool? ProjectUserSaysSaveChanges()
		{
			var defaultResult = _vm.MapProjectViewModel.CanSaveProject ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show("The current project has pending changes. Save Changes?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

			var result = res == MessageBoxResult.Yes ? true : res == MessageBoxResult.No ? false : (bool?) null;

			return result;
		}

		private bool ProjectShowOpenSaveWindow(DialogType dialogType, string? initalName, out string? selectedName, out string? description)
		{
			var projectOpenSaveVm = _vm.CreateAProjectOpenSaveViewModel(initalName, dialogType);
			var projectOpenSaveWindow = new ProjectOpenSaveWindow
			{
				DataContext = projectOpenSaveVm
			};

			if (projectOpenSaveWindow.ShowDialog() == true)
			{
				selectedName = projectOpenSaveWindow.ProjectName;
				description = projectOpenSaveWindow.ProjectDescription;
				return true;
			}
			else
			{
				selectedName = null;
				description = null;
				return false;
			}
		}

		private bool? ColorsCommitUpdates()
		{
			bool? result;

			if (_vm.ColorBandSetViewModel.IsDirty)
			{
				var defaultResult = MessageBoxResult.Yes;
				var res = MessageBox.Show("Save edits made in the ColorBand Editor?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

				if (res == MessageBoxResult.Yes)
				{
					_vm.ColorBandSetViewModel.ApplyChanges();
					result = true;
				}
				else if (res == MessageBoxResult.Cancel)
				{
					result = null;
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

		private bool ColorsShowOpenWindow(string? initalName, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			var colorBandSetOpenSaveVm = _vm.CreateACbsOpenViewModel(initalName, DialogType.Open);
			var colorBandSetOpenSaveWindow = new ColorBandSetOpenSaveWindow
			{
				DataContext = colorBandSetOpenSaveVm
			};

			try
			{
				colorBandSetOpenSaveVm.PropertyChanged += ColorBandSetOpenSaveVm_PropertyChanged;
				if (colorBandSetOpenSaveWindow.ShowDialog() == true)
				{
					var id = colorBandSetOpenSaveWindow.ColorBandSetId;
					if (id != null && colorBandSetOpenSaveVm.TryImportColorBandSet(id.Value, out colorBandSet))
					{
						return true;
					}
					else
					{
						colorBandSet = null;
						return false;
					}
				}
				else
				{
					colorBandSet = null;
					return false;
				}
			}
			finally
			{
				colorBandSetOpenSaveVm.PropertyChanged -= ColorBandSetOpenSaveVm_PropertyChanged;
			}
		}

		private void ColorBandSetOpenSaveVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is IColorBandSetOpenSaveViewModel vm)
			{
				var id = vm.SelectedColorBandSetInfo?.Id;
				if (id != null && vm.TryImportColorBandSet(id.Value, out var colorBandSet))
				{
					_vm.MapDisplayViewModel.ColorBandSet = colorBandSet;
				}
			}
		}

		private bool ColorsShowSaveWindow(ColorBandSet colorBandSet)
		{
			var colorBandSetOpenSaveVm = _vm.CreateACbsOpenViewModel(colorBandSet.Name, DialogType.Save);
			var colorBandSetOpenSaveWindow = new ColorBandSetOpenSaveWindow
			{
				DataContext = colorBandSetOpenSaveVm
			};

			if (colorBandSetOpenSaveWindow.ShowDialog() == true)
			{
				var cpy = colorBandSet.CreateNewCopy();
				cpy.Name = colorBandSetOpenSaveWindow.ColorBandSetName;
				cpy.Description = colorBandSetOpenSaveWindow.ColorBandSetDescription;

				colorBandSetOpenSaveVm.ExportColorBandSet(cpy);
				return true;
			}
			else
			{ 
				return false;
			}
		}

		private void Pan(VectorInt amount)
		{
			var newArea = new RectangleInt(new PointInt(amount), _vm.MapProjectViewModel.CanvasSize);
			_vm.MapProjectViewModel.UpdateMapView(TransformType.Pan, newArea);
		}

		private void SetWindowTitle(string? projectName)
		{
			const string dash = "\u2014";
			Title = projectName == null ? $"MainWindow {dash} {projectName}" : "MainWindow";
		}

		#endregion
	}
}
