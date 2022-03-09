using MSS.Types;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IMainWindowViewModel _vm;
		private MapDisplay _mapDisplay;

		public MainWindow()
		{
			Loaded += MainWindow_Loaded;
			ContentRendered += MainWindow_ContentRendered;
			InitializeComponent();
		}

		private void MainWindow_ContentRendered(object sender, EventArgs e)
		{
			Debug.WriteLine("The MainWindow is handling ContentRendered");

			var maxIterations = 700;
			var mSetInfo = MapJobHelper.BuildInitialMSetInfo(maxIterations);
			_vm.MapProject.LoadNewProject("Home", mSetInfo);
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
				_vm.PropertyChanged += VmPropertyChanged;

				_mapDisplay = mapDisplay1;
				_mapDisplay.DataContext = _vm.MapDisplayViewModel;

				txtIterations.LostFocus += TxtIterations_LostFocus;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		#region EVENT Handlers

		private void TxtIterations_LostFocus(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(txtIterations.Text, out var newValue))
			{
				_vm.TargetIterations = newValue;
			}

			// TODO: Respond to changes in the CME View and update the MainWindowViewModel
		}

		private void VmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapProjectViewModel.CanGoBack))
			{
				btnGoBack.IsEnabled = _vm.MapProject.CanGoBack;
				return;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CanGoForward))
			{
				btnGoForward.IsEnabled = _vm.MapProject.CanGoForward;
				return;
			}

			if (e.PropertyName == nameof(IMainWindowViewModel.TargetIterations))
			{
				txtIterations.Text = _vm.TargetIterations.ToString(CultureInfo.InvariantCulture);
			}

			if (e.PropertyName == nameof(IMainWindowViewModel.ColorMapEntries))
			{
				//TODO: Update the CME View.
			}
		}

		#endregion

		#region Button Handlers

		private const int SHIFT_AMOUNT = 16;

		private void GoLeftButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(-1 * SHIFT_AMOUNT, 0));
		}

		private void GoUpButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(0, SHIFT_AMOUNT));
		}

		private void GoRightButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(SHIFT_AMOUNT, 0));
		}

		private void GoDownButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(0, -1 * SHIFT_AMOUNT));
		}

		private void Pan(VectorInt amount)
		{
			var newArea = new RectangleInt(new PointInt(amount), _vm.MapProject.CanvasSize);
			_vm.MapProject.UpdateMapView(TransformType.Pan, newArea);
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.MapProject.CanGoBack)
			{
				_vm.MapProject.GoBack();
			}
		}

		private void GoForwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.MapProject.CanGoForward)
			{
				_vm.MapProject.GoForward();
			}
		}

		private void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Will prompt for Project Name here.");
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.MapProject.SaveProject();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion
	}
}
