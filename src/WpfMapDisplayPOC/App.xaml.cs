using MSetRepo;
using MSS.Common;
using MSS.Types;
using System.Windows;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region Configuration

		private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		private const int MONGO_DB_PORT = 27017;

		//private static readonly bool USE_ALL_CORES = true;

		//private static readonly MSetGenerationStrategy GEN_STRATEGY = MSetGenerationStrategy.DepthFirst;

		#endregion

		#region Private Properties

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;
		private readonly MapSectionHelper _mapSectionHelper;

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
			_mapSectionHelper = new MapSectionHelper(_mapSectionVectorsPool, _mapSectionZVectorsPool);

			//if (START_LOCAL_ENGINE)
			//{
			//	_mEngineServerManager = new MEngineServerManager(SERVER_EXE_PATH, LOCAL_M_ENGINE_ADDRESS);
			//}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT);

			_mainWindow = GetMainWindow(_mapSectionHelper, _repositoryAdapters);
			_mainWindow.Show();
		}

		#endregion

		#region Support Methods

		private MainWindow GetMainWindow(MapSectionHelper mapSectionHelper, RepositoryAdapters repositoryAdapters)
		{
			var vm = new MainWindowViewModel(repositoryAdapters.ProjectAdapter, repositoryAdapters.MapSectionAdapter, mapSectionHelper);

			var win = new MainWindow
			{
				DataContext = vm
			};

			//win.WindowState = WindowState.Minimized;

			return win;
		}

		#endregion
	}
}
