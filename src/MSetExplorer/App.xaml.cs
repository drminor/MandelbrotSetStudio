using MEngineClient;
using MSS.Common;
using System.Linq;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";

		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5004", "https://localhost:5001" };
		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "http://192.168.2.104:5000", "https://localhost:5001" };
		private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5001" };

		private readonly MEngineServerManager _mEngineServerManager;
		private RepositoryAdapters? _repositoryAdapters;
		private IMapLoaderManager? _mapLoaderManager;

		private AppNavWindow? _appNavWindow;

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			_mEngineServerManager = new MEngineServerManager(SERVER_EXE_PATH, M_ENGINE_END_POINT_ADDRESSES);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_mEngineServerManager.Start();

			var DROP_ALL_COLLECTIONS = false;
			var DROP_MAP_SECTIONS = false;
			var USE_MAP_SECTION_REPO = true;

			_repositoryAdapters = new RepositoryAdapters(MONGO_DB_CONN_STRING, DROP_ALL_COLLECTIONS, DROP_MAP_SECTIONS, USE_MAP_SECTION_REPO);

			_mapLoaderManager = BuildMapLoaderManager(M_ENGINE_END_POINT_ADDRESSES, _repositoryAdapters.MapSectionAdapter, USE_MAP_SECTION_REPO);

			_appNavWindow = GetAppNavWindow(_repositoryAdapters, _mapLoaderManager);
			_appNavWindow.Show();
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

		private IMapLoaderManager BuildMapLoaderManager(string[] mEngineEndPointAddress, IMapSectionAdapter? mapSectionAdapter, bool useTheMapSectionRepo)
		{
			var mEngineClients = mEngineEndPointAddress.Select(x => new MClient(x)).ToArray();
			var mapSectionRequestProcessor = MapSectionRequestProcessorProvider.CreateMapSectionRequestProcessor(mEngineClients, mapSectionAdapter, useTheMapSectionRepo);

			var mapSectionHelper = new MapSectionHelper();
			var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

			return result;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			_mEngineServerManager.Stop();
		}
	}
}
