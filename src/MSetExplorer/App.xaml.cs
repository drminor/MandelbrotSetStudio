using MEngineClient;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	public delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string initialName, DialogType dialogType);

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";
		private const string M_ENGINE_END_POINT_ADDRESS = "https://localhost:5001";

		private ProjectAdapter _projectAdapter;

		private MapProjectViewModel _jobStack;
		private MapLoaderManager _mapLoaderManager;

		private Process _serverProcess;

		protected override void OnStartup(StartupEventArgs e)
		{
			var DROP_MAP_SECTIONS = false;

			//var USE_MAP_NAV_SIM = false;
			var USE_MAP_SECTION_REPO = true;

			base.OnStartup(e);

			StartServer();

			_projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING, CreateProjectInfo);

			if (DROP_MAP_SECTIONS)
			{
				_projectAdapter.DropSubdivisionsAndMapSectionsCollections();
			}

			_projectAdapter.CreateCollections();

			var mapSectionRequestProcessor = MapSectionRequestProcessorProvider.CreateMapSectionRequestProcessor(M_ENGINE_END_POINT_ADDRESS, MONGO_DB_CONN_STRING, USE_MAP_SECTION_REPO);

			_jobStack = new MapProjectViewModel(_projectAdapter, RMapConstants.BLOCK_SIZE);
			_mapLoaderManager = new MapLoaderManager(mapSectionRequestProcessor);

			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, RMapConstants.BLOCK_SIZE);

			IColorBandViewModel colorBandViewModel = new ColorBandViewModel(_projectAdapter);

			//var window1 = USE_MAP_NAV_SIM
			//	? new MapNavSim
			//	{
			//		DataContext = new MapNavSimViewModel(RMapConstants.BLOCK_SIZE, projectAdapter, _mapSectionRequestProcessor)
			//	}
			//	: (Window)new MainWindow
			//	{
			//		DataContext = new MainWindowViewModel(projectAdapter, mapDisplayViewModel, mapLoaderJobStack)
			//  };

			var window1 = new MainWindow
			{
				DataContext = new MainWindowViewModel(_jobStack, mapDisplayViewModel, CreateAProjectOpenSaveViewModel, colorBandViewModel)
			};

			window1.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (_jobStack != null)
			{
				_jobStack.Dispose();
			}

			if (_mapLoaderManager != null)
			{
				_mapLoaderManager.Dispose();
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

		private IProjectInfo CreateProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int minMapCoordsExponent, int minSamplePointDeltaExponent)
		{
			return new ProjectInfo(project, lastSaved, numberOfJobs, minMapCoordsExponent, minSamplePointDeltaExponent);
		}

		private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, initalName, dialogType);
		}


	}
}
