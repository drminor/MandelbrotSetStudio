using MapSectionProviderLib;
using MEngineClient;
using MSetRepo;
using MSS.Common;
using System.Windows;

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

			var mEngineClient = new MClient(M_ENGINE_END_POINT_ADDRESS);
			var mapSectionRepo = MSetRepoHelper.GetMapSectionRepo(MONGO_DB_CONN_STRING);

			MapSectionPersistProcessor mapSectionPersistProcessor = null;
			//var mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionRepo);

			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mEngineClient, mapSectionRepo, mapSectionPersistProcessor);

			var viewModel = new MainWindowViewModel(RMapConstants.BLOCK_SIZE, projectAdapter, mapSectionRequestProcessor);
			window.DataContext = viewModel;

			window.Show();
		}
	}
}
