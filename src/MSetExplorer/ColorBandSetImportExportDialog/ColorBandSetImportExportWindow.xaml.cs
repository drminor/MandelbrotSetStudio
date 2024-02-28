using MongoDB.Bson;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandSetImportExportWindow.xaml
	/// </summary>
	public partial class ColorBandSetImportExportWindow : Window
	{
		private IColorBandSetImportExportViewModel _vm;

		#region Constructor

		public ColorBandSetImportExportWindow()
		{
			_vm = (IColorBandSetImportExportViewModel)DataContext;
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
				_vm = (IColorBandSetImportExportViewModel)DataContext;
				borderTop.DataContext = DataContext;

				btnSave.Content = _vm.DialogType == DialogType.Open ? "Import" : "Export";
				Title = _vm.DialogType == DialogType.Open ? "Import ColorBandSet" : "Export ColorBandSet";

				lvColorBandSets.ItemsSource = _vm.ColorBandSetInfos;
				lvColorBandSets.SelectionChanged += LvColorBandSets_SelectionChanged;

				lvColorBandSets.MouseDoubleClick += LvColorBandSets_MouseDoubleClick;

				txtName.LostFocus += TxtName_LostFocus;

				_ = txtName.Focus();
				btnSave.IsEnabled = _vm.SelectedName != null;

				Debug.WriteLine("The ColorBandSetImportExportWindow is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void LvColorBandSets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TakeSelection();
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
			btnSave.IsEnabled = _vm.SelectedName != null;
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
			TakeSelection();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TakeSelection()
		{
			if (_vm.DialogType == DialogType.Save)
			{
				if (ColorBandSetName != null && _vm.IsNameTaken(ColorBandSetName))
				{
					var res = MessageBox.Show("A ColorBandSet already exists with this name. Do you want to overwrite?", "Overwrite Existing Project", MessageBoxButton.YesNo, MessageBoxImage.Hand, MessageBoxResult.No, MessageBoxOptions.None);

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
