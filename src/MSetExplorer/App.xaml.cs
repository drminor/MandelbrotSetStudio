using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			MainWindow window = new MainWindow();
			var viewModel = new MainWindowViewModel(M_ENGINE_END_POINT_ADDRESS);
			window.DataContext = viewModel;

			window.Show();
		}
	}
}
