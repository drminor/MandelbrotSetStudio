using MapSectionProviderLib;
using MEngineClient;
using MSetGenP;
using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net6.0\MEngineService.exe";

		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\x64\Debug\MEngineService.exe";

		private const string LOCAL_M_ENGINE_ADDRESS = "https://localhost:5001";
		private static readonly string[] REMOTE_M_ENGINE_ADDRESSES = new string[] { "http://192.168.2.109:5000" };

		private static readonly bool CREATE_COLLECTIONS = false;
		private static readonly bool CLEAN_UP_JOB_MAP_SECTIONS = false;

		//private static readonly MEngineClientImplementation CLIENT_IMPLEMENTATION = MEngineClientImplementation.InProcess;
		//private static readonly MEngineClientImplementation CLIENT_IMPLEMENTATION = MEngineClientImplementation.LocalScalar;

		//private static readonly MEngineClientImplementation CLIENT_IMPLEMENTATION = MEngineClientImplementation.LocalVector;
		private static readonly MEngineClientImplementation CLIENT_IMPLEMENTATION = MEngineClientImplementation.LocalVectorMark2;

		private static readonly bool USE_DEPTH_FIRST_ITERATOR = false;


		private static readonly bool START_LOCAL_ENGINE = false; // If true, we will start the local server's executable. If false, then use Multiple Startup Projects when debugging.
		private static readonly bool USE_LOCAL_ENGINE = false; // If true, we will host a server -- AND include it in the list of servers to use by our client.
		private static readonly bool USE_REMOTE_ENGINE = false;  // If true, send part of our work to the remote server(s)
		private static readonly bool USE_ALL_CORES = true; // If true, a MEngine thread for each processor will be created, otherwise all requests will be serviced on a single thread.

		private const bool FETCH_ZVALUES_LOCALLY = false;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionValuesPool _mapSectionValuesPool;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly MEngineServerManager? _mEngineServerManager;
		private RepositoryAdapters? _repositoryAdapters;
		private IMapLoaderManager? _mapLoaderManager;
		//private MapSectionPersistProcessor? _mapSectionPersistProcessor;

		private AppNavWindow? _appNavWindow;

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

			_mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionValuesPool = new MapSectionValuesPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);

			_mapSectionHelper = new MapSectionHelper(_mapSectionVectorsPool, _mapSectionValuesPool);

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

			if (_repositoryAdapters.ProjectAdapter is ProjectAdapter pa)
			{
				DoSchemaUpdates(pa);
			}

			if (_repositoryAdapters.MapSectionAdapter is MapSectionAdapter maForSu)
			{
				DoSchemaUpdates(maForSu);
			}

			if (CLEAN_UP_JOB_MAP_SECTIONS && _repositoryAdapters.MapSectionAdapter is MapSectionAdapter ma)
			{
				var report = GetJobMapSectionsReferenceReport(ma);
				Debug.WriteLine(report);

				report = DeleteNonExtantJobsReferenced(ma);
				Debug.WriteLine(report);
			}

			var mEngineAddresses = USE_REMOTE_ENGINE ? REMOTE_M_ENGINE_ADDRESSES.ToList() : new List<string>();

			if (USE_LOCAL_ENGINE)
			{
				mEngineAddresses.Add(LOCAL_M_ENGINE_ADDRESS);
			}

			var mEngineClients = ChooseMEngineClientImplementation(CLIENT_IMPLEMENTATION, mEngineAddresses, _repositoryAdapters.MapSectionAdapter);


			_mapLoaderManager = BuildMapLoaderManager(_mapSectionVectorsPool, _mapSectionHelper, mEngineClients, USE_ALL_CORES, _repositoryAdapters.MapSectionAdapter, FETCH_ZVALUES_LOCALLY);

			_appNavWindow = GetAppNavWindow(_mapSectionHelper, _repositoryAdapters, _mapLoaderManager);
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

			//if (_mapSectionPersistProcessor != null)
			//{
			//	_mapSectionPersistProcessor.Dispose();
			//}

			_mEngineServerManager?.Stop();

			_mapSectionValuesPool.Clear();
		}

		private AppNavWindow GetAppNavWindow(MapSectionHelper mapSectionHelper, RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager)
		{
			var appNavViewModel = new AppNavViewModel(mapSectionHelper, repositoryAdapters, mapLoaderManager);

			var appNavWindow = new AppNavWindow
			{
				DataContext = appNavViewModel
			};

			appNavWindow.WindowState = WindowState.Minimized;

			return appNavWindow;
		}


		private IMEngineClient[] ChooseMEngineClientImplementation(MEngineClientImplementation clientImplementation, IList<string> mEngineEndPointAddresses, IMapSectionAdapter mapSectionAdapter)
		{
			var result = clientImplementation switch
			{
				MEngineClientImplementation.Remote => CreateMEngineClients(mEngineEndPointAddresses),
				MEngineClientImplementation.InProcess => throw new NotImplementedException("The MSetExplorer project is not compatible with the MapSetGenerator C++ project."), // CreateInProcessMEngineClient(mapSectionAdapter, out _mapSectionPersistProcessor),
				MEngineClientImplementation.LocalScalar => new IMEngineClient[] { new MClientLocalScalar() },
				MEngineClientImplementation.LocalVector => new IMEngineClient[] { new MClientLocalVector() },
				MEngineClientImplementation.LocalVectorMark2 => new IMEngineClient[] { new MClientLocal(USE_DEPTH_FIRST_ITERATOR) },
				_ => throw new NotSupportedException($"The value of {clientImplementation} is not recognized."),
			};

			return result;
		}

		private IMEngineClient[] CreateMEngineClients(IList<string> mEngineEndPointAddresses)
		{
			var mEngineClients = mEngineEndPointAddresses.Select(x => new MClient(x)).ToArray();
			return mEngineClients;
		}

		//private IMEngineClient[] CreateInProcessMEngineClient(IMapSectionAdapter mapSectionAdapter, out MapSectionPersistProcessor mapSectionPersistProcessor)
		//{
		//	mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionAdapter);
		//	var inProcessClient = new MClientInProcess(mapSectionAdapter, mapSectionPersistProcessor);
		//	var mEngineClients = new[] { inProcessClient };
		//	return mEngineClients;
		//}

		private IMapLoaderManager BuildMapLoaderManager(MapSectionVectorsPool mapSectionVectorsPool, MapSectionHelper mapSectionHelper, IMEngineClient[] mEngineClients, bool useAllCores, IMapSectionAdapter mapSectionAdapter, bool fetchZValues)
		{
			var mapSectionRequestProcessor = CreateMapSectionRequestProcessor(mEngineClients, useAllCores, mapSectionAdapter, mapSectionVectorsPool, fetchZValues);

			var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

			return result;
		}

		private MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, bool useAllCores, IMapSectionAdapter mapSectionAdapter, MapSectionVectorsPool mapSectionVectorsPool, bool fetchZValues)
		{
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients, useAllCores);
			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionVectorsPool, mapSectionAdapter, mapSectionGeneratorProcessor, mapSectionResponseProcessor, fetchZValues);

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

		private void DoSchemaUpdates(ProjectAdapter projectAdapter)
		{
			projectAdapter.DoSchemaUpdates();


			//projectAdapter.AddIsIsAlternatePathHeadToAllJobs();
			//projectAdapter.RemoveColorBandSetIdFromProject();
			//projectAdapter.RemoveEscapeVels();
		}

		private void DoSchemaUpdates(MapSectionAdapter mapSectionAdapter)
		{
			mapSectionAdapter.DoSchemaUpdates();
			//mapSectionAdapter.AddSubdivisionId();
		}

		private string GetJobMapSectionsReferenceReport(MapSectionAdapter mapSectionAdapter)
		{
			var report = mapSectionAdapter.GetJobMapSectionsReferenceReport();
			return report;
		}

		private string DeleteNonExtantJobsReferenced(MapSectionAdapter mapSectionAdapter)
		{
			var jobsNotFound = mapSectionAdapter.DeleteNonExtantJobsReferenced();

			var sb = new StringBuilder();

			sb.AppendLine("JobIds referenced in one or more JobMapSectionRecords for which no Job record exists.");
			sb.AppendLine("JobId\tMapSectionsDelerted");
			foreach(Tuple<string, long?> entry in jobsNotFound)
			{
				sb.AppendLine($"{entry.Item1}\t{entry.Item2}");
			}

			return sb.ToString();
		}

		private enum MEngineClientImplementation
		{
			Remote,
			InProcess,
			LocalScalar,
			LocalVector,
			LocalVectorMark2
		}

	}
}
