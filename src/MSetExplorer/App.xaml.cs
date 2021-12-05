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
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";
		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var window = new MainWindow();
			var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);

			IMEngineClient mClient = new MClient(M_ENGINE_END_POINT_ADDRESS);
			IMapSectionRepo mapSectionRepo = MSetRepoHelper.GetMapSectionRepo(MONGO_DB_CONN_STRING);
			var mapSectionProvider = new MapSectionProvider(mClient, mapSectionRepo);

			var viewModel = new MainWindowViewModel(RMapConstants.BLOCK_SIZE, projectAdapter, mapSectionProvider);
			window.DataContext = viewModel;

			window.Show();
		}
	}
}
