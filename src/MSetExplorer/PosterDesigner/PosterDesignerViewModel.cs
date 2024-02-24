using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class PosterDesignerViewModel : ViewModelBase, IPosterDesignerViewModel
	{
		//private readonly MapJobHelper _mapJobHelper;
		//private readonly IMapLoaderManager _mapLoaderManager;
		//private readonly ViewModelFactory _viewModelFactory;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private readonly bool _useDetailedDebug = true;

		#region Constructor

		public PosterDesignerViewModel(IPosterViewModel posterViewModel, IMapDisplayViewModel mapDisplayViewModel, 
			ICbsHistogramViewModel cbsHistogramViewModel, IJobTreeViewModel jobTreeViewModel,
						/*IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, */
			IMapSectionHistogramProcessor mapSectionHistogramProcessor, ViewModelFactory viewModelFactory)
		{
			//_mapJobHelper = mapJobHelper;
			//_mapLoaderManager = mapLoaderManager;

			PosterViewModel = posterViewModel;
			JobTreeViewModel = jobTreeViewModel;

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;

			MapDisplayViewModel = mapDisplayViewModel;

			PosterViewModel.PropertyChanged += PosterViewModel_PropertyChanged;

			MapDisplayViewModel.MapViewUpdateCompleted += MapDisplayViewModel_MapViewUpdateCompleted;

			ViewModelFactory = viewModelFactory;

			MapCoordsViewModel = viewModelFactory.CreateAMapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			CbsHistogramViewModel = cbsHistogramViewModel;
			CbsHistogramViewModel.PropertyChanged += CbsHistogramViewModel_PropertyChanged;
			CbsHistogramViewModel.ColorBandSetUpdateRequested += CbsHistogramViewModel_ColorBandSetUpdateRequested;
		}

		#endregion

		#region Public Properties

		public IPosterViewModel PosterViewModel { get; }
		public IJobTreeViewModel JobTreeViewModel { get; }

		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; }
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		//public ColorBandSetViewModel ColorBandSetViewModel { get; }

		public ICbsHistogramViewModel CbsHistogramViewModel { get; }

		public ViewModelFactory ViewModelFactory { get; init;}

		#endregion

		#region Event Handlers

		private void PosterViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Update the MapCalcSettings, MapCoords and Map Display with the new Job Area and Calc Settings
			if (e.PropertyName == nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings))
			{
				SubmitMapDisplayJob();
			}

			if (e.PropertyName == nameof(IPosterViewModel.CurrentJob))
			{
				Debug.WriteLine("The PosterViewModel's CurrentJob is changing.");
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IPosterViewModel.CurrentColorBandSet))
			{
				CbsHistogramViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;

				MapDisplayViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;

				// Don't update the ColorBandSetHistogram's ViewModel, if this is a preview.
				if (!PosterViewModel.ColorBandSetIsPreview)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel. Just before setting the CbsHistogramViewModel's ColorBandSet to a value with id: {PosterViewModel.CurrentColorBandSet.Id}.");
					CbsHistogramViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;
				}

				Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel. Just before setting the MapDisplayViewModel's ColorBandSet to a value with id: {PosterViewModel.CurrentColorBandSet.Id}.");
				MapDisplayViewModel.ColorBandSet = PosterViewModel.CurrentColorBandSet;


			}
		}

		//private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
		//	{
		//		MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
		//	}

		//	else if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
		//	{
		//		MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
		//	}

		//	else if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
		//	{
		//		var cbsvmCbsIsNull = ColorBandSetViewModel.ColorBandSet == null ? string.Empty : "Not";

		//		//Debug.WriteLine($"PosterDesignerViewModel is handling ColorBandViewModel PropertyChanged-CurrentColorBand. HighLightSelectedColorBand: {MapDisplayViewModel.HighlightSelectedColorBand}, " +
		//		//	$"CbsViewModel's ColorBandSet is {cbsvmCbsIsNull} null.");

		//		Debug.WriteLine($"PosterDesignerViewModel is handling ColorBandViewModel PropertyChanged-CurrentColorBand." +
		//			$"CbsViewModel's ColorBandSet is {cbsvmCbsIsNull} null.");

		//		//if (MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null)

		//		if (ColorBandSetViewModel.ColorBandSet != null)
		//		{
		//			//MapDisplayViewModel.CurrentColorBand = ColorBandSetViewModel.CurrentColorBand;

		//			var currentColorBandIndex = ColorBandSetViewModel.ColorBandSet.CurrentColorBandIndex;
		//			MapDisplayViewModel.CurrentColorBandIndex = currentColorBandIndex;
		//		}
		//	}
		//}

		private void CbsHistogramViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbsHistogramViewModel.UseEscapeVelocities))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel is handling CbsHistogramViewModel PropertyChanged-UseEscapeVelocities.");
				MapDisplayViewModel.UseEscapeVelocities = CbsHistogramViewModel.UseEscapeVelocities;
			}

			if (e.PropertyName == nameof(CbsHistogramViewModel.HighlightSelectedBand))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel is handling CbsHistogramViewModel PropertyChanged-HighlightSelectedColorBand.");
				MapDisplayViewModel.HighlightSelectedColorBand = CbsHistogramViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(CbsHistogramViewModel.CurrentColorBand))
			{
				var cbsvmCbsIsNull = CbsHistogramViewModel.ColorBandSet == null ? string.Empty : " Not";

				Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel is handling CbsHistogramViewModel PropertyChanged-CurrentColorBand." +
					$"CbsViewModel's ColorBandSet is{cbsvmCbsIsNull} null.");

				if (CbsHistogramViewModel.ColorBandSet != null)
				{
					var currentColorBandIndex = CbsHistogramViewModel.CurrentColorBandIndex;
					MapDisplayViewModel.CurrentColorBandIndex = currentColorBandIndex;
				}
			}
		}

		private void MapDisplayViewModel_MapViewUpdateCompleted(object? sender, MapViewUpdateCompletedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"PosterDesignerViewModel is handling MapDisplayViewModel-MapViewUpdateCompleted for Job: {e.JobNumber}");

			var histogramDataWasEmpty = !CbsHistogramViewModel.ApplyHistogram();
			CbsHistogramViewModel.RefreshDisplay();

			if (histogramDataWasEmpty)
			{
				Debug.WriteLineIf(_useDetailedDebug, "PosterDesignerViewModel::OnDisplayJobCompleted. WARNING: Values are all zero on call to CbsHistogramViewModel.RefreshData.");
			}

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
				PosterViewModel.SaveTheZValues = e.SaveTheZValues;
			}

			// Update the CalculateEscapeVelocities
			else if (e.MapSettingsUpdateType == MapSettingsUpdateType.CalculateEscapeVelocities)
			{
				PosterViewModel.CalculateEscapeVelocities = e.CalculateEscapeVelocities;
			}
		}

		//private void ColorBandSetViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		//{
		//	var colorBandSet = e.ColorBandSet;

		//	if (e.IsPreview)
		//	{
		//		Debug.WriteLine($"MainWindow got a CBS preview with Id = {colorBandSet.Id}");
		//		//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
		//		PosterViewModel.PreviewColorBandSet = colorBandSet;
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"MainWindow got a CBS update with Id = {colorBandSet.Id}");
		//		//MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
		//		PosterViewModel.CurrentColorBandSet = colorBandSet;
		//	}
		//}

		private void CbsHistogramViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is handling 'CbsHistogramViewModel_ColorBandSetUpdateRequested' with Id = {colorBandSet.Id}. (IsPreview:{e.IsPreview}).");

			if (e.IsPreview)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's PreviewColorBandSet to a new value having Id = {colorBandSet.Id}");
				PosterViewModel.PreviewColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"ExplorerViewModel is setting the ProjectViewModel's CurrentColorBandSet to a new value having Id = {colorBandSet.Id}");
				PosterViewModel.CurrentColorBandSet = colorBandSet;
			}
		}
		#endregion

		#region Private Methods

		private void SubmitMapDisplayJob()
		{
			if (MapDisplayViewModel.ViewportSize.Width < 2 || MapDisplayViewModel.ViewportSize.Height < 2)
			{
				Debug.WriteLine("ViewportSize is zero.");
			}

			var areaColorAndCalcSettings = PosterViewModel.CurrentAreaColorAndCalcSettings;
			var existingMapCalcSettings = areaColorAndCalcSettings.MapCalcSettings;

			var newMapCalcSettings = new MapCalcSettings(existingMapCalcSettings.TargetIterations, existingMapCalcSettings.Threshold, PosterViewModel.CalculateEscapeVelocities, PosterViewModel.SaveTheZValues);
			var updatedSettings = areaColorAndCalcSettings.UpdateWith(newMapCalcSettings);

			MapCalcSettingsViewModel.MapCalcSettings = newMapCalcSettings;

			// Update the MapDisplayView model for a new Poster or Poster Job.
			var currentPoster = PosterViewModel.CurrentPoster;

			if (currentPoster != null)
			{
				var currentjob = currentPoster.CurrentJob;
				if (currentjob != null)
				{
					var posterSize = currentPoster.PosterSize;

					var displayPosition = currentPoster.DisplayPosition;
					var displayZoom = currentPoster.DisplayZoom;

					MapDisplayViewModel.SubmitJob(updatedSettings, posterSize, displayPosition, displayZoom);

					UpdateTheMapCoordsView(currentjob);
				}
			}
			else
			{
				MapDisplayViewModel.CancelJob();
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
