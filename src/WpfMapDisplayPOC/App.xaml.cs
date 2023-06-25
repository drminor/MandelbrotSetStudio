using MapSectionProviderLib;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using System.Collections.Generic;
using System;
using System.Windows;
using MEngineClient;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region Configuration

		private const string MONGO_DB_NAME = "MandelbrotProjects";
		private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		private const int MONGO_DB_PORT = 27017;

		private static readonly bool USE_ALL_CORES = true;
		private static readonly MSetGenerationStrategy GEN_STRATEGY = MSetGenerationStrategy.DepthFirst;

		#endregion

		#region Private Properties

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private RepositoryAdapters? _repositoryAdapters;
		//private IMapLoaderManager? _mapLoaderManager;

		private MainWindow? _mainWindow;

		#endregion

		#region Constructor, Startup and Exit

		public App()
		{
			//Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

			_mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionZVectorsPool = new MapSectionZVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionBuilder = new MapSectionBuilder(_mapSectionVectorsPool, _mapSectionZVectorsPool);

			//if (START_LOCAL_ENGINE)
			//{
			//	_mEngineServerManager = new MEngineServerManager(SERVER_EXE_PATH, LOCAL_M_ENGINE_ADDRESS);
			//}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, MONGO_DB_NAME);

			var mEngineClients = CreateTheMEngineClients(GEN_STRATEGY, USE_ALL_CORES);
			var mapSectionRequestProcessor = CreateMapSectionRequestProcessor(mEngineClients, _repositoryAdapters.MapSectionAdapter, _mapSectionBuilder);


			_mainWindow = GetMainWindow(mapSectionRequestProcessor, _mapSectionBuilder, _repositoryAdapters);
			_mainWindow.Show();
		}

		#endregion

		#region Support Methods

		private MainWindow GetMainWindow(MapSectionRequestProcessor mapSectionRequestProcessor, MapSectionBuilder mapSectionHelper, RepositoryAdapters repositoryAdapters)
		{
			var vm = new MainWindowViewModel(mapSectionRequestProcessor, repositoryAdapters.ProjectAdapter, repositoryAdapters.MapSectionAdapter, mapSectionHelper);

			var win = new MainWindow
			{
				DataContext = vm
			};

			//win.WindowState = WindowState.Minimized;

			return win;
		}

		private IMEngineClient[] CreateTheMEngineClients(MSetGenerationStrategy mSetGenerationStrategy, bool useAllCores)
		{
			var result = new List<IMEngineClient>();

			var localTaskCount = GetLocalTaskCount(useAllCores);

			for (var i = 0; i < localTaskCount; i++)
			{
				result.Add(new MClientLocal(mSetGenerationStrategy));
			}

			return result.ToArray();
		}

		private int GetLocalTaskCount(bool useAllCores)
		{
			int result;

			if (useAllCores)
			{
				var numberOfLogicalProc = Environment.ProcessorCount;
				result = numberOfLogicalProc - 1;
			}
			else
			{
				result = 1;
			}

			return result;
		}

		private MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionAdapter, MapSectionBuilder mapSectionHelper)
		{
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients);
			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionAdapter, mapSectionHelper);
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionAdapter, mapSectionHelper, mapSectionGeneratorProcessor, mapSectionResponseProcessor, mapSectionPersistProcessor);

			return mapSectionRequestProcessor;
		}


		#endregion
	}
}
