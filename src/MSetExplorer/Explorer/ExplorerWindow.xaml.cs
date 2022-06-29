using MSS.Types;
using MSetExplorer.ScreenHelpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics.CodeAnalysis;
using MSS.Types.MSet;
using MSS.Common;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class ExplorerWindow : Window, IHaveAppNavRequestResponse
	{
		private IExplorerViewModel _vm;

		#region Constructor

		public ExplorerWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = (IExplorerViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += ExplorerWindow_Loaded;
			Closing += ExplorerWindow_Closing;
			ContentRendered += ExplorerWindow_ContentRendered;
			InitializeComponent();
		}

		private void ExplorerWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Explorer Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IExplorerViewModel)DataContext;
				_vm.MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

				jobProgress1.DataContext = _vm.CreateAJobProgressViewModel();
				mapDisplay1.DataContext = _vm.MapDisplayViewModel;

				_vm.ColorBandSetViewModel.PropertyChanged += ColorBandSetViewModel_PropertyChanged;
				colorBandView1.DataContext = _vm.ColorBandSetViewModel;

				mapCalcSettingsView1.DataContext = _vm.MapCalcSettingsViewModel;

				mapCoordsView1.DataContext = _vm.MapCoordsViewModel;

				cbsh1.DataContext = _vm.ColorBandSetHistogramViewModel;

				Debug.WriteLine("The Explorer Window is now loaded");
			}
		}

		private void ExplorerWindow_ContentRendered(object? sender, EventArgs e)
		{
			Debug.WriteLine("The Explorer Window is handling ContentRendered");
			//LoadNewProject();
			//ShowMapCoordsEditor();
			//ShowCoordsEditor();

			//SpIdxTest();
		}

		private void ExplorerWindow_Closing(object sender, CancelEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				e.Cancel = true;
			}
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
				Title = GetWindowTitle(_vm.MapProjectViewModel.CurrentProject?.Name, _vm.MapProjectViewModel.CurrentColorBandSet.Name);
				CommandManager.InvalidateRequerySuggested();
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProjectOnFile) || e.PropertyName == nameof(IMapProjectViewModel.CurrentProjectIsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private void ColorBandSetViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.AverageMapSectionTargetIteration))
			{
				_vm.MapCalcSettingsViewModel.TargetIterationsAvailable = _vm.ColorBandSetViewModel.AverageMapSectionTargetIteration;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.IsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		#endregion

		#region Window Button Handlers

		private void CloseAndReturnButton_Click(object sender, RoutedEventArgs e)
		{
			CloseOrExit(OnCloseBehavior.ReturnToTopNav);
		}

		private void ExitButton_Click(object sender, RoutedEventArgs e)
		{
			CloseOrExit(OnCloseBehavior.Close);
		}

		private void CloseOrExit(OnCloseBehavior onCloseBehavior)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			_vm.MapProjectViewModel.ProjectClose();
			AppNavRequestResponse.OnCloseBehavior = onCloseBehavior;
			Close();
		}

		// Show Hide Coords Window
		private void CoordsWindow_Checked(object sender, RoutedEventArgs e)
		{
			var showCoord = mnuItem_CoordsWindow.IsChecked;
			dispSecMapCoords.Visibility = showCoord ? Visibility.Visible : Visibility.Collapsed;
		}

		// Show Hide CalcSettings Window
		private void CalcSettingsWindow_Checked(object sender, RoutedEventArgs e)
		{
			var showCalcSettings = mnuItem_CalcWindow.IsChecked;
			dispSecMapCalcSettings.Visibility = showCalcSettings ? Visibility.Visible : Visibility.Collapsed;
		}

		// Show Hide Job Tree
		private void JobTree_Checked(object sender, RoutedEventArgs e)
		{
			var showJobTreeControl = mnuItem_JobTreeWindow.IsChecked;
			colFarRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			Width = showJobTreeControl ? 2075 : 1665;
		}

		private void ToggleJobTreeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ToggleJobTreeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var showJobTreeControl = mnuItem_JobTreeWindow.IsChecked;
			showJobTreeControl = !showJobTreeControl;

			mnuItem_JobTreeWindow.IsChecked = showJobTreeControl;
			mnuItem_CoordsWindow.IsChecked = !showJobTreeControl;
			mnuItem_CalcWindow.IsChecked = !showJobTreeControl;
			//mnuItem_ColorBandWindow.IsChecked = !showJobTreeControl;

			colFarRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			colRight.Visibility = !showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			colLeft.Visibility = !showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;

			//Width = showJobTreeControl ? 2075 : 1665;
		}

		#endregion

		#region Map Nav Button Handlers

		private void GoBack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoBack ?? false;
		}

		private void GoBack_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var skipPanJobs = !( Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
			_ = _vm.MapProjectViewModel.GoBack(skipPanJobs);
		}

		private void GoForward_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CanGoForward ?? false;
		}

		private void GoForward_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var skipPanJobs = !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
			_ = _vm.MapProjectViewModel.GoForward(skipPanJobs);
		}

		#endregion

		#region Project Button Handlers

		// New
		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
			}
			else if (saveResult == SaveResult.SaveCancelled)
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
		
		// Project Save
		private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentProjectOnFile ?? false;
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.MapProjectViewModel.ProjectSave();
		}

		// Project Save As
		private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentProject != null;
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

		// Project Edit Coords
		private void EditCoordsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void EditCoordsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ShowCoordsEditor();
		}

		private void CreatePosterButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			var curJob = _vm.MapProjectViewModel.CurrentJob;

			if (curJob.IsEmpty)
			{
				_ = MessageBox.Show("Cannot create a poster, there is no current job.");
			}
			else
			{
				if (SavePosterInteractive(_vm.MapProjectViewModel.CurrentProjectName, out var name, out var description))
				{
					var currentSize = _vm.GetCanvasSize(curJob);
					var newPoster = _vm.MapProjectViewModel.PosterCreate(name, description, currentSize);
					_vm.MapProjectViewModel.ProjectClose();

					AppNavRequestResponse.OnCloseBehavior = OnCloseBehavior.ReturnToTopNav;
					AppNavRequestResponse.ResponseCommand = RequestResponseCommand.OpenPoster;
					AppNavRequestResponse.ResponseParameters = new string[] { newPoster.Name, "OpenSizeDialog" };

					Close();
				}
			}
		}

		#endregion

		#region Colors Button Handlers

		// Colors Import
		private void ColorsOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentProject != null;
		}

		private void ColorsOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var initialName = _vm.MapProjectViewModel.CurrentColorBandSet.Name;
			if (ColorsShowOpenWindow(initialName, out var colorBandSet))
			{
				Debug.WriteLine($"Importing ColorBandSet with Id: {colorBandSet.Id}, name: {colorBandSet.Name}.");

				var adjustedCbs = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, _vm.MapProjectViewModel.CurrentJob.MapCalcSettings.TargetIterations);
				_vm.MapProjectViewModel.UpdateColorBandSet(adjustedCbs);
			}
			else
			{
				Debug.WriteLine($"User declined to import a ColorBandSet.");
				var projectsColorBandSet = _vm.MapProjectViewModel.CurrentColorBandSet;

				if (_vm.MapDisplayViewModel.ColorBandSet != projectsColorBandSet)
				{
					_vm.MapDisplayViewModel.SetColorBandSet(projectsColorBandSet, updateDisplay: true);
				}
			}
		}

		// Colors Export
		private void ColorsSaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentProject != null;
		}

		private void ColorsSaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var curColorBandSet = _vm.MapProjectViewModel.CurrentColorBandSet;

			_ = ColorsShowSaveWindow(curColorBandSet);
		}

		// Use Escape Velocities
		private void UseEscapeVelocities_Checked(object sender, RoutedEventArgs e)
		{
			var useEscVelocities = mnuItem_UseEscapeVelocities.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.UseEscapeVelocities = useEscVelocities;
			}
		}

		private void UseEscapeVelocities_Unchecked(object sender, RoutedEventArgs e)
		{
			var useEscVelocities = mnuItem_UseEscapeVelocities.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.UseEscapeVelocities = useEscVelocities;
			}
		}

		// Use RealTime Updates
		private void UseRealTimePreview_Checked(object sender, RoutedEventArgs e)
		{
			var useRealTimePreview = mnuItem_UseRealTimePreview.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.UseRealTimePreview = useRealTimePreview;
			}
		}

		private void UseRealTimePreview_Unchecked(object sender, RoutedEventArgs e)
		{
			var useRealTimePreview = mnuItem_UseRealTimePreview.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.UseRealTimePreview = useRealTimePreview;
			}
		}

		// High Light (only) the Selected Color Band
		private void HighlightSelected_Checked(object sender, RoutedEventArgs e)
		{
			var highlightSelectedCb = mnuItem_HighlightSelectedBand.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.HighlightSelectedBand = highlightSelectedCb;
			}
		}

		private void HighlightSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			var highlightSelectedCb = mnuItem_HighlightSelectedBand.IsChecked;
			if (_vm != null)
			{
				_vm.ColorBandSetViewModel.HighlightSelectedBand = highlightSelectedCb;
			}
		}

		#endregion

		#region Pan Button Handlers

		// TODO: Make the base shift amount proportional to the map view size.
		private const int SHIFT_AMOUNT = 16;

		private void Pan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.MapProjectViewModel?.CurrentProject != null;
		}

		private void PanLeft_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(PanDirection.Left, GetPanAmountQualifer(), SHIFT_AMOUNT);
		}

		private void PanUp_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(PanDirection.Up, GetPanAmountQualifer(), SHIFT_AMOUNT);
		}

		private void PanRight_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(PanDirection.Right, GetPanAmountQualifer(), SHIFT_AMOUNT);
		}

		private void PanDown_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			Pan(PanDirection.Down, GetPanAmountQualifer(), SHIFT_AMOUNT);
		}

		#endregion

		#region Zoom Out Button Handlers

		private void ZoomOut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CurrentProject != null;
		}

		private void ZoomOut12_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x12, SHIFT_AMOUNT);
		}

		private void ZoomOut25_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x25, SHIFT_AMOUNT);
		}

		private void ZoomOut50_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x50, SHIFT_AMOUNT);
		}

		private void ZoomOut100_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x100, SHIFT_AMOUNT);
		}

		private void ZoomOutCustom_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: Create Custom ZoomOut Dialog Box
			_ = MessageBox.Show("Custom ZoomOut.");
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

		#region Private Methods - Project

		private void LoadNewProject()
		{
			var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN;
			var mapCalcSettings = new MapCalcSettings(targetIterations: 700, requestsPerJob: 100);

			LoadNewProject(coords, mapCalcSettings);
		}

		private void LoadNewProject(RRectangle coords, MapCalcSettings mapCalcSettings)
		{
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			_vm.MapProjectViewModel.ProjectStartNew(coords, colorBandSet, mapCalcSettings);
		}

		private SaveResult ProjectSaveChanges()
		{
			var curProject = _vm.MapProjectViewModel.CurrentProject;

			if (curProject == null)
			{
				return SaveResult.NoChangesToSave;
			}

			if (!_vm.MapProjectViewModel.CurrentProjectIsDirty)
			{
				if (_vm.MapProjectViewModel.IsCurrentJobIdChanged)
				{
					if (_vm.MapProjectViewModel.CurrentProjectOnFile)
					{
						// Silently record the new CurrentJob selection
						_vm.MapProjectViewModel.ProjectSave();
						return SaveResult.CurrentJobAutoSaved;
					}
				}

				return SaveResult.NoChangesToSave;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return SaveResult.SaveCancelled;
			}

			var triResult = ProjectUserSaysSaveChanges();

			if (triResult == true)
			{
				if (_vm.MapProjectViewModel.CurrentProjectOnFile)
				{
					// The Project is on-file, just save the pending changes.
					_vm.MapProjectViewModel.ProjectSave();
					return SaveResult.ChangesSaved;
				}
				else
				{
					// The Project is not on-file, must ask user for the name and optional description.
					triResult = SaveProjectInteractive(curProject);
					if (triResult == true)
					{
						return SaveResult.ChangesSaved;
					}
					else
					{
						return SaveResult.SaveCancelled;
					}
				}
			}
			else if (triResult == false)
			{
				return SaveResult.NotSavingChanges;
			}
			else
			{
				return SaveResult.SaveCancelled;
			}
		}

		private bool? SaveProjectInteractive(Project curProject)
		{
			bool? result;

			var initialName = curProject.Name;

			if (ProjectShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description))
			{
				if (selectedName != null)
				{
					Debug.WriteLine($"Saving project with name: {selectedName}.");
					// TODO: Handle cases where ProjectSaveAs fails.
					result = _vm.MapProjectViewModel.ProjectSaveAs(selectedName, description);
				}
				else
				{
					Debug.WriteLine($"No name was provided. Cancelling the Save operation.");
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
			var defaultResult = _vm.MapProjectViewModel.CurrentProjectOnFile ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show("The current project has pending changes. Save Changes?", "Pending Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

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

		private string GetWindowTitle(string? projectName, string? colorBandSetName)
		{
			const string dash = "\u2014";

			var result = projectName != null
				? colorBandSetName != null 
					? $"Explorer Window {dash} {projectName} {dash} {colorBandSetName}" 
					: $"Explorer Window {dash} {projectName}"
				: "Explorer Window";

			return result;
		}

		private void ShowCoordsEditor()
		{
			CoordsEditorViewModel coordsEditorViewModel;
			MapCalcSettings mapCalcSettings;

			var curJob = _vm.MapProjectViewModel.CurrentJob;
			if (!curJob.IsEmpty)
			{
				coordsEditorViewModel = _vm.CreateACoordsEditorViewModel(curJob.Coords, _vm.MapProjectViewModel.CanvasSize, allowEdits: true);
				mapCalcSettings = curJob.MapCalcSettings;
			}
			else
			{
				//var x1 = "-0.477036968733327014028268226139546";
				//var x2 = "-0.477036964892343354414420540166062";
				//var y1 = "0.535575821681765930306959274776606";
				//var y2 = "0.535575824239325800205884281044245";

				////var x1 = "-0.4770369687333";
				////var x2 = "-0.4770369648923";
				////var y1 = "0.5355758216817";
				////var y2 = "0.5355758242393";

				//var coords = new RRectangle(x1, x2, y1, y2);
				//coordsEditorViewModel = _vm.CreateACoordsEditorViewModel(coords, _vm.MapProjectViewModel.CanvasSize, allowEdits: false);
				//mapCalcSettings = new MapCalcSettings(targetIterations: 700, requestsPerJob: 100);

				return;
			}

			var coordsEditorWindow = new CoordsEditorWindow()
			{
				DataContext = coordsEditorViewModel
			};

			if (coordsEditorWindow.ShowDialog() == true)
			{
				if (!curJob.IsEmpty)
				{
					//var saveResult = ProjectSaveChanges();
					//if (saveResult == SaveResult.ChangesSaved)
					//{
					//	_ = MessageBox.Show("Changes Saved");
					//}
					//else if (saveResult == SaveResult.NotSavingChanges)
					//{
					//	_ = _vm.MapProjectViewModel.DeleteMapSectionsSinceLastSave();
					//}
					//else if (saveResult == SaveResult.SaveCancelled)
					//{
					//	// user cancelled.
					//	return;
					//}

					return;
				}

				var newCoords = coordsEditorViewModel.Coords;
				LoadNewProject(newCoords, mapCalcSettings);
			}
		}

		//private void ShowMapCoordsEdTest()
		//{
		//	var mapCoordsEditorViewModel = new MapCoordsEdTestViewModel();
		//	var mapCoordsEditorWindow = new MapCoordsEdTestWindow()
		//	{
		//		DataContext = mapCoordsEditorViewModel
		//	};

		//	if (mapCoordsEditorWindow.ShowDialog() == true)
		//	{
		//		_ = MessageBox.Show("Saved.");
		//	}
		//	//else
		//	//{
		//	//	_ = MessageBox.Show("Cancelled.");
		//	//}
		//}

		private void SpIdxTest()
		{
			var sPoint = new RPoint(-5, -4, -1);
			var l1 = new MapSectionSpIdxItem(sPoint);
			Debug.WriteLine(l1);


			var nPoint = new RPoint(l1.XValues[6].Value * 8, l1.YValues[6].Value * 8, sPoint.Exponent - 3); ;
			var l2 = new MapSectionSpIdxItem(nPoint);
			Debug.WriteLine(l2);

			nPoint = new RPoint(l2.XValues[6].Value * 8, l2.YValues[2].Value * 8, nPoint.Exponent - 3);

			var l3 = new MapSectionSpIdxItem(nPoint);
			Debug.WriteLine(l3);
		}

		#endregion

		#region Private Methods - Poster

		private bool SavePosterInteractive(string? initialName, [MaybeNullWhen(false)] out string name, out string? description)
		{
			bool result;

			if (PosterShowOpenSaveWindow(DialogType.Save, initialName, out name, out description))
			{
				if (name != null)
				{
					result = true;
				}
				else
				{
					Debug.WriteLine($"No name was provided. Cancelling the Create Poster operation.");
					result = false;
				}
			}
			else
			{
				result = false;
			}

			return result;
		}

		private bool PosterShowOpenSaveWindow(DialogType dialogType, string? initalName, out string? selectedName, out string? description)
		{
			var posterOpenSaveVm = _vm.CreateAPosterOpenSaveViewModel(initalName, dialogType);
			var posterOpenSaveWindow = new PosterOpenSaveWindow
			{
				DataContext = posterOpenSaveVm
			};

			if (posterOpenSaveWindow.ShowDialog() == true)
			{
				selectedName = posterOpenSaveWindow.PosterName;
				description = posterOpenSaveWindow.PosterDescription;
				return true;
			}
			else
			{
				selectedName = null;
				description = null;
				return false;
			}
		}

		#endregion

		#region Private Methods -- Colors

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
				else
				{
					result = res == MessageBoxResult.Cancel ? null : false;
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
					_vm.MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
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

		#endregion

		#region Private Methods -- Pan and ZoomOut

		private void Pan(PanDirection direction, PanAmountQualifer qualifer, int amount)
		{
			var qualifiedAmount = GetPanAmount(amount, qualifer);
			var panVector = GetPanVector(direction, qualifiedAmount);
			var newArea = new RectangleInt(new PointInt(panVector), _vm.MapProjectViewModel.CanvasSize);
			_vm.MapProjectViewModel.UpdateMapView(TransformType.Pan, newArea);
		}

		private int GetPanAmount(int baseAmount, PanAmountQualifer qualifer)
		{
			var targetAmount = qualifer switch
			{
				PanAmountQualifer.Fine => baseAmount,
				PanAmountQualifer.Regular => baseAmount * 8,
				PanAmountQualifer.Course => baseAmount * 64,
				_ => baseAmount * 8,
			};

			var result = RMapHelper.CalculatePitch(_vm.MapProjectViewModel.CanvasSize, targetAmount);

			return result;
		}

		private VectorInt GetPanVector(PanDirection direction, int amount)
		{
			return direction switch
			{
				PanDirection.Left => new VectorInt(-1 * amount, 0),
				PanDirection.Up => new VectorInt(0, amount),
				PanDirection.Right => new VectorInt(amount, 0),
				PanDirection.Down => new VectorInt(0, -1 * amount),
				_ => new VectorInt(),
			};
		}

		private PanAmountQualifer GetPanAmountQualifer()
		{
			return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
				? PanAmountQualifer.Course
				: Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
				? PanAmountQualifer.Fine 
				: PanAmountQualifer.Regular;
		}

		private void ZoomOut(ZoomOutAmountQualifer qualifer, int amount)
		{
			var qualifiedAmount = GetZoomOutAmount(amount, qualifer);
			var curArea = new RectangleInt(new PointInt(), _vm.MapProjectViewModel.CanvasSize);
			var newArea = curArea.Expand(new SizeInt(qualifiedAmount));

			_vm.MapProjectViewModel.UpdateMapView(TransformType.ZoomOut, newArea);
		}

		private int GetZoomOutAmount(int baseAmount, ZoomOutAmountQualifer qualifer)
		{
			var targetAmount = qualifer switch
			{
				ZoomOutAmountQualifer.x12 => baseAmount * 8,   // 128
				ZoomOutAmountQualifer.x25 => baseAmount * 16,  // 256
				ZoomOutAmountQualifer.x50 => baseAmount * 32,  // 512
				ZoomOutAmountQualifer.x100 => baseAmount * 64, // 1024
				_ => baseAmount * 32,
			};

			var result = RMapHelper.CalculatePitch(_vm.MapDisplayViewModel.CanvasSize, targetAmount);

			return result;
		}

		#endregion

		private enum SaveResult
		{
			NoChangesToSave,
			CurrentJobAutoSaved,
			ChangesSaved,
			NotSavingChanges,
			SaveCancelled,
		}

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

	}
}
