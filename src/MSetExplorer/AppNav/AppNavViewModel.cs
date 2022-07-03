using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	public delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	public delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	public delegate CoordsEditorViewModel CoordsEditorViewModelCreator(RRectangle coords, SizeInt canvasSize, bool allowEdits);

	public class AppNavViewModel
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;

		public AppNavViewModel(RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager)
		{
			_projectAdapter = repositoryAdapters.ProjectAdapter;
			_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;
			_sharedColorBandSetAdapter = repositoryAdapters.SharedColorBandSetAdapter;
			_mapJobHelper = new MapJobHelper(repositoryAdapters.MapSectionAdapter);
			_mapLoaderManager = mapLoaderManager;
		}

		public ExplorerViewModel GetExplorerViewModel()
		{
			// Map Project ViewModel
			var mapProjectViewModel = new MapProjectViewModel(_projectAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			var result = new ExplorerViewModel(mapProjectViewModel, mapDisplayViewModel, colorBandSetViewModel, colorBandSetHistogramViewModel,
				_mapLoaderManager,
				CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel, CreateAPosterOpenSaveViewModel, CreateACoordsEditorViewModel);

			return result;
		}

		public PosterDesignerViewModel GetPosterDesignerViewModel()
		{
			// Poster ViewModel
			var posterViewModel = new PosterViewModel(_projectAdapter);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(_mapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			IMapScrollViewModel mapScrollViewModel = new MapScrollViewModel(mapDisplayViewModel);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections, mapSectionHistogramProcessor);

			// ColorBand Histogram ViewModel
			var colorBandSetHistogramViewModel = new ColorBandSetHistogramViewModel(mapSectionHistogramProcessor);

			var result = new PosterDesignerViewModel(posterViewModel, mapScrollViewModel, colorBandSetViewModel, colorBandSetHistogramViewModel,
				_mapJobHelper, _mapLoaderManager, 
				CreateAPosterOpenSaveViewModel, CreateACbsOpenSaveViewModel, CreateACoordsEditorViewModel);

			return result;
		}

		private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, initalName, dialogType);
		}

		private IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		private IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new PosterOpenSaveViewModel(_mapLoaderManager, _projectAdapter, initalName, dialogType);
		}

		private CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(coords, canvasSize, allowEdits, _mapJobHelper);
			return result;
		}


		#region Utilities

		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc)
		{
			var numberOfRecordsDeleted = _mapSectionAdapter.DeleteMapSectionsCreatedSince(dateCreatedUtc, overrideRecentGuard: true);
			return numberOfRecordsDeleted;
		}

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
		public long? DoSchemaUpdates()
		{
			var numUpdated = ((ProjectAdapter)_projectAdapter).RemoveOldMapAreaPropsFromAllJobs();
			return numUpdated;
		}


		#endregion

	}
}
