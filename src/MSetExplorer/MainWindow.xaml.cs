using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowViewModel _vm;
		private MapDisplay _mapDisplay;

		public MainWindow()
		{
			Loaded += MainWindow_Loaded;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext != null)
			{
				_vm = (MainWindowViewModel)DataContext;
				_mapDisplay = mapDisplay1;

				_vm.PropertyChanged += _vm_PropertyChanged;

				btnGoBack.IsEnabled = _vm.CanGoBack;

				//btnGoBack.IsEnabled = _vm.CanGoBack;
				//var canvasSize = GetCanvasControlSize(MainCanvas);
				//var maxIterations = 700;
				//var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);
				//_vm.LoadMap("initial job", canvasSize, mSetInfo, canvasSize, clearExistingMapSections: false);
			}
		}

		private void _vm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CanGoBack")
			{
				btnGoBack.IsEnabled = _vm.CanGoBack;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			//HideScreenSections();
			//var canvasSize = GetCanvasControlSize(MainCanvas);
			//_vm.GoBack(canvasSize, clearExistingMapSections: false);

			_mapDisplay.GoBack();
			btnGoBack.IsEnabled = _vm.CanGoBack;
		}

	}
}
