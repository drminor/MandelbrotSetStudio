using MapSectionProviderLib;
using MSetExplorer.XPoc;
using MSetExplorer.XPoc.PerformanceHarness;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using System;

namespace MSetExplorer
{
	internal class AppNavViewModel
	{
		private static readonly bool _useSimpleJobTree = true;

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly ViewModelFactory _viewModelFactory;

		public AppNavViewModel(MapSectionHelper mapSectionHelper, RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, MapSectionRequestProcessor	mapSectionRequestProcessor)
		{
			_mapSectionHelper = mapSectionHelper;
			_projectAdapter = repositoryAdapters.ProjectAdapter;
			_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;
			_sharedColorBandSetAdapter = repositoryAdapters.SharedColorBandSetAdapter;

			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			_mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor:10, RMapConstants.BLOCK_SIZE);

			_mapLoaderManager = mapLoaderManager;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_viewModelFactory = new ViewModelFactory(_projectAdapter, _mapSectionAdapter, _sharedColorBandSetAdapter, _mapLoaderManager);
		}

		public ExplorerViewModel GetExplorerViewModel()
		{
			// Project ViewModel
			var projectViewModel = new ProjectViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapJobHelper, _mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);

			var result = new ExplorerViewModel(projectViewModel, mapDisplayViewModel, colorBandSetViewModel, colorBandSetHistogramViewModel, jobTreeViewModel,
				_mapLoaderManager, _viewModelFactory);

			return result;
		}

		public PosterDesignerViewModel GetPosterDesignerViewModel()
		{
			// Poster ViewModel
			var posterViewModel = new PosterViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapJobHelper, _mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);

			var result = new PosterDesignerViewModel(posterViewModel, mapDisplayViewModel/* mapScrollViewModel*/, colorBandSetViewModel, colorBandSetHistogramViewModel, jobTreeViewModel,
				_mapJobHelper, _mapLoaderManager, _viewModelFactory);

			return result;
		}

		public XSamplingEditorViewModel GetXSamplingEditorViewModel()
		{
			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			var result = new XSamplingEditorViewModel(subdivisionProvider);
			return result;
		}

		public PerformanceHarnessMainWinViewModel GetPerformanceHarnessMainWinViewModel()
		{
			// Project ViewModel
			//var projectViewModel = new ProjectViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			//IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			//var histogram = new HistogramA(0);
			//var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			//var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			//var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			//var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);
			//var result = new ExplorerViewModel(projectViewModel, mapDisplayViewModel, colorBandSetViewModel, colorBandSetHistogramViewModel, jobTreeViewModel,
			//	_mapLoaderManager,
			//	CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel, CreateAPosterOpenSaveViewModel, CreateACoordsEditorViewModel);

			var result = new PerformanceHarnessMainWinViewModel(_mapSectionRequestProcessor, _mapJobHelper, _mapSectionHelper);
			return result;
		}

		private long? DropRecentMapSections(IMapSectionDeleter mapSectionDeleter)
		{
			var lastSaved = DateTime.Parse("2022-05-29");
			var result = mapSectionDeleter.DeleteMapSectionsCreatedSince(lastSaved, overrideRecentGuard: true);
			return result;

		}

		#region Utilities

		//// Remove FetchZValuesFromRepo property from MapSections
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((MapSectionAdapter)_mapSectionAdapter).RemoveFetchZValuesProp();
		//	return numUpdated;
		//}

		//// Update all Job Records to use MapAreaInfo
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((ProjectAdapter)_projectAdapter).UpdateAllJobsToUseMapAreaInfoRec1();
		//	return numUpdated;
		//}

		//// Remove all (orphaned) Job Records. (I.e., those without a Project.)
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((ProjectAdapter)_projectAdapter).UpdateAllJobsToUseMapAreaInfoRec2();
		//	return numUpdated;
		//}

		//// Update all Job Records to have a MapCalcSettings
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((ProjectAdapter)_projectAdapter).UpdateAllJobsToHaveMapCalcSettings();
		//	return numUpdated;
		//}

		//// Update all Job Records to have a MapCalcSettings
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((ProjectAdapter)_projectAdapter).RemoveFetchZValuesPropFromAllJobs();
		//	numUpdated += ((ProjectAdapter)_projectAdapter).RemoveFetchZValuesPropFromAllJobs2();
		//	return numUpdated;
		//}

		// Remove the old properties that are now part of the MapAreaInfo record
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((ProjectAdapter)_projectAdapter).RemoveOldMapAreaPropsFromAllJobs();
		//	return numUpdated;
		//}


		#endregion

	}
}
