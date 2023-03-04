using MapSectionProviderLib;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer.XPoc.PerformanceHarness
{
	internal class PerformanceHarnessMainWinViewModel : ViewModelBase
	{
		#region Private Properties

		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;

		public JobProgressInfo? JobProgressInfo;
		public List<MapSectionProcessInfo> MapSectionProcessInfos;

		public MathOpCounts MathOpCounts { get; set; }

		//private MapLoader? _mapLoader;
		//private Task? _startTask;

		//private Stopwatch _stopWatch;
		//private bool _receviedTheLastOne;

		#endregion

		#region Constructor

		public PerformanceHarnessMainWinViewModel(MapSectionRequestProcessor mapSectionRequestProcessor, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper)
        {
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_mapSectionRequestProcessor.UseRepo = false;

			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			MapSectionProcessInfos = new List<MapSectionProcessInfo>();
			MathOpCounts = new MathOpCounts();

			NotifyPropChangedMaxPeek();
		}

		#endregion

		#region Public Properties

		private int _generatedCount;
		public int GeneratedCount
		{
			get => _generatedCount;
			set => _generatedCount = value;
		}

		private long _overallElapsed;
		public long OverallElapsed
		{
			get => _overallElapsed;
			set => _overallElapsed = value;
		}

		private long _processingElapsed;
		public long ProcessingElapsed
		{
			get => _processingElapsed;
			set => _processingElapsed = value;
		}

		private long _generationElapsed;
		public long GenerationElapsed
		{
			get => _generationElapsed;
			set => _generationElapsed = value;
		}

		private long _multiplications;
		public long Multiplications
		{
			get => _multiplications;
			set => _multiplications = value;
		}

		private long _additions;
		public long Additions
		{
			get => _additions;
			set => _additions = value;
		}

		private long _negations;
		public long Negations
		{
			get => _negations;
			set => _negations = value;
		}

		private long _conversions;
		public long Conversions
		{
			get => _conversions;
			set => _conversions = value;
		}

		private long _splits;
		public long Splits
		{
			get => _splits;
			set => _splits = value;
		}

		private long _comparisons;
		public long Comparisons
		{
			get => _comparisons;
			set => _comparisons = value;
		}

		private long _calcs;
		public long Calcs
		{
			get => _calcs;
			set => _calcs = value;
		}

		private long _unusedCalcs;
		public long UnusedCalcs
		{
			get => _unusedCalcs;
			set => _unusedCalcs = value;
		}

		//public int MaxPeakSections
		//{
		//	get => _mapSectionHelper.MaxPeakSections;
		//	set { }
		//}

		public int MaxPeakSectionVectors
		{
			get => _mapSectionHelper.MaxPeakSectionVectors;
			set { }
		}

		public int MaxPeakSectionZVectors
		{
			get => _mapSectionHelper.MaxPeakSectionZVectors;
			set { }
		}

		#endregion

		#region Public Methods

		public void RunBaseLine()
		{
			NotifyPropChangedMaxPeek();

			MapSectionProcessInfos.Clear();

			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);

			var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN;
			var mapCalcSettings = new MapCalcSettings(targetIterations: 400, requestsPerJob: 100);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

			//var jobProgressInfo = RunJob(job, out var mapLoader, out var startTask);

			var ownerId = job.ProjectId.ToString();
			var jobOwnerType = JobOwnerType.Project;

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(job.Coords, job.CanvasSize, RMapConstants.BLOCK_SIZE);
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, job.MapCalcSettings);

			var mapLoader = new MapLoader(MapSectionReady, _mapSectionRequestProcessor);

			var stopWatch = Stopwatch.StartNew();
			var startTask = mapLoader.Start(mapSectionRequests);
			
			JobProgressInfo = new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count);

			mapLoader.SectionLoaded += MapLoader_SectionLoaded;

			for (var i = 0; i < 100; i++)
			{
				Thread.Sleep(100);

				if (startTask.IsCompleted)
				{
					stopWatch.Stop();
					break;
				}
				//Debug.WriteLine($"Cnt: {i}. RunBaseLine is sleeping for 100ms.");
			}

			if (JobProgressInfo != null)
			{
				Debug.WriteLine($"Fetched: {JobProgressInfo.FetchedCount}, Generated: {JobProgressInfo.GeneratedCount}.");
				UpdateUi(stopWatch, JobProgressInfo);
			}
			else
			{
				Debug.WriteLine("The JobProgressInfo is null.");
			}
		}

		private void UpdateUi(Stopwatch stopwatch, JobProgressInfo jobProgressInfo)
		{
			//if (_startTask == null || _mapLoader == null)
			//{
			//	Debug.WriteLine("The start task or the MapLoader is null.");
			//	return;
			//}
			//else
			//{
			//	if (_startTask.IsCompletedSuccessfully)
			//	{
			//		Debug.WriteLine($"Task for {JobProgressInfo.JobNumber} completed successfully.");
			//	}
			//	else
			//	{
			//		Debug.WriteLine($"Task for {JobProgressInfo.JobNumber} did NOT complete successfully. The exception is {_startTask.Exception}.");
			//	}
			//}

			var mops = new MathOpCounts();
			var sumProcessingDurations = 0d;
			var sumGenerationDurations = 0d;

			foreach (var x in MapSectionProcessInfos)
			{
				if (x.MathOpCounts != null)
				{
					mops.Update(x.MathOpCounts);
				}

				if (x.ProcessingDuration.HasValue)
				{
					sumProcessingDurations += x.ProcessingDuration.Value.TotalMilliseconds;
				}

				if (x.GenerationDuration.HasValue)
				{
					sumGenerationDurations += x.GenerationDuration.Value.TotalMilliseconds;
				}
			}

			MathOpCounts = mops;

			Debug.WriteLine($"Generated {jobProgressInfo.GeneratedCount} sections in {stopwatch.ElapsedMilliseconds}. Performed: {mops.NumberOfMultiplications}. " +
				$"Performed: {mops.NumberOfCalcs} used iterations and {mops.NumberOfUnusedCalcs} unused iterations.");

			GeneratedCount = jobProgressInfo.GeneratedCount;

			OverallElapsed = stopwatch.ElapsedMilliseconds;
			ProcessingElapsed = (long)Math.Round(sumProcessingDurations * 1000);
			GenerationElapsed = (long)Math.Round(sumGenerationDurations * 1000);

			Multiplications = mops.NumberOfMultiplications;
			Additions = mops.NumberOfAdditions;
			Negations = mops.NumberOfNegations;
			Conversions = mops.NumberOfConversions;
			Splits = mops.NumberOfSplits;
			Comparisons = mops.NumberOfComparisons;

			Calcs = (long)mops.NumberOfCalcs;
			UnusedCalcs = (long) mops.NumberOfUnusedCalcs;

			HandleRunComplete();
			NotifyPropChangedMaxPeek();
		}

		private void MapLoader_SectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			MapSectionProcessInfos.Add(e);

			if (JobProgressInfo != null)
			{
				if (e.FoundInRepo)
				{
					JobProgressInfo.FetchedCount++;

				}
				else
				{
					JobProgressInfo.GeneratedCount++;
				}

				//if (JobProgressInfo.IsComplete || _receviedTheLastOne)
				//{
				//	_stopWatch.Stop();

				//	if (_startTask == null || _mapLoader == null)
				//	{
				//		Debug.WriteLine("The start task or the MapLoader is null.");
				//		return;
				//	}
				//	else
				//	{
				//		if (_startTask.IsCompletedSuccessfully)
				//		{
				//			Debug.WriteLine($"Task for {JobProgressInfo.JobNumber} completed successfully.");
				//		}
				//		else
				//		{
				//			Debug.WriteLine($"Task for {JobProgressInfo.JobNumber} did NOT complete successfully. The exception is {_startTask.Exception}.");
				//		}
				//	}

				//	var mops = new MathOpCounts();
				//	var sumProcessingDurations = new TimeSpan();
				//	var sumGenerationDurations = new TimeSpan();

				//	foreach (var x in MapSectionProcessInfos)
				//	{
				//		if (x.MathOpCounts != null)
				//		{
				//			mops.Update(x.MathOpCounts);
				//		}

				//		if (x.ProcessingDuration.HasValue)
				//		{
				//			sumProcessingDurations.Add(x.ProcessingDuration.Value);
				//		}

				//		if (x.GenerationDuration.HasValue)
				//		{
				//			sumGenerationDurations.Add(x.GenerationDuration.Value);
				//		}
				//	}


				//	MathOpCounts = mops;

				//	Debug.WriteLine($"Generated {JobProgressInfo.GeneratedCount} sections in {_stopWatch.ElapsedMilliseconds}. Performed: {mops.NumberOfMultiplications}." +
				//		$"{mops.NumberOfCalcs} iteration calculation used; {mops.NumberOfUnusedCalcs} unused.");

				//	GeneratedCount = JobProgressInfo.GeneratedCount;

				//	OverallElapsed = _stopWatch.ElapsedMilliseconds;
				//	ProcessingElapsed = (long)Math.Round(sumProcessingDurations.TotalMilliseconds);
				//	GenerationElapsed = (long)Math.Round(sumProcessingDurations.TotalMilliseconds);

				//	Multiplications = mops.NumberOfMultiplications;
				//	Calcs = (long)mops.NumberOfCalcs;

				//	_synchronizationContext?.Post((o) => HandleRunComplete(), null);

				//}
			}
		}

		private void HandleRunComplete()
		{
			OnPropertyChanged(nameof(GeneratedCount));
			OnPropertyChanged(nameof(OverallElapsed));
			OnPropertyChanged(nameof(ProcessingElapsed));
			OnPropertyChanged(nameof(GenerationElapsed));

			OnPropertyChanged(nameof(Multiplications));
			OnPropertyChanged(nameof(Additions));
			OnPropertyChanged(nameof(Negations));
			OnPropertyChanged(nameof(Conversions));
			OnPropertyChanged(nameof(Splits));
			OnPropertyChanged(nameof(Comparisons));


			OnPropertyChanged(nameof(Calcs));
			OnPropertyChanged(nameof(UnusedCalcs));
		}

		private void NotifyPropChangedMaxPeek()
		{
			//OnPropertyChanged(nameof(MaxPeakSections));
			OnPropertyChanged(nameof(MaxPeakSectionVectors));
			OnPropertyChanged(nameof(MaxPeakSectionZVectors));
		}

		private void MapSectionReady(MapSection mapSection, int jobId, bool isLast)
		{
			if (isLast)
			{
				//_receviedTheLastOne = true;
				Debug.WriteLine($"{jobId} is complete. Received {MapSectionProcessInfos.Count} process infos.");
			}
			else
			{
				//Debug.WriteLine($"Got a mapSection.");
			}
		}

		public void StopJob(int jobNumber)
		{
			//DoWithWriteLock(() =>
			//{
			//	StopCurrentJobInternal(jobNumber);
			//});
		}

		private void StopCurrentJobInternal(int jobNumber)
		{
			//var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

			//if (request != null)
			//{
			//	request.MapLoader.Stop();
			//}
		}

		public void ResetMapSectionRequestProcessor()
		{
			_mapSectionRequestProcessor.UseRepo = true;
		}

		//private JobProgressInfo RunJob(Job job, out MapLoader mapLoader, out Task startTask)
		//{
		//	var ownerId = job.ProjectId.ToString();
		//	var jobOwnerType = JobOwnerType.Project;

		//	var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(job.Coords, job.CanvasSize, RMapConstants.BLOCK_SIZE);
		//	var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, job.MapCalcSettings);

		//	mapLoader = new MapLoader(mapAreaInfo.MapBlockOffset, MapSectionReady, _mapSectionHelper, _mapSectionRequestProcessor);

		//	_stopWatch = Stopwatch.StartNew();
		//	startTask = mapLoader.Start(mapSectionRequests);

		//	var result = new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count);

		//	mapLoader.SectionLoaded += MapLoader_SectionLoaded;

		//	return result;
		//}

		#endregion
	}
}
