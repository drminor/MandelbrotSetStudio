using MSS.Common;
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
		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private readonly ViewModelFactory _viewModelFactory;

		private double _dispWidth;
		private double _dispHeight;

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public ExplorerViewModel(IProjectViewModel projectViewModel, IMapDisplayViewModel mapDisplayViewModel,
			ICbsHistogramViewModel cbsHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
			//IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper,
			IMapSectionHistogramProcessor mapSectionHistogramProcessor, ViewModelFactory viewModelFactory)
		{

			//_mapLoaderManager = mapLoaderManager;
			//_mapJobHelper = mapJobHelper;

			_viewModelFactory = viewModelFactory;

			ProjectViewModel = projectViewModel;
			ProjectViewModel.PropertyChanged += ProjectViewModel_PropertyChanged;

			JobTreeViewModel = jobTreeViewModel;

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;

			JobProgressViewModel = _viewModelFactory.CreateAJobProgressViewModel();

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;
			MapDisplayViewModel.MapViewUpdateCompleted += MapDisplayViewModel_MapViewUpdateCompleted;

			DispWidth = MapDisplayViewModel.ViewportSize.Width;
			DispHeight = MapDisplayViewModel.ViewportSize.Height;

			MapCoordsViewModel = _viewModelFactory.CreateAMapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			CbsHistogramViewModel = cbsHistogramViewModel;
			CbsHistogramViewModel.PropertyChanged += CbsHistogramViewModel_PropertyChanged;
			CbsHistogramViewModel.ColorBandSetUpdateRequested += CbsHistogramViewModel_ColorBandSetUpdateRequested; 
		}

		#endregion

		#region Public Properties

		public IProjectViewModel ProjectViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }
		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public JobProgressViewModel JobProgressViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; } 
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }

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
				if (ProjectViewModel.CurrentProject != null && !ProjectViewModel.CurrentJob.IsEmpty)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling ProjectViewModel PropertyChanged-CurrentJob.");
					SubmitMapDisplayJob();
				}
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IProjectViewModel.CurrentColorBandSet))
			{
				if (ProjectViewModel.CurrentProject == null || ProjectViewModel.CurrentJob.IsEmpty)
				{
					return;
				}

				// Don't update the ColorBandSetHistogram's ViewModel, if this is a preview.
				if (!ProjectViewModel.ColorBandSetIsPreview)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel. Just before setting the CbsHistogramViewModel's ColorBandSet to a value with id: {ProjectViewModel.CurrentColorBandSet.Id}.");
					CbsHistogramViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
				}

				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel. Just before setting the MapDisplayViewModel's ColorBandSet to a value with id: {ProjectViewModel.CurrentColorBandSet.Id}.");
				MapDisplayViewModel.ColorBandSet = ProjectViewModel.CurrentColorBandSet;
			}
		}

		private void CbsHistogramViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbsHistogramViewModel.UseEscapeVelocities))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling CbsHistogramViewModel PropertyChanged-UseEscapeVelocities.");
				MapDisplayViewModel.UseEscapeVelocities = CbsHistogramViewModel.UseEscapeVelocities;
			}

			if (e.PropertyName == nameof(CbsHistogramViewModel.HighlightSelectedBand))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling CbsHistogramViewModel PropertyChanged-HighlightSelectedColorBand.");
				MapDisplayViewModel.HighlightSelectedColorBand = CbsHistogramViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(CbsHistogramViewModel.CurrentColorBand))
			{
				var cbsvmCbsIsNull = CbsHistogramViewModel.ColorBandSet == null ? string.Empty : " Not";
				var projectVMCurrentProjectIsNull = ProjectViewModel.CurrentProject != null ? string.Empty : " Not";

				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling CbsHistogramViewModel PropertyChanged-CurrentColorBand." +
					$"CbsViewModel's ColorBandSet is{cbsvmCbsIsNull} null. The ProjectViewModel's CurrentProject is{projectVMCurrentProjectIsNull} null.");

				if (CbsHistogramViewModel.ColorBandSet != null && ProjectViewModel.CurrentProject != null)
				{
					var currentColorBandIndex = CbsHistogramViewModel.CurrentColorBandIndex;
					MapDisplayViewModel.CurrentColorBandIndex = currentColorBandIndex;
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

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			DispWidth = e.AdjustedDisplaySize.Width;
			DispHeight = e.AdjustedDisplaySize.Height;

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

		private void MapDisplayViewModel_MapViewUpdateCompleted(object? sender, MapViewUpdateCompletedEventArgs e)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling MapDisplayViewModel-MapViewUpdateCompleted for Job: {e.JobNumber}. Number of Historgrams processed: {_mapSectionHistogramProcessor.NumberOfSectionsProcessed}.");
			Debug.WriteLine($"ExplorerViewModel is handling MapDisplayViewModel-MapViewUpdateCompleted for Job: {e.JobNumber}. Number of Historgrams processed: {_mapSectionHistogramProcessor.NumberOfSectionsProcessed}. " +
				$"The CbsHistogramViewModel's ColorBandSet Id = {CbsHistogramViewModel.ColorBandSet.Id} / Id of CBS under Edit: {CbsHistogramViewModel.ColorBandSetBeingEditedId}.");

			var histogramDataWasEmpty = !CbsHistogramViewModel.ApplyHistogram();
			CbsHistogramViewModel.RefreshDisplay();

			if (histogramDataWasEmpty)
			{
				Debug.WriteLineIf(_useDetailedDebug, "ExplorerViewModel::OnDisplayJobCompleted. WARNING: Values are all zero on call to CbsHistogramViewModel.RefreshData.");
			}
			//else
			//{
			//	var dictCode = HistogramHelper.HistogramToCSharp(_mapSectionHistogramProcessor.Histogram);
			//	Debug.WriteLine($"{dictCode}");
			//}

			MapCalcSettingsViewModel.TargetIterationsAvailable = _mapSectionHistogramProcessor.GetAverageMapSectionTargetIteration();
		}

		private void MapCalcSettingsViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			// Update the Target Iterations
			if (e.MapSettingsUpdateType == MapSettingsUpdateType.TargetIterations)
			{
				CbsHistogramViewModel.ApplyChanges(e.TargetIterations);
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

		private void CbsHistogramViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling 'CbsHistogramViewModel_ColorBandSetUpdateRequested' with Id = {colorBandSet.Id}. (IsPreview:{e.IsPreview}).");

			if (e.IsPreview)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's PreviewColorBandSet to a new value having Id = {colorBandSet.Id}");
				ProjectViewModel.PreviewColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's CurrentColorBandSet to a new value having Id = {colorBandSet.Id}");
				ProjectViewModel.CurrentColorBandSet = colorBandSet;
			}
		}

		#endregion

		#region Private Methods

		private void SubmitMapDisplayJob()
		{
			var curJob = ProjectViewModel.CurrentJob;
			var curJobId = curJob.Id;

			var areaColorAndCalcSettings = new AreaColorAndCalcSettings
				(
				curJobId,
				OwnerType.Project,
				curJob.MapAreaInfo,
				ProjectViewModel.CurrentColorBandSet,
				curJob.MapCalcSettings
				);

			var existingMapCalcSettings = areaColorAndCalcSettings.MapCalcSettings;

			var newMapCalcSettings = new MapCalcSettings(existingMapCalcSettings.TargetIterations, existingMapCalcSettings.Threshold, ProjectViewModel.CalculateEscapeVelocities, ProjectViewModel.SaveTheZValues);
			var newAreaColorAndCalcSettings = areaColorAndCalcSettings.UpdateWith(newMapCalcSettings);
			var colorBandSet = newAreaColorAndCalcSettings.ColorBandSet;

			Debug.WriteLine($"ExplorerViewModel. SubmittingMapDisplayJob. The ColorBandSetId: {colorBandSet.Id}/{colorBandSet.LastUpdatedUtc} with TargetIterations: {colorBandSet.TargetIterations}. " +
				$"The CbsHistogramViewModel has Id: {CbsHistogramViewModel.ColorBandSet.Id}. The CbsHistogramViewModel IsDirty = {CbsHistogramViewModel.IsDirty}.");

			MapCalcSettingsViewModel.MapCalcSettings = newMapCalcSettings;
			CbsHistogramViewModel.ColorBandSet = colorBandSet;

			_ = MapDisplayViewModel.SubmitJob(newAreaColorAndCalcSettings);

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

					//MapDisplayViewModel.DisplayJobCompleted -= MapDisplayViewModel_DisplayJobCompleted;
					MapDisplayViewModel.MapViewUpdateCompleted -= MapDisplayViewModel_MapViewUpdateCompleted;

					MapCalcSettingsViewModel.MapSettingsUpdateRequested -= MapCalcSettingsViewModel_MapSettingsUpdateRequested;

					CbsHistogramViewModel.PropertyChanged -= CbsHistogramViewModel_PropertyChanged;
					CbsHistogramViewModel.ColorBandSetUpdateRequested -= CbsHistogramViewModel_ColorBandSetUpdateRequested;

					MapDisplayViewModel.Dispose();

					CbsHistogramViewModel.Dispose();

					_mapSectionHistogramProcessor.Dispose();
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
