using MSetExplorer.ScreenHelpers;
using MSS.Common;
using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for PosterDesignerWindow.xaml
	/// </summary>
	public partial class PosterDesignerWindow : Window, IHaveAppNavRequestResponse
	{
		private IPosterDesignerViewModel _vm;

		private CreateImageProgressWindow? _createImageProgressWindow;

		#region Constructor

		public PosterDesignerWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = (IPosterDesignerViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;
			_createImageProgressWindow = null;

			Loaded += PosterDesignerWindow_Loaded;
			Closing += PosterDesignerWindow_Closing;
			ContentRendered += PosterDesignerWindow_ContentRendered;
			InitializeComponent();
		}

		private void PosterDesignerWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IPosterDesignerViewModel)DataContext;
				_vm.PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;

				_vm.PosterViewModel.LogicalDisplaySize = _vm.MapDisplayViewModel.LogicalDisplaySize;
				_vm.MapScrollViewModel.CanvasSize = _vm.MapDisplayViewModel.CanvasSize;

				mapScroll1.DataContext = _vm.MapScrollViewModel;
				_vm.MapScrollViewModel.PropertyChanged += MapScrollViewModel_PropertyChanged;

				_vm.ColorBandSetViewModel.PropertyChanged += ColorBandSetViewModel_PropertyChanged;
				colorBandView1.DataContext = _vm.ColorBandSetViewModel;

				mapCalcSettingsView1.DataContext = _vm.MapCalcSettingsViewModel;

				mapCoordsView1.DataContext = _vm.MapCoordsViewModel;

				scrBarZoom.Minimum = 1;
				scrBarZoom.Value = 1;

				scrBarZoom.Maximum = 10;
				scrBarZoom.SmallChange = 0.1;
				scrBarZoom.LargeChange = 1;

				scrBarZoom.Value = 1;

				scrBarZoom.Scroll += ScrBarZoom_Scroll;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		private void PosterDesignerWindow_ContentRendered(object? sender, EventArgs e)
		{
			Debug.WriteLine("The PosterDesigner Window is handling ContentRendered");

			if (AppNavRequestResponse.RequestCommand == RequestResponseCommand.OpenPoster)
			{
				var posterName = AppNavRequestResponse.RequestParameters?[0];

				if (posterName != null)
				{
					_ = _vm.PosterViewModel.PosterOpen(posterName);
				}
			}
		}

		private void PosterDesignerWindow_Closing(object sender, CancelEventArgs e)
		{
			var saveResult = PosterSaveChanges();
			if (saveResult == SaveResultP.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResultP.SaveCancelled)
			{
				// user cancelled.
				e.Cancel = true;
			}
		}

		#endregion

		#region Private Methods

		private void SetDisplayZoom(double val)
		{
			_vm.MapScrollViewModel.DisplayZoom = val;
			var adjustedDisplayZoom = _vm.MapScrollViewModel.DisplayZoom;
			txtblkZoomValue.Text = Math.Round(adjustedDisplayZoom, 2).ToString(CultureInfo.InvariantCulture);
			
			_vm.MapScrollViewModel.VerticalPosition = 0;
			_vm.MapScrollViewModel.HorizontalPosition = 0;
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPosterViewModel.CurrentPoster))
			{
				Title = GetWindowTitle(_vm.PosterViewModel.CurrentPoster?.Name, _vm.PosterViewModel.ColorBandSet?.Name);
				CommandManager.InvalidateRequerySuggested();
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProjectOnFile) || e.PropertyName == nameof(IMapProjectViewModel.CurrentProjectIsDirty))
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private void MapScrollViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.MaximumDisplayZoom))
			{
				scrBarZoom.Minimum = 1;
				scrBarZoom.Maximum = _vm.MapScrollViewModel.MaximumDisplayZoom;
				scrBarZoom.LargeChange = scrBarZoom.Maximum / 8;
				scrBarZoom.SmallChange = scrBarZoom.LargeChange / 8;
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

		private void ScrBarZoom_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			if (_vm.PosterViewModel.CurrentPoster != null)
			{
				var val = e.NewValue;
				if (val < 1)
				{
					val = 1;
				}

				SetDisplayZoom(val);
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
			var saveResult = PosterSaveChanges();
			if (saveResult == SaveResultP.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResultP.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			_vm.PosterViewModel.PosterClose();
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

		#endregion

		#region Project Button Handlers

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var saveResult = PosterSaveChanges();
			if (saveResult == SaveResultP.ChangesSaved)
			{
				_ = MessageBox.Show("Changes Saved");
			}
			else if (saveResult == SaveResultP.SaveCancelled)
			{
				// user cancelled.
				return;
			}

			var initialName = _vm.PosterViewModel.CurrentPosterName;
			if (PosterShowOpenSaveWindow(DialogType.Open, initialName, out var selectedName, out _))
			{
				if (selectedName != null)
				{
					Debug.WriteLine($"Opening project with name: {selectedName}.");
					_ = _vm.PosterViewModel.PosterOpen(selectedName);
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
			e.CanExecute = _vm?.PosterViewModel.CurrentPosterOnFile ?? false;
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_vm.PosterViewModel.PosterSave();
		}

		// Project Save As
		private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
		}

		private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var curPoster = _vm.PosterViewModel.CurrentPoster;

			if (curPoster == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			_ = SavePosterInteractive(curPoster);
		}

		private void CreateImageCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
		}

		private void CreateImageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (_createImageProgressWindow != null && IsWindowOpen(_createImageProgressWindow))
			{
				_createImageProgressWindow.WindowState = WindowState.Normal;
				return;
			}

			var curPoster = _vm.PosterViewModel.CurrentPoster;

			if (curPoster == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var initialImageFilename = GetImageFilename(curPoster.Name, curPoster.JobAreaInfo.CanvasSize.Width);

			if (TryGetImagePath(initialImageFilename, out var imageFilePath))
			{
				_createImageProgressWindow = StartImageCreation(imageFilePath, curPoster, useEscapeVelocities: true);

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

		private CreateImageProgressWindow StartImageCreation(string imageFilePath, Poster poster, bool useEscapeVelocities)
		{
			var createImageProgressViewModel = _vm.CreateACreateImageProgressViewModel(imageFilePath);

			createImageProgressViewModel.CreateImage(imageFilePath, poster);

			var result = new CreateImageProgressWindow()
			{
				DataContext = createImageProgressViewModel
			};

			return result;
		}

		private string GetImageFilename(string projectName, int imageWidth)
		{
			var result = $"{projectName}_{imageWidth}_v4.png";
			return result;
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

		#endregion

		#region Colors Button Handlers

		// Colors Import
		private void ColorsOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
		}

		private void ColorsOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var curPoster = _vm.PosterViewModel.CurrentPoster;

			if (curPoster == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

			var initialName = _vm.PosterViewModel.ColorBandSet?.Name ?? string.Empty;
			if (ColorsShowOpenWindow(initialName, out var colorBandSet))
			{
				Debug.WriteLine($"Importing ColorBandSet with Id: {colorBandSet.Id}, name: {colorBandSet.Name}.");

				var adjustedCbs = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, curPoster.MapCalcSettings.TargetIterations);
				_vm.PosterViewModel.UpdateColorBandSet(adjustedCbs);
			}
			else
			{
				Debug.WriteLine($"User declined to import a ColorBandSet.");
				var projectsColorBandSet = _vm.PosterViewModel.ColorBandSet;

				if (_vm.MapDisplayViewModel.ColorBandSet != projectsColorBandSet && projectsColorBandSet != null)
				{
					_vm.MapDisplayViewModel.SetColorBandSet(projectsColorBandSet, updateDisplay: true);
				}
			}
		}

		// Colors Export
		private void ColorsSaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
		}

		private void ColorsSaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var curColorBandSet = _vm.PosterViewModel.ColorBandSet;

			if (curColorBandSet == null)
			{
				return;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return;
			}

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
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
		}

		private void PanLeft_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			//var x = _vm.MapDisplayViewModel.CanvasControlOffset;
			//var y = _vm.MapDisplayViewModel.ClipRegion;

			//_ = MessageBox.Show($"The Canvas Control Offset is {x}. The DrawingGroup's Clip Region is {y},");
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

		#region DisplayZoom Min Max Button Handlers

		private void ButtonSetMaxZoom_Click(object sender, RoutedEventArgs e)
		{
			var max = _vm.MapScrollViewModel.MaximumDisplayZoom;
			scrBarZoom.Value = max;
			SetDisplayZoom(max);
		}

		private void ButtonSetMinZoom_Click(object sender, RoutedEventArgs e)
		{
			scrBarZoom.Value = 1;
			SetDisplayZoom(1);
		}

		#endregion

		#region Zoom Out Button Handlers

		private void ZoomOut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm?.PosterViewModel.CurrentPoster != null;
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

		#region Private Methods - Poster

		private SaveResultP PosterSaveChanges()
		{
			// TODO: Replace with PosterDesigner viewmodel
			var curProject = _vm.PosterViewModel.CurrentPoster;

			if (curProject == null)
			{
				return SaveResultP.NoChangesToSave;
			}

			if (!_vm.PosterViewModel.CurrentPosterIsDirty)
			{
				if (_vm.PosterViewModel.CurrentPosterOnFile)
				{
					// Silently record the new CurrentJob selection
					_vm.PosterViewModel.PosterSave();
					return SaveResultP.CurrentJobAutoSaved;
				}

				return SaveResultP.NoChangesToSave;
			}

			if (!ColorsCommitUpdates().HasValue)
			{
				return SaveResultP.SaveCancelled;
			}

			var triResult = PosterUserSaysSaveChanges();

			if (triResult == true)
			{
				if (_vm.PosterViewModel.CurrentPosterOnFile)
				{
					// The Project is on-file, just save the pending changes.
					_vm.PosterViewModel.PosterSave();
					return SaveResultP.ChangesSaved;
				}
				else
				{
					// The Project is not on-file, must ask user for the name and optional description.
					triResult = SavePosterInteractive(curProject);
					if (triResult == true)
					{
						return SaveResultP.ChangesSaved;
					}
					else
					{
						return SaveResultP.SaveCancelled;
					}
				}
			}
			else if (triResult == false)
			{
				return SaveResultP.NotSavingChanges;
			}
			else
			{
				return SaveResultP.SaveCancelled;
			}
		}

		private bool? SavePosterInteractive(Poster curPoster)
		{
			bool? result;

			var initialName = curPoster.Name;

			if (PosterShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description))
			{
				if (selectedName != null)
				{
					Debug.WriteLine($"Saving project with name: {selectedName}.");
					// TODO: Handle cases where ProjectSaveAs fails.
					result = _vm.PosterViewModel.PosterSaveAs(selectedName, description);
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

		private bool? PosterUserSaysSaveChanges()
		{
			// TODO: replace with PosterDesigner ViewModel
			var defaultResult = _vm.PosterViewModel.CurrentPosterOnFile ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show("The current poster has pending changes. Save Changes?", "Pending Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

			var result = res == MessageBoxResult.Yes ? true : res == MessageBoxResult.No ? false : (bool?)null;

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

		private string GetWindowTitle(string? posterName, string? colorBandSetName)
		{
			const string dash = "\u2014";

			var result = posterName != null
				? colorBandSetName != null
					? $"Designer Window {dash} {posterName} {dash} {colorBandSetName}"
					: $"Designer Window {dash} {posterName}"
				: "Designer Window";

			return result;
		}

		private void ShowCoordsEditor()
		{
			CoordsEditorViewModel coordsEditorViewModel;

			var curPoster = _vm.PosterViewModel.CurrentPoster;
			var posterSize = _vm.MapScrollViewModel.PosterSize;

			if (! (curPoster != null && posterSize.HasValue) )
			{
				return;
			}

			coordsEditorViewModel = new CoordsEditorViewModel(curPoster.JobAreaInfo.Coords, posterSize.Value, allowEdits: true, _vm.ProjectAdapter);

			var coordsEditorWindow = new CoordsEditorWindow()
			{
				DataContext = coordsEditorViewModel
			};

			if (coordsEditorWindow.ShowDialog() == true)
			{
				var saveResult = PosterSaveChanges();
				if (saveResult == SaveResultP.ChangesSaved)
				{
					_ = MessageBox.Show("Changes Saved");
				}
				else if (saveResult == SaveResultP.SaveCancelled)
				{
					// user cancelled.
					return;
				}

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
			var newArea = new RectangleInt(new PointInt(panVector), _vm.PosterViewModel.CanvasSize);
			_vm.PosterViewModel.UpdateMapView(TransformType.Pan, newArea);
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

			var result = RMapHelper.CalculatePitch(_vm.PosterViewModel.CanvasSize, targetAmount);

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
			//_ = MessageBox.Show($"Zooming Out. Amount = {amount}.");

			var qualifiedAmount = GetZoomOutAmount(amount, qualifer);
			var curArea = new RectangleInt(new PointInt(), _vm.PosterViewModel.CanvasSize);
			var newArea = curArea.Expand(new SizeInt(qualifiedAmount));

			_vm.PosterViewModel.UpdateMapView(TransformType.ZoomOut, newArea);
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

		private enum SaveResultP
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
