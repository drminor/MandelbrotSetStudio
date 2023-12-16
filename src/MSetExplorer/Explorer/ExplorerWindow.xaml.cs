using MongoDB.Bson;
using MSetExplorer.ScreenHelpers;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class ExplorerWindow : Window, IHaveAppNavRequestResponse
	{
		private IExplorerViewModel _vm;

		private CreateImageProgressWindow? _createImageProgressWindow;

		#region Constructor

		public ExplorerWindow(IExplorerViewModel dataContext, AppNavRequestResponse appNavRequestResponse)
		{
			DataContext = dataContext;
			_vm = dataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += ExplorerWindow_Loaded;
			ContentRendered += ExplorerWindow_ContentRendered;
			Closing += ExplorerWindow_Closing;
			Unloaded += ExplorerWindow_Unloaded;
			SizeChanged += ExplorerWindow_SizeChanged;

			InitializeComponent();

			mapDisplay1.DataContext = _vm.MapDisplayViewModel;
			jobProgress1.DataContext = _vm.JobProgressViewModel;

			//colorBandView1.DataContext = _vm.ColorBandSetViewModel;
			cbsHistogram1.DataContext = _vm.CbsHistogramViewModel;

			mapCalcSettingsView1.DataContext = _vm.MapCalcSettingsViewModel;
			mapCoordsView1.DataContext = _vm.MapCoordsViewModel;
			jobTree1.DataContext = _vm.JobTreeViewModel;

			_vm.MapCoordsIsVisible = mnuItem_CoordsWindow.IsChecked;
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
				_vm.ProjectViewModel.PropertyChanged += ProjectViewModel_PropertyChanged;
			
				//_vm.ColorBandSetViewModel.PropertyChanged += ColorBandSetViewModel_PropertyChanged;
				_vm.CbsHistogramViewModel.PropertyChanged += CbsHistogramViewModel_PropertyChanged;
				//_vm.CbsHistogramViewModel.ColorBandCutoffChanged += CbshDisplayViewModel_ColorBandCutoffChanged;

				// Position the Window near the left top.
				Left = 20;
				Top = 20;

				Debug.WriteLine("The Explorer Window is now loaded");
			}
		}

		private void ExplorerWindow_Unloaded(object sender, RoutedEventArgs e)
		{
			Loaded -= ExplorerWindow_Loaded;
			ContentRendered -= ExplorerWindow_ContentRendered;
			Closing -= ExplorerWindow_Closing;
			Unloaded -= ExplorerWindow_Unloaded;

			_vm.ProjectViewModel.PropertyChanged -= ProjectViewModel_PropertyChanged;

			//_vm.ColorBandSetViewModel.PropertyChanged -= ColorBandSetViewModel_PropertyChanged;
			_vm.CbsHistogramViewModel.PropertyChanged -= CbsHistogramViewModel_PropertyChanged;
			//_vm.CbsHistogramViewModel.ColorBandCutoffChanged -= CbshDisplayViewModel_ColorBandCutoffChanged;
		}

		private void ExplorerWindow_ContentRendered(object? sender, EventArgs e)
		{
			Debug.WriteLine("The Explorer Window is handling ContentRendered");

			//LoadNewProject();
			//ShowMapCoordsEditor();
			//ShowCoordsEditor();

			//SpIdxTest();

			DisplayJobTree(show: true);

			//if (AppNavRequestResponse.RequestCommand == RequestResponseCommand.OpenProject)
			//{
			//	OpenProjectFromAppRequest(AppNavRequestResponse.RequestParameters);
			//}
			//else if (AppNavRequestResponse.RequestCommand == RequestResponseCommand.CreateNewProject)
			//{
			//	LoadNewProject();
			//}

			//LoadNewProject();
			//OpenProjectFromAppRequest(new string[] {"Art3"});

			mapDisplay1.UpdateDisplaySize(_vm.MapDisplayViewModel.ViewportSize);
		}

		private void ExplorerWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var explorerSize = new SizeDbl(ActualWidth, ActualHeight);
			Debug.WriteLine($"The ExplorerWindow is now {explorerSize}:");
		}

		private void ExplorerWindow_Closing(object? sender, CancelEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				//_ = MessageBox.Show("Changes Saved");

				//_vm.MapDisplayViewModel.ClearDisplay();
				_vm.ProjectViewModel.ProjectClose();

			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();

				//_vm.MapDisplayViewModel.ClearDisplay();
				_vm.ProjectViewModel.ProjectClose();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				e.Cancel = true;
			}
		}

		#endregion

		#region Event Handlers

		private void ProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(IProjectViewModel.CanGoBack))
			//{
			//	btnGoBack.IsEnabled = _vm.ProjectViewModel.CanGoBack;
			//	return;
			//}

			//if (e.PropertyName == nameof(IProjectViewModel.CanGoForward))
			//{
			//	btnGoForward.IsEnabled = _vm.ProjectViewModel.CanGoForward;
			//	return;
			//}

			if (e.PropertyName == nameof(IProjectViewModel.CurrentProject))
			{
				Title = GetWindowTitle(_vm.ProjectViewModel.CurrentProject?.Name, _vm.ProjectViewModel.CurrentColorBandSet.Name);
				CommandManager.InvalidateRequerySuggested();
			}

			if (e.PropertyName is (nameof(IProjectViewModel.CurrentProjectOnFile)) or (nameof(IProjectViewModel.CurrentProjectIsDirty)) or (nameof(IProjectViewModel.CurrentJob)))
			{
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

		private void CbsHistogramViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbsHistogramViewModel.IsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		//private void CbshDisplayViewModel_ColorBandCutoffChanged(object? sender, (int, int) e)
		//{
		//	var colorBandIndex = e.Item1;
		//	var newCutoff = e.Item2;

		//	if (_vm.ColorBandSetViewModel.ColorBandSet != null)
		//	{
		//		try
		//		{
		//			_vm.ColorBandSetViewModel.UpdateCutoff(colorBandIndex, newCutoff);
		//		}
		//		catch
		//		{
		//			//Debug.WriteLine($"Could not update the VM's ColorBandSetViewModel's ColorBand at index: {colorBandIndex} with newValue: {newCutoff}.");
		//			throw;
		//		}
		//	}
		//}

		#endregion

		#region Window Close / Exit / Return  Button Handlers

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
				//_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			//_vm.MapDisplayViewModel.ClearDisplay();
			_vm.ProjectViewModel.ProjectClose();

			AppNavRequestResponse.OnCloseBehavior = onCloseBehavior;
			Close();
		}

		#endregion

		#region Window Component Show / Hide Handlers

		// Show Hide Coords Window
		private void CoordsWindow_Checked(object sender, RoutedEventArgs e)
		{
			if (dispSecMapCoords == null) return;

			var showCoord = mnuItem_CoordsWindow.IsChecked;
			dispSecMapCoords.Visibility = showCoord ? Visibility.Visible : Visibility.Collapsed;

			// TODO: Create Bindings for these WindowComponent Is Visible settings.
			_vm.MapCoordsIsVisible = showCoord;
		}

		// Show Hide CalcSettings Window
		private void CalcSettingsWindow_Checked(object sender, RoutedEventArgs e)
		{
			if (dispSecMapCalcSettings == null) return;

			var showCalcSettings = mnuItem_CalcWindow.IsChecked;
			dispSecMapCalcSettings.Visibility = showCalcSettings ? Visibility.Visible : Visibility.Collapsed;
		}

		// Show Hide Job Tree
		private void JobTree_Checked(object sender, RoutedEventArgs e)
		{
			var showJobTreeControl = mnuItem_JobTreeWindow.IsChecked;
			colFarRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			Width = showJobTreeControl ? 1882 : 1475;
		}

		private void DisplayJobTree(bool show)
		{
			mnuItem_JobTreeWindow.IsChecked = show;

			colFarRight.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
			Width = show ? 1882 : 1475;
		}

		// Show Hide Color Band Histogram component
		private void Histogram_Checked(object sender, RoutedEventArgs e)
		{
			if (botRow == null) return;

			var showHistogramControl = mnuItem_HistogramWindow.IsChecked;
			botRow.Visibility = showHistogramControl ? Visibility.Visible : Visibility.Collapsed;
			_vm.CbsHistogramViewModel.IsEnabled = showHistogramControl;
			Height = showHistogramControl ? 1344 : 1097;
		}

		private void ToggleJobTreeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ToggleJobTreeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var showJobTreeControl = mnuItem_JobTreeWindow.IsChecked;
			showJobTreeControl = !showJobTreeControl;

			//mnuItem_JobTreeWindow.IsChecked = showJobTreeControl;
			//mnuItem_CoordsWindow.IsChecked = !showJobTreeControl;
			//mnuItem_CalcWindow.IsChecked = !showJobTreeControl;
			////mnuItem_ColorBandWindow.IsChecked = !showJobTreeControl;

			//colFarRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			//colRight.Visibility = !showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			//colLeft.Visibility = !showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;

			mnuItem_JobTreeWindow.IsChecked = showJobTreeControl;
			mnuItem_CoordsWindow.IsChecked = showJobTreeControl;
			mnuItem_CalcWindow.IsChecked = showJobTreeControl;
			//mnuItem_ColorBandWindow.IsChecked = !showJobTreeControl;

			colFarRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			colRight.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			colLeft.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;
			colLeftBorder.Visibility = showJobTreeControl ? Visibility.Visible : Visibility.Collapsed;

			//Width = showJobTreeControl ? 2075 : 1665;
		}

		#endregion

		#region Map Nav Button Handlers

		private void GoBack_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			//Debug.WriteLine("GoBack_CanExecute is firing.");
			e.CanExecute = _vm?.ProjectViewModel?.CanGoBack(skipPanJobs:false) ?? false;
		}

		private void GoBack_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var skipPanJobs = !( Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
			_ = _vm.ProjectViewModel.GoBack(skipPanJobs);
		}

		private void GoForward_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel?.CanGoForward(skipPanJobs:false) ?? false;
		}

		private void GoForward_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var skipPanJobs = !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
			_ = _vm.ProjectViewModel.GoForward(skipPanJobs);
		}

		#endregion

		#region Project Button Handlers

		// New
		private void NewButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				//_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResult.NotSavingChanges)
			{
				_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			//_vm.MapDisplayViewModel.ClearDisplay();
			_vm.ProjectViewModel.ProjectClose();

			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = ProjectSaveChanges();
			if (saveResult == SaveResult.ChangesSaved)
			{
				//_ = MessageBox.Show("Changes Saved");
			}
			//else if (saveResult == SaveResult.NotSavingChanges)
			//{
			//	_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
			//}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			var initialName = _vm.ProjectViewModel.CurrentProjectName;
			if (ProjectShowOpenSaveWindow(DialogType.Open, initialName, out var selectedName, out _))
			{
				if (selectedName != null)
				{
					if (_vm.ProjectViewModel.CurrentProject != null)
					{
						Debug.WriteLine($"Closing the current project. Name: {_vm.ProjectViewModel.CurrentProjectName}.");

						if (saveResult == SaveResult.NotSavingChanges)
						{
							Debug.WriteLine("User chose not save changes... Deleting MapSections for unsaved Jobs.");

							_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
						}

						//_vm.MapDisplayViewModel.ClearDisplay();
						_vm.ProjectViewModel.ProjectClose();
					}

					Debug.WriteLine($"Opening project with name: {selectedName}.");
					_ = _vm.ProjectViewModel.ProjectOpen(selectedName);
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
			e.CanExecute = _vm?.ProjectViewModel?.CurrentProjectOnFile ?? false;
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var shouldContinue = ProjectSaveConfirmOnHighJobCount();

			if (shouldContinue != true)
			{
				return;
			}

			if (!_vm.ProjectViewModel.ProjectSave())
			{
				_ = MessageBox.Show("Could not save changes.");
			}
			else
			{
				//_ = MessageBox.Show("Changes Saved");
			}
		}

		// Project Save As
		private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel?.CurrentProject != null;
		}

		private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var curProject = _vm.ProjectViewModel.CurrentProject;

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
				_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
			}
			else if (saveResult == SaveResult.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			var curJob = _vm.ProjectViewModel.CurrentJob;

			if (curJob.IsEmpty)
			{
				_ = MessageBox.Show("Cannot create a poster, there is no current job.");
			}
			else
			{
				if (SavePosterInteractive(_vm.ProjectViewModel.CurrentProjectName, out var name, out var description))
				{
					var currentDisplaySize = _vm.MapDisplayViewModel.ViewportSize;
					var tentativePosterSize = RMapHelper.GetCanvasSize(currentDisplaySize, new SizeDbl(1024));

					if (_vm.ProjectViewModel.TryCreatePoster(name, description, tentativePosterSize, out var newPoster))
					{
						//_vm.MapDisplayViewModel.ClearDisplay();
						_vm.ProjectViewModel.ProjectClose();

						AppNavRequestResponse.OnCloseBehavior = OnCloseBehavior.ReturnToTopNav;
						AppNavRequestResponse.ResponseCommand = RequestResponseCommand.OpenPoster;
						AppNavRequestResponse.ResponseParameters = new string[] { newPoster.Name, "OpenSizeDialog" };

						Close();
					}
					else
					{
						_ = MessageBox.Show("Could not create the new poster.");
					}

				}
			}
		}

		private void CreateImageCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel.CurrentProject != null;
		}

		private void CreateImageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_createImageProgressWindow != null && IsWindowOpen(_createImageProgressWindow))
			{
				_createImageProgressWindow.WindowState = WindowState.Normal;
				return;
			}

			var curProject = _vm.ProjectViewModel.CurrentProject;

			if (curProject == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			// TODO: Provide UI to specify the size of the new Image file.
			//var imageSize = curProject.CurrentJob.CanvasSize.Scale(4);
			//var imageSize = new SizeInt(4096);

			var imageSize = _vm.MapDisplayViewModel.ViewportSize.Round();

			var areaColorAndCalcSettings = GetAreaColorAndCalcSettings();

			var initialImageFilename = GetImageFilename(curProject.Name, imageSize.Width);

			if (TryGetImagePath(initialImageFilename, out var imageFilePath))
			{
				// Pan our image so that the new image is close but not identical
				Pan(PanDirection.Left, PanAmountQualifer.Regular, SHIFT_AMOUNT);

				Debug.WriteLine($"The ExplorerWindow is StartingImageCreation. SaveTheZValues = {areaColorAndCalcSettings.MapCalcSettings.SaveTheZValues}.");

				_createImageProgressWindow = StartImageCreation(imageFilePath, areaColorAndCalcSettings, new SizeDbl(imageSize));

				_createImageProgressWindow.Show();
			}
		}

		private bool IsWindowOpen(Window window)
		{
			return window != null && Application.Current.Windows.Cast<Window>().Any(x => x.GetHashCode() == window.GetHashCode());
		}

		private bool TryGetImagePath(string initalName, [MaybeNullWhen(false)] out string imageFilePath)
		{
			var defaultOutputFolderPath = Properties.Settings.Default.DefaultOutputFolderPath;
			var createImageViewModel = new CreateImageViewModel(defaultOutputFolderPath, initalName);
			var createImageDialog = new CreateImageDialog()
			{
				DataContext = createImageViewModel
			};

			if (createImageDialog.ShowDialog() == true && createImageViewModel.ImageFileName != null)
			{
				imageFilePath = Path.Combine(createImageViewModel.FolderPath, createImageViewModel.ImageFileName);
				return true;
			}
			else
			{
				imageFilePath = null;
				return false;
			}
		}

		private CreateImageProgressWindow StartImageCreation(string imageFilePath, AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl imageSize)
		{
			var viewModelFactory = _vm.ViewModelFactory;
			var createImageProgressViewModel = viewModelFactory.CreateACreateImageProgressViewModel(ImageFileType.PNG);

			var jobId = areaColorAndCalcSettings.JobId;
			var useEscapeVelocities = _vm.CbsHistogramViewModel.UseEscapeVelocities;
			createImageProgressViewModel.CreateImage(imageFilePath, areaColorAndCalcSettings, imageSize, useEscapeVelocities);

			var result = new CreateImageProgressWindow()
			{
				DataContext = createImageProgressViewModel
			};

			return result;
		}

		private AreaColorAndCalcSettings GetAreaColorAndCalcSettings()
		{
			var curJob = _vm.ProjectViewModel.CurrentJob;
			var curJobId = curJob.Id;

			var newMapCalcSettings = curJob.MapCalcSettings;
			var newMapAreaInfo = curJob.MapAreaInfo;
			var newColorBandSet = _vm.ProjectViewModel.CurrentColorBandSet;

			var areaColorAndCalcSettings = new AreaColorAndCalcSettings
				(
				curJobId,
				OwnerType.Project,
				newMapAreaInfo,
				newColorBandSet,
				newMapCalcSettings
				);

			return areaColorAndCalcSettings;
		}

		private string GetImageFilename(string projectName, int imageWidth)
		{
			var result = $"{projectName}_{imageWidth}_v4.png";
			return result;
		}

		#endregion

		#region Colors Button Handlers

		// Colors Import
		private void ColorsOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel?.CurrentProject != null;
		}

		private void ColorsOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var initialName = _vm.ProjectViewModel.CurrentColorBandSet.Name;
			if (ColorsShowOpenWindow(initialName, out var colorBandSet))
			{
				Debug.WriteLine($"Importing ColorBandSet with Id: {colorBandSet.Id}, name: {colorBandSet.Name}.");

				var targetIterations1 = _vm.ProjectViewModel.CurrentColorBandSet.HighCutoff;

				var targetIterations2 = _vm.ProjectViewModel.CurrentJob.MapCalcSettings.TargetIterations;

				Debug.Assert(targetIterations1 == targetIterations2, "TargetInterations Mismatch.");


				var adjustedCbs = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, _vm.ProjectViewModel.CurrentJob.MapCalcSettings.TargetIterations);
				_vm.ProjectViewModel.CurrentColorBandSet = adjustedCbs;
			}
			else
			{
				Debug.WriteLine($"User declined to import a ColorBandSet.");
				_vm.ProjectViewModel.PreviewColorBandSet = null;
			}
		}

		// Colors Export
		private void ColorsSaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel?.CurrentProject != null;
		}

		private void ColorsSaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var curColorBandSet = _vm.ProjectViewModel.CurrentColorBandSet;

			_ = ColorsShowSaveWindow(curColorBandSet);
		}

		// Use Escape Velocities
		private void UseEscapeVelocities_Checked(object sender, RoutedEventArgs e)
		{
			var useEscVelocities = mnuItem_UseEscapeVelocities.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.UseEscapeVelocities = useEscVelocities;
			}
		}

		private void UseEscapeVelocities_Unchecked(object sender, RoutedEventArgs e)
		{
			var useEscVelocities = mnuItem_UseEscapeVelocities.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.UseEscapeVelocities = useEscVelocities;
			}
		}

		// Use RealTime Updates
		private void UseRealTimePreview_Checked(object sender, RoutedEventArgs e)
		{
			var useRealTimePreview = mnuItem_UseRealTimePreview.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.UseRealTimePreview = useRealTimePreview;
			}
		}

		private void UseRealTimePreview_Unchecked(object sender, RoutedEventArgs e)
		{
			var useRealTimePreview = mnuItem_UseRealTimePreview.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.UseRealTimePreview = useRealTimePreview;
			}
		}

		// High Light (only) the Selected Color Band
		private void HighlightSelected_Checked(object sender, RoutedEventArgs e)
		{
			var highlightSelectedCb = mnuItem_HighlightSelectedBand.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.HighlightSelectedBand = highlightSelectedCb;
			}
		}

		private void HighlightSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			var highlightSelectedCb = mnuItem_HighlightSelectedBand.IsChecked;
			if (_vm != null)
			{
				_vm.CbsHistogramViewModel.HighlightSelectedBand = highlightSelectedCb;
			}
		}

		#endregion

		#region Pan Button Handlers

		// TODO: Make the base shift amount proportional to the map view size.
		private const int SHIFT_AMOUNT = 16;

		private void Pan_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.ProjectViewModel?.CurrentProject != null;
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
			e.CanExecute = _vm.ProjectViewModel.CurrentProject != null;
		}

		private void ZoomOut12_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x12);
		}

		private void ZoomOut25_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x25);
		}

		private void ZoomOut50_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x50);
		}

		private void ZoomOut100_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ZoomOut(ZoomOutAmountQualifer.x100);
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
			//Width = Math.Max(1280, 128 * ((Width + 128) / 128));
			Width += 128;
			//_vm.BumpDispWidth(increase: true);
		}

		private void W_Down_Button_Click(object sender, RoutedEventArgs e)
		{
			//Width = Math.Min(128, 128 * ((Width - 128) / 128));
			Width -= 128;
			//_vm.BumpDispWidth(increase: false);
		}

		private void H_Up_Button_Click(object sender, RoutedEventArgs e)
		{
			//Height = Math.Min(1280, 128 * ((Height + 128) / 128));
			Height += 128;
			//_vm.BumpDispHeight(increase: true);

			//_vm.MapDisplayViewModel.DisplayZoom += 0.1; 

		}

		private void H_Down_Button_Click(object sender, RoutedEventArgs e)
		{
			//Height = Math.Max(128, 128 * ((Height - 128) / 128));
			Height -= 128;
			//_vm.BumpDispHeight(increase: false);
			//_vm.MapDisplayViewModel.DisplayZoom -= 0.1;
		}

		#endregion

		#region Private Methods - Project

		private void OpenProjectFromAppRequest(string[]? requestParameters)
		{
			if (requestParameters == null || requestParameters.Length < 1)
			{
				throw new InvalidOperationException("The Project's name must be included in the RequestParameters when the Command = 'OpenProject.'");
			}

			var projectName = requestParameters[0];

			_ = _vm.ProjectViewModel.ProjectOpen(projectName);
		}

		private void LoadNewProject()
		{
			var mapCalcSettings = RMapConstants.BuildMapCalcSettings();
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var mapAreaInfo = RMapConstants.BuildHomeArea();

			LoadNewProject(mapAreaInfo, colorBandSet, mapCalcSettings);
		}

		private void LoadNewProject(MapCenterAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			_vm.ProjectViewModel.ProjectStartNew(mapAreaInfo, colorBandSet, mapCalcSettings);
		}

		private SaveResult ProjectSaveChanges()
		{
			var curProject = _vm.ProjectViewModel.CurrentProject;

			if (curProject == null)
			{
				return SaveResult.NoChangesToSave;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return SaveResult.SaveCancelled;
			}

			SaveResult result;

			if (!_vm.ProjectViewModel.CurrentProjectIsDirty)
			{
				result = ProjectSaveCurrentJobId();
			}
			else
			{
				result = ProjectSaveDirtyJob(curProject);
			}

			return result;
		}

		// TODO: See if similar changes to the ProjectSave logic can be applied to the Poster
		private SaveResult ProjectSaveCurrentJobId()
		{
			SaveResult result;

			if (_vm.ProjectViewModel.IsCurrentJobIdChanged)
			{
				if (_vm.ProjectViewModel.CurrentProjectOnFile)
				{
					Debug.WriteLine($"Saving Project Silently: Not Dirty, but the Currently Selected Job has been updated.");

					// Silently record the new CurrentJob selection
					result = _vm.ProjectViewModel.ProjectSave() ? SaveResult.CurrentJobAutoSaved : SaveResult.NoChangesToSave;
				}
				else
				{
					result = SaveResult.NoChangesToSave;
				}
			}
			else
			{
				result = SaveResult.NotSavingChanges;
			}

			return result;
		}

		private SaveResult ProjectSaveDirtyJob(Project curProject)
		{
			SaveResult result;

			var triResult = ProjectSaveUserConfirm();

			if (triResult == true)
			{
				if (_vm.ProjectViewModel.CurrentProjectOnFile)
				{
					// The Project is on-file, just save the pending changes.
					result = _vm.ProjectViewModel.ProjectSave() ? SaveResult.ChangesSaved : SaveResult.NoChangesToSave;
				}
				else
				{
					// The Project is not on-file, must ask user for the name and optional description.
					triResult = SaveProjectInteractive(curProject);
					result = triResult == true ? SaveResult.ChangesSaved : SaveResult.SaveCancelled;
				}
			}
			else
			{
				result = triResult == false ? SaveResult.NotSavingChanges : SaveResult.SaveCancelled;
			}

			return result;
		}

		private bool? ProjectSaveUserConfirm()
		{
			var numberOfDirtyJobs = _vm.ProjectViewModel.GetGetNumberOfDirtyJobs();

			if (numberOfDirtyJobs == 0)
			{
				return true;
			}

			var	message = $"The current project has {numberOfDirtyJobs} un-saved jobs. Save Changes?";

			var defaultResult = _vm.ProjectViewModel.CurrentProjectOnFile ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show(message, "Pending Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);
			var result = res == MessageBoxResult.Yes ? true : res == MessageBoxResult.No ? false : (bool?)null;

			return result;
		}

		private bool? ProjectSaveConfirmOnHighJobCount()
		{
			var numberOfDirtyJobs = _vm.ProjectViewModel.GetGetNumberOfDirtyJobs();
			if (numberOfDirtyJobs > 3)
			{
				var x = MessageBox.Show($"There are {numberOfDirtyJobs} un-saved jobs. Continue to save?", "Number of Un-Saved Jobs is High", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
				if (x == MessageBoxResult.Yes)
				{
					return true;
				}
				else if (x == MessageBoxResult.No)
				{
					return false;
				}
				else if (x == MessageBoxResult.Cancel)
				{
					return null;
				}
			}
			return true;
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

					// TODO: Add error handling around ProjectSaveAs.
					if (!_vm.ProjectViewModel.ProjectSaveAs(selectedName, description, out var errorText))
					{
						_ = MessageBox.Show($"Could not save the project using the new name: {selectedName}. The error is {errorText}");
						result = false;
					}
					else
					{
						result = true;
					}
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

		private bool ProjectShowOpenSaveWindow(DialogType dialogType, string? initalName, out string? selectedName, out string? description)
		{
			var projectOpenSaveVm = _vm.ViewModelFactory.CreateAProjectOpenSaveViewModel(initalName, dialogType);
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
			ColorBandSet colorBandSet;
			MapCalcSettings mapCalcSettings;

			var curJob = _vm.ProjectViewModel.CurrentJob;
			if (!curJob.IsEmpty)
			{
				var displaySize = _vm.MapDisplayViewModel.ViewportSize;

				coordsEditorViewModel = _vm.ViewModelFactory.CreateACoordsEditorViewModel(curJob.MapAreaInfo, displaySize, allowEdits: true);
				colorBandSet = _vm.ProjectViewModel.CurrentColorBandSet;
				mapCalcSettings = curJob.MapCalcSettings;
			}
			else
			{
				var displaySize = _vm.MapDisplayViewModel.ViewportSize;

				//var x1 = "-0.477036968733327014028268226139546";
				//var x2 = "-0.477036964892343354414420540166062";
				//var y1 = "0.535575821681765930306959274776606";
				//var y2 = "0.535575824239325800205884281044245";

				//var x1 = "-0.4770369687333";
				//var x2 = "-0.4770369648923";
				//var y1 = "0.5355758216817";
				//var y2 = "0.5355758242393";

				//var x1 = "-0.81976318359375";
				//var x2 = "-0.78863525390625";
				//var y1 = "0.1584930419921875";
				//var y2 = "0.1891632080078125";

				var x1 = "-0.7595138884311722904385533";
				var x2 = "-0.75951388005302078454406";
				var y1 = "0.08547031274046901216934202";
				var y2 = "0.08547032099541240768303396";


				var coords = RValueHelper.BuildRRectangle(new string[] { x1, x2, y1, y2 });
				coordsEditorViewModel = _vm.ViewModelFactory.CreateACoordsEditorViewModel(coords, displaySize, allowEdits: true);
				mapCalcSettings = RMapConstants.BuildMapCalcSettings();
				colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			}

			var coordsEditorWindow = new CoordsEditorWindow()
			{
				DataContext = coordsEditorViewModel
			};

			if (coordsEditorWindow.ShowDialog() == true)
			{
				if (!curJob.IsEmpty)
				{
					var saveResult = ProjectSaveChanges();
					if (saveResult == SaveResult.ChangesSaved)
					{
						_ = MessageBox.Show("Changes Saved");
					}
					else if (saveResult == SaveResult.NotSavingChanges)
					{
						_ = _vm.ProjectViewModel.DeleteMapSectionsForUnsavedJobs();
					}
					else if (saveResult == SaveResult.SaveCancelled)
					{
						// user cancelled.
						return;
					}

					return;
				}

				//_vm.MapDisplayViewModel.ClearDisplay();
				_vm.ProjectViewModel.ProjectClose();

				var mapAreaInfoV2 = coordsEditorViewModel.GetMapCenterAndDeltaDepreciated();
				LoadNewProject(mapAreaInfoV2, colorBandSet, mapCalcSettings);
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


			var nPoint = new RPoint(l1.XValues[6].Value * 8, l1.YValues[6].Value * 8, sPoint.Exponent - 3);
			var l2 = new MapSectionSpIdxItem(nPoint);
			Debug.WriteLine(l2);

			nPoint = new RPoint(l2.XValues[6].Value * 8, l2.YValues[2].Value * 8, nPoint.Exponent - 3);

			var l3 = new MapSectionSpIdxItem(nPoint);
			Debug.WriteLine(l3);
		}

		#endregion

		#region Private Methods - Poster

		private bool SavePosterInteractive(string? initialName, [NotNullWhen(true)] out string? name, out string? description)
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

		private bool PosterShowOpenSaveWindow(DialogType dialogType, string? initalName, [NotNullWhen(true)] out string? selectedName, out string? description)
		{
			var posterOpenSaveVm = _vm.ViewModelFactory.CreateAPosterOpenSaveViewModel(initalName, dialogType);
			var posterOpenSaveWindow = new PosterOpenSaveWindow
			{
				DataContext = posterOpenSaveVm
			};

			if (posterOpenSaveWindow.ShowDialog() == true)
			{
				selectedName = posterOpenSaveWindow.PosterName ?? string.Empty; // TODO: Fix Me
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

			if (_vm.CbsHistogramViewModel.IsDirty)
			{
				var defaultResult = MessageBoxResult.Yes;
				var res = MessageBox.Show("Save edits made in the ColorBand Editor?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

				if (res == MessageBoxResult.Yes)
				{
					_vm.CbsHistogramViewModel.ApplyChanges();
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
			var colorBandSetOpenSaveVm = _vm.ViewModelFactory.CreateACbsOpenSaveViewModel(initalName, DialogType.Open);
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
					//_vm.MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
					_vm.ProjectViewModel.PreviewColorBandSet = colorBandSet;
				}
			}
		}

		private bool ColorsShowSaveWindow(ColorBandSet colorBandSet)
		{
			var colorBandSetOpenSaveVm = _vm.ViewModelFactory.CreateACbsOpenSaveViewModel(colorBandSet.Name, DialogType.Save);
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
			var currentMapAreaInfo = _vm.MapDisplayViewModel.CurrentAreaColorAndCalcSettings?.MapAreaInfo ?? null;

			if (currentMapAreaInfo != null)
			{ 
				var qualifiedAmount = GetPanAmount(amount, qualifer);
				var panVector = GetPanVector(direction, qualifiedAmount);
				//var newArea = new RectangleInt(new PointInt(panVector), _vm.ProjectViewModel.CanvasSize);

				_vm.ProjectViewModel.UpdateMapView(TransformType.Pan, panVector, factor:1, currentMapAreaInfo);
			}
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

			var displaySize = _vm.MapDisplayViewModel.ViewportSize;
			var result = RMapHelper.CalculatePitch(displaySize, targetAmount);

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

		private void ZoomOut(ZoomOutAmountQualifer qualifer)
		{
			var currentMapAreaInfo = _vm.MapDisplayViewModel.CurrentAreaColorAndCalcSettings?.MapAreaInfo ?? null;

			if (currentMapAreaInfo != null)
			{
				var qualifiedAmount = GetZoomOutAmount(qualifer);
				//var curArea = new RectangleInt(new PointInt(), _vm.ProjectViewModel.CanvasSize);
				//var newArea = curArea.Expand(new SizeInt(qualifiedAmount));

				_vm.ProjectViewModel.UpdateMapView(TransformType.ZoomOut, new VectorInt(1, 1), factor: qualifiedAmount, currentMapAreaInfo);
			}
		}

		private double GetZoomOutAmount(ZoomOutAmountQualifer qualifer)
		{
			//var targetAmount = qualifer switch
			//{
			//	ZoomOutAmountQualifer.x12 => baseAmount * 8,   // 128
			//	ZoomOutAmountQualifer.x25 => baseAmount * 16,  // 256
			//	ZoomOutAmountQualifer.x50 => baseAmount * 32,  // 512
			//	ZoomOutAmountQualifer.x100 => baseAmount * 64, // 1024
			//	_ => baseAmount * 32,
			//};

			var zoomInFactor = qualifer switch
			{
				ZoomOutAmountQualifer.x12 => 2,		//	* 2
				ZoomOutAmountQualifer.x25 => 4,		//	* 4
				ZoomOutAmountQualifer.x50 => 8,		//	* 8
				ZoomOutAmountQualifer.x100 => 16,	//	* 16
				_ => 8,								//	Default = 8
			};

			return 1 / (double) zoomInFactor;
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

	}
}
