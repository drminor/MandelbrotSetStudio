using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";
		//private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		////private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5004", "https://localhost:5001" };
		////private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "http://192.168.2.104:5000", "https://localhost:5001" };
		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5001" };

		private readonly MEngineServerManager _mEngineServerManager;

		private AppNavWindow? _appNavWindow;

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			_mEngineServerManager = new MEngineServerManager();
		}

		void App_Startup(object sender, StartupEventArgs e)
		{
			Debug.WriteLine("H");
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_mEngineServerManager.Start();

			_appNavWindow = new AppNavWindow
			{
				DataContext = new AppNavViewModel()
			};

			_appNavWindow.WindowState = WindowState.Minimized;

			_appNavWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			_mEngineServerManager.Stop();
		}

		//private void OldStartup()
		//{
		//	var DO_START_SERVER = true;

		//	var DROP_ALL_COLLECTIONS = false;
		//	var DROP_MAP_SECTIONS = false;
		//	var USE_MAP_SECTION_REPO = true;

		//	//base.OnStartup(e);

		//	// Project Repository Adapter
		//	_projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);

		//	if (DROP_ALL_COLLECTIONS)
		//	{
		//		_projectAdapter.DropCollections();
		//	}
		//	else if (DROP_MAP_SECTIONS)
		//	{
		//		_projectAdapter.DropSubdivisionsAndMapSectionsCollections();
		//	}

		//	_projectAdapter.CreateCollections();

		//	DoSchemaUpdates();

		//	if (DO_START_SERVER)
		//	{
		//		StartServer(M_ENGINE_END_POINT_ADDRESSES);
		//	}

		//	_sharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(MONGO_DB_CONN_STRING);
		//	_sharedColorBandSetAdapter.CreateCollections();

		//	// Map Project ViewModel
		//	_mapProjectViewModel = new MapProjectViewModel(_projectAdapter, RMapConstants.BLOCK_SIZE);

		//	// Map Display View Model
		//	var mapSectionHelper = new MapSectionHelper();
		//	_mapLoaderManager = BuildMapLoaderManager(M_ENGINE_END_POINT_ADDRESSES, MONGO_DB_CONN_STRING, USE_MAP_SECTION_REPO, mapSectionHelper);
		//	IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

		//	// ColorBand ViewModel
		//	_colorBandViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections);

		//	// Main Window
		//	var exlorerWindow = new ExplorerWindow
		//	{
		//		DataContext = new ExplorerViewModel(_mapProjectViewModel, mapDisplayViewModel, _colorBandViewModel, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel)
		//	};

		//	exlorerWindow.Show();
		//}

		//private MapLoaderManager BuildMapLoaderManager(string[] mEngineEndPointAddress, string dbProviderConnectionString, bool useTheMapSectionRepo, MapSectionHelper mapSectionHelper)
		//{
		//	var mEngineClients = mEngineEndPointAddress.Select(x => new MClient(x)).ToArray();

		//	//var mEngineClient = new MClient(mEngineEndPointAddress);
		//	var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(dbProviderConnectionString);

		//	var mapSectionRequestProcessor = MapSectionRequestProcessorProvider.CreateMapSectionRequestProcessor(mEngineClients, mapSectionAdapter, useTheMapSectionRepo);

		//	var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

		//	return result;
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

		//protected override void OnExit(ExitEventArgs e)
		//{
		//	base.OnExit(e);

		//	if (_mapProjectViewModel != null)
		//	{
		//		((MapProjectViewModel) _mapProjectViewModel).Dispose();
		//	}

		//	if (_mapLoaderManager != null)
		//	{
		//		_mapLoaderManager.Dispose();
		//		_mapLoaderManager = null;
		//	}

		//	if (_colorBandViewModel != null)
		//	{
		//		_colorBandViewModel.Dispose();
		//		_colorBandViewModel = null;
		//	}

		//	Debug.WriteLine("The request MapSectionRequestProcessor has been closed.");

		//	StopServer();
		//}

		//private void StartServer(string[] urls)
		//{
		//	var exists = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(SERVER_EXE_PATH)).Length > 0;
		//	if (!exists)
		//	{
		//		foreach(var ep in urls)
		//		{
		//			if (ep.ToLower().Contains("localhost"))
		//			{
		//				var proc = Process.Start(SERVER_EXE_PATH, " --urls " + ep);
		//				_serverProcesses.Add(proc);
		//			}
		//		}
		//	}
		//}

		//private void StopServer()
		//{
		//	foreach(var proc in _serverProcesses)
		//	{
		//		proc.Kill();
		//	}
		//}

		//private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		//{
		//	return _projectAdapter == null
		//              ? throw new InvalidOperationException("Cannot create a Project OpenSave ViewModel, the ProjectAdapter is null.")
		//		: new ProjectOpenSaveViewModel(_projectAdapter, initalName, dialogType);
		//}

		//private IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		//{
		//	return _sharedColorBandSetAdapter == null
		//		? throw new InvalidOperationException("Cannot create a ColorBandSet OpenSave ViewModel, the Shared ColorBandSet Adapter is null.")
		//		: new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		//}

	}
}
