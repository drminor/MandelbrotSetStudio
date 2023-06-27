using MapSectionProviderLib;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WpfMapDisplayPOC
{
	public class MainWindowViewModel : INotifyPropertyChanged
	{
		#region Private Properties

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapJobHelper _mapJobHelper;
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly DtoMapper _dtoMapper;

		private int _targetIterations;
		private ColorMap _colorMap;

		private Stopwatch _sw;

		#endregion

		#region Constructor

		public MainWindowViewModel(MapSectionRequestProcessor mapSectionRequestProcessor, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_mapSectionVectorProvider = mapSectionVectorProvider; 
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_mapSectionRequestProcessor.UseRepo = true;

			_mapSectionAdapter = mapSectionAdapter;
			var subdivisionProvider = new SubdivisonProvider(_mapSectionAdapter);
			_mapJobHelper = new MapJobHelper(subdivisionProvider, toleranceFactor: 10, RMapConstants.BLOCK_SIZE);

			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionBuilder = new MapSectionBuilder();
			_dtoMapper = new DtoMapper();

			_targetIterations = 400;
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(_targetIterations);
			_colorMap = new ColorMap(colorBandSet);

			_sw = Stopwatch.StartNew();
		}

		#endregion

		#region Public Properties

		private string _uiResults = string.Empty;
		private string _bgResults = string.Empty;
		private string _opResults = string.Empty;

		public string UiResults
		{
			get => _uiResults;
			set { _uiResults = value; OnPropertyChanged(); }
		}

		public string BgResults
		{
			get => _bgResults;
			set { _bgResults = value; OnPropertyChanged(); }
		}

		public string OpResults
		{
			get => _opResults;
			set { _opResults = value; OnPropertyChanged(); }
		}

		#endregion

		#region Public LoadMap Methods

		public void RunHomeJob(Action<MapSection> callback)
		{
			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);

			var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN;
			//var coords = new RRectangle(-4, 4, -4, 4, -1);
			//var coords = new RRectangle(0, 4, 0, 4, -1);

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, canvasSize);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 1000, threshold: 4);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);

			var job = _mapJobHelper.BuildHomeJob(mapAreaInfo, colorBandSet.Id, mapCalcSettings);

			RunTest(job, callback);
		}

		//public void RunDenseLC2(Action<MapSection> callback)
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

		//	RunTest(job, callback);
		//}

		public void RunDenseLC4(Action<MapSection> callback)
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

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, canvasSize);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 400, threshold: 4);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);

			var job = _mapJobHelper.BuildHomeJob(mapAreaInfo, colorBandSet.Id, mapCalcSettings);

			RunTest(job, callback);
		}

		#endregion

		#region Public Support Methods

		public IList<MapSectionRequest> GetSectionRequestsForJob(string strJobId)
		{
			var jobId = ObjectId.Parse(strJobId);

			var job = _projectAdapter.GetJob(jobId);

			if (job.MapCalcSettings.TargetIterations != _targetIterations)
			{
				_targetIterations = job.MapCalcSettings.TargetIterations;
				var colorBandSet = RMapConstants.BuildInitialColorBandSet(_targetIterations);
				_colorMap = new ColorMap(colorBandSet);
			}

			var areaInfoWithSize = _mapJobHelper.GetMapAreaWithSizeFat(job.MapAreaInfo, new SizeDbl(1024));

			var result = _mapSectionBuilder.CreateSectionRequests(jobId.ToString(), JobOwnerType.Project, areaInfoWithSize, job.MapCalcSettings);

			return result;
		}

		public MapSection? GetMapSection(MapSectionRequest mapSectionRequest, int jobNumber)
		{
			var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();

			var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
			var blockPosition = _dtoMapper.MapTo(mapSectionRequest.BlockPosition);
			var mapSectionResponse = _mapSectionAdapter.GetMapSection(subdivisionId, blockPosition, mapSectionVectors);

			if (mapSectionResponse != null)
			{
				var result = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobNumber);
				return result;
			}
			else
			{
				return null;
			}
		}

		public bool TryGetPixelArray(MapSection mapSection, out TimeSpan duration, [MaybeNullWhen(false)] out byte[] pixelArray)
		{
			if (mapSection.MapSectionVectors != null)
			{
				_sw.Restart();
				LoadPixelArray(mapSection.MapSectionVectors, _colorMap, !mapSection.IsInverted);
				pixelArray = mapSection.MapSectionVectors.BackBuffer;
				_sw.Stop();

				duration = _sw.Elapsed;
				return true;
			}
			else
			{
				pixelArray = null;

				duration = TimeSpan.Zero;
				return false;
			}
		}

		#endregion

		#region Private Methods

		private void RunTest(Job job, Action<MapSection> callback)
		{
			var jobId = job.Id.ToString();
			var jobOwnerType = JobOwnerType.Project;

			var stopwatch = Stopwatch.StartNew();
			//_stopwatch1.Restart();
			//AddTiming("Start");

			var areaInfoWithSize = _mapJobHelper.GetMapAreaWithSizeFat(job.MapAreaInfo, new SizeDbl(1024));

			//AddTiming("GetMapAreaInfo");

			var mapSectionRequests = _mapSectionBuilder.CreateSectionRequests(jobId, jobOwnerType, areaInfoWithSize, job.MapCalcSettings);
			//AddTiming("CreateSectionRequest");

			var limbCount = mapSectionRequests[0].LimbCount;

			var newJobNumber = _mapSectionRequestProcessor.GetNextRequestId();

			var mapLoader = new MapLoader(newJobNumber, callback, _mapSectionRequestProcessor);
			//AddTiming("Construct MapLoader");
			//mapLoader.SectionLoaded += MapLoader_SectionLoaded;

			var startTask = mapLoader.Start(mapSectionRequests);
			//AddTiming("Start MapLoader");

			//JobProgressInfo = new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count);

			//for (var i = 0; i < 100; i++)
			//{
			//	Thread.Sleep(100);

			//	if (startTask.IsCompleted)
			//	{
			//		stopwatch.Stop();
			//		break;
			//	}
			//	//Debug.WriteLine($"Cnt: {i}. RunBaseLine is sleeping for 100ms.");
			//}

			//AddTiming("MapLoader Completed");

			//if (JobProgressInfo != null)
			//{
			//	Debug.WriteLine($"Fetched: {JobProgressInfo.FetchedCount}, Generated: {JobProgressInfo.GeneratedCount}. MapLoader Overall Time: {mapLoader.ElaspedTime}.");

			//	//var prevTm = 0L;

			//	//foreach(var tm in Timings)
			//	//{
			//	//	Debug.WriteLine($"{tm.Item2}:{tm.Item1}\t{tm.Item1 - prevTm}");
			//	//	prevTm = tm.Item1;
			//	//}

			//	UpdateUi(stopwatch, JobProgressInfo, mapLoader.ElaspedTime);
			//}
			//else
			//{
			//	Debug.WriteLine("The JobProgressInfo is null.");
			//}
		}

		//private void MapSectionReady(MapSection mapSection)
		//{
		//	if (mapSection.IsLastSection)
		//	{
		//		//_receviedTheLastOne = true;
		//		//Debug.WriteLine($"{mapSection.JobNumber} is complete. Received {MapSectionProcessInfos.Count} process infos.");
		//	}
		//	else
		//	{
		//		//Debug.WriteLine($"Got a mapSection.");
		//	}
		//}

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

		#endregion


		#region Pixel Array Support

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		public void LoadPixelArray(MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert)
		{
			Debug.Assert(mapSectionVectors.ReferenceCount > 0, "Getting the Pixel Array from a MapSectionVectors whose RefCount is < 1.");

			// Currently EscapeVelocities are not supported.
			//var useEscapeVelocities = colorMap.UseEscapeVelocities;
			var useEscapeVelocities = false;

			var rowCount = mapSectionVectors.BlockSize.Height;
			var sourceStride = mapSectionVectors.BlockSize.Width;
			var maxRowIndex = mapSectionVectors.BlockSize.Height - 1;

			//_pixelArraySize = _blockSize.NumberOfCells * BYTES_PER_PIXEL;
			var pixelStride = sourceStride * BYTES_PER_PIXEL;

			//var invert = !mapSection.IsInverted;
			var backBuffer = mapSectionVectors.BackBuffer;

			var counts = mapSectionVectors.Counts;
			//var previousCountVal = counts[0];

			var resultRowPtr = invert ? maxRowIndex * pixelStride : 0;
			var resultRowPtrIncrement = invert ? -1 * pixelStride : pixelStride;
			var sourcePtrUpperBound = rowCount * sourceStride;

			if (useEscapeVelocities)
			{
				var escapeVelocities = new ushort[counts.Length]; // mapSectionValues.EscapeVelocities;
				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var diagSum = 0;

					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						//TrackValueSwitches(countVal, ref previousCountVal);

						var escapeVelocity = escapeVelocities[sourcePtr] / VALUE_FACTOR;
						CheckEscapeVelocity(escapeVelocity);

						colorMap.PlaceColor(countVal, escapeVelocity, new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL));

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;

						diagSum += countVal;
					}

					if (diagSum < 10)
					{
						Debug.WriteLine("Counts are empty.");
					}
				}
			}
			else
			{
				// The main for loop on GetPixel Array 
				// is for each row of pixels (0 -> 128)
				//		for each pixel in that row (0, -> 128)
				// each new row advanced the resultRowPtr to the pixel byte address at column 0 of the current row.
				// if inverted, the first row = 127 * # of bytes / Row (Pixel stride)

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						//TrackValueSwitches(countVal, ref previousCountVal);

						colorMap.PlaceColor(countVal, escapeVelocity: 0, new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL));

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}
		}

		[Conditional("DEBUG2")]
		//private void TrackValueSwitches(ushort countVal, ref ushort previousCountVal)
		//{
		//	if (countVal != previousCountVal)
		//	{
		//		NumberOfCountValSwitches++;
		//		previousCountVal = countVal;
		//	}
		//}

		[Conditional("DEBUG2")]
		private void CheckEscapeVelocity(double escapeVelocity)
		{
			if (escapeVelocity > 1.0)
			{
				Debug.WriteLine($"The Escape Velocity is greater than 1.0");
			}
		}

		#endregion


		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion


		//public MapSection GetMapSection(ObjectId mapSectionId)
		//{
		//	var mapSectionVectors = _mapSectionBuilder.ObtainMapSectionVectors();

		//	var mapSectionResponse = _mapSectionAdapter.GetMapSection(mapSectionId, mapSectionVectors);

		//	if (mapSectionResponse == null)
		//	{
		//		throw new KeyNotFoundException($"No MapSection on file for MapSectionId: {mapSectionId}; Job: {CurrentJobId}.");
		//	}

		//	var jobNumber = 1;
		//	var repoBlockPosition = mapSectionResponse.BlockPosition;
		//	var mapBlockOffset = new BigVector();	// Used to get the screen position
		//	var isInverted = false;					// Used to get the screen position
		//	var subdivisionId = mapSectionResponse.SubdivisionId;
		//	var blockSize = RMapConstants.BLOCK_SIZE;
		//	var targetIterations = mapSectionResponse.MapCalcSettings.TargetIterations;

		//	var result = _mapSectionBuilder.CreateMapSection(jobNumber, repoBlockPosition, mapBlockOffset, isInverted, subdivisionId, blockSize, mapSectionVectors, targetIterations);

		//	//var result = _mapSectionBuilder.CreateMapSection(mapSectionRequest, mapSectionVectors, jobId: 1, new BigVector());

		//	return result;
		//}
	}
}
