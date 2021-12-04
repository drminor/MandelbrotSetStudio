using MSS.Common;
using MSetRepo;
using MEngineClient;
using System.Windows;
using MapSectionProviderLib;

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

			var window = new MainWindow();

			IMEngineClient mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);
			IMapSectionRepo mapSectionRepo = new MapSectionAdapter();

			var mapSectionProvider = new MapSectionProvider(mClient, mapSectionRepo);


			var viewModel = new MainWindowViewModel(mapSectionProvider);
			window.DataContext = viewModel;

			window.Show();
		}
	}
}
