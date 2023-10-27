using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;

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

		public ObservableCollection<MapSectionProcessInfo> MapSectionProcessInfos { get; init; }

		public JobProgressInfo CurrentJobProgressInfo
		{
			get => _currentJobProgressInfo;
			set
			{
				if (value != _currentJobProgressInfo)
				{
					_currentJobProgressInfo = value;
					OnPropertyChanged();
				}
			}
		}

		public int TotalSections => CurrentJobProgressInfo.TotalSections;

		public int CancelledCount => CurrentJobProgressInfo.CancelledCount;

		public int FetchedCount => CurrentJobProgressInfo.FetchedCount;

		public int GeneratedCount => CurrentJobProgressInfo.GeneratedCount;

		public double PercentComplete => CurrentJobProgressInfo.PercentComplete;

		public TimeSpan RunTime => CurrentJobProgressInfo.RunTime;

		public TimeSpan EstimatedTimeRemaining => CurrentJobProgressInfo.EstimatedTimeRemaining;

		public int IterationsPerSecond { get; set; }

		#endregion

		#region Event Handlers

		private void MapLoaderManager_RequestAdded(object? sender, MsrJob e)
		{
			_synchronizationContext?.Post((o) => HandleRequestAdded(e), null);
		}

		private void MsrJob_MapSectionLoaded(object? sender, MapSectionRequest e)
		{
			//Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleMapSectionLoaded(e), null);
		}

		private void MsrJob_JobHasCompleted(object? sender, EventArgs e)
		{
			if (sender is MsrJob msrJob)
			{
				//_synchronizationContext?.Post((o) => HandleJobHasCompleted(msrJob), null);

				if (msrJob.MapLoaderJobNumber == CurrentJobProgressInfo.JobNumber)
				{
					CurrentJobProgressInfo.IsComplete = true;
				}

			}
		}

		#endregion

		#region Private Methods

		private void HandleRequestAdded(MsrJob msrJob)
		{
			msrJob.MapSectionLoaded += MsrJob_MapSectionLoaded;
			msrJob.JobHasCompleted += MsrJob_JobHasCompleted;

			MapSectionProcessInfos.Clear();
			IterationsPerSecond = 0;

			CurrentJobProgressInfo = new JobProgressInfo(msrJob.MapLoaderJobNumber, "Temp", msrJob.ProcessingStartTime ?? DateTime.Now, msrJob.SectionsRequested, msrJob.SectionsFoundInRepo);
			CurrentJobProgressInfo.DateCreatedUtc = DateTime.UtcNow;

			OnPropertyChanged(nameof(TotalSections));

			OnPropertyChanged(nameof(FetchedCount));
			OnPropertyChanged(nameof(GeneratedCount));

			OnPropertyChanged(nameof(PercentComplete));
			OnPropertyChanged(nameof(RunTime));
			OnPropertyChanged(nameof(EstimatedTimeRemaining));
			OnPropertyChanged(nameof(IterationsPerSecond));
		}

		private void HandleMapSectionLoaded(MapSectionRequest mapSectionRequest)
		{
			var mapSectionProcessInfo = CreateMSProcInfo(mapSectionRequest);

			MapSectionProcessInfos.Add(mapSectionProcessInfo);

			if (mapSectionProcessInfo.JobNumber == CurrentJobProgressInfo.JobNumber)
			{
				if (mapSectionProcessInfo.RequestWasCancelled)
				{
					CurrentJobProgressInfo.CancelledCount += 1;
				}
				else if (mapSectionProcessInfo.FoundInRepo)
				{
					CurrentJobProgressInfo.FetchedCount += 1;
				}
				else
				{
					CurrentJobProgressInfo.GeneratedCount += 1;
				}

				OnPropertyChanged(nameof(FetchedCount));
				OnPropertyChanged(nameof(GeneratedCount));

				OnPropertyChanged(nameof(PercentComplete));
				OnPropertyChanged(nameof(RunTime));
				OnPropertyChanged(nameof(EstimatedTimeRemaining));

				if (CurrentJobProgressInfo.SectionsReceived == CurrentJobProgressInfo.TotalSections)
				{
					HandleJobHasCompleted(mapSectionRequest);
				}
			}
		}

		private void HandleJobHasCompleted(MapSectionRequest mapSectionRequest)
		{
			var msrJob = mapSectionRequest.MsrJob;

			msrJob.JobHasCompleted -= MsrJob_JobHasCompleted;
			msrJob.MapSectionLoaded -= MsrJob_MapSectionLoaded;

			//var totalExecutionTime = msrJob.TotalExecutionTime;
			var totalExecutionTime = RunTime;
			Report(totalExecutionTime);

			//MapSectionProcessInfos.Clear();
		}

		private void HandleJobHasCompleted(MsrJob msrJob)
		{
			if (msrJob.MapLoaderJobNumber == CurrentJobProgressInfo.JobNumber)
			{
				CurrentJobProgressInfo.IsComplete = true;
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

		[Conditional("DEBUG")]
		private void Report(TimeSpan totalExecutionTime)
		{
			var mops = new MathOpCounts();
			//var sumProcessingDurations = new TimeSpan();
			var sumGenerationDurations = new TimeSpan();
			var haveMops = false;


			foreach (var x in MapSectionProcessInfos)
			{
				if (x.JobNumber == CurrentJobProgressInfo.JobNumber)
				{
					if (x.MathOpCounts != null)
					{
						mops.Update(x.MathOpCounts);
						haveMops = true;
					}

					//if (x.ProcessingDuration.HasValue)
					//{
					//	sumProcessingDurations += x.ProcessingDuration.Value;
					//}

					if (x.GenerationDuration.HasValue)
					{
						sumGenerationDurations += x.GenerationDuration.Value;
					}
				}
			}

			var sectionsGenerated = CurrentJobProgressInfo.GeneratedCount;
			var numberOfProcessors = 5;

			var averageProcessTimePerSection = totalExecutionTime * numberOfProcessors / sectionsGenerated;
			Debug.WriteLine($"Generated {sectionsGenerated} sections in {totalExecutionTime}. Average Processing Time / Section: {averageProcessTimePerSection}; Calculation Time: {sumGenerationDurations / numberOfProcessors}.");

			var multiplications = mops.NumberOfMultiplications;
			var calcs = (long)mops.NumberOfCalcs;
			var unusedCalcs = (long)mops.NumberOfUnusedCalcs;

			Debug.WriteLine($"Performed: {multiplications:N0} multiplications. Iterations: {calcs:N0}; Discarded Iterations: {unusedCalcs:N0}.");

			if (haveMops)
			{
				IterationsPerSecond = (int)(calcs / totalExecutionTime.TotalSeconds);
				OnPropertyChanged(nameof(IterationsPerSecond));
			}

			//var processingElapsed = (long)Math.Round(sumProcessingDurations * 1000);
			//var generationElapsed = (long)Math.Round(sumGenerationDurations * 1000);

			//Debug.WriteLine($"Total Processing Time: {sumProcessingDurations}; Time to generate: {sumGenerationDurations}; Multiplications: {multiplications}; Iterations: {calcs}; Discarded Iterations: {unusedCalcs}.");
		}

		#endregion
	}
}
