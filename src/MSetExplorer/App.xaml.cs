using ImageBuilder;
using MEngineClient;
using MSS.Common;
using MSS.Common.MSetRepo;
using System;
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

		// TODO: Use the configuration properties to store the OutputImageFolderPath
		private static readonly string OutputImageFolderPath = @"C:\_MandelbrotSetImages";

		private readonly MEngineServerManager _mEngineServerManager;
		private RepositoryAdapters? _repositoryAdapters;
		private IMapLoaderManager? _mapLoaderManager;
		private PngBuilder? _pngBuilder;

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

			_repositoryAdapters = new RepositoryAdapters(MONGO_DB_CONN_STRING, USE_MAP_SECTION_REPO);

			if (DROP_ALL_COLLECTIONS)
			{
				_repositoryAdapters.ProjectAdapter.DropCollections();
			}
			else if (DROP_MAP_SECTIONS)
			{
				_repositoryAdapters.ProjectAdapter.DropSubdivisionsAndMapSectionsCollections();
			}

			_mapLoaderManager = BuildMapLoaderManager(M_ENGINE_END_POINT_ADDRESSES, _repositoryAdapters.MapSectionAdapter, USE_MAP_SECTION_REPO);

			_pngBuilder = BuildPngBuilder(OutputImageFolderPath, M_ENGINE_END_POINT_ADDRESSES, _repositoryAdapters.MapSectionAdapter, _mapLoaderManager);

			_appNavWindow = GetAppNavWindow(_repositoryAdapters, _mapLoaderManager, _pngBuilder);
			_appNavWindow.Show();
		}

		private AppNavWindow GetAppNavWindow(RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, PngBuilder pngBuilder)
		{
			var appNavViewModel = new AppNavViewModel(repositoryAdapters, mapLoaderManager, pngBuilder);

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

		private PngBuilder BuildPngBuilder(string outputFolderPath, string[] mEngineEndPointAddress, IMapSectionAdapter? mapSectionAdapter, IMapLoaderManager mapLoaderManager)
		{
			if (mapSectionAdapter == null)
			{
				throw new InvalidOperationException("BuildPngBuilder requires a non-null MapSectionAdapter.");
			}


			var mEngineClient = new MClient(mEngineEndPointAddress[0]);
			var result = new PngBuilder(outputFolderPath, mEngineClient, mapSectionAdapter, mapLoaderManager);

			return result;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			_mEngineServerManager.Stop();

			// TODO: Dispose the MapLoaderManager, MapSectionRequestProcessor, etc.
		}

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
