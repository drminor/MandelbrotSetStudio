using ImageBuilder;
using ImageBuilderWPF;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Windows.Media;

namespace MSetExplorer
{
	//internal delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	//internal delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	//internal delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	//internal delegate CoordsEditorViewModel CoordsEditorViewModelCreator(MapCenterAndDelta mapCenterAndDelta, SizeDbl canvasSize, bool allowEdits);


	public class ViewModelFactory
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private readonly SizeInt _blockSize;

		public ViewModelFactory(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, SharedColorBandSetAdapter sharedColorBandSetAdapter, IMapLoaderManager mapLoaderManager, 
			MapSectionVectorProvider mapSectionVectorProvider, SizeInt blockSize)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_sharedColorBandSetAdapter = sharedColorBandSetAdapter;
			_mapLoaderManager = mapLoaderManager;
			_mapSectionVectorProvider = mapSectionVectorProvider;

			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			_mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, _blockSize);
		}

		// Project Open/Save
		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, initalName, dialogType);
		}

		// Poster Open/Save
		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var viewModelFactory = this;
			return new PosterOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, viewModelFactory, initalName, dialogType);
		}

		// JobDetails
		public JobDetailsViewModel CreateAJobDetailsDialog(IJobOwnerInfo jobOwnerInfo)
		{
			return new JobDetailsViewModel(jobOwnerInfo, _projectAdapter, _mapSectionAdapter);
		}

		// Open/Save ColorBandSet
		public IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType, IEnumerable<ColorBandSetInfo> cbsInfos)
		{
			return new ColorBandSetOpenSaveViewModel(_projectAdapter, initalName, dialogType, cbsInfos);
		}

		// Open/Save ColorBandSet
		public IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(ObjectId projectId, string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_projectAdapter, projectId, initalName, dialogType);
		}

		// Import/Export ColorBandSet
		public IColorBandSetImportExportViewModel CreateACbsImportExportViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetImportExportViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		// Coords Editor
		public CoordsEditorViewModel CreateACoordsEditorViewModel(MapCenterAndDelta mapAreaInfoV2, SizeDbl canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(_mapJobHelper, mapAreaInfoV2, canvasSize, allowEdits);
			return result;
		}

		// Coords Editor
		public CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeDbl canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(_mapJobHelper, coords, canvasSize, allowEdits);
			return result;
		}

		// Create Image Progress 
		public CreateImageProgressViewModel CreateACreateImageProgressViewModel(ImageFileType imageFileType)
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			// TODO: Start using IImageBuilderWPF.
			// TODO: Support additional Image File Types.
			
			IImageBuilder imageBuilder;

			if (imageFileType == ImageFileType.PNG)
			{
				imageBuilder = new PngBuilder(_mapLoaderManager, _mapSectionVectorProvider);
			}
			else
			{
				imageBuilder = new WmpBuilder(_mapLoaderManager, _mapSectionVectorProvider);
			}

			var result = new CreateImageProgressViewModel(imageBuilder, mapJobHelper);
			
			return result;
		}

		// Poster Size Editor Preview
		public LazyMapPreviewImageProvider GetPreviewImageProvider(AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl posterSize, bool useEscapeVelocitites, Color fallbackColor)
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			//var bitmapBuilder = new BitmapBuilder(_mapLoaderManager, _mapSectionVectorProvider);
			var imageDataBuilder = new ImageSourceBuilder(_mapLoaderManager, _mapSectionVectorProvider);

			var result = new LazyMapPreviewImageProvider(areaColorAndCalcSettings, posterSize, useEscapeVelocitites, fallbackColor, mapJobHelper, imageDataBuilder);
			return result;
		}

		// Project - Job Progress Component
		public JobProgressViewModel CreateAJobProgressViewModel()
		{
			var result = new JobProgressViewModel(_mapLoaderManager);
			return result;
		}

		public MapCoordsViewModel CreateAMapCoordsViewModel()
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			var result = new MapCoordsViewModel(mapJobHelper);
			return result;
		}


		public MapJobHelper ProvisionAMapJopHelper()
		{
			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			var mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, _blockSize);

			return mapJobHelper;
		}

	}
}
