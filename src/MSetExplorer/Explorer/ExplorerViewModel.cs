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

		private readonly bool _useDetailedDebug = false;

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
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ProjectViewModel PropertyChanged-CurrentProject.");
				JobTreeViewModel.CurrentProject = ProjectViewModel.CurrentProject;
			}

			// Update the MSet Info and Map Display with the new Job
			if (e.PropertyName == nameof(IProjectViewModel.CurrentJob))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ProjectViewModel PropertyChanged-CurrentJob.");
				SubmitMapDisplayJob();
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IProjectViewModel.CurrentColorBandSet))
			{
				//Debug.WriteLine($"ExplorerViewModel is handling ProjectViewModel PropertyChanged-CurrentColorBandSet. The Project's CurrentColorBandSet has Id: {ProjectViewModel.CurrentColorBandSet.Id}.");

				Debug.WriteLineIf(_useDetailedDebug, $"Just before setting the ColorBandSetViewModel's ColorBandSet to a value with id: {ProjectViewModel.CurrentColorBandSet.Id}.");
				ColorBandSetViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;

				Debug.WriteLineIf(_useDetailedDebug, $"Just before setting the CbsHistogramViewModel's ColorBandSet to a value with id: {ProjectViewModel.CurrentColorBandSet.Id}.");
				CbsHistogramViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;

				if (ProjectViewModel.CurrentProject != null)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Just before setting the MapDisplayViewModel's ColorBandSet to a value with id: {ProjectViewModel.CurrentColorBandSet.Id}.");
					MapDisplayViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ProjectViewModel PropertyChanged-CurrentColorBandSet. Not updating the MapDisplayViewModel's ColorBandSet -- The CurrentProject is Null.");
				}
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ColorBandViewModel PropertyChanged-UseEscapeVelocities.");
				MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ColorBandViewModel PropertyChanged-HighlightSelectedColorBand.");
				MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
			{
				var cbsvmCbsIsNull = ColorBandSetViewModel.ColorBandSet == null ? string.Empty : "Not";
				var projectVMCurrentProjectIsNull = ProjectViewModel.CurrentProject != null ? string.Empty : "Not";

				//Debug.WriteLineIf(_useDetailedDebug$"ExplorerViewModel is handling ColorBandViewModel PropertyChanged-CurrentColorBand. HighLightSelectedColorBand: {MapDisplayViewModel.HighlightSelectedColorBand}, " +
				//	$"CbsViewModel's ColorBandSet is {cbsvmCbsIsNull} null. The ProjectViewModel's CurrentProject is {projectVMCurrentProjectIsNull} null.");

				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ColorBandViewModel PropertyChanged-CurrentColorBand." +
					$"CbsViewModel's ColorBandSet is {cbsvmCbsIsNull} null. The ProjectViewModel's CurrentProject is {projectVMCurrentProjectIsNull} null.");

				//if (MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null && ProjectViewModel.CurrentProject != null)

				if (ColorBandSetViewModel.ColorBandSet != null && ProjectViewModel.CurrentProject != null)
				{
					//MapDisplayViewModel.CurrentColorBand = ColorBandSetViewModel.CurrentColorBand;

					var selectedColorBandIndex = ColorBandSetViewModel.ColorBandSet.SelectedColorBandIndex;
					MapDisplayViewModel.SelectedColorBandIndex = selectedColorBandIndex;
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
			Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling MapDisplayViewModel-DisplayJobCompleted for Job: {e}");

			ColorBandSetViewModel.RefreshPercentages();
			var histogramDataWasEmpty = CbsHistogramViewModel.RefreshDisplay();

			if (histogramDataWasEmpty)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ExplorerViewModel::OnDisplayJobCompleted. WARNING: Values are all zero on call to CbsHistogramViewModel.RefreshData.");
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

			// Update the SaveTheZValues
			else if (e.MapSettingsUpdateType == MapSettingsUpdateType.SaveTheZValues)
			{
				ProjectViewModel.SaveTheZValues = e.SaveTheZValues;
			}

			// Update the CalculateEscapeVelocities
			else if (e.MapSettingsUpdateType == MapSettingsUpdateType.CalculateEscapeVelocities)
			{
				ProjectViewModel.CalculateEscapeVelocities = e.CalculateEscapeVelocities;
			}
		}

		private void ColorBandSetViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling 'ColorBandSetViewModel_ColorBandSetUpdateRequested' with Id = {colorBandSet.Id}. (IsPreview:{e.IsPreview}).");

			if (e.IsPreview)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's PreviewColorBandSet to a new value having Id = {colorBandSet.Id}");

				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
				ProjectViewModel.PreviewColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's CurrentColorBandSet to a new value having Id = {colorBandSet.Id}");

				//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				ProjectViewModel.CurrentColorBandSet = colorBandSet;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the CbsHistogramViewModel's ColorBandSet to a new value having Id = {colorBandSet.Id}");

			CbsHistogramViewModel.ColorBandSet = colorBandSet;
		}

		#endregion

		#region Private Methods

		private void SubmitMapDisplayJob()
		{
			var curJob = ProjectViewModel.CurrentJob;
			var curJobId = curJob.Id.ToString();

			var newMapAreaInfo = curJob.MapAreaInfo;
			var newColorBandSet = ProjectViewModel.CurrentColorBandSet;

			var existingMapCalcSettings = curJob.MapCalcSettings;
			var newMapCalcSettings = new MapCalcSettings(existingMapCalcSettings.TargetIterations, existingMapCalcSettings.Threshold, ProjectViewModel.CalculateEscapeVelocities, ProjectViewModel.SaveTheZValues);

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
