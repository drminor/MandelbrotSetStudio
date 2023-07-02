using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
using MSS.Types;
using MSS.Common.MSet;
using System.Windows;
using System;
using MongoDB.Bson;
using ImageBuilder;
using System.Windows.Media;
using PngImageLib;

namespace MSetExplorer
{
	internal delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	internal delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	//internal delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, DialogType dialogType, Func<long, Job, SizeDbl>? deleteNonEssentialMapSectionsFunction);

	internal delegate CoordsEditorViewModel CoordsEditorViewModelCreator(MapAreaInfo2 mapAreaInfo2, SizeDbl canvasSize, bool allowEdits);

	public class ViewModelFactory
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;

		public ViewModelFactory(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, SharedColorBandSetAdapter sharedColorBandSetAdapter, IMapLoaderManager mapLoaderManager)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_sharedColorBandSetAdapter = sharedColorBandSetAdapter;

			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			_mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, RMapConstants.BLOCK_SIZE);

			_mapLoaderManager = mapLoaderManager;
		}

		// Project Open/Save
		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, initalName, dialogType);
		}

		// Poster Open/Save
		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType, Func<Job, SizeDbl, bool, long>? deleteNonEssentialMapSectionsFunction, ViewModelFactory viewModelFactory)
		{
			return new PosterOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, viewModelFactory, deleteNonEssentialMapSectionsFunction, initalName, dialogType);
		}

		// JobDetils
		public JobDetailsViewModel CreateAJobDetailsDialog(ObjectId ownerId, OwnerType ownerType, ObjectId? initialJobId, Func<Job, SizeDbl, bool, long>? deleteNonEssentialMapSectionsFunction)
		{
			return new JobDetailsViewModel(_projectAdapter, _mapSectionAdapter, deleteNonEssentialMapSectionsFunction, ownerId, ownerType, initialJobId);
		}

		// Import/Export ColorBandSet
		public IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		// Coords Editor
		public CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfoV2, SizeDbl canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(_mapJobHelper, mapAreaInfoV2, canvasSize, allowEdits);
			return result;
		}

		// Create Image Progress 
		public CreateImageProgressViewModel CreateACreateImageProgressViewModel()
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder, mapJobHelper);
			return result;
		}


		//var createImageProgressViewModel = _vm.CreateACreateImageProgressViewModel(/*imageFilePath, areaColorAndCalcSettings, imageSize*/);

		//var mapAreaInfoWithSize = _vm.GetMapAreaWithSizeFat(areaColorAndCalcSettings.MapAreaInfo, imageSize);
		//var jobId = new ObjectId(areaColorAndCalcSettings.JobId);
		//createImageProgressViewModel.CreateImage(imageFilePath, jobId, OwnerType.Poster, mapAreaInfoWithSize, areaColorAndCalcSettings.ColorBandSet, areaColorAndCalcSettings.MapCalcSettings);



		// Poster Size Editor Preview
		public LazyMapPreviewImageProvider GetPreviewImageProvider(ObjectId jobId, MapAreaInfo2 mapAreaInfo, SizeDbl previewImagesize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, 
			bool useEscapeVelocitites, Color fallbackColor)
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);
			var result = new LazyMapPreviewImageProvider(mapJobHelper, bitmapBuilder, jobId, OwnerType.Poster, mapAreaInfo, previewImagesize, colorBandSet, mapCalcSettings, useEscapeVelocitites, fallbackColor);
			return result;
		}

		// Project - Job Progress Component
		public JobProgressViewModel CreateAJobProgressViewModel()
		{
			var result = new JobProgressViewModel(_mapLoaderManager);
			return result;
		}

		public MapJobHelper ProvisionAMapJopHelper()
		{
			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			var mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, RMapConstants.BLOCK_SIZE);

			return mapJobHelper;
		}

	}
}
