﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for PosterOpenSaveWindow.xaml
	/// </summary>
	public partial class PosterOpenSaveWindow : Window
	{ 
		private IPosterOpenSaveViewModel _vm;

		#region Constructor 

		public PosterOpenSaveWindow()
		{
			_vm = (IPosterOpenSaveViewModel)DataContext;

			Loaded += ProjectOpenSaveWindow_Loaded;
			InitializeComponent();
		}

		private void ProjectOpenSaveWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the PosterOpenSave Window is being loaded.");
				return;
			}
			else
			{
				_vm = (IPosterOpenSaveViewModel)DataContext;
				borderTop.DataContext = DataContext;

				btnSave.Content = _vm.DialogType == DialogType.Open ? "Open" : "Save";
				Title = _vm.DialogType == DialogType.Open ? "Open Poster" : "Save Poster";

				lvPosters.ItemsSource = _vm.Posters;
				lvPosters.SelectionChanged += LvPosters_SelectionChanged;

				lvPosters.MouseDoubleClick += LvPosters_MouseDoubleClick;

				txtName.Text = _vm.SelectedName;
				txtName.LostFocus += TxtName_LostFocus;

				_ = txtName.Focus();
				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The PosterOpenSave Window is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void LvPosters_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			TakeSelection();
		}

		private void TxtName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtName.Text))
			{
				_vm.SelectedName = _vm.SelectedPoster?.Name;
				_vm.UserIsSettingTheName = false;
			}
			else
			{
				if (txtName.Text == _vm.SelectedPoster?.Name)
				{
					_vm.UserIsSettingTheName = false;
					_vm.SelectedName = _vm.SelectedPoster?.Name;
				}
				else
				{
					_vm.UserIsSettingTheName = true;
				}
			}

			btnSave.IsEnabled = _vm.SelectedName != null;
		}

		private void LvPosters_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnSave.IsEnabled = _vm.SelectedName != null;
		}

		#endregion

		#region Public Properties

		public string? PosterName => _vm.SelectedName;
		public string? PosterDescription => _vm.SelectedDescription;

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			TakeSelection();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.DeleteSelected();
		}

		private void TakeSelection()
		{
			if (_vm.DialogType == DialogType.Save)
			{
				if (_vm.IsNameTaken(PosterName))
				{
					var res = MessageBox.Show("A Poster already exists with this name. Do you want to overwrite?", "Overwrite Existing Poster", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

					if (res == MessageBoxResult.No)
					{
						return;
					}
				}
			}

			DialogResult = true;
			Close();
		}

		#endregion

	}
}
