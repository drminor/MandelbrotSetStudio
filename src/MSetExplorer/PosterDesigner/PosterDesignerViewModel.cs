using ImageBuilder;
using MSetExplorer.MapDisplay.ScrollAndZoom;
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
		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly PosterOpenSaveViewModelCreator _posterOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;
		private readonly CoordsEditorViewModelCreator _coordsEditorViewModelCreator;

		private SizeDbl _mapDisplaySize;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, /*IMapScrollViewModel mapScrollViewModel*/ IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
			MapJobHelper mapJobHelper, IMapLoaderManager mapLoaderManager, PosterOpenSaveViewModelCreator posterOpenSaveViewModelCreator, 
			CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator, CoordsEditorViewModelCreator coordsEditorViewModelCreator)
		{
			_mapJobHelper = mapJobHelper;
			_mapLoaderManager = mapLoaderManager;

			PosterViewModel = posterViewModel;
			JobTreeViewModel = jobTreeViewModel;

			//MapScrollViewModel = mapScrollViewModel;
			MapDisplayViewModel = mapDisplayViewModel;

			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;
			
			//MapScrollViewModel.PropertyChanged += MapScrollViewModel_PropertyChanged;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;

			MapDisplaySize = MapDisplayViewModel.ViewPortSize;

			_posterOpenSaveViewModelCreator = posterOpenSaveViewModelCreator;
			_cbsOpenSaveViewModelCreator = cbsOpenSaveViewModelCreator;
			_coordsEditorViewModelCreator = coordsEditorViewModelCreator;

			MapCoordsViewModel = new MapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;

			ColorBandSetHistogramViewModel = colorBandSetHistogramViewModel;
		}

		#endregion

		#region Public Properties

		public IPosterViewModel PosterViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }

		//public IMapScrollViewModel MapScrollViewModel { get; }
		//public IMapDisplayViewModel MapDisplayViewModel => MapScrollViewModel.MapDisplayViewModel;

		public IMapDisplayViewModel MapDisplayViewModel { get; }

		//public IMapDisplayViewModel MapScrollViewModel { get; }


		public MapCoordsViewModel MapCoordsViewModel { get; }
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		public SizeDbl MapDisplaySize
		{
			get => _mapDisplaySize;
			set
			{
				if (value != _mapDisplaySize)
				{
					//var prev = _mapDisplaySize;
					_mapDisplaySize = value;
					//Debug.WriteLine($"Raising the OnPropertyChanged for the MapDisplaySize. Old = {prev}, new = {value}.");
					OnPropertyChanged(nameof(IPosterDesignerViewModel.MapDisplaySize));
				}
				else
				{
					//Debug.WriteLine($"Not raising the OnPropertyChanged for the MapDisplaySize, the value {value} is unchanged.");
				}
			}
		}

		#endregion

		#region Public Methods

		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType)
		{
			var result = _posterOpenSaveViewModelCreator(initalName, useEscapeVelocities, dialogType);
			return result;
		}

		public IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType)
		{
			var result = _cbsOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public CreateImageProgressViewModel CreateACreateImageProgressViewModel()
		{
			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder);
			return result;
		}

		public CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfo2, SizeInt canvasSize, bool allowEdits)
		{
			var result = _coordsEditorViewModelCreator(mapAreaInfo2, canvasSize, allowEdits);
			return result;
		}

		public LazyMapPreviewImageProvider GetPreviewImageProvider (MapAreaInfo2 mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocitites, SizeInt previewImagesize, Color fallbackColor)
		{
			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);
			var result = new LazyMapPreviewImageProvider(bitmapBuilder, _mapJobHelper, mapAreaInfo, previewImagesize, colorBandSet, mapCalcSettings, useEscapeVelocitites, fallbackColor);
			return result;
		}

		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeInt posterSize, VectorInt offsetFromCenter, RectangleDbl screenArea, SizeDbl newMapSize)
		{
			var result = PosterViewModel.GetUpdatedMapAreaInfo(mapAreaInfo, posterSize, offsetFromCenter, screenArea, newMapSize);
			return result;
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPosterViewModel.CurrentJob))
			{
				//MapDisplayViewModel.PosterSize = PosterViewModel.PosterSize;
			}

			//else if (e.PropertyName == nameof(IPosterViewModel.LogicalDisplaySize))
			//{
			//	MapDisplayViewModel.PosterSize = PosterViewModel.PosterSize;
			//	MapDisplayViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
			//	MapDisplayViewModel.HorizontalPosition = PosterViewModel.DisplayPosition.X;
			//	MapDisplayViewModel.InvertedVerticalPosition = PosterViewModel.DisplayPosition.Y;
			//}

			//// Update the MapCalcSettings, MapCoords and Map Display with the new Poster
			//else if (e.PropertyName == nameof(IPosterViewModel.CurrentPoster))
			//{
			//	var curPoster = PosterViewModel.CurrentPoster;

			//	if (curPoster != null)
			//	{
			//		MapDisplayViewModel.PosterSize = PosterViewModel.PosterSize;
			//		MapDisplayViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
			//		MapDisplayViewModel.HorizontalPosition = PosterViewModel.DisplayPosition.X;
			//		MapDisplayViewModel.InvertedVerticalPosition = PosterViewModel.DisplayPosition.Y;
			//	}
			//	else
			//	{
			//		//MapDisplayViewModel.PosterSize = null;

			//		// Let the MapDisplay know to stop any current MapLoader job.
			//		MapDisplayViewModel.CancelJob();
			//	}
			//}

			// Update the MapCalcSettings, MapCoords and Map Display with the new Job Area and Calc Settings
			else if (e.PropertyName == nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings))
			{
				var areaColorAndCalcSettings = PosterViewModel.CurrentAreaColorAndCalcSettings;

				MapCalcSettingsViewModel.MapCalcSettings = areaColorAndCalcSettings.MapCalcSettings;

				// UPDATE THE MapDisplayView model for a new Poster or Poster Job.

				var currentPoster = PosterViewModel.CurrentPoster;

				if (currentPoster != null)
				{
					var currentjob = currentPoster.CurrentJob;
					if (currentjob != null)
					{
						UpdateTheMapCoordsView(currentjob);
						var posterSize = currentPoster.PosterSize;
						MapDisplayViewModel.SubmitJob(areaColorAndCalcSettings, posterSize);
					}
				}

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

		private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Let the Map Project know about Map Display size changes
			if (e.PropertyName == nameof(IMapDisplayViewModel.ViewPortSize))
			{
				MapDisplaySize = MapDisplayViewModel.ViewPortSize;
				//PosterViewModel.CanvasSize = MapDisplayViewModel.ViewPortSize;
			}

			//else if (e.PropertyName == nameof(IMapDisplayViewModel.LogicalDisplaySize))
			//{
			//	PosterViewModel.DisplayZoom = MapDisplayViewModel.DisplayZoom;
			//	PosterViewModel.LogicalDisplaySize = MapDisplayViewModel.LogicalDisplaySize;
			//}

			if (e.PropertyName == nameof(IMapDisplayViewModel.HorizontalPosition))
			{
				PosterViewModel.DisplayPosition = new VectorInt((int)Math.Round(MapDisplayViewModel.HorizontalPosition), PosterViewModel.DisplayPosition.Y);
			}

			else if (e.PropertyName == nameof(IMapDisplayViewModel.InvertedVerticalPosition))
			{
				PosterViewModel.DisplayPosition = new VectorInt(PosterViewModel.DisplayPosition.X, (int)Math.Round(MapDisplayViewModel.InvertedVerticalPosition));
			}

			//else if (e.PropertyName == nameof(IMapScrollViewModel.DisplayZoom))
			//{
			//	PosterViewModel.DisplayZoom = MapDisplayViewModel.DisplayZoom;
			//}

		}

		//private void MapScrollViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(IMapScrollViewModel.HorizontalPosition))
		//	{
		//		PosterViewModel.DisplayPosition = new VectorInt((int)Math.Round(MapDisplayViewModel.HorizontalPosition), PosterViewModel.DisplayPosition.Y);
		//	}

		//	else if (e.PropertyName == nameof(IMapScrollViewModel.InvertedVerticalPosition))
		//	{
		//		PosterViewModel.DisplayPosition = new VectorInt(PosterViewModel.DisplayPosition.X, (int)Math.Round(MapDisplayViewModel.InvertedVerticalPosition));
		//	}

		//	else if (e.PropertyName == nameof(IMapScrollViewModel.DisplayZoom))
		//	{
		//		PosterViewModel.DisplayZoom = MapDisplayViewModel.DisplayZoom;
		//	}
		//}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			// TODO: Verify that the Poster Designer will not be handling MapView Updates
		}

		private void MapDisplayViewModel_DisplayJobCompleted(object? sender, int e)
		{
			ColorBandSetViewModel.RefreshPercentages();
			ColorBandSetHistogramViewModel.RefreshHistogramDisplay();
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

		#endregion
	}
}
