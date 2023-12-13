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
		#region Private Fields

		private static readonly SizeInt _blockSize = RMapConstants.BLOCK_SIZE;
		private static readonly bool _useSimpleJobTree = true;

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly ViewModelFactory _viewModelFactory;

		#endregion

		#region Constructor

		public AppNavViewModel(MapSectionVectorProvider mapSectionVectorProvider, RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_projectAdapter = repositoryAdapters.ProjectAdapter;
			_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;
			_sharedColorBandSetAdapter = repositoryAdapters.SharedColorBandSetAdapter;

			_mapJobHelper = mapJobHelper;

			_mapLoaderManager = mapLoaderManager;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_viewModelFactory = new ViewModelFactory(_projectAdapter, _mapSectionAdapter, _sharedColorBandSetAdapter, _mapLoaderManager, _mapSectionVectorProvider, _blockSize);
		}

		#endregion

		#region Public Methods

		public ExplorerViewModel GetExplorerViewModel()
		{
			// Project ViewModel
			var projectViewModel = new ProjectViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper);

			// Map Display View Model
			IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapSectionVectorProvider, _mapJobHelper, _blockSize);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram, mapDisplayViewModel.MapSections);
			//var colorBandSetViewModel = new ColorBandSetViewModel(mapSectionHistogramProcessor);

			// ColorBandSet Histogram ViewModel
			var cbsHistogramViewModel = new CbsHistogramViewModel(mapSectionHistogramProcessor);	

			var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);

			var result = new ExplorerViewModel(projectViewModel, mapDisplayViewModel/*, colorBandSetViewModel*/, cbsHistogramViewModel, jobTreeViewModel,
				//_mapLoaderManager, _mapJobHelper,
				mapSectionHistogramProcessor, _viewModelFactory);

			return result;
		}

		public PosterDesignerViewModel GetPosterDesignerViewModel()
		{
			// Poster ViewModel
			var posterViewModel = new PosterViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper);

			// Map Display View Model
			IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapSectionVectorProvider, _mapJobHelper, _blockSize);

			// ColorBand ViewModel
			var histogram = new HistogramA(0);
			var mapSectionHistogramProcessor = new MapSectionHistogramProcessor(histogram, mapDisplayViewModel.MapSections);
			var colorBandSetViewModel = new ColorBandSetViewModel(mapSectionHistogramProcessor);

			// ColorBandSet Histogram ViewModel
			var cbsHistogramViewModel = new CbsHistogramViewModel(mapSectionHistogramProcessor);

			var jobTreeViewModel = new JobTreeViewModel(_projectAdapter, _mapSectionAdapter, _useSimpleJobTree);

			var result = new PosterDesignerViewModel(posterViewModel, mapDisplayViewModel/*, colorBandSetViewModel*/, cbsHistogramViewModel, jobTreeViewModel,
				//_mapLoaderManager, _mapJobHelper,
				mapSectionHistogramProcessor, _viewModelFactory);

			return result;
		}

		public XSamplingEditorViewModel GetXSamplingEditorViewModel()
		{
			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			var result = new XSamplingEditorViewModel(subdivisionProvider);
			return result;
		}

		public SamplePointDeltaTestViewModel GetPointDeltaTestViewModel()
		{
			var result = new SamplePointDeltaTestViewModel();
			return result;
		}

		public PerformanceHarnessMainWinViewModel GetPerformanceHarnessMainWinViewModel()
		{
			// Project ViewModel
			//var projectViewModel = new ProjectViewModel(_projectAdapter, _mapSectionAdapter, _mapJobHelper, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			//IMapDisplayViewModel mapDisplayViewModel = new MapSectionDisplayViewModel(_mapLoaderManager, _mapSectionBuilder, RMapConstants.BLOCK_SIZE);

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

			var result = new PerformanceHarnessMainWinViewModel(_mapSectionRequestProcessor, _mapJobHelper, _mapSectionVectorProvider);
			return result;
		}

		//public StorageModelPOC GetStorageModelPOC()
		//{
		//	if (_projectAdapter is ProjectAdapter pa && _mapSectionAdapter is MapSectionAdapter ma)
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

	}
}
