﻿using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics.CodeAnalysis;

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
			if (ProjectSaveChanges())
			{
				_ = MessageBox.Show("Changes Saved");
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
			if (ProjectSaveChanges())
			{
				_ = MessageBox.Show("Changes Saved");
			}

			LoadNewProject();
		}

		// Open
		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			if (ProjectSaveChanges())
			{
				_ = MessageBox.Show("Changes Saved");
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

			var initialName = curProject?.Name;
			var curColorBandSetIds = curProject?.ColorBandSetSNs;
			var curColorBandSet = curProject?.CurrentColorBandSet;

			if (ProjectShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description))
			{
				if (selectedName != null && curColorBandSetIds != null && curColorBandSet != null)
				{
					Debug.WriteLine($"Saving project with name: {selectedName}.");
					_vm.MapProjectViewModel.ProjectSaveAs(selectedName, description, curColorBandSetIds, curColorBandSet);
				}
				else
				{
					Debug.WriteLine($"Cannot saving project with name: {selectedName}.");
				}
			}
		}

		#endregion

		#region Colors Button Handlers

		// Colors Open
		private void ColorsOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CurrentProject != null;
		}

		private void ColorsOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (ColorsSaveChanges())
			{
				_ = MessageBox.Show("Changes Saved");
			}

			var initialName = _vm.MapProjectViewModel.CurrentColorBandSet?.Name;
			if (ColorsShowOpenSaveWindow(DialogType.Open, initialName, out var selectedName, out _, out _, out var serialNumber))
			{
				//	if (serialNumber == null)
				//	{
				//		Debug.WriteLine($"WARNING: Cannot open a ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
				//	}
				//	else
				//	{
				//		Debug.WriteLine($"Opening ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
				//		if (!_vm.MapProjectViewModel.ColorBandSetOpen(serialNumber.Value))
				//		{
				//			_ = MessageBox.Show($"Could not open a ColorBandSet with {serialNumber.Value}.");
				//		}
				//	}

				Debug.WriteLine($"Opening ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
				if (!_vm.MapProjectViewModel.ColorBandSetOpen(serialNumber))
				{
					_ = MessageBox.Show($"Could not open a ColorBandSet with {serialNumber}.");
				}
			}
		}

			// Colors Save
		private void ColorsSaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CanSaveColorBandSet || _vm.ColorBandSetViewModel.IsDirty;
		}

		private void ColorsSaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = ColorsCommitUpdates();

			if (_vm.MapProjectViewModel.CanSaveColorBandSet)
			{
				// The ColorBandSet is on-file, just save the pending changes.
				_vm.MapProjectViewModel.ColorBandSetSave();
			}
			else
			{
				SaveColors();
			}
		}

		// Colors SaveAs
		private void ColorsSaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _vm.MapProjectViewModel.CurrentProject != null;
		}

		private void ColorsSaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_ = ColorsCommitUpdates();
			SaveColors();
		}

		private void SaveColors()
		{
			var curProject = _vm.MapProjectViewModel.CurrentProject;
			var initialName = curProject?.CurrentColorBandSet?.Name;

			if (ColorsShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description, out var versionNumber, out var serialNumber))
			{
				if (selectedName == null)
				{
					Debug.WriteLine($"WARNING: Cannot save a ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
				}
				else
				{
					Debug.WriteLine($"Saving ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
					_vm.MapProjectViewModel.ColorBandSetSaveAs(selectedName, description, versionNumber);
				}
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

		private bool ProjectSaveChanges()
		{
			_ = ColorsCommitUpdates();
			bool result;

			if (_vm.MapProjectViewModel.CurrentProjectIsDirty)
			{
				if (ProjectUserSaysSaveChanges())
				{
					if (_vm.MapProjectViewModel.CanSaveProject)
					{
						// The Project is on-file, just save the pending changes.
						_vm.MapProjectViewModel.ProjectSave();
						result = true;
					}
					else
					{
						// The Project is not on-file, must ask user for the name and optional description.
						var curProject = _vm.MapProjectViewModel.CurrentProject;

						var initialName = curProject?.Name;
						var curColorBandSetIds = curProject?.ColorBandSetSNs;
						var curColorBandSet = curProject?.CurrentColorBandSet;

						if (ProjectShowOpenSaveWindow(DialogType.Save, initialName, out var selectedName, out var description))
						{
							if (selectedName != null && curColorBandSetIds != null && curColorBandSet != null)
							{
								Debug.WriteLine($"Saving project with name: {selectedName}.");
								_vm.MapProjectViewModel.ProjectSaveAs(selectedName, description, curColorBandSetIds, curColorBandSet);
								result = true;
							}
							else
							{
								Debug.WriteLine($"Cannot save project with name: {selectedName}.");
								result = false;
							}
						}
						else
						{
							result = false;
						}
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

		private bool ProjectUserSaysSaveChanges()
		{
			var defaultResult = _vm.MapProjectViewModel.CanSaveProject ? MessageBoxResult.Yes : MessageBoxResult.No;
			var res = MessageBox.Show("The current project has pending changes. Save Changes?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

			var result = res == MessageBoxResult.Yes;

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

		private bool ColorsCommitUpdates()
		{
			bool result;

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
					result = false;
				}
			}
			else
			{
				result = false;
			}

			return result;
		}

		private bool ColorsSaveChanges()
		{
			_ = ColorsCommitUpdates();
			bool result;

			if (_vm.MapProjectViewModel.CurrentColorBandSetIsDirty)
			{
				if (ColorsUserSaysSaveChanges())
				{
					if (_vm.MapProjectViewModel.CanSaveColorBandSet)
					{
						// The ColorBandSet is on-file, just save the pending changes.
						_vm.MapProjectViewModel.ColorBandSetSave();
						result = true;
					}
					else
					{
						// The ColorBandSet is not on-file, must ask user for the name and optional description.
						var initialName = _vm.MapProjectViewModel.CurrentColorBandSet?.Name;
						if (ColorsShowOpenSaveWindow(DialogType.Open, initialName, out var selectedName, out _, out _, out var serialNumber))
						{
							//if (serialNumber != null)
							//{
							//	Debug.WriteLine($"Opening ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
							//	if (_vm.MapProjectViewModel.ColorBandSetOpen(serialNumber.Value))
							//	{
							//		result = true;
							//	}
							//	else
							//	{
							//		result = false;
							//		_ = MessageBox.Show($"Could not open a ColorBandSet with {serialNumber.Value}.");
							//	}
							//}
							//else
							//{
							//	Debug.WriteLine($"WARNING: Cannot open a ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
							//	result = false;
							//}

							Debug.WriteLine($"Opening ColorBandSet with serial: {serialNumber}, name: {selectedName}.");
							if (_vm.MapProjectViewModel.ColorBandSetOpen(serialNumber))
							{
								result = true;
							}
							else
							{
								result = false;
								_ = MessageBox.Show($"Could not open a ColorBandSet with {serialNumber}.");
							}
						}
						else
						{
							// User declined to save the ColorBandSet
							result = false;
						}
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

		private bool ColorsUserSaysSaveChanges()
		{
			var defaultResult = MessageBoxResult.Yes;
			var res = MessageBox.Show("The current ColorBandSet has pending changes. Save Changes?", "Changes Made", MessageBoxButton.YesNoCancel, MessageBoxImage.Hand, defaultResult, MessageBoxOptions.None);

			var result = res == MessageBoxResult.Yes;

			return result;
		}

		private bool ColorsShowOpenSaveWindow(DialogType dialogType, string? initalName, out string? selectedName, out string? description, out int? versionNumber, [MaybeNullWhen(false)] out Guid serialNumber)
		{
			var colorBandSetOpenSaveVm = _vm.CreateAColorBandSetOpenSaveViewModel(initalName, dialogType);
			var colorBandSetOpenSaveWindow = new ColorBandSetOpenSaveWindow
			{
				DataContext = colorBandSetOpenSaveVm
			};

			if (colorBandSetOpenSaveWindow.ShowDialog() == true)
			{
				selectedName = colorBandSetOpenSaveWindow.ColorBandSetName;
				description = colorBandSetOpenSaveWindow.ColorBandSetDescription;
				versionNumber = colorBandSetOpenSaveWindow.ColorBandSetVersionNumber;
				serialNumber = colorBandSetOpenSaveWindow.ColorBandSetSerialNumber ?? Guid.Empty;
				return true;
			}
			else
			{
				selectedName = null;
				description = null;
				versionNumber = 0;
				serialNumber = Guid.Empty;
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
