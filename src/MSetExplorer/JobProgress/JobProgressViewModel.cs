using MSS.Common;
using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace MSetExplorer
{
	public class JobProgressViewModel : ViewModelBase
	{
		#region Constructor

		private readonly SynchronizationContext? _synchronizationContext;
		private readonly IMapLoaderManager _mapLoaderManager;
		private JobProgressInfo _currentJobProgressInfo;

		public JobProgressViewModel(IMapLoaderManager mapLoaderManager)
		{
			Debug.WriteLine("The JobProgressViewModel is being loaded.");
			_synchronizationContext = SynchronizationContext.Current;
			_mapLoaderManager = mapLoaderManager;
			_currentJobProgressInfo = new JobProgressInfo(0, "temp", DateTime.UtcNow, 0);
			MapSectionProcessInfos = new ObservableCollection<MapSectionProcessInfo>();


			// TODO: Only subscribe if we are visible.
			_mapLoaderManager.RequestAdded += MapLoaderManager_RequestAdded;
			_mapLoaderManager.SectionLoaded += MapLoaderManager_SectionLoaded;	
		}

		private void MapLoaderManager_RequestAdded(object? sender, JobProgressInfo e)
		{
			_synchronizationContext?.Post((o) => HandleRequstAdded(e), null);
		}

		private void HandleRequstAdded(JobProgressInfo jobProgressInfo)
		{
			CurrentJobProgressInfo = jobProgressInfo;
			CurrentJobProgressInfo.DateCreatedUtc = DateTime.UtcNow;

			OnPropertyChanged(nameof(TotalSections));
		}

		private void MapLoaderManager_SectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			//Debug.WriteLine($"Got a RequestCompleted event. JobNumber: {e.JobNumber}, Number Completed: {e.RequestsCompleted}.");
			_synchronizationContext?.Post((o) => HandleMapSectionLoaded(e), null);
		}

		private void HandleMapSectionLoaded(MapSectionProcessInfo mapSectionProcessInfo)
		{
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

			if (mapSectionProcessInfo.IsLastSection)
			{
				Report(_mapLoaderManager.GetExecutionTimeForJob(mapSectionProcessInfo.JobNumber));
			}

			//OnPropertyChanged(nameof(RunTime));
			//OnPropertyChanged(nameof(EstimatedTimeRemaining));

			//OnPropertyChanged(nameof(FetchedCount));
			//OnPropertyChanged(nameof(GeneratedCount));

			//OnPropertyChanged(nameof(PercentComplete));

			////MapSectionProcessInfos.Clear();
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
					if (x.MathOpCounts != null)
					{
						mops.Update(x.MathOpCounts);
					}

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
