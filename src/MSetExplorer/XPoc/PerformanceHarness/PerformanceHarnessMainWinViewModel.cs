using MapSectionProviderLib;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

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

		#endregion

		#region Constructor

		public PerformanceHarnessMainWinViewModel(MapSectionRequestProcessor mapSectionRequestProcessor, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper)
        {
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_mapSectionRequestProcessor.UseRepo = false;

			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			_overallElapsed = string.Empty;
			_generationElapsed = string.Empty;
			_processingElapsed = string.Empty;

			MapSectionProcessInfos = new List<MapSectionProcessInfo>();
			MathOpCounts = new MathOpCounts();

			NotifyPropChangedMaxPeek();
		}

		#endregion

		#region Public Properties

		private int _limbCount;
		public int LimbCount
		{
			get => _limbCount;
			set => _limbCount = value;
		}

		private int _generatedCount;
		public int GeneratedCount
		{
			get => _generatedCount;
			set => _generatedCount = value;
		}

		private string _overallElapsed;
		public string OverallElapsed
		{
			get => _overallElapsed;
			set => _overallElapsed = value;
		}

		private string _processingElapsed;
		public string ProcessingElapsed
		{
			get => _processingElapsed;
			set => _processingElapsed = value;
		}

		private string _generationElapsed;
		public string GenerationElapsed
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

		private long _totalCountOfAllOps;
		public long TotalCountOfAllOps
		{
			get => _totalCountOfAllOps;
			set => _totalCountOfAllOps = value;
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

		private long _vectorsNegatedForMult;
		public long VectorsNegatedForMult
		{
			get => _vectorsNegatedForMult;
			set => _vectorsNegatedForMult = value;
		}

		#endregion

		#region Public Methods

		public void RunHomeJob()
		{
			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);

			//var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN; // new RRectangle(-4, 4, -4, 4, -1);

			var coords = new RRectangle(0, 4, 0, 4, -1);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 1000, threshold:4, requestsPerJob: 100);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

			RunTest(job);
		}

		//public void RunDenseLC2()
		//{
		//	var blockSize = RMapConstants.BLOCK_SIZE;
		//	var sizeInWholeBlocks = new SizeInt(8);
		//	var canvasSize = sizeInWholeBlocks.Scale(blockSize);

		//	//var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

		//	var x1 = 32;
		//	var x2 = 64;
		//	var y1 = 32;
		//	var y2 = 64;
		//	var exponent = -8;
		//	var coords = new RRectangle(x1, x2, y1, y2, exponent, precision: RMapConstants.DEFAULT_PRECISION);

		//	var mapCalcSettings = new MapCalcSettings(targetIterations: 400, threshold:4, requestsPerJob: 100);
		//	var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
		//	var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

		//	RunTest(job);
		//}

		public void RunDenseLC4()
		{
			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);

			//var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

			//P1: -14560970492204182605182 / 2 ^ 74; 2388421341043486517661 / 2 ^ 74,
			//P2: -14560970492204182605180 / 2 ^ 74; 2388421341043486517663 / 2 ^ 74.
			//SamplePointDelta: 1 / 2 ^ 83; 1 / 2 ^ 83


			var x1 = BigInteger.Parse("-14560970492204182605182");
			var x2 = BigInteger.Parse("-14560970492204182605180");
			var y1 = BigInteger.Parse("2388421341043486517661");
			var y2 = BigInteger.Parse("2388421341043486517663");
			var exponent = -74;
			var coords = new RRectangle(x1, x2, y1, y2, exponent, precision: RMapConstants.DEFAULT_PRECISION);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 400, threshold: 4, requestsPerJob: 100);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

			RunTest(job);
		}


		#endregion

		#region Private Methods

		private void RunTest(Job job)
		{
			NotifyPropChangedMaxPeek();
			MapSectionProcessInfos.Clear();

			var ownerId = job.ProjectId.ToString();
			var jobOwnerType = JobOwnerType.Project;

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(job.Coords, job.CanvasSize, RMapConstants.BLOCK_SIZE);
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, job.MapCalcSettings);

			LimbCount = mapSectionRequests[0].LimbCount;

			var mapLoader = new MapLoader(MapSectionReady, _mapSectionRequestProcessor);
			mapLoader.SectionLoaded += MapLoader_SectionLoaded;

			var stopWatch = Stopwatch.StartNew();
			var startTask = mapLoader.Start(mapSectionRequests);

			JobProgressInfo = new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count);

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
				Debug.WriteLine($"Fetched: {JobProgressInfo.FetchedCount}, Generated: {JobProgressInfo.GeneratedCount}. MapLoader Overall Time: {mapLoader.ElaspedTime}.");
				UpdateUi(stopWatch, JobProgressInfo, mapLoader.ElaspedTime);
			}
			else
			{
				Debug.WriteLine("The JobProgressInfo is null.");
			}
		}

		private void UpdateUi(Stopwatch stopwatch, JobProgressInfo jobProgressInfo, TimeSpan mapLoaderOverall)
		{
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
					sumProcessingDurations += x.ProcessingDuration.Value.TotalSeconds;
				}

				if (x.GenerationDuration.HasValue)
				{
					sumGenerationDurations += x.GenerationDuration.Value.TotalSeconds;
				}
			}

			MathOpCounts = mops;

			Debug.WriteLine($"Generated {jobProgressInfo.GeneratedCount} sections in {stopwatch.ElapsedMilliseconds}. Performed: {mops.NumberOfMultiplications}. " +
				$"Performed: {mops.NumberOfCalcs} used iterations and {mops.NumberOfUnusedCalcs} unused iterations.");

			GeneratedCount = jobProgressInfo.GeneratedCount;
			var threadCount = 5;

			var diff = stopwatch.Elapsed - mapLoaderOverall;

			var diffS = Math.Round(diff.TotalMilliseconds, 4).ToString();

			OverallElapsed = Math.Round(mapLoaderOverall.TotalSeconds , 4).ToString();
			ProcessingElapsed = Math.Round(sumProcessingDurations / threadCount, 6).ToString();
			GenerationElapsed = Math.Round(sumGenerationDurations / threadCount, 6).ToString();

			Multiplications = mops.NumberOfMultiplications;
			Additions = mops.NumberOfAdditions;

			VectorsNegatedForMult = mops.NumberOfNegations;

			//Negations = mops.NumberOfNegations;
			Conversions = mops.NumberOfConversions;
			Splits = mops.NumberOfSplits;
			Comparisons = mops.NumberOfComparisons;

			Calcs = (long)mops.NumberOfCalcs;
			UnusedCalcs = (long) mops.NumberOfUnusedCalcs;

			TotalCountOfAllOps = Multiplications + Additions + Negations + Conversions + Splits + Comparisons;

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
			}
		}

		private void HandleRunComplete()
		{
			OnPropertyChanged(nameof(LimbCount));
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
			OnPropertyChanged(nameof(TotalCountOfAllOps));
			OnPropertyChanged(nameof(VectorsNegatedForMult));

			OnPropertyChanged(nameof(Calcs));
			OnPropertyChanged(nameof(UnusedCalcs));
		}

		private void NotifyPropChangedMaxPeek()
		{
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

		public void ResetMapSectionRequestProcessor()
		{
			_mapSectionRequestProcessor.UseRepo = true;
		}

		#endregion
	}
}
