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
using System.Threading;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		#region Private Fields

		// TODO: Have the MapSectionService get the MongoDb connection string from the appsettings.json file.

		//private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		//private const int MONGO_DB_PORT = 27017;

		//private static readonly IMapSectionAdapter _mapSectionAdapter;
		//private static readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private static readonly SizeInt _blockSize;
		//private static MapSectionVectorsPool _mapSectionVectorsPool;
		//private static MapSectionZVectorsPool _mapSectionZVectorsPool;
		//private static MapSectionVectorProvider _mapSectionVectorProvider;

		private DtoMapper _dtoMapper;

		private static int _sectionCntr;

		private readonly IMapSectionGenerator _generator;

		#endregion

		#region Constructors

		static MapSectionService()
		{
			_blockSize = RMapConstants.BLOCK_SIZE;
			_sectionCntr = 0;

			//var repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, "MandelbrotProjects");
			//_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;

			//_mapSectionVectorsPool = new MapSectionVectorsPool(_blockSize, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			//_mapSectionZVectorsPool = new MapSectionZVectorsPool(_blockSize, RMapConstants.DEFAULT_LIMB_COUNT, initialSize: RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			//_mapSectionVectorProvider = new MapSectionVectorProvider(_mapSectionVectorsPool, _mapSectionZVectorsPool);

			//_mapSectionPersistProcessor = new MapSectionPersistProcessor(_mapSectionAdapter, _mapSectionVectorProvider);

			//_sectionCntr = 0;
			//Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");
		}

		public MapSectionService()
		{
			MSetGenerationStrategy = MSetGenerationStrategy.DepthFirst;
			EndPointAddress = "CSharp_DepthFirstGenerator";

			_dtoMapper = new DtoMapper();

			_generator = new MapSectionGeneratorDepthFirst(RMapConstants.DEFAULT_LIMB_COUNT, _blockSize);
		}

		#endregion

		#region Public Properties

		public MSetGenerationStrategy MSetGenerationStrategy { get; init; }

		public string EndPointAddress { get; init; }
		public bool IsLocal => false;

		#endregion

		#region Public Methods

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionServiceRequest, CallContext context = default)
		{
			var cts = new CancellationTokenSource();

			try
			{
				var mapSectionRequest = MapFrom(mapSectionServiceRequest);

				//var counts = new ushort[_blockSize.NumberOfCells];
				//var escapeVelocities = new ushort[_blockSize.NumberOfCells];
				//var backBuffer = new byte[0];
				//var mapSectionVectors = new MapSectionVectors(_blockSize, counts, escapeVelocities, backBuffer);

				////var mapSectionVectors = _mapSectionVectorProvider.ObtainMapSectionVectors();
				////mapSectionVectors.ResetObject();

				//mapSectionRequest.MapSectionVectors = mapSectionVectors;

				var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, cts.Token);

				var mapSectionServiceResponse = MapTo(mapSectionResponse, mapSectionServiceRequest/*, counts, escapeVelocities*/, mapSectionRequest.GenerationDuration ?? TimeSpan.Zero);

				//var msv = mapSectionResponse.MapSectionVectors;

				//if (msv != null)
				//{
				//	if (msv.Counts.Length > 0)
				//	{
				//		mapSectionServiceResponse.Counts = new ushort[msv.Counts.Length];
				//		Array.Copy(msv.Counts, mapSectionServiceResponse.Counts, msv.Counts.Length);
				//	}
				//	else
				//	{
				//		mapSectionServiceResponse.Counts = Array.Empty<ushort>();
				//	}

				//	if (msv.EscapeVelocities.Length > 0)
				//	{
				//		mapSectionServiceResponse.EscapeVelocities = new ushort[msv.EscapeVelocities.Length];
				//		Array.Copy(msv.EscapeVelocities, mapSectionServiceResponse.EscapeVelocities, msv.EscapeVelocities.Length);
				//	}
				//	else
				//	{
				//		mapSectionServiceResponse.EscapeVelocities = Array.Empty<ushort>();
				//	}
				//}

				//_mapSectionVectorProvider.ReturnMapSectionResponse(mapSectionResponse);

				return mapSectionServiceResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSection raised Exception: {e}.");
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

			var counts = new ushort[_blockSize.NumberOfCells];
			var currentCounts = req.Counts;
			if (currentCounts.Length > 0)
			{
				Array.Copy(currentCounts, counts, currentCounts.Length);
			}

			var escapeVelocities = new ushort[_blockSize.NumberOfCells];
			var currentEscapeVelocities = req.EscapeVelocities;
			if (currentEscapeVelocities.Length > 0)
			{
				Array.Copy(currentEscapeVelocities, escapeVelocities, currentEscapeVelocities.Length);
			}

			var backBuffer = new byte[0];
			var mapSectionVectors = new MapSectionVectors(_blockSize, counts, escapeVelocities, backBuffer);
			mapSectionRequest.MapSectionVectors = mapSectionVectors;

			return mapSectionRequest;
		}

		private MapSectionServiceResponse MapTo(MapSectionResponse mapSectionResponse, MapSectionServiceRequest req/*, ushort[] counts, ushort[] escapeVelocities*/, TimeSpan generationDuration)
		{
			var mapSectionVectors = mapSectionResponse.MapSectionVectors;

			if (mapSectionVectors == null)
			{
				throw new InvalidOperationException("GenerateMapSection returned a response that has a null value for its MapSectionVectors property.");
			}

			var mapSectionServiceResponse = new MapSectionServiceResponse()
			{
				MapSectionId = req.MapSectionId,
				SubdivisionId = req.SubdivisionId,
				BlockPosition = req.BlockPosition,
				RequestCompleted = mapSectionResponse.RequestCompleted,
				AllRowsHaveEscaped = mapSectionResponse.AllRowsHaveEscaped,
				RequestCancelled = mapSectionResponse.RequestCancelled,
				TimeToGenerate = generationDuration.TotalMilliseconds,
				MathOpCounts = mapSectionResponse.MathOpCounts?.Clone(),
				Counts = mapSectionVectors.Counts,
				EscapeVelocities = mapSectionVectors.EscapeVelocities
			};

			return mapSectionServiceResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			try
			{
				var mapSectionResponse = _generator.GenerateMapSection(mapSectionRequest, ct);

				if (++_sectionCntr % 10 == 0)
				{
					//Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
					Console.WriteLine($"The MEngineClient has processed {_sectionCntr} requests.");
				}

				//mapSectionResponse.IncludeZValues = false;

				return mapSectionResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}

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

		#endregion
	}
}
