using MapSectionProviderLib;
using MEngineClient;
using MSS.Common;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		private const int MONGO_DB_PORT = 27017;

		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";

		private const string LOCAL_M_ENGINE_ADDRESS = "https://localhost:5001";
		private static readonly string[] REMOTE_M_ENGINE_ADDRESSES = new string[] { "http://192.168.2.109:5000" };

		private static readonly bool CREATE_COLLECTIONS = false;

		private static readonly bool START_LOCAL_ENGINE = true; // If true, we will start the local server's executable. If false, then use Multiple Startup Projects when debugging.
		private static readonly bool USE_LOCAL_ENGINE = true; // If true, we will host a server -- AND include it in the list of servers to use by our client.
		private static readonly bool USE_REMOTE_ENGINE = false;  // If true, send part of our work to the remote server(s)

		private const bool FETCH_ZVALUES_LOCALLY = false;

		private readonly MEngineServerManager? _mEngineServerManager;
		private RepositoryAdapters? _repositoryAdapters;
		private IMapLoaderManager? _mapLoaderManager;

		private AppNavWindow? _appNavWindow;

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

			if (START_LOCAL_ENGINE)
			{
				_mEngineServerManager = new MEngineServerManager(SERVER_EXE_PATH, LOCAL_M_ENGINE_ADDRESS);
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_mEngineServerManager?.Start();

			_repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, CREATE_COLLECTIONS);
			//PrepareRepositories(DROP_ALL_COLLECTIONS, DROP_MAP_SECTIONS, DROP_RECENT_MAP_SECTIONS, _repositoryAdapters);

			var mEngineAddresses = USE_REMOTE_ENGINE ? REMOTE_M_ENGINE_ADDRESSES.ToList() : new List<string>();

			if (USE_LOCAL_ENGINE)
			{
				mEngineAddresses.Add(LOCAL_M_ENGINE_ADDRESS);
			}

			_mapLoaderManager = BuildMapLoaderManager(mEngineAddresses, _repositoryAdapters.MapSectionAdapter, FETCH_ZVALUES_LOCALLY);

			_appNavWindow = GetAppNavWindow(_repositoryAdapters, _mapLoaderManager);
			_appNavWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (_mapLoaderManager != null)
			{
				// This disposes the MapSectionRequestProcessor, MapSectionGeneratorProcessor and MapSectionResponseProcessor.
				_mapLoaderManager.Dispose();
			}

			_mEngineServerManager?.Stop();
		}

		private AppNavWindow GetAppNavWindow(RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager)
		{
			var appNavViewModel = new AppNavViewModel(repositoryAdapters, mapLoaderManager);

			var appNavWindow = new AppNavWindow
			{
				DataContext = appNavViewModel
			};

			appNavWindow.WindowState = WindowState.Minimized;

			return appNavWindow;
		}

		private IMapLoaderManager BuildMapLoaderManager(IList<string> mEngineEndPointAddress, IMapSectionAdapter mapSectionAdapter, bool fetchZValues)
		{
			var mEngineClients = mEngineEndPointAddress.Select(x => new MClient(x)).ToArray();
			var mapSectionRequestProcessor = CreateMapSectionRequestProcessor(mEngineClients, mapSectionAdapter, fetchZValues);

			var mapSectionHelper = new MapSectionHelper();
			var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

			return result;
		}

		private MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionAdapter, bool fetchZValues)
		{
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients);
			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionAdapter, mapSectionGeneratorProcessor, mapSectionResponseProcessor, fetchZValues);

			return mapSectionRequestProcessor;
		}

		//private const bool DROP_RECENT_MAP_SECTIONS = false;
		//private const bool DROP_ALL_COLLECTIONS = false;
		//private const bool DROP_MAP_SECTIONS = false;

		//private void PrepareRepositories(bool dropAllCollections, bool dropMapSections, bool dropRecentMapSections, RepositoryAdapters repositoryAdapters)
		//{
		//	if (dropAllCollections)
		//	{
		//		repositoryAdapters.ProjectAdapter.DropCollections();
		//	}
		//	else if (dropMapSections)
		//	{
		//		repositoryAdapters.ProjectAdapter.DropSubdivisionsAndMapSectionsCollections();
		//	}

		//	if (dropRecentMapSections)
		//	{
		//		var lastSaved = DateTime.Parse("2022-05-29");
		//		repositoryAdapters.MapSectionAdapter.DeleteMapSectionsCreatedSince(lastSaved, overrideRecentGuard: true);
		//	}
		//}

		//private void DoSchemaUpdates()
		//{
		//	//_projectAdapter.AddColorBandSetIdToAllJobs();
		//	//_projectAdapter.AddIsPreferredChildToAllJobs();

		//	//var report = _projectAdapter.FixAllJobRels();
		//	//Debug.WriteLine(report);

		//	//var report1 = _projectAdapter.OpenAllJobs();
		//	//Debug.WriteLine($"Could not open these projects:\n {string.Join("; ", report1)}");

		//	//Debug.WriteLine("About to call DeleteUnusedColorBandSets.");
		//	//var report = _projectAdapter.DeleteUnusedColorBandSets();
		//	//Debug.WriteLine(report);

		//	//var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_CONN_STRING);
		//	//mapSectionAdapter.AddCreatedDateToAllMapSections();

		//	//var res = MessageBox.Show("FixAll complete, stop application?", "done", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
		//	//if (res == MessageBoxResult.Yes)
		//	//{
		//	//	Current.Shutdown();
		//	//	return;
		//	//}
		//}

	}
}
