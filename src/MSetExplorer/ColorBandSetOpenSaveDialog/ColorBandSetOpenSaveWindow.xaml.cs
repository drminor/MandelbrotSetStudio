using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

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
			OverwriteExisting = null;

			_vm = (IColorBandSetOpenSaveViewModel)DataContext;
			Loaded += ColorBandSetOpenSaveWindow_Loaded;
			InitializeComponent();
		}

		private void ColorBandSetOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IColorBandSetOpenSaveViewModel)DataContext;
				borderTop.DataContext = DataContext;

				btnSave.Content = _vm.DialogType == DialogType.Open ? "Open" : "Save";
				Title = _vm.DialogType == DialogType.Open ? "Open ColorBandSet" : "Save ColorBandSet";

				lvColorBandSets.ItemsSource = _vm.ColorBandSetInfos;
				lvColorBandSets.SelectionChanged += LvColorBandSets_SelectionChanged;

				lvColorBandSets.MouseDoubleClick += LvColorBandSets_MouseDoubleClick;

				txtName.LostFocus += TxtName_LostFocus;

				_ = txtName.Focus();
				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ColorBandSetOpenSaveWindow is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void LvColorBandSets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (ColorBandSetName != null)
			{
				TakeSelection(ColorBandSetName);
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

		public bool? OverwriteExisting { get; set; }

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (ColorBandSetName != null)
			{
				TakeSelection(ColorBandSetName);
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TakeSelection(string selectedName)
		{
			if (_vm.DialogType == DialogType.Save)
			{
				if (_vm.IsNameTaken(selectedName))
				{
					var msg = "A ColorBandSet already exists with this name. Do you want to overwrite?";
					var res = MessageBox.Show(msg, "Overwrite Existing ColorBandSet", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

					if (res == MessageBoxResult.No)
					{
						return;
					}
				}
			}

			if (_vm.DialogType == DialogType.Open)
			{
				if (_vm.IsNameTaken(selectedName))
				{
					var msg = $"Opening the selected ColorBandSet will result in a new ColorBandSet being created with Target Iterations = {_vm.TargetIterations}. " +
						$"A ColorBandSet already exists with this name with the Target Iterations = {_vm.TargetIterations}. Do you want to overwrite?";

					var res = MessageBox.Show(msg, "Overwrite Existing ColorBandSet", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

					if (res == MessageBoxResult.No)
					{
						OverwriteExisting = false;
					}
					else if (res == MessageBoxResult.Yes)
					{
						OverwriteExisting = true;
					}
				}
				else
				{
					OverwriteExisting = null;
				}
			}

			DialogResult = true;
			Close();
		}

		#endregion
	}
}
