using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for AppNavWindow.xaml
	/// </summary>
	public partial class AppNavWindow : Window
	{
		private AppNavViewModel _vm;

		private Window? _lastWindow;

		public AppNavWindow()
		{
			_lastWindow = null;

			_vm = (AppNavViewModel)DataContext;
			Loaded += AppNavWindow_Loaded;
			InitializeComponent();
		}

		private void AppNavWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the AppNav Window is being loaded.");
				return;
			}
			else
			{
				_vm = (AppNavViewModel)DataContext;

				if (!Properties.Settings.Default.ShowTopNav)
				{
					GoToExplorer();
				}
				else
				{
					WindowState = WindowState.Normal;
				}

				Debug.WriteLine("The AppNav Window is now loaded");
			}
		}

		private void ExploreButton_Click(object sender, RoutedEventArgs e)
		{
			GoToExplorer();
		}

		private void DesignPosterButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("De Po");
		}


		private void GoToExplorer()
		{
			var explorerViewModel = _vm.GetExplorerViewModel();

			var explorerWindow = new ExplorerWindow
			{
				DataContext = explorerViewModel
			};

			_lastWindow = explorerWindow;

			explorerWindow.Owner = Application.Current.MainWindow;
			explorerWindow.Closed += ExplorerWindow_Closed;
			explorerWindow.Show();
			_ = explorerWindow.Focus();

			Hide();
		}

		private void ExplorerWindow_Closed(object? sender, System.EventArgs e)
		{
			if (_lastWindow != null)
			{
				_lastWindow.Closed -= ExplorerWindow_Closed;
				_lastWindow = null;
			}

			if (Properties.Settings.Default.ShowTopNav)
			{
				Show();
				WindowState = WindowState.Normal;
			}
			else
			{
				ExitApp();
			}
		}

		private void LeaveButton_Click(object sender, RoutedEventArgs e)
		{
			ExitApp();
		}

		private void ExitApp()
		{
			Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
			Close();
		}
	}
}
