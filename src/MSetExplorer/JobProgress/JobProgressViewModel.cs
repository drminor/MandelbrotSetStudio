using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
			_currentJobProgressInfo = new JobProgressInfo();
			_isEnabled = false;

			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();
			IterationsPerSecond = 0;
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

			CurrentJobProgressInfo = new JobProgressInfo(msrJob.MapLoaderJobNumber, msrJob.ProcessingStartTime ?? DateTime.UtcNow, msrJob.SectionsRequested, msrJob.SectionsFoundInRepo);

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

			ReportProcessTimeAndMathOpDetails();
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
		private void ReportProcessTimeAndMathOpDetails()
		{
			var totalExecutionTime = RunTime;
			var sectionsGenerated = CurrentJobProgressInfo.GeneratedCount;
			var numberOfProcessors = 5;

			var averageProcessTimePerSection = totalExecutionTime * numberOfProcessors / sectionsGenerated;
			var totalGenerationDuration = MapSectionProcessInfos.Where(x => x.JobNumber == CurrentJobProgressInfo.JobNumber).Sum(x => x.GenerationDuration?.TotalSeconds);
			var calculationTime = totalGenerationDuration / numberOfProcessors;

			Debug.WriteLine($"Generated {sectionsGenerated} sections in {totalExecutionTime}. Average Processing Time / Section: {averageProcessTimePerSection}; Calculation Time: {calculationTime}.");

			if (AccumulateMops(out var mops))
			{
				var multiplications = mops.NumberOfMultiplications;
				var calcs = (long)mops.NumberOfCalcs;
				var unusedCalcs = (long)mops.NumberOfUnusedCalcs;

				Debug.WriteLine($"Performed: {multiplications:N0} multiplications. Iterations: {calcs:N0}; Discarded Iterations: {unusedCalcs:N0}.");

				IterationsPerSecond = (int)(calcs / totalExecutionTime.TotalSeconds);
				OnPropertyChanged(nameof(IterationsPerSecond));
			}
		}

		private bool AccumulateMops([NotNullWhen(true)] out MathOpCounts? mathOpCounts)
		{
			var itemsWithMops = MapSectionProcessInfos.Where(x => x.JobNumber == CurrentJobProgressInfo.JobNumber && x.MathOpCounts != null);

			if (!itemsWithMops.Any())
			{
				mathOpCounts = null;
				return false;
			}

			mathOpCounts = new MathOpCounts();

			foreach (var x in itemsWithMops)
			{
				mathOpCounts.Update(x.MathOpCounts!);
			}

			return true;
		}

		#endregion
	}
}
