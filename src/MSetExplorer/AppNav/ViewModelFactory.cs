using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
using MSS.Types;
using MSS.Common.MSet;
using System.Windows;

namespace MSetExplorer
{
	internal delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	internal delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	internal delegate IPosterOpenSaveViewModel PosterOpenSaveViewModelCreator(string? initialName, bool useEscapeVelocities, DialogType dialogType);

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

		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ProjectOpenSaveViewModel(_projectAdapter, _mapSectionAdapter, initalName, dialogType);
		}

		public IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return new ColorBandSetOpenSaveViewModel(_sharedColorBandSetAdapter, initalName, dialogType);
		}

		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType)
		{
			return new PosterOpenSaveViewModel(_mapLoaderManager, _projectAdapter, _mapSectionAdapter, initalName, useEscapeVelocities, dialogType);
		}

		public CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfoV2, SizeDbl canvasSize, bool allowEdits)
		{
			var result = new CoordsEditorViewModel(_mapJobHelper, mapAreaInfoV2, canvasSize, allowEdits);
			return result;
		}

	}
}
