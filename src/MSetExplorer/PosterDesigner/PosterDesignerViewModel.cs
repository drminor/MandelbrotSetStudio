using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System;
using System.Diagnostics;
using ImageBuilder;

namespace MSetExplorer
{
	public class PosterDesignerViewModel : ViewModelBase, IPosterDesignerViewModel
	{
		private readonly PngBuilder _pngBuilder;
		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;


		private int _dispWidth;
		private int _dispHeight;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, IMapScrollViewModel mapScrollViewModel, ColorBandSetViewModel colorBandViewModel,
			IProjectAdapter projectAdapter, PngBuilder pngBuilder,	ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator)
		{
			ProjectAdapter = projectAdapter;
			_pngBuilder = pngBuilder;

			PosterViewModel = posterViewModel;
			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;

			MapScrollViewModel = mapScrollViewModel;
			MapScrollViewModel.PropertyChanged += MapScrollViewModel_PropertyChanged;


			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			PosterViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			DispWidth = MapDisplayViewModel.CanvasSize.Width;
			DispHeight = MapDisplayViewModel.CanvasSize.Height;

			_projectOpenSaveViewModelCreator = projectOpenSaveViewModelCreator;
			_cbsOpenSaveViewModelCreator = cbsOpenSaveViewModelCreator;

			MapCoordsViewModel = new MapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;
		}

		private void MapScrollViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapScrollViewModel.HorizontalPosition) || e.PropertyName == nameof(IMapScrollViewModel.VerticalPosition))
			{
				if (PosterViewModel.CurrentPoster != null)
				{
					PosterViewModel.CurrentPoster.DisplayPosition = new VectorInt((int)Math.Round(MapScrollViewModel.HorizontalPosition), (int)Math.Round(MapScrollViewModel.VerticalPosition));
				}
			}
		}

		#endregion

		#region Public Properties

		public IPosterViewModel PosterViewModel { get; }
		public IMapScrollViewModel MapScrollViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel => MapScrollViewModel.MapDisplayViewModel;

		public MapCoordsViewModel MapCoordsViewModel { get; }
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

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

		public IProjectAdapter ProjectAdapter { get; init; }

		#endregion

		#region Public Methods

		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _projectOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType)
		{
			var result = _cbsOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public void PrintPoster(Poster poster)
		{
			_pngBuilder.Build(poster);
		}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Update the MapCalcSettings, MapCoords and Map Display with the new Poster
			if (e.PropertyName == nameof(IPosterViewModel.CurrentPoster))
			{
				//var jobAreaAndCalcSettings = PosterViewModel.JobAreaAndCalcSettings;

				//MapCalcSettingsViewModel.MapCalcSettings = jobAreaAndCalcSettings.MapCalcSettings;
				//MapCoordsViewModel.CurrentJobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
				//MapScrollViewModel.CurrentJobAreaAndCalcSettings = jobAreaAndCalcSettings;

				var curPoster = PosterViewModel.CurrentPoster;

				if (curPoster != null)
				{
					MapScrollViewModel.VMax = curPoster.JobAreaInfo.CanvasSize.Height;
					MapScrollViewModel.HMax = curPoster.JobAreaInfo.CanvasSize.Width;
				}
			}

			// Update the MapCalcSettings, MapCoords and Map Display with the new Job Area and Calc Settings
			if (e.PropertyName == nameof(IPosterViewModel.JobAreaAndCalcSettings))
			{
				var jobAreaAndCalcSettings = PosterViewModel.JobAreaAndCalcSettings;

				MapCalcSettingsViewModel.MapCalcSettings = jobAreaAndCalcSettings.MapCalcSettings;
				MapCoordsViewModel.CurrentJobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
				MapScrollViewModel.CurrentJobAreaAndCalcSettings = jobAreaAndCalcSettings;
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IPosterViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;

				if (PosterViewModel.CurrentColorBandSet != null)
				{
					MapDisplayViewModel.SetColorBandSet(PosterViewModel.CurrentColorBandSet, updateDisplay: true);
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
