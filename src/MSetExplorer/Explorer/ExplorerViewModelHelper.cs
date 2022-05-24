using MEngineClient;
using MSetRepo;
using MSS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	public delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	public delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	public class ExplorerViewModelHelper
	{
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5004", "https://localhost:5001" };
		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "http://192.168.2.104:5000", "https://localhost:5001" };
		private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5001" };

		private ProjectAdapter? _projectAdapter;
		private SharedColorBandSetAdapter? _sharedColorBandSetAdapter;

		private IMapProjectViewModel? _mapProjectViewModel;
		private MapLoaderManager? _mapLoaderManager;
		private ColorBandSetViewModel? _colorBandViewModel;

		public ExplorerViewModelHelper()
		{
			_projectAdapter = null;
			_mapProjectViewModel = null;
			_mapLoaderManager = null;
		}

		public ExplorerViewModel GetExplorerViewModel()
		{
			var DROP_ALL_COLLECTIONS = false;
			var DROP_MAP_SECTIONS = false;
			var USE_MAP_SECTION_REPO = true;

			//base.OnStartup(e);

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

			//DoSchemaUpdates();

			_sharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(MONGO_DB_CONN_STRING);
			_sharedColorBandSetAdapter.CreateCollections();

			// Map Project ViewModel
			_mapProjectViewModel = new MapProjectViewModel(_projectAdapter, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			_mapLoaderManager = BuildMapLoaderManager(M_ENGINE_END_POINT_ADDRESSES, MONGO_DB_CONN_STRING, USE_MAP_SECTION_REPO, mapSectionHelper);
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			_colorBandViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections);

			var result = new ExplorerViewModel(_mapProjectViewModel, mapDisplayViewModel, _colorBandViewModel, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel);

			return result;
		}

		private MapLoaderManager BuildMapLoaderManager(string[] mEngineEndPointAddress, string dbProviderConnectionString, bool useTheMapSectionRepo, MapSectionHelper mapSectionHelper)
		{
			var mEngineClients = mEngineEndPointAddress.Select(x => new MClient(x)).ToArray();

			//var mEngineClient = new MClient(mEngineEndPointAddress);
			var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(dbProviderConnectionString);

			var mapSectionRequestProcessor = MapSectionRequestProcessorProvider.CreateMapSectionRequestProcessor(mEngineClients, mapSectionAdapter, useTheMapSectionRepo);

			var result = new MapLoaderManager(mapSectionHelper, mapSectionRequestProcessor);

			return result;
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
