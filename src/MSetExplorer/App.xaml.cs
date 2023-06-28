using MapSectionProviderLib;
using MEngineClient;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using ProjectRepo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using System.Text;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region Configuration

		//private const string MONGO_DB_NAME = "MandelbrotProjects";
		//private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		//private const int MONGO_DB_PORT = 27017;

		private static readonly bool USE_ALL_CORES = true;

		private static readonly MSetGenerationStrategy GEN_STRATEGY = MSetGenerationStrategy.DepthFirst;

		private static readonly bool DO_SCHEMA_UPDATES = false;
		private static readonly bool CREATE_COLLECTIONS = false;
		private static readonly bool CREATE_COLLECTION_INDEXES = false;

		private static readonly bool CREATE_JOB_MAP_SECTION_REPORT = false;

		private static readonly bool FIND_AND_DELETE_ORPHAN_MAP_SECTIONS = false;

		private static readonly bool DELETE_JOB_MAP_MAP_REFS = false;
		private static readonly bool DELETE_JOB_MAP_JOB_REFS = false;

		private static readonly bool POPULATE_JOB_MAP_SECTIONS = false;

		private static readonly DateTime DELETE_MAP_SECTIONS_AFTER_DATE = DateTime.Parse("2093-05-01");
		private static readonly bool DROP_RECENT_MAP_SECTIONS = false;
		private static readonly bool DROP_MAP_SECTIONS_AND_SUBDIVISIONS = false;

		#endregion

		#region Private Properties

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private RepositoryAdapters? _repositoryAdapters;
		//private readonly MEngineServerManager? _mEngineServerManager;
		private IMapLoaderManager? _mapLoaderManager;

		private AppNavWindow? _appNavWindow;

		#endregion

		#region Constructor, Startup and Exit

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

			_mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionZVectorsPool = new MapSectionZVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.DEFAULT_LIMB_COUNT, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionVectorProvider = new MapSectionVectorProvider(_mapSectionVectorsPool, _mapSectionZVectorsPool);


			//if (START_LOCAL_ENGINE)
			//{
			//	_mEngineServerManager = new MEngineServerManager(SERVER_EXE_PATH, LOCAL_M_ENGINE_ADDRESS);
			//}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			//_mEngineServerManager?.Start();

			if (!TryGetRepositoryAdapters(out _repositoryAdapters))
			{
				Current.Shutdown();
				return;
			}


			var subdivisionProvider = new SubdivisonProvider(_repositoryAdapters.MapSectionAdapter);
			var mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, RMapConstants.BLOCK_SIZE);

			#region Repo Maintenance

			if (DROP_MAP_SECTIONS_AND_SUBDIVISIONS)
			{
				_repositoryAdapters.MapSectionAdapter.DropMapSectionsAndSubdivisions();
			}
			else if (DROP_RECENT_MAP_SECTIONS)
			{
				_repositoryAdapters.MapSectionAdapter.DeleteMapSectionsCreatedSince(DELETE_MAP_SECTIONS_AFTER_DATE, overrideRecentGuard: true);
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

			if (CREATE_JOB_MAP_SECTION_REPORT)
			{
				var report = CreateJobMapSectionsReferenceReport(_repositoryAdapters.MapSectionAdapter);
				Debug.WriteLine(report);
			}

			if (FIND_AND_DELETE_ORPHAN_MAP_SECTIONS)
			{
				FindAndDeleteOrphanMapSections(_repositoryAdapters.MapSectionAdapter);
			}

			if (DELETE_JOB_MAP_MAP_REFS)
			{
				CheckAndDeleteMapRefsFromJobMapCollection(_repositoryAdapters.MapSectionAdapter);
			}

			if (DELETE_JOB_MAP_JOB_REFS)
			{
				CheckAndDeleteJobRefsFromJobMapCollection(_repositoryAdapters.ProjectAdapter, _repositoryAdapters.MapSectionAdapter);
			}

			if (POPULATE_JOB_MAP_SECTIONS)
			{
				var report = PopulateJobMapSections(_repositoryAdapters.ProjectAdapter, _repositoryAdapters.MapSectionAdapter, mapJobHelper);
				Debug.WriteLine(report);
			}

			if (CREATE_JOB_MAP_SECTION_REPORT | FIND_AND_DELETE_ORPHAN_MAP_SECTIONS | DELETE_JOB_MAP_MAP_REFS | DELETE_JOB_MAP_JOB_REFS | POPULATE_JOB_MAP_SECTIONS)
			{
				if (MessageBoxResult.No == MessageBox.Show("Reporting completed. Continue with startup?", "Continue?", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No))
				{
					Current.Shutdown();
					return;
				}
			}

			#endregion

			var mEngineClients = CreateTheMEngineClients(GEN_STRATEGY, USE_ALL_CORES);

			var mapSectionRequestProcessor = CreateMapSectionRequestProcessor(mEngineClients, _repositoryAdapters.MapSectionAdapter, _mapSectionVectorProvider);
			_mapLoaderManager = new MapLoaderManager(mapSectionRequestProcessor);

			_appNavWindow = GetAppNavWindow(_mapSectionVectorProvider, _repositoryAdapters, _mapLoaderManager, mapJobHelper, mapSectionRequestProcessor);
			_appNavWindow.Show();
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

		#region Cleanup Map Sections

		private string PopulateJobMapSections(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			//var report = ProjectAndMapSectionHelper.PopulateJobMapSectionsForAllProjects(projectAdapter, mapSectionAdapter, mapJobHelper);

			var report = ProjectAndMapSectionHelper.PopulateJobMapSectionsForAllPosters(projectAdapter, mapSectionAdapter, mapJobHelper);

			return report;
		}

		private string CreateJobMapSectionsReferenceReport(IMapSectionAdapter mapSectionAdapter)
		{
			if (mapSectionAdapter is MapSectionAdapter ma)
			{
				var report = ma.GetJobMapSectionsToMapSectionReferenceReport();
				return report;
			}
			else
			{
				return "Report not available.";
			}
		}

		private void FindAndDeleteOrphanMapSections(IMapSectionAdapter mapSectionAdapter)
		{
			var report = FindOrphanMapSections(mapSectionAdapter, out var mapSectionIdsWithNoJob);
			Debug.WriteLine(report);

			var countOrphanMapSections = mapSectionIdsWithNoJob.Count;

			if (countOrphanMapSections > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOrphanMapSections} MapSection that are not reference by any job?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = mapSectionAdapter.DeleteMapSectionsInList(mapSectionIdsWithNoJob);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");
				}
			}
		}

		private string FindOrphanMapSections(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> mapSectionIdsWithNoJob)
		{
			var report = ProjectAndMapSectionHelper.FindOrphanMapSections(mapSectionAdapter, out mapSectionIdsWithNoJob);
			return report;
		}

		private void CheckAndDeleteJobRefsFromJobMapCollection(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			var report = CheckJobRefsFromJobMapCollection(projectAdapter, mapSectionAdapter, out var jobMapSectionIdsWithMissingJobRecord, out var subdivisionIdsForMissingJobs);
			Debug.WriteLine(report);

			var countOfRecordsWithMissingMapSection = jobMapSectionIdsWithMissingJobRecord.Count;

			if (countOfRecordsWithMissingMapSection > 0)
			{
				var formattedSubdivisionList = string.Join("\n", subdivisionIdsForMissingJobs);
				Debug.WriteLine($"SubdivisionIds for the records with a missing JobRecord:\n{formattedSubdivisionList}\n");

				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOfRecordsWithMissingMapSection} JobMapSection records referencing a Job record that does not exist?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = mapSectionAdapter.DeleteJobMapSectionsInList(jobMapSectionIdsWithMissingJobRecord);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");

					//MessageBox.Show("This is in progress -- must test first.");
				}
			}
		}

		private string CheckJobRefsFromJobMapCollection(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingJobRecord, out List<ObjectId> subdivisionIdsForMissingJobs)
		{
			var report = ProjectAndMapSectionHelper.CheckJobRefsAndSubdivisions(projectAdapter, mapSectionAdapter, out jobMapSectionIdsWithMissingJobRecord, out subdivisionIdsForMissingJobs);
			return report;
		}

		private void CheckAndDeleteMapRefsFromJobMapCollection(IMapSectionAdapter mapSectionAdapter)
		{
			var report = CheckMapRefsFromJobMapCollection(mapSectionAdapter, out var jobMapSectionIdsWithMissingMapSection, out var subdivisionIdsForMissingMapSections);
			Debug.WriteLine(report);

			var countOfRecordsWithMissingMapSection = jobMapSectionIdsWithMissingMapSection.Count;

			if (countOfRecordsWithMissingMapSection > 0)
			{
				var formattedSubdivisionList = string.Join("\n", subdivisionIdsForMissingMapSections);
				Debug.WriteLine($"SubdivisionIds for the records with a missing MapSection:\n{formattedSubdivisionList}\n");

				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOfRecordsWithMissingMapSection} JobMapSection records referencing a MapSection record that does not exist?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = mapSectionAdapter.DeleteJobMapSectionsInList(jobMapSectionIdsWithMissingMapSection);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");
				}
			}
		}

		private string CheckMapRefsFromJobMapCollection(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingMapSection, out List<ObjectId> subdivisionIdsForMissingMapSections)
		{
			var report = ProjectAndMapSectionHelper.CheckMapRefsAndSubdivisions(mapSectionAdapter, out jobMapSectionIdsWithMissingMapSection, out subdivisionIdsForMissingMapSections);
			return report;
		}

		#endregion

		#region MEngine Support

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

		#region MEngine Constants

		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net6.0\MEngineService.exe";
		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\x64\Debug\MEngineService.exe";
		//private const string LOCAL_M_ENGINE_ADDRESS = "https://localhost:5001";
		//private static readonly string[] REMOTE_M_ENGINE_ADDRESSES = new string[] { "http://192.168.2.109:5000" };		//private static readonly bool START_LOCAL_ENGINE = false; // If true, we will start the local server's executable. If false, then use Multiple Startup Projects when debugging.
		//private static readonly bool USE_LOCAL_ENGINE = false; // If true, we will host a server -- AND include it in the list of servers to use by our client.
		//private static readonly bool USE_REMOTE_ENGINE = false;  // If true, send part of our work to the remote server(s)

		#endregion

		private IMEngineClient[] CreateTheMEngineClients(bool useRemoteEngine, bool useLocalEngine, MSetGenerationStrategy mSetGenerationStrategy, bool useAllCores)
		{
			//var mEngineAddresses = useRemoteEngine ? REMOTE_M_ENGINE_ADDRESSES.ToList() : new List<string>();

			//if (useLocalEngine)
			//{
			//	mEngineAddresses.Add(LOCAL_M_ENGINE_ADDRESS);
			//}
			var result = new List<IMEngineClient>();

			var localTaskCount = GetLocalTaskCount(useAllCores);

			for (var i = 0; i < localTaskCount; i++)
			{
				result.Add(new MClientLocal(mSetGenerationStrategy));
			}

			return result.ToArray();
		}

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

		#endregion

		#region AppNav Window

		private AppNavWindow GetAppNavWindow(MapSectionVectorProvider mapSectionVectorProvider, RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			var appNavViewModel = new AppNavViewModel(mapSectionVectorProvider, repositoryAdapters, mapLoaderManager, mapJobHelper, mapSectionRequestProcessor);

			var appNavWindow = new AppNavWindow
			{
				DataContext = appNavViewModel
			};

			appNavWindow.WindowState = WindowState.Minimized;

			return appNavWindow;
		}

		#endregion
	}
}
