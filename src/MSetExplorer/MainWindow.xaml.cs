using MSS.Types;
using System;
using System.Diagnostics;
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
			var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);
			_vm.JobStack.LoadNewProject("Home", mSetInfo);
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

				txtIterations.TextChanged += TxtIterations_TextChanged;

				Debug.WriteLine("The MainWindow is now loaded");
			}
		}

		#region EVENT Handlers

		private void TxtIterations_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			try
			{
				_vm.Iterations = int.Parse(txtIterations.Text);
			}
			catch
			{

			}
		}

		private void VmPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IJobStack.CanGoBack))
			{
				btnGoBack.IsEnabled = _vm.JobStack.CanGoBack;
				return;
			}

			if (e.PropertyName == nameof(IJobStack.CanGoForward))
			{
				btnGoForward.IsEnabled = _vm.JobStack.CanGoForward;
				return;
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
			Pan(new VectorInt(0, -1 * SHIFT_AMOUNT));
		}

		private void GoRightButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(SHIFT_AMOUNT, 0));
		}

		private void GoDownButton_Click(object sender, RoutedEventArgs e)
		{
			Pan(new VectorInt(0, SHIFT_AMOUNT));
		}

		private void Pan(VectorInt amount)
		{
			_vm.MapDisplayViewModel.UpdateMapViewPan(new ImageDraggedEventArgs(TransformType.Pan, amount.Invert()));
		}

		private void GoBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.JobStack.CanGoBack)
			{
				_vm.JobStack.GoBack();
			}
		}

		private void GoForwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm.JobStack.CanGoForward)
			{
				_vm.JobStack.GoForward();
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.JobStack.SaveProject();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion
	}
}
