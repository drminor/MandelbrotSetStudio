using MEngineDataContracts;
using MongoDB.Bson;
using MSetGeneratorPrototype;
using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProtoBuf.Grpc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using static MongoDB.Driver.WriteConcern;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		#region Private Fields

		private static int _sectionCntr;

		private const int VALUE_SIZE = 4;

		// TODO: Have the MapSectionService get the MongoDb connection string from the appsettings.json file.
		//private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		//private const int MONGO_DB_PORT = 27017;

		//private static readonly IMapSectionAdapter _mapSectionAdapter;
		//private static readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private DtoMapper _dtoMapper;
		private readonly SizeInt _blockSize;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionGenerator _generator;

		#endregion

		#region Constructors

		static MapSectionService()
		{
			_sectionCntr = 0;
		}

		public MapSectionService()
		{
			_blockSize = RMapConstants.BLOCK_SIZE;

			var defaultLimbCount = RMapConstants.DEFAULT_LIMB_COUNT;
			var initialPoolSize = RMapConstants.MAP_SECTION_INITIAL_POOL_SIZE;

			_dtoMapper = new DtoMapper();
			_mapSectionVectorProvider = CreateMapSectionVectorProvider(_blockSize, defaultLimbCount, initialPoolSize);
			_generator = new MapSectionGeneratorDepthFirst(defaultLimbCount, _blockSize);

			//var repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, "MandelbrotProjects");
			//_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;
			//_mapSectionPersistProcessor = new MapSectionPersistProcessor(_mapSectionAdapter, _mapSectionVectorProvider);

			//Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		#endregion

		#region Public Properties

		//public MSetGenerationStrategy MSetGenerationStrategy { get; init; }

		//public string EndPointAddress { get; init; }

		#endregion

		#region Public Methods

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionServiceRequest, CallContext context = default)
		{
			var cts = new CancellationTokenSource();

			try
			{
				var mapSectionRequest = MapFrom(mapSectionServiceRequest);

				var stopWatch = Stopwatch.StartNew();
				var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, cts.Token);
				stopWatch.Stop();

				var mapSectionServiceResponse = MapTo(mapSectionResponse, mapSectionServiceRequest, stopWatch.Elapsed);

				return mapSectionServiceResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"MapSectionService: GenerateMapSection raised Exception: {e}.");
				throw;
			}
		}

		#endregion

		#region Private Methods

		private MapSectionRequest MapFrom(MapSectionServiceRequest req)
		{
			var blockPosition = _dtoMapper.MapFrom(req.BlockPosition);
			var mapPosition = _dtoMapper.MapFrom(req.MapPosition);
			var samplePointDelta = _dtoMapper.MapFrom(req.SamplePointDelta);

			var mapSectionRequest = new MapSectionRequest(JobType.FullScale, req.JobId, req.OwnerType, req.SubdivisionId, req.SubdivisionId, req.ScreenPosition,
				screenPositionRelativeToCenter: new VectorInt(),
				mapBlockOffset: new BigVector(),
				blockPosition: blockPosition,
				mapPosition: mapPosition,
				isInverted: false,
				req.Precision,
				req.LimbCount,
				req.BlockSize,
				samplePointDelta: samplePointDelta,
				req.MapCalcSettings,
				requestNumber: 0);

			var mapSectionVectors2 = GetVectors2(req);
			mapSectionRequest.MapSectionVectors2 = mapSectionVectors2;

			var mapSectionZVectors = GetZVectors(req);
			mapSectionRequest.MapSectionZVectors = mapSectionZVectors;

			//if (req.Zrs.Length > 0)
			//{
			//	var mapSectionZVectors = GetZVectors(req);
			//	mapSectionRequest.MapSectionZVectors = mapSectionZVectors;
			//}
			//else
			//{
			//	if (req.MapCalcSettings.SaveTheZValues)
			//	{
			//		mapSectionRequest.MapSectionZVectors = new MapSectionZVectors(req.BlockSize, req.LimbCount);
			//	}
			//}

			return mapSectionRequest;
		}



		private MapSectionVectors2? GetVectors2(MapSectionServiceRequest req)
		{
			if (req.Counts == null || req.Counts.Length == 0)
			{
				return null;
			}

			var backBuffer = new byte[0];
			var mapSectionVectors2 = new MapSectionVectors2(_blockSize, req.Counts, req.EscapeVelocities, backBuffer);

			return mapSectionVectors2;
		}

		private MapSectionZVectors? GetZVectors(MapSectionServiceRequest req)
		{
			if (req.Zrs == null || req.Zrs.Length == 0)
			{
				return null;
			}

			// Layout parameters
			var valueCount = req.BlockSize.NumberOfCells;
			var rowCount = req.BlockSize.Height;
			var totalBytesForFlags = valueCount * VALUE_SIZE;			// ValueCount * VALUE_SIZE
			var totalByteCount = totalBytesForFlags * req.LimbCount;	// ValueCount * VALUE_SIZE * LimbCount;

			var zrs = req.Zrs.Length > 0 ? req.Zrs : new byte[totalByteCount];
			var zis = req.Zis.Length > 0 ? req.Zis : new byte[totalByteCount];
			var hasEscapedFlags = req.HasEscapedFlags.Length > 0 ? req.HasEscapedFlags : new byte[totalBytesForFlags];
			var rowHasEscaped = req.RowHasEscaped.Length > 0 ? GetBoolsFromBytes(req.RowHasEscaped) : new bool[rowCount];

			var mapSectionZVectors = new MapSectionZVectors(req.BlockSize, req.LimbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

			return mapSectionZVectors;
		}

		private bool[] GetBoolsFromBytes(byte[] rowHasEscaped)
		{
			return rowHasEscaped.Select(x => x == 1).ToArray();
		}

		private MapSectionServiceResponse MapTo(MapSectionResponse mapSectionResponse, MapSectionServiceRequest req, TimeSpan generationDuration)
		{
			var mapSectionVectors2 = mapSectionResponse.MapSectionVectors2;

			if (mapSectionVectors2 == null)
			{
				throw new InvalidOperationException("GenerateMapSection returned a response that has a null value for its MapSectionVectors2 property.");
			}

			var mapSectionServiceResponse = new MapSectionServiceResponse()
			{
				MapSectionId = req.MapSectionId,
				SubdivisionId = req.SubdivisionId,
				BlockPosition = req.BlockPosition,
				RequestCompleted = mapSectionResponse.RequestCompleted,
				AllRowsHaveEscaped = mapSectionResponse.AllRowsHaveEscaped,
				RequestCancelled = mapSectionResponse.RequestCancelled,
				TimeToGenerateMs = generationDuration.TotalMilliseconds,
				MathOpCounts = mapSectionResponse.MathOpCounts?.Clone(),

				Counts = mapSectionVectors2.Counts,
				EscapeVelocities = mapSectionVectors2.EscapeVelocities
			};

			var mapSectionZVectors = mapSectionResponse.MapSectionZVectors;

			if (mapSectionZVectors != null)
			{
				mapSectionServiceResponse.Zrs = mapSectionZVectors.Zrs;
				mapSectionServiceResponse.Zis = mapSectionZVectors.Zis;
				mapSectionServiceResponse.HasEscapedFlags = mapSectionZVectors.HasEscapedFlags;
				mapSectionServiceResponse.RowHasEscaped = mapSectionZVectors.GetBytesForRowHasEscaped();
			}
			else
			{
				mapSectionServiceResponse.Zrs = Array.Empty<byte>();
				mapSectionServiceResponse.Zis = Array.Empty<byte>();
				mapSectionServiceResponse.HasEscapedFlags = Array.Empty<byte>();
				mapSectionServiceResponse.RowHasEscaped = Array.Empty<byte>();
			}

			return mapSectionServiceResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			try
			{
				//if (mapSectionRequest.MapSectionVectors == null)
				//{
				//	var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				//	mapSectionVectors.ResetObject();
				//	mapSectionRequest.MapSectionVectors = mapSectionVectors;
				//}

				if (mapSectionRequest.MapSectionVectors2 == null)
				{
					var mapSectionVectors2 = new MapSectionVectors2(RMapConstants.BLOCK_SIZE);
					mapSectionRequest.MapSectionVectors2 = mapSectionVectors2;
				}

				if (mapSectionRequest.MapCalcSettings.SaveTheZValues && mapSectionRequest.MapSectionZVectors == null)
				{
					var mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(mapSectionRequest.LimbCount);
					mapSectionZVectors.ResetObject();
					mapSectionRequest.MapSectionZVectors = mapSectionZVectors;
				}

				var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

				if (++_sectionCntr % 10 == 0)
				{
					//Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
					Console.WriteLine($"MapSectionService: The Map Section Generator has processed {_sectionCntr} requests.");
				}

				return mapSectionResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}

		private MapSectionVectorProvider CreateMapSectionVectorProvider(SizeInt blockSize, int defaultLimbCount, int initialPoolSize)
		{
			var mapSectionVectorsPool = new MapSectionVectorsPool(blockSize, initialPoolSize);
			var mapSectionZVectorsPool = new MapSectionZVectorsPool(blockSize, defaultLimbCount, initialPoolSize);
			var mapSectionVectorProvider = new MapSectionVectorProvider(mapSectionVectorsPool, mapSectionZVectorsPool);

			return mapSectionVectorProvider;
		}

		//private MapSectionVectors? GetVectors(MapSectionServiceRequest req)
		//{
		//	if (req.Counts == null || req.Counts.Length == 0)
		//	{
		//		return null;
		//	}

		//	// Counts
		//	var counts = GetUShortsFromBytes(req.Counts, _blockSize);

		//	// Escape Velocities
		//	ushort[] escapeVelocities;

		//	if (req.EscapeVelocities != null && req.EscapeVelocities.Length > 0)
		//	{
		//		escapeVelocities = GetUShortsFromBytes(req.EscapeVelocities, _blockSize);
		//	}
		//	else
		//	{
		//		escapeVelocities = new ushort[_blockSize.NumberOfCells];
		//	}

		//	var backBuffer = new byte[0];
		//	var mapSectionVectors = new MapSectionVectors(_blockSize, counts, escapeVelocities, backBuffer);

		//	return mapSectionVectors;
		//}

		//private ushort[] GetUShortsFromBytes(byte[] bytes, SizeInt blockSize) 
		//{
		//	var result = new ushort[blockSize.NumberOfCells];

		//	var destBackEscapeVelocities = MemoryMarshal.Cast<ushort, byte>(result);

		//	for (var i = 0; i < bytes.Length; i++)
		//	{
		//		destBackEscapeVelocities[i] = bytes[i];
		//	}

		//	return result;
		//}

		#endregion

		#region Test Support

		public MapSectionServiceResponse GenerateMapSectionTest(string dummy, CallContext context = default)
		{
			var jobId = ObjectId.GenerateNewId().ToString();
			var mapSectionId = ObjectId.GenerateNewId().ToString();
			var subdivisionId = ObjectId.GenerateNewId().ToString();

			var dummyMapSectionServiceRequest = new MapSectionServiceRequest
			{
				MapSectionId = mapSectionId,
				JobId = jobId,
				OwnerType = OwnerType.Project,
				SubdivisionId = subdivisionId,
				ScreenPosition = new PointInt(),
				BlockPosition = new BigVectorDto(),
				MapPosition = new RPointDto(),
				BlockSize = _blockSize,
				SamplePointDelta = new RSizeDto(),
				MapCalcSettings = new MapCalcSettings(),
				Precision = RMapConstants.DEFAULT_PRECISION,
				LimbCount = 1,
				IncreasingIterations = false
			};

			//var dummyMapSectionRequest = new MapSectionRequest
			//	(
			//		JobType.FullScale,
			//		jobId,
			//		OwnerType.Project,
			//		subdivisionId,
			//		subdivisionId,
			//		new PointInt(),
			//		new VectorInt(),
			//		new BigVector(),
			//		new BigVector(),
			//		new RPoint(),
			//		isInverted: false,
			//		precision: RMapConstants.DEFAULT_PRECISION,
			//		limbCount: 1,
			//		blockSize: _blockSize,
			//		new RSize(),
			//		new MapCalcSettings(),
			//		requestNumber: 0
			//	);

			var result = new MapSectionServiceResponse(dummyMapSectionServiceRequest);

			return result;
		}

		#endregion

		#region Public Methods - Old

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default)
		//{
		//	await Task.Delay(100);

		//	var stringVals = MapSectionGenerator.GetStringVals(mapSectionRequest);
		//	Debug.WriteLine($"The string vals are {stringVals[0]}, {stringVals[1]}, {stringVals[2]}, {stringVals[3]}.");

		//	var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest, _mapSectionAdapter);

		//	//var idStr = string.IsNullOrEmpty(mapSectionResponse.MapSectionId) ? "new" : mapSectionResponse.MapSectionId;
		//	//if (++_sectionCntr % 10 == 0)
		//	//{
		//	//	Debug.WriteLine($"Adding MapSectionResponse with ID: {idStr} to the MapSection Persist Processor. Generated {_sectionCntr} Map Sections.");
		//	//}
		//	//_mapSectionPersistProcessor.AddWork(mapSectionResponse);

		//	if (++_sectionCntr % 10 == 0)
		//	{
		//		Debug.WriteLine($"Generated {_sectionCntr} Map Sections.");
		//	}

		//	mapSectionResponse.IncludeZValues = false;

		//	var mapSectionResponse = new MapSectionResponse(mapSectionRequest, isCancelled: true);

		//	return mapSectionResponse;
		//}

		//private MapSectionZVectors GetZVectors(MapSectionServiceRequest req)
		//{
		//	// Layout parameters
		//	var valueCount = req.BlockSize.NumberOfCells;
		//	var rowCount = req.BlockSize.Height;
		//	var totalBytesForFlags = valueCount * VALUE_SIZE;
		//	var totalByteCount = totalBytesForFlags * req.LimbCount; // ValueCount * LimbCount * VALUE_SIZE;

		//	// Reals
		//	var zrs = new byte[totalByteCount];
		//	var currentZrs = req.Zrs;
		//	if (currentZrs.Length > 0)
		//	{
		//		Array.Copy(currentZrs, zrs, currentZrs.Length);
		//	}

		//	// Imaginary
		//	var zis = new byte[totalByteCount];
		//	var currentZis = req.Zis;
		//	if (currentZis.Length > 0)
		//	{
		//		Array.Copy(currentZis, zis, currentZis.Length);
		//	}

		//	// Has Escaped Flags
		//	var currentHasEscapedFlags = req.HasEscapedFlags;
		//	var hasEscapedFlags = currentHasEscapedFlags.Length > 0 ? currentHasEscapedFlags : new byte[totalBytesForFlags];

		//	// Row Has Escaped
		//	var currentRowHasEscaped = req.RowHasEscaped;
		//	var rowHasEscaped = currentRowHasEscaped.Length > 0 ? ConvertRowHasEscapedBytes(currentRowHasEscaped) : new bool[rowCount];

		//	var mapSectionZVectors = new MapSectionZVectors(req.BlockSize, req.LimbCount, zrs, zis, hasEscapedFlags, rowHasEscaped);

		//	return mapSectionZVectors;
		//}

		//private MapSectionVectors GetVectors_Old_Old(MapSectionServiceRequest req)
		//{
		//	// Counts
		//	var counts = new ushort[_blockSize.NumberOfCells];
		//	var currentCounts = req.Counts;
		//	if (currentCounts.Length > 0)
		//	{
		//		Array.Copy(currentCounts, counts, currentCounts.Length);
		//	}

		//	// Escape Velocities
		//	var escapeVelocities = new ushort[_blockSize.NumberOfCells];
		//	var currentEscapeVelocities = req.EscapeVelocities;
		//	if (currentEscapeVelocities.Length > 0)
		//	{
		//		Array.Copy(currentEscapeVelocities, escapeVelocities, currentEscapeVelocities.Length);
		//	}

		//	// MapSectionVectors
		//	var backBuffer = new byte[0];
		//	var mapSectionVectors = new MapSectionVectors(_blockSize, counts, escapeVelocities, backBuffer);

		//	return mapSectionVectors;
		//}


		//private MapSectionVectors GetVectors_Old(MapSectionServiceRequest req)
		//{
		//	// Counts
		//	ushort[] counts;

		//	if (req.Counts != null && req.Counts.Length > 0)
		//	{
		//		counts = req.Counts;
		//	}
		//	else
		//	{
		//		counts = new ushort[_blockSize.NumberOfCells];
		//	}

		//	// Escape Velocities
		//	ushort[] escapeVelocities;

		//	if (req.EscapeVelocities != null && req.EscapeVelocities.Length > 0)
		//	{
		//		escapeVelocities =	req.EscapeVelocities;
		//	}
		//	else
		//	{
		//		escapeVelocities = new ushort[_blockSize.NumberOfCells];
		//	}

		//	// MapSectionVectors
		//	var backBuffer = new byte[0];
		//	var mapSectionVectors = new MapSectionVectors(_blockSize, counts, escapeVelocities, backBuffer);

		//	return mapSectionVectors;
		//}

		#endregion
	}
}
