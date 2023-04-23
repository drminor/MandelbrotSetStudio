using MapSectionProviderLib;
using MSetExplorer.MapDisplay.ScrollAndZoom;
using MSetExplorer.XPoc;
using MSetExplorer.XPoc.PerformanceHarness;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	internal delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	internal delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	internal delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, bool useEscapeVelocities, DialogType dialogType);

	internal delegate CoordsEditorViewModel CoordsEditorViewModelCreator(MapAreaInfo2 mapAreaInfo2, SizeInt canvasSize, bool allowEdits);

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
				_mapLoaderManager,
				CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel, CreateAPosterOpenSaveViewModel, CreateACoordsEditorViewModel);

			return result;
		}

		public PosterDesignerViewModel GetPosterDesignerViewModel()
		{
			// Poster ViewModel
			var posterViewModel = new PosterViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapJobHelper, _mapSectionHelper, RMapConstants.BLOCK_SIZE);

			IMapScrollViewModel mapScrollViewModel = new MapScrollViewModel(mapDisplayViewModel);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);

			var result = new PosterDesignerViewModel(posterViewModel, mapScrollViewModel, colorBandSetViewModel, colorBandSetHistogramViewModel, jobTreeViewModel,
				_mapJobHelper, _mapLoaderManager,
				CreateAPosterOpenSaveViewModel, CreateACbsOpenSaveViewModel, CreateACoordsEditorViewModel);

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

		private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, initalName, dialogType);
		}

		private IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		private IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType)
		{
			return new PosterOpenSaveViewModel(_mapLoaderManager, _projectAdapter, _mapSectionAdapter, initalName, useEscapeVelocities, dialogType);
		}

		private CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfoV2, SizeInt canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(mapAreaInfoV2, canvasSize, allowEdits);
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
