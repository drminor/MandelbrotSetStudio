using ImageBuilder;
using MSS.Common;
using System;

namespace MSetExplorer
{
	public delegate IProjectOpenSaveViewModel ProjectOpenSaveViewModelCreator(string? initialName, DialogType dialogType);
	public delegate IColorBandSetOpenSaveViewModel CbsOpenSaveViewModelCreator(string? initialName, DialogType dialogType);

	public class AppNavViewModel
	{
		public RepositoryAdapters RepositoryAdapters { get; init; }
		public IMapLoaderManager MapLoaderManager { get; init; }
		public PngBuilder PngBuilder { get; init; }

		public AppNavViewModel(RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager, PngBuilder pngBuilder)
		{
			RepositoryAdapters = repositoryAdapters;
			MapLoaderManager = mapLoaderManager;
			PngBuilder = pngBuilder;
		}

		public ExplorerViewModel GetExplorerViewModel()
		{
			// Map Project ViewModel
			var mapProjectViewModel = new MapProjectViewModel(RepositoryAdapters.ProjectAdapter, RMapConstants.BLOCK_SIZE);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(MapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			// ColorBand ViewModel
			var colorBandViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections);

			var result = new ExplorerViewModel(mapProjectViewModel, mapDisplayViewModel, colorBandViewModel, RepositoryAdapters.ProjectAdapter, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel);

			return result;
		}

		public PosterDesignerViewModel GetPosterDesignerViewModel()
		{
			// Poster ViewModel
			var posterViewModel = new PosterViewModel(RepositoryAdapters.ProjectAdapter);

			// Map Display View Model
			var mapSectionHelper = new MapSectionHelper();
			IMapDisplayViewModel mapDisplayViewModel = new MapDisplayViewModel(MapLoaderManager, mapSectionHelper, RMapConstants.BLOCK_SIZE);

			IMapScrollViewModel mapScrollViewModel = new MapScrollViewModel(mapDisplayViewModel);

			// ColorBand ViewModel
			var colorBandViewModel = new ColorBandSetViewModel(mapDisplayViewModel.MapSections);

			var result = new PosterDesignerViewModel(posterViewModel, mapScrollViewModel, colorBandViewModel, RepositoryAdapters.ProjectAdapter, PngBuilder, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel);

			return result;
		}

		private IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return RepositoryAdapters.ProjectAdapter == null
				? throw new InvalidOperationException("Cannot create a Project OpenSave ViewModel, the ProjectAdapter is null.")
				: new ProjectOpenSaveViewModel(RepositoryAdapters.ProjectAdapter, initalName, dialogType);
		}

		private IColorBandSetOpenSaveViewModel CreateACbsOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			return RepositoryAdapters.SharedColorBandSetAdapter == null
				? throw new InvalidOperationException("Cannot create a ColorBandSet OpenSave ViewModel, the Shared ColorBandSet Adapter is null.")
				: new ColorBandSetOpenSaveViewModel(RepositoryAdapters.SharedColorBandSetAdapter, initalName, dialogType);
		}


	}
}
