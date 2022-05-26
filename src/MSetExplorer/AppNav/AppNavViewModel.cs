﻿using MSS.Common;
using System;

namespace MSetExplorer
{
	public class AppNavViewModel
	{
		public RepositoryAdapters RepositoryAdapters { get; init; }
		public IMapLoaderManager MapLoaderManager { get; init; }

		public AppNavViewModel(RepositoryAdapters repositoryAdapters, IMapLoaderManager mapLoaderManager)
		{
			RepositoryAdapters = repositoryAdapters;
			MapLoaderManager = mapLoaderManager;
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

			var result = new ExplorerViewModel(mapProjectViewModel, mapDisplayViewModel, colorBandViewModel, CreateAProjectOpenSaveViewModel, CreateACbsOpenSaveViewModel);

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
