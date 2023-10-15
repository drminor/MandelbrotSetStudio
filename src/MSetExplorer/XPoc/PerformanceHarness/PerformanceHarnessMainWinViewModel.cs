using MapSectionProviderLib;
using MSS.Common;
using MSS.Common.MSet;
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
		#region Private Fields

		private readonly CancellationTokenSource _cts;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private readonly MapLoaderManager _mapLoaderManager;

		private readonly MapSectionBuilder _mapSectionBuilder;

		public JobProgressInfo? JobProgressInfo;

		//public List<MapSectionProcessInfo> MapSectionProcessInfos;
		public List<MapSection> MapSections;

		//public List<Tuple<long, string>> Timings;

		private Stopwatch _stopwatch1;


		public MathOpCounts MathOpCounts { get; set; }

		//private bool _receivedTheLastOne;

		private int _nextMapLoaderJobNumber = 0;

		private MsrJob? _currentMsrJob;

		#endregion

		#region Constructor

		public PerformanceHarnessMainWinViewModel(MapSectionRequestProcessor mapSectionRequestProcessor, MapJobHelper mapJobHelper, MapSectionVectorProvider mapSectionVectorProvider)
        {
			_stopwatch1 = Stopwatch.StartNew();
			_stopwatch1.Stop();

			_cts = new CancellationTokenSource();
			_currentMsrJob = null;

			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_mapSectionRequestProcessor.UseRepo = false;

			_mapJobHelper = mapJobHelper;
			_mapSectionVectorProvider = mapSectionVectorProvider;

			_mapLoaderManager = new MapLoaderManager(_mapSectionRequestProcessor);

			_mapSectionBuilder = new MapSectionBuilder();


			_overallElapsed = string.Empty;
			_generationElapsed = string.Empty;
			_processingElapsed = string.Empty;

			//_receivedTheLastOne = false;

			//MapSectionProcessInfos = new List<MapSectionProcessInfo>();
			MapSections = new List<MapSection>();

			//Timings = new List<Tuple<long, string>>();
			MathOpCounts = new MathOpCounts();

			NotifyPropChangedMaxPeek();
		}

		#endregion

		#region Public Properties

		private bool _useEscapeVelocities;

		public bool UseEscapeVelocities
		{
			get => _useEscapeVelocities;
			set
			{
				if (value != _useEscapeVelocities)
				{
					_useEscapeVelocities = value;
					OnPropertyChanged();
				}
			}
		}

		private bool _saveTheZValues;
		public bool SaveTheZValues
		{
			get => _saveTheZValues;
			set
			{
				if (value != _saveTheZValues)
				{
					_saveTheZValues = value;
					OnPropertyChanged();
				}
			}
		}

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
			get => _mapSectionVectorProvider.MaxPeakSectionVectors;
			set { }
		}

		public int MaxPeakSectionZVectors
		{
			get => _mapSectionVectorProvider.MaxPeakSectionZVectors;
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
			var mapAreaInfo = _mapJobHelper.GetCenterAndDelta(coords, canvasSize);

			var targetIterations = 1000;
			var threshold = UseEscapeVelocities ? RMapConstants.DEFAULT_NORMALIZED_THRESHOLD : RMapConstants.DEFAULT_THRESHOLD;

			var mapCalcSettings = new MapCalcSettings(targetIterations, threshold, UseEscapeVelocities, SaveTheZValues);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);

			var job = _mapJobHelper.BuildHomeJob(OwnerType.Project, mapAreaInfo, colorBandSet.Id, mapCalcSettings);

			_currentMsrJob = RunTest(job, _nextMapLoaderJobNumber++);

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
			//var sizeInWholeBlocks = new SizeInt(8);
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


			var mapAreaInfo = _mapJobHelper.GetCenterAndDelta(coords, canvasSize);

			var targetIterations = 400;
			var threshold = UseEscapeVelocities ? RMapConstants.DEFAULT_NORMALIZED_THRESHOLD : RMapConstants.DEFAULT_THRESHOLD; 

			var mapCalcSettings = new MapCalcSettings(targetIterations, threshold, UseEscapeVelocities, SaveTheZValues);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			
			var job = _mapJobHelper.BuildHomeJob(OwnerType.Project, mapAreaInfo, colorBandSet.Id, mapCalcSettings);

			_currentMsrJob = RunTest(job, _nextMapLoaderJobNumber++);
		}

		#endregion

		#region Private Methods

		//private void RunTestOld(Job job, int mapLoaderJobNumber)
		//{
		//	//_receivedTheLastOne = false;
		//	//MapSectionProcessInfos.Clear();

		//	foreach (var ms in MapSections)
		//	{
		//		_mapSectionVectorProvider.ReturnToPool(ms);
		//	}

		//	MapSections.Clear();
		//	//Timings.Clear();

		//	NotifyPropChangedMaxPeek();

		//	var jobId = job.Id.ToString();
		//	var ownerType = OwnerType.Project;
		//	var jobType = JobType.FullScale;

		//	var stopwatch = Stopwatch.StartNew();
		//	//_stopwatch1.Restart();
		//	//AddTiming("Start");


		//	//AddTiming("GetMapAreaInfo");

		//	var oldAreaInfo = _mapJobHelper.GetMapAreaWithSize(job.MapAreaInfo, new SizeDbl(1024));

		//	//var mapAreaInfoWithSize = GetMapAreaWithSizeFat(areaColorAndCalcSettings, imageSize);
		//	//var jobId = new ObjectId(areaColorAndCalcSettings.JobId);
		//	//createImageProgressViewModel.CreateImage(imageFilePath, jobId, mapAreaInfoWithSize, areaColorAndCalcSettings.ColorBandSet, areaColorAndCalcSettings.MapCalcSettings);

		//	//var mapSectionRequests = _mapSectionBuilder.CreateSectionRequests(mapLoaderJobNumber, jobType, jobId, ownerType, oldAreaInfo, job.MapCalcSettings);

		//	var msrJob = CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, job.Id.ToString(), ownerType, oldAreaInfo, job.MapCalcSettings);
		//	var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(oldAreaInfo.CanvasSize.Round(), oldAreaInfo.CanvasControlOffset, oldAreaInfo.Subdivision.BlockSize);
		//	var mapSectionRequests = _mapSectionBuilder.CreateSectionRequests(msrJob, mapExtentInBlocks);

		//	//AddTiming("CreateSectionRequest");

		//	LimbCount = mapSectionRequests[0].LimbCount;

		//	// TODO: Update the PerformanceViewModel to use the MapLoaderManager instead of the MapLoader.
			
		//	var newJobNumber = _mapSectionRequestProcessor.GetNextJobNumber();

		//	var mapLoader = new MapLoader(newJobNumber, MapSectionReady, _mapSectionRequestProcessor);
		//	//AddTiming("Construct MapLoader");
		//	//mapLoader.SectionLoaded += MapLoader_SectionLoaded;

		//	var startTask = mapLoader.Start(mapSectionRequests);
		//	//AddTiming("Start MapLoader");

		//	JobProgressInfo = new JobProgressInfo(newJobNumber, "temp", DateTime.Now, mapSectionRequests.Count, numberOfSectionsFetched: 0);

		//	for (var i = 0; i < 1000; i++)
		//	{
		//		Thread.Sleep(100);

		//		if (startTask.IsCompleted)
		//		{
		//			stopwatch.Stop();
		//			break;
		//		}

		//		//if (_receivedTheLastOne)
		//		//{
		//		//	stopwatch.Stop();
		//		//	break;
		//		//}

		//		//Debug.WriteLine($"Cnt: {i}. RunBaseLine is sleeping for 100ms.");
		//	}

		//	//AddTiming("MapLoader Completed");

		//	if (JobProgressInfo != null)
		//	{
		//		Debug.WriteLine($"Fetched: {JobProgressInfo.FetchedCount}, Generated: {JobProgressInfo.GeneratedCount}. MapLoader Overall Time: {mapLoader.ElaspedTime}.");

		//		//var prevTm = 0L;

		//		//foreach(var tm in Timings)
		//		//{
		//		//	Debug.WriteLine($"{tm.Item2}:{tm.Item1}\t{tm.Item1 - prevTm}");
		//		//	prevTm = tm.Item1;
		//		//}

		//		UpdateUi(stopwatch, JobProgressInfo, mapLoader.ElaspedTime);
		//	}
		//	else
		//	{
		//		Debug.WriteLine("The JobProgressInfo is null.");
		//	}

		//	foreach (var ms in MapSections)
		//	{
		//		_mapSectionVectorProvider.ReturnToPool(ms);
		//	}

		//	MapSections.Clear();
		//}

		private MsrJob RunTest(Job job, int mapLoaderJobNumber)
		{
			//_receivedTheLastOne = false;
			//MapSectionProcessInfos.Clear();

			foreach (var ms in MapSections)
			{
				_mapSectionVectorProvider.ReturnToPool(ms);
			}

			MapSections.Clear();
			//Timings.Clear();

			NotifyPropChangedMaxPeek();

			var jobId = job.Id.ToString();
			var ownerType = OwnerType.Project;
			var jobType = JobType.FullScale;

			//var stopwatch = Stopwatch.StartNew();
			_stopwatch1.Restart();
			//AddTiming("Start");

			var oldAreaInfo = _mapJobHelper.GetMapPositionSizeAndDelta(job.MapAreaInfo, new SizeDbl(1024));
			//AddTiming("GetMapAreaInfo");

			var msrJob = CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, job.Id.ToString(), ownerType, oldAreaInfo, job.MapCalcSettings);
			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(oldAreaInfo.CanvasSize.Round(), oldAreaInfo.CanvasControlOffset, oldAreaInfo.Subdivision.BlockSize);
			var mapSectionRequests = _mapSectionBuilder.CreateSectionRequests(msrJob, mapExtentInBlocks);
			//AddTiming("CreateSectionRequest");

			JobProgressInfo = new JobProgressInfo(msrJob.MapLoaderJobNumber, "Temp", DateTime.Now, msrJob.TotalNumberOfSectionsRequested, msrJob.SectionsFoundInRepo);

			List<MapSection> mapSections = _mapLoaderManager.Push(msrJob, mapSectionRequests, MapSectionReady, MapViewUpdateIsComplete, _cts.Token, out var requestsPendingGenerations);

			return msrJob;
		}

		private void RunTestContinuation(MsrJob msrJob)
		{
			//AddTiming("MapLoader Completed");

			if (JobProgressInfo != null)
			{
				TimeSpan? elaspedTime;

				if (msrJob.ProcessingEndTime.HasValue && msrJob.ProcessingStartTime.HasValue)
				{
					elaspedTime = msrJob.ProcessingEndTime - msrJob.ProcessingStartTime;

				}
				else
				{
					elaspedTime = _stopwatch1.Elapsed;
				}

				Debug.WriteLine($"Fetched: {JobProgressInfo.FetchedCount}, Generated: {JobProgressInfo.GeneratedCount}. MapLoader Overall Time: {elaspedTime}.");

				//var prevTm = 0L;

				//foreach(var tm in Timings)
				//{
				//	Debug.WriteLine($"{tm.Item2}:{tm.Item1}\t{tm.Item1 - prevTm}");
				//	prevTm = tm.Item1;
				//}

				//UpdateUi(_stopwatch1, JobProgressInfo, mapLoader.ElaspedTime);
				UpdateUi(_stopwatch1, JobProgressInfo, elaspedTime.Value);
			}
			else
			{
				Debug.WriteLine("The JobProgressInfo is null.");
			}

			foreach (var ms in MapSections)
			{
				_mapSectionVectorProvider.ReturnToPool(ms);
			}

			MapSections.Clear();
		}

		private void MapSectionReady(MapSection mapSection)
		{
			if (mapSection.MapSectionProcessInfo != null)
			{
				MapSections.Add(mapSection);

				if (JobProgressInfo != null)
				{
					if (mapSection.MapSectionProcessInfo.FoundInRepo)
					{
						JobProgressInfo.FetchedCount++;
						//AddTiming($"Fectched: {JobProgressInfo.FetchedCount}");
					}
					else
					{
						JobProgressInfo.GeneratedCount++;
						//AddTiming($"Generated: {JobProgressInfo.GeneratedCount}");
					}
				}

			}

			if (mapSection.IsLastSection)
			{
				//_receivedTheLastOne = true;
				Debug.WriteLine($"{mapSection.JobNumber} is complete. Received {MapSections.Count} map sections.");
			}
			else
			{
				//Debug.WriteLine($"Got a mapSection.");
			}
		}

		private void MapViewUpdateIsComplete(int jobNumber, bool isCancelled)
		{
			if (_currentMsrJob != null)
			{
				RunTestContinuation(_currentMsrJob);
			}
			else
			{
				throw new InvalidOperationException("The Current MsrJob is null!");
			}
		}

		private void UpdateUi(Stopwatch stopwatch, JobProgressInfo jobProgressInfo, TimeSpan mapLoaderOverall)
		{
			var mops = new MathOpCounts();
			var sumProcessingDurations = 0d;
			var sumGenerationDurations = 0d;

			foreach (var x in MapSections)
			{
				if (x.MathOpCounts != null)
				{
					mops.Update(x.MathOpCounts);
				}

				if (x.MapSectionProcessInfo != null)
				{
					sumProcessingDurations += x.MapSectionProcessInfo.ProcessingDuration?.TotalSeconds ?? 0d;
					sumGenerationDurations += x.MapSectionProcessInfo.GenerationDuration?.TotalSeconds ?? 0d;

				}
			}

			MathOpCounts = mops;

			Debug.WriteLine($"Generated {jobProgressInfo.GeneratedCount} sections in {stopwatch.ElapsedMilliseconds}. Performed: {mops.NumberOfMultiplications}. " +
				$"Performed: {mops.NumberOfCalcs} used iterations and {mops.NumberOfUnusedCalcs} unused iterations.");

			GeneratedCount = jobProgressInfo.GeneratedCount;
			var threadCount = 5;

			var diff = stopwatch.Elapsed - mapLoaderOverall;

			//var diffS = Math.Round(diff.TotalMilliseconds, 4).ToString();

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

		//private void MapLoader_SectionLoaded(object? sender, MapSectionProcessInfo e)
		//{
		//	MapSectionProcessInfos.Add(e);

		//	if (JobProgressInfo != null)
		//	{
		//		if (e.FoundInRepo)
		//		{
		//			JobProgressInfo.FetchedCount++;
		//			//AddTiming($"Fectched: {JobProgressInfo.FetchedCount}");
		//		}
		//		else
		//		{
		//			JobProgressInfo.GeneratedCount++;
		//			//AddTiming($"Generated: {JobProgressInfo.GeneratedCount}");
		//		}
		//	}
		//}

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

		public void ResetMapSectionRequestProcessor()
		{
			_mapSectionRequestProcessor.UseRepo = true;
		}

		private static List<MapSectionRequest> GetMapSectionRequests(JobType jobType, Job job, OwnerType jobOwnerType, SizeDbl displaySize, MapJobHelper mapJobHelper, MapSectionBuilder mapSectionBuilder, int mapLoaderJobNumber)
		{
			var mapAreaInfo = job.MapAreaInfo;
			var mapCalcSettings = job.MapCalcSettings;

			var mapAreaInfoV1 = mapJobHelper.GetMapPositionSizeAndDelta(mapAreaInfo, displaySize);

			var msrJob = CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, job.Id.ToString(), jobOwnerType, mapAreaInfoV1, mapCalcSettings);

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfoV1.CanvasSize.Round(), mapAreaInfoV1.CanvasControlOffset, mapAreaInfoV1.Subdivision.BlockSize);

			var mapSectionRequests = mapSectionBuilder.CreateSectionRequests(msrJob, mapExtentInBlocks);

			return mapSectionRequests;
		}

		private static MsrJob CreateMapSectionRequestJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapPositionSizeAndDelta mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

			var limbCount = GetLimbCount(precision);

			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset,
				precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesYZero);

			return msrJob;
		}

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private static int GetLimbCount(int precision)
		{
			var adjustedPrecision = precision + 2;
			var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: adjustedPrecision);

			var adjustedLimbCount = Math.Max(apFixedPointFormat.LimbCount, 2);

			return adjustedLimbCount;
		}

		private MapPositionSizeAndDelta GetMapPositionSizeAndDelta(MapCenterAndDelta mapCenterAndDelta, SizeDbl imageSize)
		{
			var result = _mapJobHelper.GetMapPositionSizeAndDelta(mapCenterAndDelta, imageSize);

			return result;
		}

		//private List<Tuple<long, string>> AddTiming(string desc)
		//{
		//	Timings.Add(new Tuple<long, string>(_stopwatch1.ElapsedMilliseconds, desc));
		//	return Timings;
		//}

		#endregion
	}
}
