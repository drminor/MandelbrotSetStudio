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
	internal delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	internal delegate CoordsEditorViewModel CoordsEditorViewModelCreator(MapCenterAndDelta mapCenterAndDelta, SizeDbl canvasSize, bool allowEdits);


	public class ViewModelFactory
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SharedColorBandSetAdapter _sharedColorBandSetAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;

		private readonly SizeInt _blockSize;

		public ViewModelFactory(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, SharedColorBandSetAdapter sharedColorBandSetAdapter, IMapLoaderManager mapLoaderManager, SizeInt blockSize)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_sharedColorBandSetAdapter = sharedColorBandSetAdapter;
			_mapLoaderManager = mapLoaderManager;

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

		// Import/Export ColorBandSet
		public IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		// Coords Editor
		public CoordsEditorViewModel CreateACoordsEditorViewModel(MapCenterAndDelta mapAreaInfoV2, SizeDbl canvasSize, bool allowEdits)
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


		// Poster Size Editor Preview
		public LazyMapPreviewImageProvider GetPreviewImageProvider(ObjectId jobId, MapCenterAndDelta mapAreaInfo, SizeDbl posterSize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, 
			bool useEscapeVelocitites, Color fallbackColor)
		{
			var mapJobHelper = ProvisionAMapJopHelper();

			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);
			var result = new LazyMapPreviewImageProvider(mapJobHelper, bitmapBuilder, jobId, OwnerType.Poster, mapAreaInfo, posterSize, colorBandSet, mapCalcSettings, useEscapeVelocitites, fallbackColor);
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
