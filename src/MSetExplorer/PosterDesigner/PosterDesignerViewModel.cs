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
	public class PosterDesignerViewModel : ViewModelBase, IPosterDesignerViewModel
	{
		private readonly MapJobHelper _mapJobHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly PosterOpenSaveViewModelCreator _posterOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;
		private readonly CoordsEditorViewModelCreator _coordsEditorViewModelCreator;

		private int _dispWidth;
		private int _dispHeight;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, IMapScrollViewModel mapScrollViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel,
			MapJobHelper mapJobHelper, IMapLoaderManager mapLoaderManager, PosterOpenSaveViewModelCreator posterOpenSaveViewModelCreator, 
			CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator, CoordsEditorViewModelCreator coordsEditorViewModelCreator)
		{
			_mapJobHelper = mapJobHelper;
			_mapLoaderManager = mapLoaderManager;

			PosterViewModel = posterViewModel;
			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;

			MapScrollViewModel = mapScrollViewModel;
			MapScrollViewModel.PropertyChanged += MapScrollViewModel_PropertyChanged;

			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			PosterViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			DispWidth = MapDisplayViewModel.CanvasSize.Width;
			DispHeight = MapDisplayViewModel.CanvasSize.Height;

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
		public IMapScrollViewModel MapScrollViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel => MapScrollViewModel.MapDisplayViewModel;

		public MapCoordsViewModel MapCoordsViewModel { get; }
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		public int DispWidth
		{
			get => _dispWidth;
			set
			{
				if (value != _dispWidth)
				{
					_dispWidth = value;
					OnPropertyChanged();
				}
			}
		}

		public int DispHeight
		{
			get => _dispHeight;
			set
			{
				if (value != _dispHeight)
				{
					_dispHeight = value;
					OnPropertyChanged();
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

		public MapAreaInfo GetUpdatedJobAreaInfo(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize)
		{
			var mapPosition = mapAreaInfo.Coords.Position;
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var screenAreaInt = screenArea.Round();
			var coords = RMapHelper.GetMapCoords(screenAreaInt, mapPosition, samplePointDelta);

			var rCoords = Reducer.Reduce(coords);

			if (screenArea.Position.X == 0 && screenArea.Position.Y == 0)
			{
				if (rCoords != mapAreaInfo.Coords)
				{
					Debug.WriteLine($"Coords were updated, but the new ScreenArea's position is zero.");
					//throw new InvalidOperationException("if the pos has not changed, the coords should not change.");
				}
			}


			var posterSize = newMapSize.Round();
			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var jobAreaInfo = _mapJobHelper.GetJobAreaInfo(coords, posterSize, blockSize);

			return jobAreaInfo;
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IPosterViewModel.PosterSize))
			{
				MapScrollViewModel.PosterSize = PosterViewModel.PosterSize;
			}
			else if (e.PropertyName == nameof(IPosterViewModel.DisplayZoom))
			{
				MapScrollViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
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
				MapCoordsViewModel.CurrentJobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
				MapDisplayViewModel.CurrentJobAreaAndCalcSettings = jobAreaAndCalcSettings;
			}

			//else if (e.PropertyName == nameof(IPosterViewModel.DisplayZoom))
			//{
			//	MapDisplayViewModel.DisplayZoom = PosterViewModel.DisplayZoom;
			//}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IPosterViewModel.ColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = PosterViewModel.ColorBandSet;

				if (PosterViewModel.ColorBandSet != null)
				{
					MapDisplayViewModel.SetColorBandSet(PosterViewModel.ColorBandSet, updateDisplay: true);
				}
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
			{
				MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
			{
				MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
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
				DispWidth = MapDisplayViewModel.CanvasSize.Width;
				DispHeight = MapDisplayViewModel.CanvasSize.Height;
				PosterViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}

			if (e.PropertyName == nameof(IMapDisplayViewModel.LogicalDisplaySize))
			{
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
