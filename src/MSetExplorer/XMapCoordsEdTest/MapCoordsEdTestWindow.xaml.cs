using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapCoordsEditorWindow.xaml
	/// </summary>
	public partial class MapCoordsEdTestWindow : Window
	{
		private MapCoordsEdTestViewModel? _vm;

		#region Constructor

		public MapCoordsEdTestWindow()
		{
			Loaded += MapCoordsEditorWindow_Loaded;
			InitializeComponent();
		}

		private void MapCoordsEditorWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapCoordsEditor Window is being loaded.");
				return;
			}
			else
			{
				_vm = (MapCoordsEdTestViewModel)DataContext;

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				Debug.WriteLine("The MapCoordsEditor Window is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MapCoordsEdTestViewModel.StringVal))
			{

			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion

	}
}
