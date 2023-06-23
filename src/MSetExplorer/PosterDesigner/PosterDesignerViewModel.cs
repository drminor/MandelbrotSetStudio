using ImageBuilder;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class PosterDesignerViewModel : ViewModelBase, IPosterDesignerViewModel
	{
		//private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly ViewModelFactory _viewModelFactory;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ICbshDisplayViewModel cbshDisplayViewModel, IJobTreeViewModel jobTreeViewModel,
			IMapLoaderManager mapLoaderManager, ViewModelFactory viewModelFactory)
		{
			//_mapJobHelper = mapJobHelper;
			_mapLoaderManager = mapLoaderManager;

			PosterViewModel = posterViewModel;
			JobTreeViewModel = jobTreeViewModel;

			MapDisplayViewModel = mapDisplayViewModel;

			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;
			
			
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;


			_viewModelFactory = viewModelFactory;

			MapCoordsViewModel = new MapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;

			CbshDisplayViewModel = cbshDisplayViewModel;
		}

		#endregion

		#region Public Properties

		public IPosterViewModel PosterViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }

		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; }
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

		//public ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }
		public ICbshDisplayViewModel CbshDisplayViewModel { get; }

		public ViewModelFactory ViewModelFactory => _viewModelFactory;

		//public SizeDbl MapDisplaySize
		//{
		//	get => _mapDisplaySize;
		//	set
		//	{
		//		if (value != _mapDisplaySize)
		//		{
		//			//var prev = _mapDisplaySize;
		//			_mapDisplaySize = value;
		//			//Debug.WriteLine($"Raising the OnPropertyChanged for the MapDisplaySize. Old = {prev}, new = {value}.");
		//			OnPropertyChanged(nameof(IPosterDesignerViewModel.MapDisplaySize));
		//		}
		//		else
		//		{
		//			//Debug.WriteLine($"Not raising the OnPropertyChanged for the MapDisplaySize, the value {value} is unchanged.");
		//		}
		//	}
		//}

		#endregion

		#region Public Methods

		public CreateImageProgressViewModel CreateACreateImageProgressViewModel()
		{
			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder);
			return result;
		}

		public LazyMapPreviewImageProvider GetPreviewImageProvider(MapAreaInfo2 mapAreaInfo, SizeInt previewImagesize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocitites, Color fallbackColor)
		{
			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);
			var result = new LazyMapPreviewImageProvider(bitmapBuilder, mapAreaInfo, previewImagesize, colorBandSet, mapCalcSettings, useEscapeVelocitites, fallbackColor);
			return result;
		}

		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea)
		{
			var result = PosterViewModel.GetUpdatedMapAreaInfo(mapAreaInfo, currentPosterSize, newPosterSize, screenArea);
			return result;
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Update the MapCalcSettings, MapCoords and Map Display with the new Job Area and Calc Settings
			if (e.PropertyName == nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings))
			{
				RunCurrentJob();
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IPosterViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;

				MapDisplayViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;
			}
		}

		private void UpdateTheMapCoordsView(Job currentJob)
		{
			var oldAreaInfo = MapDisplayViewModel.LastMapAreaInfo;

			if (oldAreaInfo != null)
			{
				MapCoordsViewModel.JobId = currentJob.Id.ToString();
				MapCoordsViewModel.CurrentMapAreaInfo = oldAreaInfo;
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
			{
				MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
			}

			else if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
			{
				MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
			}

			else if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
			{
				if (MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null)
				{
					MapDisplayViewModel.CurrentColorBand = ColorBandSetViewModel.CurrentColorBand;
				}
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			if (e.IsPreview)
			{
				// Calculate new Coords for preview

				//// TODO: After testing is complete, uncomment-out this code.
				//var mapAreaInfo = ProjectViewModel.GetUpdatedMapAreaInfo(e.TransformType, e.ScreenArea, e.CurrentMapAreaInfo);
				//if (mapAreaInfo != null)
				//{
				//	MapCoordsViewModel.Preview(mapAreaInfo);
				//}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				PosterViewModel.UpdateMapSpecs(e.TransformType, e.PanAmount, e.Factor, e.CurrentMapAreaInfo);
			}
		}

		private void MapDisplayViewModel_DisplayJobCompleted(object? sender, int e)
		{
			ColorBandSetViewModel.RefreshPercentages();
			CbshDisplayViewModel.RefreshHistogramDisplay();
		}

		private void MapCalcSettingsViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			// Update the Target Iterations
			if (e.MapSettingsUpdateType == MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.ApplyChanges(e.TargetIterations);
			}
		}

		private void ColorBandSetViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			if (e.IsPreview)
			{
				Debug.WriteLine($"MainWindow got a CBS preview with Id = {colorBandSet.Id}");
				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
				PosterViewModel.PreviewColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLine($"MainWindow got a CBS update with Id = {colorBandSet.Id}");
				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				PosterViewModel.CurrentColorBandSet = colorBandSet;
			}
		}

		public void RunCurrentJob()
		{
			var areaColorAndCalcSettings = PosterViewModel.CurrentAreaColorAndCalcSettings;

			MapCalcSettingsViewModel.MapCalcSettings = areaColorAndCalcSettings.MapCalcSettings;

			// Update the MapDisplayView model for a new Poster or Poster Job.
			var currentPoster = PosterViewModel.CurrentPoster;

			if (currentPoster != null)
			{
				var currentjob = currentPoster.CurrentJob;
				if (currentjob != null)
				{
					UpdateTheMapCoordsView(currentjob);
					var posterSize = currentPoster.PosterSize;

					var displayPosition = currentPoster.DisplayPosition;
					var displayZoom = currentPoster.DisplayZoom;

					MapDisplayViewModel.SubmitJob(areaColorAndCalcSettings, posterSize, displayPosition, displayZoom);
				}
			}
		}

		#endregion
	}
}
