using MapSectionProviderLib;
using MEngineClient;
using MSetRepo;
using MSS.Common;
using System.Diagnostics;
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

		MapSectionPersistProcessor _mapSectionPersistProcessor;
		MapSectionRequestProcessor _mapSectionRequestProcessor;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var window = new MainWindow();
			var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);
			projectAdapter.CreateCollections();

			var mEngineClient = new MClient(M_ENGINE_END_POINT_ADDRESS);
			var mapSectionRepo = MSetRepoHelper.GetMapSectionRepo(MONGO_DB_CONN_STRING);

			//_mapSectionPersistProcessor = null;
			_mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionRepo);

			_mapSectionRequestProcessor = new MapSectionRequestProcessor(mEngineClient, mapSectionRepo, _mapSectionPersistProcessor);

			var viewModel = new MainWindowViewModel(RMapConstants.BLOCK_SIZE, projectAdapter, _mapSectionRequestProcessor);
			window.DataContext = viewModel;

			window.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (_mapSectionRequestProcessor != null)
			{
				_mapSectionRequestProcessor.Dispose();
			}

			if (_mapSectionPersistProcessor != null)
			{
				_mapSectionPersistProcessor.Dispose();
			}

			Debug.WriteLine("The request and persist processors have been closed.");

		}

	}
}
