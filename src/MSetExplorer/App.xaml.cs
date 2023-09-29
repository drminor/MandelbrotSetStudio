using MapSectionProviderLib;
using MEngineClient;
using MEngineDataContracts;
using MongoDB.Bson;
using MSetExplorer.RepositoryManagement;
using MSetGeneratorPrototype;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static readonly bool TEST_STORAGE_MODEL = false;

		private static readonly bool TEST_JOB_DETAILS_DIALOG = false;

		#region Configuration

		//private const string MONGO_DB_NAME = "MandelbrotProjects";
		//private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		//private const int MONGO_DB_PORT = 27017;

		private static readonly string LOCAL_IP_ADDRESS = "192.168.2.100";

		//private static readonly string[] REMOTE_SERVICE_END_POINTS = new string[] { "http://localhost:5000" };
		//private static readonly string[] REMOTE_SERVICE_END_POINTS = new string[] { "http://192.168.2.108:5000" };
		private static readonly string[] REMOTE_SERVICE_END_POINTS = new string[] { "http://192.168.2.100:5000" };

		private static readonly bool USE_ALL_CORES = true;
		private static readonly bool USE_REMOTE_ENGINES = false;
		private static readonly bool USE_LOCAL_ENGINE = true;

		//private static readonly MSetGenerationStrategy GEN_STRATEGY = MSetGenerationStrategy.DepthFirst;

		private static readonly bool CHECK_CONN_BEFORE_USE = false;

		private static readonly bool DO_SCHEMA_UPDATES = false;
		private static readonly bool CREATE_COLLECTIONS = false;
		private static readonly bool CREATE_COLLECTION_INDEXES = false;

		private static readonly bool CREATE_JOB_MAP_SECTION_REPORT = false;

		private static readonly bool FIND_AND_DELETE_ORPHAN_JOBS = false;
		private static readonly bool FIND_AND_DELETE_ORPHAN_MAP_SECTIONS = false;
		private static readonly bool FIND_AND_DELETE_ORPHAN_SUBDIVISIONS = false;

		private static readonly bool DELETE_JOB_MAP_JOB_REFS = false;
		private static readonly bool DELETE_JOB_MAP_MAP_REFS = false; // True

		private static readonly bool POPULATE_JOB_MAP_SECTIONS_FOR_PROJECTS = false;
		private static readonly bool POPULATE_JOB_MAP_SECTIONS_FOR_POSTERS = false;

		private static readonly bool UPDATE_JOB_SUBDIVSION_IDS_FOR_ALL_JobMapSections = false;

		private static readonly DateTime DROP_MAP_SECTIONS_AFTER_DATE = DateTime.Parse("2083-09-10");
		private static readonly bool DROP_RECENT_MAP_SECTIONS = false;
		private static readonly bool DROP_RECENT_JOB_MAP_SECTIONS = false;

		private static readonly bool DROP_MAP_SECTIONS_AND_SUBDIVISIONS = false;

		//private Stopwatch? _ambientStopWatch;

		#endregion

		#region Private Properties

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private RepositoryAdapters? _repositoryAdapters;

		private IMapLoaderManager? _mapLoaderManager;

		private AppNavWindow? _appNavWindow;

		#endregion

		#region Constructor, Startup and Exit

		public App()
		{
			_mapSectionVectorProvider = CreateMapSectionVectorProvider(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.MAP_SECTION_INITIAL_POOL_SIZE);

			//_ambientStopWatch = Stopwatch.StartNew();
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			//var currentStopwatch = _ambientStopWatch ?? Stopwatch.StartNew();
			base.OnStartup(e);

			if (TEST_STORAGE_MODEL)
			{
				TestStorageModel();

				MessageBox.Show("Exiting.");
				Current.Shutdown();
				return;
			}
			else if (TEST_JOB_DETAILS_DIALOG)
			{
				TestJobDetailsWindow();

				MessageBox.Show("Exiting.");
				Current.Shutdown();
				return;
			}

			//Debug.WriteLine($"Before Get Repos. {currentStopwatch.ElapsedMilliseconds}.");

			if (CHECK_CONN_BEFORE_USE)
			{
				if (!TryGetRepositoryAdapters(out _repositoryAdapters))
				{
					Current.Shutdown();
					return;
				}
				//Debug.WriteLine($"After Get Repos. {currentStopwatch.ElapsedMilliseconds}.");
			}
			else
			{
				_repositoryAdapters = GetRepositoryAdaptersFast();
				//Debug.WriteLine($"After Get Repos FAST. {currentStopwatch.ElapsedMilliseconds}.");
			}

			var subdivisionProvider = new SubdivisonProvider(_repositoryAdapters.MapSectionAdapter);
			//Debug.WriteLine($"After Create SubdivisionProvider. {currentStopwatch.ElapsedMilliseconds}.");

			var mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, RMapConstants.BLOCK_SIZE);
			//Debug.WriteLine($"After Create MapJobHelper. {currentStopwatch.ElapsedMilliseconds}.");

			var repositoryIntegrityUtility = new RepoIntegrityUtility(_repositoryAdapters.ProjectAdapter, _repositoryAdapters.MapSectionAdapter, mapJobHelper);

			#region Repo Maintenance

			if (DROP_MAP_SECTIONS_AND_SUBDIVISIONS)
			{
				Debug.WriteLine("Not dropping All MapSections and Subdivisions.");
				//_repositoryAdapters.MapSectionAdapter.DropMapSectionsAndSubdivisions();
			}
			else
			{
				if (DROP_RECENT_MAP_SECTIONS)
				{
					var countMapSectionsDeleted =  _repositoryAdapters.MapSectionAdapter.DeleteMapSectionsCreatedSince(DROP_MAP_SECTIONS_AFTER_DATE, overrideRecentGuard: true);
					MessageBox.Show($"Deleted {countMapSectionsDeleted} MapSection records that have been created since {DROP_MAP_SECTIONS_AFTER_DATE}.");
				}

				if (DROP_RECENT_JOB_MAP_SECTIONS)
				{
					var countJobMapSectionsDeleted = _repositoryAdapters.MapSectionAdapter.DeleteJobMapSectionsCreatedSince(DROP_MAP_SECTIONS_AFTER_DATE, overrideRecentGuard: true);
					MessageBox.Show($"Deleted {countJobMapSectionsDeleted} JobMapSection records that have been created since {DROP_MAP_SECTIONS_AFTER_DATE}.");
				}
			}

			if (DROP_MAP_SECTIONS_AND_SUBDIVISIONS | DROP_RECENT_MAP_SECTIONS | DROP_RECENT_JOB_MAP_SECTIONS)
			{
				MessageBox.Show("MapSection / JobMapSection Maintenance Completed.");
				Current.Shutdown();
				return;
			}

			if (CREATE_COLLECTIONS)
			{
				_repositoryAdapters.CreateCollections();
			}
			else if (CREATE_COLLECTION_INDEXES)
			{
				_repositoryAdapters.CreateCollectionIndexes();
			}
			else
			{
				_repositoryAdapters.WarmUp();
			}

			if (DO_SCHEMA_UPDATES)
			{
				DoSchemaUpdates(_repositoryAdapters);
				MessageBox.Show("Schema Updates are completed.");

				Current.Shutdown();
				return;
			}

			if (UPDATE_JOB_SUBDIVSION_IDS_FOR_ALL_JobMapSections)
			{
				repositoryIntegrityUtility.UpdateJobMapSectionSubdivisionIds();
			}

			if (CREATE_JOB_MAP_SECTION_REPORT)
			{
				var report = repositoryIntegrityUtility.CreateJobMapSectionsReferenceReport();
				Debug.WriteLine(report);
			}

			if (FIND_AND_DELETE_ORPHAN_JOBS)
			{
				repositoryIntegrityUtility.FindAndDeleteOrphanJobs();
			}

			if (FIND_AND_DELETE_ORPHAN_MAP_SECTIONS)
			{
				repositoryIntegrityUtility.FindAndDeleteOrphanMapSections();
			}

			if (FIND_AND_DELETE_ORPHAN_SUBDIVISIONS)
			{
				repositoryIntegrityUtility.FindAndDeleteOrphanSubdivisions();
			}

			if (DELETE_JOB_MAP_JOB_REFS)
			{
				repositoryIntegrityUtility.CheckAndDeleteJobRefsFromJobMapCollection();
			}

			if (DELETE_JOB_MAP_MAP_REFS)
			{
				repositoryIntegrityUtility.CheckAndDeleteMapRefsFromJobMapCollection();
			}

			// TODO: Check each JobMapSection to make sure its JobOwnerType and the referenced Job Record's JobOwnerType match.

			if (POPULATE_JOB_MAP_SECTIONS_FOR_PROJECTS)
			{
				var report = repositoryIntegrityUtility.PopulateJobMapSections(OwnerType.Project);
				Debug.WriteLine(report);
			}

			if (POPULATE_JOB_MAP_SECTIONS_FOR_POSTERS)
			{
				var report = repositoryIntegrityUtility.PopulateJobMapSections(OwnerType.Poster);
				Debug.WriteLine(report);
			}

			if (CREATE_JOB_MAP_SECTION_REPORT 
				| FIND_AND_DELETE_ORPHAN_JOBS | FIND_AND_DELETE_ORPHAN_MAP_SECTIONS | FIND_AND_DELETE_ORPHAN_SUBDIVISIONS
				| DELETE_JOB_MAP_MAP_REFS | DELETE_JOB_MAP_JOB_REFS
				| POPULATE_JOB_MAP_SECTIONS_FOR_PROJECTS | POPULATE_JOB_MAP_SECTIONS_FOR_POSTERS)
			{
				if (MessageBoxResult.No == MessageBox.Show("Reporting completed. Continue with startup?", "Continue?", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No))
				{
					Current.Shutdown();
					return;
				}
			}

			#endregion

			var mEngineClients = CreateTheMEngineClients(USE_ALL_CORES, REMOTE_SERVICE_END_POINTS, useRemoteEngine: USE_REMOTE_ENGINES, useLocalEngine: USE_LOCAL_ENGINE, 
				mapSectionGeneratorCreator: CreateMapSectionGenerator, _mapSectionVectorProvider);
			//Debug.WriteLine($"After Create MEngineClients. {currentStopwatch.ElapsedMilliseconds}.");

			var mapSectionRequestProcessor = CreateMapSectionRequestProcessor(mEngineClients, _repositoryAdapters.MapSectionAdapter, _mapSectionVectorProvider);
			//Debug.WriteLine($"After Create MapSectionProcesors. {currentStopwatch.ElapsedMilliseconds}.");

			_mapLoaderManager = new MapLoaderManager(mapSectionRequestProcessor);
			//Debug.WriteLine($"After Create MapLoaderManager. {currentStopwatch.ElapsedMilliseconds}.");

			var appNavViewModel = GetAppNavViewModel(_mapSectionVectorProvider, _repositoryAdapters, _mapLoaderManager, mapJobHelper, mapSectionRequestProcessor);
			_appNavWindow = GetAppNavWindow(appNavViewModel);
			//Debug.WriteLine($"After Get AppNavWindow. {currentStopwatch.ElapsedMilliseconds}.");

			_appNavWindow.Show();
			//Debug.WriteLine($"After AppNav Show. {currentStopwatch.ElapsedMilliseconds}.");

			//if (_ambientStopWatch != null)
			//{
			//	_ambientStopWatch.Stop();
			//	_ambientStopWatch = null;
			//}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (_mapLoaderManager != null)
			{
				// This disposes the MapSectionRequestProcessor, MapSectionGeneratorProcessor, MapSectionResponseProcessor, MapSectionPersistProcessor
				_mapLoaderManager.Dispose();
			}

			//_mEngineServerManager?.Stop();
		}

		#endregion

		#region Get Repository Adapters

		private bool TryGetRepositoryAdapters([NotNullWhen(true)] out RepositoryAdapters? repositoryAdapters)
		{
			var (repoDbServerName, repoDbPort, repoDbName) = GetConnectionSettings();

			var haveServerName = !string.IsNullOrEmpty(repoDbServerName?.Trim());
			var haveDbName = !string.IsNullOrEmpty(repoDbName?.Trim());
			var serviceIsRunning = ServiceHelper.CheckService(RMapConstants.SERVICE_NAME) == ServiceControllerStatus.Running;
			var isConnected = serviceIsRunning && CheckRepoConnectivity(repoDbServerName, repoDbPort, repoDbName);

			if (!haveServerName || !haveDbName || !serviceIsRunning || !isConnected)
			{
				if (TryEditConnectionSettings(repoDbServerName, repoDbPort, repoDbName, out var s, out var p, out var d))
				{
					repoDbServerName = s;
					repoDbPort = p;
					repoDbName = d;

					SaveConnectionSettings(repoDbServerName, repoDbPort, repoDbName);
				}
				else
				{
					repositoryAdapters = null;
					return false;
				}
			}

			if (repoDbServerName == null || repoDbName == null)
			{
				repositoryAdapters = null;
				return false;
			}
			else
			{
				repositoryAdapters = new RepositoryAdapters(repoDbServerName, repoDbPort, repoDbName);
				return true;
			}
		}

		private RepositoryAdapters GetRepositoryAdaptersFast()
		{
			var (repoDbServerName, repoDbPort, repoDbName) = GetConnectionSettings();

			var repositoryAdapters = new RepositoryAdapters(repoDbServerName!, repoDbPort, repoDbName!);
			return repositoryAdapters;
		}

		private (string? serverName, int port, string? databaseName) GetConnectionSettings()
		{
			var appSettings = ConfigurationManager.AppSettings;

			var serverName = appSettings["MongoDbServer"];
			var strPort = appSettings["MongoDbPort"];
			var dbName = appSettings["MongoDbName"];

			if (!int.TryParse(strPort, out var port))
			{
				port = RMapConstants.DEFAULT_MONGO_DB_PORT;
			}

			return new(serverName, port, dbName);
		}

		private bool TryEditConnectionSettings(string? initialServerName, int initialPort, string? initialDatabaseName, [NotNullWhen(true)] out string? serverName, out int port, [NotNullWhen(true)] out string? databaseName)
		{
			var repoConnParametersViewModel = new RepoConnParametersViewModel(initialServerName, initialPort, initialDatabaseName);
			
			var repoConnParametersDialog = new RepoConnParametersDialog()
			{
				DataContext = repoConnParametersViewModel
			};

			if (repoConnParametersDialog.ShowDialog() == true && repoConnParametersViewModel.ServerName != null && repoConnParametersViewModel.DatabaseName != null)
			{
				serverName = repoConnParametersViewModel.ServerName;
				port = repoConnParametersViewModel.Port;
				databaseName = repoConnParametersViewModel.DatabaseName;

				return true;
			}
			else
			{
				serverName = initialServerName;
				port = initialPort;
				databaseName = initialDatabaseName;
				return false;
			}
		}

		private void SaveConnectionSettings(string? serverName, int port, string? databaseName)
		{
			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			var settings = config.AppSettings.Settings;

			AddOrUpdate(settings, "MongoDbServer", serverName);
			AddOrUpdate(settings, "MongoDbPort", port.ToString());
			AddOrUpdate(settings, "MongoDbName", databaseName);


			config.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
		}

		private void AddOrUpdate(KeyValueConfigurationCollection settings, string key, string? value)
		{
			if (settings[key] == null)
			{
				settings.Add(key, value);
			}
			else
			{
				if (value == null)
				{
					settings.Remove(key);
				}
				else
				{
					settings[key].Value = value;
				}
			}
		}

		private bool CheckRepoConnectivity(string? serverName, int port, string? databaseName)
		{
			if (serverName != null && databaseName != null)
			{
				var dbProvider = new DbProvider(serverName, port, databaseName);
				var result = dbProvider.TestConnection(databaseName, TimeSpan.FromSeconds(10));
				return result;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Schema Update Support

		private void DoSchemaUpdates(RepositoryAdapters repositoryAdapters)
		{
			if (repositoryAdapters.ProjectAdapter is ProjectAdapter pa)
			{
				pa.DoSchemaUpdates();

				//pa.AddIsIsAlternatePathHeadToAllJobs();
				//pa.RemoveColorBandSetIdFromProject();
				//pa.RemoveEscapeVels();
			}

			if (repositoryAdapters.MapSectionAdapter is MapSectionAdapter maForSu)
			{
				maForSu.DoSchemaUpdates();

				//maForSu.AddSubdivisionId();
			}
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

		#endregion

		#region MEngine Support

		private IMEngineClient[] CreateTheMEngineClients(bool useAllCores, string[] remoteEndPoints, bool useRemoteEngine, bool useLocalEngine, Func<IMapSectionGenerator> mapSectionGeneratorCreator, 
			MapSectionVectorProvider mapSectionVectorProvider)
		{
			var clientNumber = 0;
			var result = new List<IMEngineClient>();

			var localTaskCountAdjustment = 0;
			var localTaskCount = GetLocalTaskCount(useAllCores);
			var remoteTaskCount = GetRemoteTaskCount(useAllCores, localTaskCount);

			if (useRemoteEngine && useLocalEngine && remoteEndPoints[0].Contains(LOCAL_IP_ADDRESS))
			{
				localTaskCountAdjustment = localTaskCount / 2;
			}

			var localClientCount = localTaskCount - localTaskCountAdjustment;

			if (useLocalEngine)
			{
				for (var i = 0; i < localClientCount; i++)
				{
					var mapSectionGenerator = mapSectionGeneratorCreator();
					result.Add(new MClientLocal(clientNumber++, mapSectionGenerator, mapSectionVectorProvider));
				}

				//Debug.WriteLine($"Using {localClientCount} local engines.");
			}

			var remoteClientCount = 0;

			if (useRemoteEngine)
			{
				foreach (string remoteEndPoint in remoteEndPoints)
				{
					for (var i = 0; i < remoteTaskCount; i++)
					{
						result.Add(new MClient(clientNumber++, remoteEndPoint, mapSectionVectorProvider));
						remoteClientCount++;
					}

					//Debug.WriteLine($"Using {remoteTaskCount} engines at {remoteEndPoint}.");
				}
			}

			Debug.WriteLine($"Using {localClientCount} local and {remoteClientCount} remote MEngine Clients.");

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

		private int GetRemoteTaskCount(bool useAllCores, int localTaskCount)
		{
			int result;

			if (useAllCores)
			{
				result = (int) Math.Round(((double) localTaskCount) / 2, MidpointRounding.AwayFromZero);
			}
			else
			{
				result = 1;
			}

			return result;
		}

		private IMapSectionGenerator CreateMapSectionGenerator()
		{
			var mapSectionGenerator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, RMapConstants.BLOCK_SIZE);

			return mapSectionGenerator;
		}

		#region MEngine Constants

		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net6.0\MEngineService.exe";
		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\x64\Debug\MEngineService.exe";
		//private const string LOCAL_M_ENGINE_ADDRESS = "https://localhost:5001";
		//private static readonly string[] REMOTE_M_ENGINE_ADDRESSES = new string[] { "http://192.168.2.109:5000" };		//private static readonly bool START_LOCAL_ENGINE = false; // If true, we will start the local server's executable. If false, then use Multiple Startup Projects when debugging.
		//private static readonly bool USE_LOCAL_ENGINE = false; // If true, we will host a server -- AND include it in the list of servers to use by our client.
		//private static readonly bool USE_REMOTE_ENGINE = false;  // If true, send part of our work to the remote server(s)

		#endregion

		//private IMEngineClient[] CreateTheMEngineClients(bool useRemoteEngine, bool useLocalEngine, MSetGenerationStrategy mSetGenerationStrategy, bool useAllCores)
		//{
		//	//var mEngineAddresses = useRemoteEngine ? REMOTE_M_ENGINE_ADDRESSES.ToList() : new List<string>();

		//	//if (useLocalEngine)
		//	//{
		//	//	mEngineAddresses.Add(LOCAL_M_ENGINE_ADDRESS);
		//	//}
		//	var result = new List<IMEngineClient>();

		//	var localTaskCount = GetLocalTaskCount(useAllCores);

		//	for (var i = 0; i < localTaskCount; i++)
		//	{
		//		result.Add(new MClientLocal(mSetGenerationStrategy));
		//	}

		//	return result.ToArray();
		//}

		//var mEngineClients = ChooseMEngineClientImplementation(CLIENT_IMPLEMENTATION, mEngineAddresses, _repositoryAdapters.MapSectionAdapter);

		//private IMEngineClient[] ChooseMEngineClientImplementation(ClientImplementation clientImplementation, IList<string> mEngineEndPointAddresses, IMapSectionAdapter mapSectionAdapter)
		//{
		//	var result = clientImplementation switch
		//	{
		//		ClientImplementation.Remote => CreateMEngineClients(mEngineEndPointAddresses),

		//		ClientImplementation.InProcess => throw new NotImplementedException("The MSetExplorer project is not compatible with the MapSetGenerator C++ project."), // CreateInProcessMEngineClient(mapSectionAdapter, out _mapSectionPersistProcessor),
		//		ClientImplementation.LocalScalar => throw new NotImplementedException("The LocalScalar implementation of IMEngineClient is currently not supported"), // => new IMEngineClient[] { new MClientLocalScalar() },
		//		ClientImplementation.LocalVector => throw new NotImplementedException("The LocalScalar implementation of IMEngineClient is currently not supported"), // => new IMEngineClient[] { new MClientLocalVector() },

		//		ClientImplementation.LocalVectorMark2 => new IMEngineClient[] { new MClientLocal(USE_SINGLE_LIMB_ITERATOR, USE_DEPTH_FIRST_ITERATOR, USE_C_IMPLEMENTATION) },
		//		_ => throw new NotSupportedException($"The value of {clientImplementation} is not recognized."),
		//	};

		//	return result;
		//}

		//private IMEngineClient[] CreateMEngineClients(IList<string> mEngineEndPointAddresses)
		//{
		//	var mEngineClients = mEngineEndPointAddresses.Select(x => new MClient(x)).ToArray();
		//	return mEngineClients;
		//}

		//private IMEngineClient[] CreateInProcessMEngineClient(IMapSectionAdapter mapSectionAdapter, out MapSectionPersistProcessor mapSectionPersistProcessor)
		//{
		//	mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionAdapter);
		//	var inProcessClient = new MClientInProcess(mapSectionAdapter, mapSectionPersistProcessor);
		//	var mEngineClients = new[] { inProcessClient };
		//	return mEngineClients;
		//}

		#endregion

		#region Map Section Request Processor

		private MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider)
		{
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients);
			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionPersistProcessor = new MapSectionPersistProcessor(mapSectionAdapter, mapSectionVectorProvider);
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionAdapter, mapSectionVectorProvider, mapSectionGeneratorProcessor, mapSectionResponseProcessor, mapSectionPersistProcessor);

			return mapSectionRequestProcessor;
		}

		private MapSectionVectorProvider CreateMapSectionVectorProvider(SizeInt blockSize, int defaultLimbCount, int initialPoolSize)
		{
			var mapSectionVectorsPool = new MapSectionVectorsPool(blockSize, initialPoolSize);
			var mapSectionZVectorsPool = new MapSectionZVectorsPool(blockSize, defaultLimbCount, initialPoolSize);
			var mapSectionVectorProvider = new MapSectionVectorProvider(mapSectionVectorsPool, mapSectionZVectorsPool);

			return mapSectionVectorProvider;
		}

		#endregion

		#region AppNav Window

		private AppNavWindow GetAppNavWindow(AppNavViewModel appNavViewModel)
		{
			var appNavWindow = new AppNavWindow
			{
				DataContext = appNavViewModel
			};

			appNavWindow.WindowState = WindowState.Minimized;

			return appNavWindow;
		}

		private AppNavViewModel GetAppNavViewModel(MapSectionVectorProvider mapSectionVectorProvider, RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			var appNavViewModel = new AppNavViewModel(mapSectionVectorProvider, repositoryAdapters, mapLoaderManager, mapJobHelper, mapSectionRequestProcessor);

			return appNavViewModel;
		}

		#endregion

		#region Test Storage Model

		private void TestStorageModel()
		{
			//_repositoryAdapters = GetRepositoryAdaptersFast();

			//var storageModelPoc = GetStorageModelPOC(_repositoryAdapters.ProjectAdapter, _repositoryAdapters.MapSectionAdapter);

			//var projectId = new ObjectId("6258fe80712f62b28ce55c15");
			////storageModelPoc.PlayWithStorageModel(projectId);
		}

		//private StorageModelPOC GetStorageModelPOC(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		//{
		//	if (projectAdapter is ProjectAdapter pa && mapSectionAdapter is MapSectionAdapter ma)
		//	{
		//		var result = new StorageModelPOC(pa, ma);
		//		return result;
		//	}
		//	else
		//	{
		//		throw new InvalidOperationException("Either the _projectAdapter is not an instance of a ProjectAdapter or the _mapSectionAdapter is not an instance of a MapSectionAdapter.");
		//	}
		//}

		#endregion

		#region Test JobDetailsDialog

		private void TestJobDetailsWindow()
		{
			_repositoryAdapters = GetRepositoryAdaptersFast();

			var ownerName = "Poster Art3-13-4";
			var ownerId = new ObjectId("64913f6d0d20aad9f1a64737"); // Poster Art3-13-4
			var currentJobId = new ObjectId("649141932b7c6bda0e7ccf81");
			var ownerCreationDate = DateTime.Parse("2023-06-01 10:40:03");

			var posterInfo = new PosterInfo(ownerId, ownerName, description: null, currentJobId, new SizeDbl(1024), bytes: 0, ownerCreationDate, DateTime.MinValue, DateTime.MinValue);


			OpenJobDetailsDialog(posterInfo);
		}

		private void OpenJobDetailsDialog(IJobOwnerInfo jobOwnerInfo)
		{
			var jobDetailsViewModel = CreateAJobDetailsDialog(jobOwnerInfo);
			var jobDetailsDialog = new JobDetailsWindow
			{
				DataContext = jobDetailsViewModel
			};

			jobDetailsDialog.ShowDialog();
		}

		public JobDetailsViewModel CreateAJobDetailsDialog(IJobOwnerInfo jobOwnerInfo)
		{
			if (_repositoryAdapters == null)
			{
				throw new InvalidOperationException("The _repositoryAdapters is null.");
			}
			var projectAdapter = _repositoryAdapters.ProjectAdapter;
			var mapSectionAdapter = _repositoryAdapters.MapSectionAdapter;

			return new JobDetailsViewModel(jobOwnerInfo, projectAdapter, mapSectionAdapter);
		}

		#endregion
	}
}
