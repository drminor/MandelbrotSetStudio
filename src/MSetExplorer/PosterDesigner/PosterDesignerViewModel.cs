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
		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly PosterOpenSaveViewModelCreator _posterOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;
		private readonly CoordsEditorViewModelCreator _coordsEditorViewModelCreator;

		private SizeDbl _mapDisplaySize;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, IMapScrollViewModel mapScrollViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel,
			MapJobHelper mapJobHelper, IMapLoaderManager mapLoaderManager, PosterOpenSaveViewModelCreator posterOpenSaveViewModelCreator, 
			CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator, CoordsEditorViewModelCreator coordsEditorViewModelCreator)
		{
			_mapJobHelper = mapJobHelper;
			_mapLoaderManager = mapLoaderManager;

			PosterViewModel = posterViewModel;
			JobTreeViewModel = new JobTreeViewModel();
			MapScrollViewModel = mapScrollViewModel;

			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;
			MapScrollViewModel.PropertyChanged += MapScrollViewModel_PropertyChanged;

			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			PosterViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			MapDisplaySize = MapDisplayViewModel.CanvasSize;

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

		public IMapScrollViewModel MapScrollViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel => MapScrollViewModel.MapDisplayViewModel;

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

		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _posterOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType)
		{
			var result = _cbsOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public CreateImageProgressViewModel CreateACreateImageProgressViewModel(string imageFilePath)
		{
			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder);
			return result;
		}

		public CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits)
		{
			var result = _coordsEditorViewModelCreator(coords, canvasSize, allowEdits);
			return result;
		}

		public LazyMapPreviewImageProvider GetPreviewImageProvider (MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, SizeInt previewImagesize, Color fallbackColor)
		{
			var bitmapBuilder = new BitmapBuilder(_mapLoaderManager);
			var result = new LazyMapPreviewImageProvider(bitmapBuilder, _mapJobHelper, mapAreaInfo, previewImagesize, colorBandSet, mapCalcSettings, fallbackColor);
			return result;
		}

		public MapAreaInfo GetUpdatedMapAreaInfo(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize)
		{
			var mapPosition = mapAreaInfo.Coords.Position;
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var screenAreaInt = screenArea.Round();

			var coords = RMapHelper.GetMapCoords(screenAreaInt, mapPosition, samplePointDelta);

			CheckCoordsChange(mapAreaInfo, screenArea, coords);

			var posterSize = newMapSize.Round();
			var blockSize = mapAreaInfo.Subdivision.BlockSize;

			var result = _mapJobHelper.GetMapAreaInfo(coords, posterSize, blockSize);

			return result;
		}

		[Conditional("DEBUG")]
		private void CheckCoordsChange(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, RRectangle newCoords)
		{
			if (screenArea == new RectangleDbl(new RectangleInt(new PointInt(), mapAreaInfo.CanvasSize)))
			{
				if (Reducer.Reduce(newCoords) != Reducer.Reduce(mapAreaInfo.Coords))
				{
					Debug.WriteLine($"The new ScreenArea matches the existing ScreenArea, but the Coords were updated.");
					//throw new InvalidOperationException("if the pos has not changed, the coords should not change.");
				}
			}
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPosterViewModel.PosterSize))
			{
				MapScrollViewModel.PosterSize = PosterViewModel.PosterSize;
			}
			else if (e.PropertyName == nameof(IPosterViewModel.LogicalDisplaySize))
			{
				MapScrollViewModel.PosterSize = PosterViewModel.PosterSize;
				MapScrollViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
				MapScrollViewModel.HorizontalPosition = PosterViewModel.DisplayPosition.X;
				MapScrollViewModel.InvertedVerticalPosition = PosterViewModel.DisplayPosition.Y;
			}

			// Update the MapCalcSettings, MapCoords and Map Display with the new Poster
			else if (e.PropertyName == nameof(IPosterViewModel.CurrentPoster))
			{
				var curPoster = PosterViewModel.CurrentPoster;

				if (curPoster != null)
				{
					MapScrollViewModel.PosterSize = PosterViewModel.PosterSize;
					MapScrollViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
					MapScrollViewModel.HorizontalPosition = PosterViewModel.DisplayPosition.X;
					MapScrollViewModel.InvertedVerticalPosition = PosterViewModel.DisplayPosition.Y;
				}
				else
				{
					//MapScrollViewModel.PosterSize = null;

					// Let the MapDisplay know to stop any current MapLoader job.
					MapDisplayViewModel.CurrentJobAreaAndCalcSettings = null;
				}
			}

			// Update the MapCalcSettings, MapCoords and Map Display with the new Job Area and Calc Settings
			else if (e.PropertyName == nameof(IPosterViewModel.JobAreaAndCalcSettings))
			{
				var jobAreaAndCalcSettings = PosterViewModel.JobAreaAndCalcSettings;

				MapCalcSettingsViewModel.MapCalcSettings = jobAreaAndCalcSettings.MapCalcSettings;
				MapCoordsViewModel.CurrentMapAreaInfo = jobAreaAndCalcSettings.MapAreaInfo;

				MapDisplayViewModel.SetColorBandSet(PosterViewModel.ColorBandSet, updateDisplay: false);
				MapDisplayViewModel.CurrentJobAreaAndCalcSettings = jobAreaAndCalcSettings;
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IPosterViewModel.ColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = PosterViewModel.ColorBandSet;

				MapDisplayViewModel.SetColorBandSet(PosterViewModel.ColorBandSet, updateDisplay: true);
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
					MapDisplayViewModel.SetColorBandSet(ColorBandSetViewModel.ColorBandSet, updateDisplay: true);
				}
			}
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Let the Map Project know about Map Display size changes
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				MapDisplaySize = MapDisplayViewModel.CanvasSize;
				PosterViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}

			else if (e.PropertyName == nameof(IMapDisplayViewModel.LogicalDisplaySize))
			{
				PosterViewModel.DisplayZoom = MapDisplayViewModel.DisplayZoom;
				PosterViewModel.LogicalDisplaySize = MapDisplayViewModel.LogicalDisplaySize;
			}
		}

		private void MapScrollViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.HorizontalPosition))
			{
				PosterViewModel.DisplayPosition = new VectorInt((int)Math.Round(MapScrollViewModel.HorizontalPosition), PosterViewModel.DisplayPosition.Y);
			}

			else if (e.PropertyName == nameof(IMapScrollViewModel.InvertedVerticalPosition))
			{
				PosterViewModel.DisplayPosition = new VectorInt(PosterViewModel.DisplayPosition.X, (int)Math.Round(MapScrollViewModel.InvertedVerticalPosition));
			}

			else if (e.PropertyName == nameof(IMapScrollViewModel.DisplayZoom))
			{
				PosterViewModel.DisplayZoom = MapScrollViewModel.DisplayZoom;
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			// TODO: Verify that the Poster Designer will not be handling MapView Updates
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
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
			}
			else
			{
				Debug.WriteLine($"MainWindow got a CBS update with Id = {colorBandSet.Id}");
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				PosterViewModel.UpdateColorBandSet(colorBandSet);
			}
		}

		#endregion
	}
}
