using ImageBuilder;
using MSS.Common;
using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;

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

		public ExplorerViewModel(IProjectViewModel projectViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
			IMapLoaderManager mapLoaderManager,
			ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator, 
			PosterOpenSaveViewModelCreator posterOpenSaveViewModelCreator, CoordsEditorViewModelCreator coordsEditorViewModelCreator)
		{

			_mapLoaderManager = mapLoaderManager;

			ProjectViewModel = projectViewModel;
			ProjectViewModel.PropertyChanged += ProjectViewModel_PropertyChanged;

			JobTreeViewModel = jobTreeViewModel;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;

			ProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize.Round();
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

		#endregion

		#region Public Properties

		public IProjectViewModel ProjectViewModel { get; }
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

		public IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType)
		{
			var result = _posterOpenSaveViewModelCreator(initalName, useEscapeVelocities, dialogType);
			return result;
		}

		public CreateImageProgressViewModel CreateACreateImageProgressViewModel(string imageFilePath, bool useEscapeVelocities)
		{
			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder, useEscapeVelocities);
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

		private void ProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IProjectViewModel.CurrentProject))
			{
				JobTreeViewModel.CurrentProject = ProjectViewModel.CurrentProject;
			}

			// Update the MSet Info and Map Display with the new Job
			if (e.PropertyName == nameof(IProjectViewModel.CurrentJob))
			{
				var curJob = ProjectViewModel.CurrentJob;
				var curJobId = curJob.Id.ToString();

				var newMapCalcSettings = curJob.MapCalcSettings;
				var newMapAreaInfo = curJob.MapAreaInfo;
				var newColorBandSet = ProjectViewModel.CurrentColorBandSet;

				MapCalcSettingsViewModel.MapCalcSettings = newMapCalcSettings;

				MapCoordsViewModel.JobId = curJobId;
				MapCoordsViewModel.CurrentMapAreaInfo = newMapAreaInfo;

				var areaColorAndCalcSettings = new AreaColorAndCalcSettings
					(
					curJobId,
					JobOwnerType.Project,
					newMapAreaInfo,
					newColorBandSet,
					curJob.MapCalcSettings
					);

				ColorBandSetViewModel.ColorBandSet = newColorBandSet;
				ColorBandSetHistogramViewModel.ColorBandSet = newColorBandSet;

				MapDisplayViewModel.SubmitJob(areaColorAndCalcSettings);
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
				ColorBandSetHistogramViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;

				if (ProjectViewModel.CurrentProject != null)
				{
					MapDisplayViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
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
				if (MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null && ProjectViewModel.CurrentProject != null)
				{
					MapDisplayViewModel.ColorBandSet = ColorBandSetViewModel.ColorBandSet;
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
				ProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize.Round();
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			if (e.IsPreview)
			{
				// Calculate new Coords for preview
				var mapAreaInfo = ProjectViewModel.GetUpdatedMapAreaInfo(e.TransformType, e.ScreenArea);
				if (mapAreaInfo != null)
				{
					MapCoordsViewModel.Preview(mapAreaInfo);
				}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				ProjectViewModel.UpdateMapView(e.TransformType, e.ScreenArea);
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

				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
				ProjectViewModel.PreviewColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLine($"MainWindow ViewModel got a CBS update with Id = {colorBandSet.Id}");

				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				ProjectViewModel.CurrentColorBandSet = colorBandSet;
			}

			ColorBandSetHistogramViewModel.ColorBandSet = colorBandSet;
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
