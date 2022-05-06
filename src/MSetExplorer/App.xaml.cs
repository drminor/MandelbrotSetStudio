using MEngineClient;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	public delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	public delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";
		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";

		private ProjectAdapter? _projectAdapter;
		private SharedColorBandSetAdapter? _sharedColorBandSetAdapter;

		private IMapProjectViewModel? _mapProjectViewModel;
		private MapLoaderManager? _mapLoaderManager;
		private ColorBandSetViewModel? _colorBandViewModel;

		private Process? _serverProcess;

		public App()
		{
			_projectAdapter = null;
			_mapProjectViewModel = null;
			_mapLoaderManager = null;
			_serverProcess = null;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			var DO_START_SERVER = true;

			var DROP_ALL_COLLECTIONS = false;
			var DROP_MAP_SECTIONS = false;
			var USE_MAP_SECTION_REPO = true;

			base.OnStartup(e);

			// Project Repository Adapter
			_projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);

			if (DROP_ALL_COLLECTIONS)
			{
				_projectAdapter.DropCollections();
			}
			else if (DROP_MAP_SECTIONS)
			{
				_projectAdapter.DropSubdivisionsAndMapSectionsCollections();
			}

			_projectAdapter.CreateCollections();

			//_projectAdapter.AddColorBandSetIdToAllJobs();
			//_projectAdapter.AddIsPreferredChildToAllJobs();

			//var report = _projectAdapter.FixAllJobRels();
			//Debug.WriteLine(report);

			//var report1 = _projectAdapter.OpenAllJobs();
			//Debug.WriteLine($"Could not open these projects:\n {string.Join("; ", report1)}");

			//Debug.WriteLine("About to call DeleteUnusedColorBandSets.");
			//var report = _projectAdapter.DeleteUnusedColorBandSets();
			//Debug.WriteLine(report);

			//var res = MessageBox.Show("FixAll complete, stop application?", "done", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
			//if (res == MessageBoxResult.Yes)
			//{
			//	Current.Shutdown();
			//	return;
			//}

			if (DO_START_SERVER)
			{
				StartServer();
			}

			_sharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(MONGO_DB_CONN_STRING);
			_sharedColorBandSetAdapter.CreateCollections();

			// Map Project ViewModel
			_mapProjectViewModel = new MapProjectViewModel(_projectAdapter, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			_mapLoaderManager = BuildMapLoaderManager(M_ENGINE_END_POINT_ADDRESS, MONGO_DB_CONN_STRING, USE_MAP_SECTION_REPO, mapSectionHelper);
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			_colorBandViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections);

			// Main Window
			var window1 = new MainWindow
			{
				DataContext = new MainWindowViewModel(_mapProjectViewModel, mapDisplayViewModel, _colorBandViewModel, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel)
			};

			window1.Show();
		}

		private MapLoaderManager BuildMapLoaderManager(string mEngineEndPointAddress, string dbProviderConnectionString, bool useTheMapSectionRepo, MapSectionHelper mapSectionHelper)
		{

			var mEngineClient = new MClient(mEngineEndPointAddress);
			var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(dbProviderConnectionString);
			var mapSectionRequestProcessor = MapSectionRequestProcessorProvider.CreateMapSectionRequestProcessor(mEngineClient, mapSectionAdapter, useTheMapSectionRepo);
			var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

			return result;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (_mapProjectViewModel != null)
			{
				((MapProjectViewModel) _mapProjectViewModel).Dispose();
			}

			if (_mapLoaderManager != null)
			{
				_mapLoaderManager.Dispose();
				_mapLoaderManager = null;
			}

			if (_colorBandViewModel != null)
			{
				_colorBandViewModel.Dispose();
				_colorBandViewModel = null;
			}

			Debug.WriteLine("The request MapSectionRequestProcessor has been closed.");

			StopServer();
		}

		private void StartServer()
		{
			var exists = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(SERVER_EXE_PATH)).Length > 0;
			if (!exists)
			{
				_serverProcess = Process.Start(SERVER_EXE_PATH);
			}
		}

		private void StopServer()
		{
			if (!(_serverProcess is null))
			{
				_serverProcess.Kill();
			}
		}

		private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return _projectAdapter == null
                ? throw new InvalidOperationException("Cannot create a Project OpenSave ViewModel, the ProjectAdapter is null.")
				: new ProjectOpenSaveViewModel(_projectAdapter, initalName, dialogType);
		}

		private IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return _sharedColorBandSetAdapter == null
				? throw new InvalidOperationException("Cannot create a ColorBandSet OpenSave ViewModel, the Shared ColorBandSet Adapter is null.")
				: new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}


	}
}
