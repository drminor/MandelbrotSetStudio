using MongoDB.Bson;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetOpenSaveWindow.xaml
	/// </summary>
	public partial class ColorBandSetOpenSaveWindow : Window
	{
		private IColorBandSetOpenSaveViewModel _vm;

		#region Constructor

		public ColorBandSetOpenSaveWindow()
		{
			_vm = (IColorBandSetOpenSaveViewModel)DataContext;
			Loaded += ColorBandSetOpenSaveWindow_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the ColorBandSetOpenSave Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IColorBandSetOpenSaveViewModel)DataContext;
				borderTop.DataContext = DataContext;

				btnSave.Content = _vm.DialogType == DialogType.Open ? "Open" : "Save";
				Title = _vm.DialogType == DialogType.Open ? "Open ColorBandSet" : "Save ColorBandSet";

				// TODO: Create a filter for the list of ColorBandSetInfos to include only the 3 most recent versions of any name / target iteration group.
				lvColorBandSets.ItemsSource = _vm.ColorBandSetInfos;
				lvColorBandSets.SelectionChanged += LvColorBandSets_SelectionChanged;

				lvColorBandSets.MouseDoubleClick += LvColorBandSets_MouseDoubleClick;

				txtName.LostFocus += TxtName_LostFocus;

				_ = txtName.Focus();
				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ColorBandSetOpenSave Window is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void LvColorBandSets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (ColorBandSetName != null)
			{
				if (TakeSelection(ColorBandSetName))
				{
					DialogResult = true;
					Close();
				}
			}
		}

		private void TxtName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				_vm.SelectedName = _vm.SelectedColorBandSetInfo?.Name;
				_vm.UserIsSettingTheName = false;
			}
			else
			{
				if (txtName.Text == _vm.SelectedColorBandSetInfo?.Name)
				{
					_vm.UserIsSettingTheName = false;
					_vm.SelectedName = _vm.SelectedColorBandSetInfo?.Name;
				}
				else
				{
					_vm.UserIsSettingTheName = true;
				}
			}

			btnSave.IsEnabled = _vm.SelectedName != null;
		}

		private void LvColorBandSets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnSave.IsEnabled = ColorBandSetName != null;
		}

		#endregion

		#region Public Properties

		public ObjectId? ColorBandSetId => _vm.SelectedColorBandSetInfo?.Id;
		public string? ColorBandSetName => _vm.SelectedName;
		public string? ColorBandSetDescription => _vm.SelectedDescription;

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (ColorBandSetName != null)
			{
				if (TakeSelection(ColorBandSetName))
				{
					DialogResult = true;
					Close();
				}
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private bool TakeSelection(string selectedName)
		{
			if (_vm.DialogType == DialogType.Save)
			{
				return CheckNameOnSave(selectedName);
			}
			else
			{
				if (_vm.SelectedColorBandSetInfo == null)
				{
					throw new InvalidOperationException("Take Selection is being called but no ColorBandSetInfo is selected.");
				}

				return CheckNameAndTargetIterationsOnOpen(selectedName, _vm.SelectedColorBandSetInfo.TargetIterations, _vm.TargetIterations);
			}
		}

		private bool CheckNameOnSave(string selectedName)
		{
			if (_vm.IsNameTaken(selectedName))
			{
				var msg = "A ColorBandSet already exists with this name. Do you want to overwrite?";
				var res = MessageBox.Show(msg, "Overwrite Existing ColorBandSet", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);
				return res == MessageBoxResult.Yes;
			}
			else
			{
				return true;
			}
		}

		private bool CheckNameAndTargetIterationsOnOpen(string selectedName, int selectedTargetIterations, int currentTargetIterations)
		{
			if (selectedTargetIterations != currentTargetIterations && _vm.IsNameTaken(selectedName))
			{
				var msg = $"Opening the selected ColorBandSet will result in a new ColorBandSet being created with Target Iterations = {currentTargetIterations}. " +
					$"A ColorBandSet already exists for Target Iterations: {currentTargetIterations} with this name. Do you want create a new version using the selected item?";

				var res = MessageBox.Show(msg, "Create New Version", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No, MessageBoxOptions.None);
				return res == MessageBoxResult.Yes;
			}
			else
			{
				return true;
			}
		}

		#endregion
	}
}
