using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace MSetExplorer
{
	public class JobProgressViewModel : ViewModelBase
	{
		private readonly SynchronizationContext? _synchronizationContext;
		private readonly IMapLoaderManager _mapLoaderManager;
		private JobProgressInfo _currentJobProgressInfo;
		private bool _isEnabled;

		#region Constructor

		public JobProgressViewModel(IMapLoaderManager mapLoaderManager)
		{
			Debug.WriteLine("The JobProgressViewModel is being loaded.");
			_synchronizationContext = SynchronizationContext.Current;
			_mapLoaderManager = mapLoaderManager;
			_currentJobProgressInfo = new JobProgressInfo(0, "temp", DateTime.UtcNow, 0, 0);
			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();

			_isEnabled = false;
		}

		#endregion

		#region Event Handlers

		private void MapLoaderManager_RequestAdded(object? sender, MsrJob e)
		{
			_synchronizationContext?.Post((o) => HandleRequestAdded(e), null);
		}

		private void HandleRequestAdded(MsrJob msrJob)
		{
			msrJob.MapSectionLoaded += MsrJob_MapSectionLoaded;
			msrJob.JobHasCompleted += MsrJob_JobHasCompleted;

			CurrentJobProgressInfo = new JobProgressInfo(msrJob.MapLoaderJobNumber, "Temp", msrJob.ProcessingStartTime ?? DateTime.Now, msrJob.TotalNumberOfSectionsRequested, msrJob.SectionsFoundInRepo);
			CurrentJobProgressInfo.DateCreatedUtc = DateTime.UtcNow;

			OnPropertyChanged(nameof(TotalSections));
		}

		private void MsrJob_JobHasCompleted(object? sender, EventArgs e)
		{
			if (sender is MsrJob msrJob)
			{
				_synchronizationContext?.Post((o) => HandleJobHasCompleted(msrJob), null);
			}
		}

		private void HandleJobHasCompleted(MsrJob msrJob)
		{
			msrJob.JobHasCompleted -= MsrJob_JobHasCompleted;
			msrJob.MapSectionLoaded -= MsrJob_MapSectionLoaded;

			var totalExecutionTime = msrJob.TotalExecutionTime;
			Report(totalExecutionTime);

			OnPropertyChanged(nameof(RunTime));
			OnPropertyChanged(nameof(EstimatedTimeRemaining));

			OnPropertyChanged(nameof(FetchedCount));
			OnPropertyChanged(nameof(GeneratedCount));

			OnPropertyChanged(nameof(PercentComplete));

			MapSectionProcessInfos.Clear();
		}

		private void MsrJob_MapSectionLoaded(object? sender, MapSectionRequest e)
		{
			//Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleMapSectionLoaded(e), null);
		}

		private void HandleMapSectionLoaded(MapSectionRequest mapSectionRequest)
		{
			var mapSectionProcessInfo = CreateMSProcInfo(mapSectionRequest);

			MapSectionProcessInfos.Add(mapSectionProcessInfo);

			if (CurrentJobProgressInfo.JobNumber == mapSectionProcessInfo.JobNumber)
			{
				if (mapSectionProcessInfo.FoundInRepo)
				{
					CurrentJobProgressInfo.FetchedCount += 1;
				}
				else
				{
					CurrentJobProgressInfo.GeneratedCount += 1;
				}
			}
		}

		private MapSectionProcessInfo CreateMSProcInfo(MapSectionRequest msr)
		{
			var result = new MapSectionProcessInfo
				(
				jobNumber: msr.MapLoaderJobNumber,
				requestNumber: msr.RequestNumber,
				foundInRepo: msr.FoundInRepo,
				requestWasCompleted: msr.RequestWasCompleted,
				requestWasCancelled: msr.IsCancelled,
				requestDuration: msr.TimeToCompleteGenRequest,
				processingDuration: msr.ProcessingDuration,
				generationDuration: msr.GenerationDuration,
				mathOpCounts: msr.MathOpCounts
				);

			return result;
		}

		private void Report(TimeSpan? totalExecutionTime)
		{
			var mops = new MathOpCounts();
			var sumProcessingDurations = new TimeSpan();
			var sumGenerationDurations = new TimeSpan();

			foreach (var x in MapSectionProcessInfos)
			{
				if (x.JobNumber == CurrentJobProgressInfo.JobNumber)
				{
					//if (x.MathOpCounts != null)
					//{
					//	mops.Update(x.MathOpCounts);
					//}

					if (x.ProcessingDuration.HasValue)
					{
						sumProcessingDurations += x.ProcessingDuration.Value;
					}

					if (x.GenerationDuration.HasValue)
					{
						sumGenerationDurations += x.GenerationDuration.Value;
					}
				}
			}

			Debug.WriteLine($"Generated {CurrentJobProgressInfo.GeneratedCount} sections in {totalExecutionTime}. Performed: {mops.NumberOfMultiplications} multiplications. " +
				$"Iterations: {mops.NumberOfCalcs}; Discarded Iterations: {mops.NumberOfUnusedCalcs}.");

			//var processingElapsed = (long)Math.Round(sumProcessingDurations * 1000);
			//var generationElapsed = (long)Math.Round(sumGenerationDurations * 1000);

			var multiplications = mops.NumberOfMultiplications;
			var calcs = (long)mops.NumberOfCalcs;
			var unusedCalcs = (long)mops.NumberOfUnusedCalcs;

			Debug.WriteLine($"Total Processing Time: {sumProcessingDurations}; Time to generate: {sumGenerationDurations}; Multiplications: {multiplications}; Iterations: {calcs}; Discarded Iterations: {unusedCalcs}.");
		}

		#endregion

		#region Public Properties

		public bool IsEnabled
		{
			get => _isEnabled;
			set
			{
				if (value != _isEnabled)
				{
					_isEnabled = value;

					if (_isEnabled)
					{
						_mapLoaderManager.RequestAdded += MapLoaderManager_RequestAdded;
					}
					else
					{
						_mapLoaderManager.RequestAdded -= MapLoaderManager_RequestAdded;
					}
				}
			}
		}

		public ObservableCollection<MapSectionProcessInfo> MapSectionProcessInfos { get; }

		public JobProgressInfo CurrentJobProgressInfo
		{
			get => _currentJobProgressInfo; 
			set
			{
				if (value !=_currentJobProgressInfo)
				{
					_currentJobProgressInfo = value;
					OnPropertyChanged();
				}
			}
		}

		public int TotalSections
		{
			get => CurrentJobProgressInfo.TotalSections;
			set { }
		}

		public TimeSpan RunTime
		{
			get => CurrentJobProgressInfo.RunTime;
			set { }
		}

		public TimeSpan EstimatedTimeRemaining
		{
			get => CurrentJobProgressInfo.EstimatedTimeRemaining;
			set { }
		}

		public double PercentComplete
		{
			get => CurrentJobProgressInfo.PercentComplete;
			set { }
		}

		public int FetchedCount
		{
			get => CurrentJobProgressInfo.FetchedCount;
			set { }
		}

		public int GeneratedCount
		{
			get => CurrentJobProgressInfo.GeneratedCount;
			set { }
		}

		#endregion

	}
}
