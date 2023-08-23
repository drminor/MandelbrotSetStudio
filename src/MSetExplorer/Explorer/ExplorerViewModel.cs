using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class ExplorerViewModel : ViewModelBase, IExplorerViewModel 
	{
		//private readonly IMapLoaderManager _mapLoaderManager;
		//private readonly MapJobHelper _mapJobHelper;

		private readonly ViewModelFactory _viewModelFactory;

		private double _dispWidth;
		private double _dispHeight;

		#region Constructor

		public ExplorerViewModel(IProjectViewModel projectViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel,
			ICbsHistogramViewModel cbsHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
			/*IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, */ViewModelFactory viewModelFactory)
		{

			//_mapLoaderManager = mapLoaderManager;
			//_mapJobHelper = mapJobHelper;

			ProjectViewModel = projectViewModel;
			ProjectViewModel.PropertyChanged += ProjectViewModel_PropertyChanged;

			JobTreeViewModel = jobTreeViewModel;

			MapDisplayViewModel = mapDisplayViewModel;
			//MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.DisplayJobCompleted += MapDisplayViewModel_DisplayJobCompleted;

			DispWidth = MapDisplayViewModel.ViewportSize.Width;
			DispHeight = MapDisplayViewModel.ViewportSize.Height;

			_viewModelFactory = viewModelFactory;

			MapCoordsViewModel = viewModelFactory.CreateAMapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;

			CbsHistogramViewModel = cbsHistogramViewModel;
		}

		#endregion

		#region Public Properties

		public IProjectViewModel ProjectViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; } 
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

		public ICbsHistogramViewModel CbsHistogramViewModel { get; }

		public ViewModelFactory ViewModelFactory => _viewModelFactory;

		public bool MapCoordsIsVisible { get; set; }

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
				SubmitMapDisplayJob();
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
				CbsHistogramViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;

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
					MapDisplayViewModel.CurrentColorBand = ColorBandSetViewModel.CurrentColorBand;
				}
			}
		}

		//private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	// Let the Map Project know about Map Display size changes
		//	if (e.PropertyName == nameof(IMapDisplayViewModel.ViewportSize))
		//	{
		//		DispWidth = MapDisplayViewModel.ViewportSize.Width;
		//		DispHeight = MapDisplayViewModel.ViewportSize.Height;
		//		//ProjectViewModel.CanvasSize = MapDisplayViewModel.ViewportSize.Round();
		//	}
		//}

		private void MapDisplayViewModel_DisplayJobCompleted(object? sender, int e)
		{
			ColorBandSetViewModel.RefreshPercentages();
			var histogramDataWasEmpty = CbsHistogramViewModel.RefreshDisplay();

			if (histogramDataWasEmpty)
			{
				Debug.WriteLine("CbsHistogramViewModel: WARNING: Values are all zero on call to RefreshData -- on DisplayJobCompleted.");
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			if (e.IsPreview)
			{
				if (MapCoordsIsVisible)
				{
					if (e.IsPreviewBeingCancelled)
					{
						MapCoordsViewModel.CancelPreview();
					}
					else
					{
						// Calculate new Coords for preview

						var mapAreaInfo = ProjectViewModel.GetUpdatedMapAreaInfo(e.TransformType, e.PanAmount, e.Factor, e.CurrentMapAreaInfo);
						var displaySize = GetDisplaySize(e.DisplaySize);

						MapCoordsViewModel.Preview(mapAreaInfo, displaySize);
					}
				}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				ProjectViewModel.UpdateMapView(e.TransformType, e.PanAmount, e.Factor, e.CurrentMapAreaInfo);
			}
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

			CbsHistogramViewModel.ColorBandSet = colorBandSet;
		}

		#endregion

		#region Private Methods

		private void SubmitMapDisplayJob()
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
				OwnerType.Project,
				newMapAreaInfo,
				newColorBandSet,
				newMapCalcSettings
				);

			ColorBandSetViewModel.ColorBandSet = newColorBandSet;
			CbsHistogramViewModel.ColorBandSet = newColorBandSet;

			MapDisplayViewModel.SubmitJob(areaColorAndCalcSettings);

			UpdateTheMapCoordsView(curJob);
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

		private SizeDbl GetDisplaySize(SizeDbl displaySize)
		{
			if (displaySize.Equals(SizeDbl.Zero))
			{
				Debug.WriteLine("WARNING: The DisplaySize is zero on the MapViewUpdateRequestedEventArgs.");
				return MapDisplayViewModel.ViewportSize;
			}
			else
			{
				Debug.Assert(displaySize.Equals(MapDisplayViewModel.ViewportSize), "The DisplaySize property of the MapViewUpdateRequestedEventArgs is not equal to the MapDisplayViewModel's ViewportSize.");

				return displaySize;
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
