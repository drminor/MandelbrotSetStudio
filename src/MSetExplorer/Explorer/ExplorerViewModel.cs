using ImageBuilder;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace MSetExplorer
{
	internal class ExplorerViewModel : ViewModelBase, IExplorerViewModel 
	{
		private readonly IMapLoaderManager _mapLoaderManager;

		private readonly ViewModelFactory _viewModelFactory;

		private double _dispWidth;
		private double _dispHeight;

		#region Constructor

		public ExplorerViewModel(IProjectViewModel projectViewModel, IMapDisplayViewModel2 mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ColorBandSetHistogramViewModel colorBandSetHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
			IMapLoaderManager mapLoaderManager, ViewModelFactory viewModelFactory)
		{

			_mapLoaderManager = mapLoaderManager;

			ProjectViewModel = projectViewModel;
			ProjectViewModel.PropertyChanged += ProjectViewModel_PropertyChanged;

			JobTreeViewModel = jobTreeViewModel;

			MapDisplayViewModel = mapDisplayViewModel;
			//MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;

			DispWidth = MapDisplayViewModel.ViewPortSize.Width;
			DispHeight = MapDisplayViewModel.ViewPortSize.Height;

			_viewModelFactory = viewModelFactory;

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
		public IMapDisplayViewModel2 MapDisplayViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; } 
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		public ViewModelFactory ViewModelFactory => _viewModelFactory;

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

		public CreateImageProgressViewModel CreateACreateImageProgressViewModel()
		{
			var pngBuilder = new PngBuilder(_mapLoaderManager);
			var result = new CreateImageProgressViewModel(pngBuilder);
			return result;
		}

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

				UpdateTheMapCoordsView(curJob);
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

			if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
			{
				MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
			{
				if (MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null && ProjectViewModel.CurrentProject != null)
				{
					MapDisplayViewModel.CurrentColorBand = ColorBandSetViewModel.CurrentColorBand;
				}
			}
		}

		//private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	// Let the Map Project know about Map Display size changes
		//	if (e.PropertyName == nameof(IMapDisplayViewModel2.ViewPortSize))
		//	{
		//		DispWidth = MapDisplayViewModel.ViewPortSize.Width;
		//		DispHeight = MapDisplayViewModel.ViewPortSize.Height;
		//		//ProjectViewModel.CanvasSize = MapDisplayViewModel.ViewPortSize.Round();
		//	}
		//}

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
				ProjectViewModel.UpdateMapView(e.TransformType, e.PanAmount, e.Factor, e.CurrentMapAreaInfo);
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

					MapDisplayViewModel.MapViewUpdateRequested -= MapDisplayViewModel_MapViewUpdateRequested;
					MapDisplayViewModel.DisplayJobCompleted -= MapDisplayViewModel_DisplayJobCompleted;

					MapCalcSettingsViewModel.MapSettingsUpdateRequested -= MapCalcSettingsViewModel_MapSettingsUpdateRequested;
					ColorBandSetViewModel.PropertyChanged -= ColorBandViewModel_PropertyChanged;
					ColorBandSetViewModel.ColorBandSetUpdateRequested -= ColorBandSetViewModel_ColorBandSetUpdateRequested;

					MapDisplayViewModel.Dispose();
					ColorBandSetViewModel.Dispose();
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
