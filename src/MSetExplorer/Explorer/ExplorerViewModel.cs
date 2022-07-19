using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class ExplorerViewModel : ViewModelBase, IExplorerViewModel 
	{
		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;
		private readonly PosterOpenSaveViewModelCreator _posterOpenSaveViewModelCreator;
		private readonly CoordsEditorViewModelCreator _coordsEditorViewModelCreator;

		private readonly IMapLoaderManager _mapLoaderManager;

		private double _dispWidth;
		private double _dispHeight;

		#region Constructor

		public ExplorerViewModel(IMapProjectViewModel mapProjectViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel,
			IMapLoaderManager mapLoaderManager,
			ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator, 
			PosterOpenSaveViewModelCreator posterOpenSaveViewModelCreator, CoordsEditorViewModelCreator coordsEditorViewModelCreator)
		{

			_mapLoaderManager = mapLoaderManager;

			MapProjectViewModel = mapProjectViewModel;
			MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

			JobTreeViewModel = new JobTreeViewModel();
			JobTreeViewModel.NavigateToJobRequested += JobTreeViewModel_NavigateToJobRequested;


			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;

			MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize.Round();
			DispWidth = MapDisplayViewModel.CanvasSize.Width;
			DispHeight = MapDisplayViewModel.CanvasSize.Height;

			_projectOpenSaveViewModelCreator = projectOpenSaveViewModelCreator;
			_cbsOpenSaveViewModelCreator = cbsOpenSaveViewModelCreator;
			_posterOpenSaveViewModelCreator = posterOpenSaveViewModelCreator;
			_coordsEditorViewModelCreator = coordsEditorViewModelCreator;

			MapCoordsViewModel = new MapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;

			ColorBandSetHistogramViewModel = colorBandSetHistogramViewModel;
		}

		private void JobTreeViewModel_NavigateToJobRequested(object? sender, NavigateToJobRequestedEventArgs e)
		{
			MapProjectViewModel.CurrentJob = e.Job;
		}

		#endregion

		#region Public Properties

		public IMapProjectViewModel MapProjectViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; } 
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		public double DispWidth
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

		public double DispHeight
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

		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _posterOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits)
		{
			var result = _coordsEditorViewModelCreator(coords, canvasSize, allowEdits);
			return result;
		}

		//// TODO: Once every job has a value for the Canvas Size, then consider using that property's value instead of the current display size.
		//public SizeInt GetCanvasSize(Job job)
		//{
		//	var result = job.CanvasSize;
		//	if (result.Width == 0 || result.Height == 0)
		//	{
		//		result = MapDisplayViewModel.CanvasSize;
		//	}

		//	return result;
		//}

		public JobProgressViewModel CreateAJobProgressViewModel()
		{
			var result = new JobProgressViewModel(_mapLoaderManager);
			return result;
		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProject))
			{
				JobTreeViewModel.CurrentProject = MapProjectViewModel.CurrentProject;
			}
			
			// Update the MSet Info and Map Display with the new Job
			else if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				var curJob = MapProjectViewModel.CurrentJob;

				MapCalcSettingsViewModel.MapCalcSettings = curJob.MapCalcSettings;

				// TODO: Check This: Once every job has a valid CanvasSize, don't use the current DisplaySize.

				//var newMapAreaInfo = MapJobHelper.GetMapAreaInfo(curJob, MapDisplayViewModel.CanvasSize);

				var newMapAreaInfo = curJob.MapAreaInfo;

				MapCoordsViewModel.CurrentMapAreaInfo = newMapAreaInfo;

				var jobAreaAndCalcSettings = new JobAreaAndCalcSettings
					(
					curJob.Id.ToString(),
					JobOwnerType.Project,
					newMapAreaInfo,
					curJob.MapCalcSettings
					);

				MapDisplayViewModel.CurrentJobAreaAndCalcSettings = jobAreaAndCalcSettings;
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
				ColorBandSetHistogramViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;

				if (MapProjectViewModel.CurrentProject != null)
				{
					MapDisplayViewModel.SetColorBandSet(MapProjectViewModel.CurrentColorBandSet, updateDisplay: true);
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
				if (MapDisplayViewModel.HighlightSelectedColorBand && MapProjectViewModel.CurrentProject != null && ColorBandSetViewModel.ColorBandSet != null)
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
				MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize.Round();
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			if (e.IsPreview)
			{
				// Calculate new Coords for preview
				var mapAreaInfo = MapProjectViewModel.GetUpdatedMapAreaInfo(e.TransformType, e.ScreenArea);
				if (mapAreaInfo != null)
				{
					MapCoordsViewModel.Preview(mapAreaInfo);
				}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				MapProjectViewModel.UpdateMapView(e.TransformType, e.ScreenArea);
			}
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
				Debug.WriteLine($"MainWindow ViewModel got a CBS preview with Id = {colorBandSet.Id}");
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
				ColorBandSetHistogramViewModel.ColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLine($"MainWindow ViewModel got a CBS update with Id = {colorBandSet.Id}");
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				MapProjectViewModel.UpdateColorBandSet(colorBandSet);
				ColorBandSetHistogramViewModel.ColorBandSet = colorBandSet;
			}
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					MapDisplayViewModel.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			System.GC.SuppressFinalize(this);
		}

		#endregion
	}
}
