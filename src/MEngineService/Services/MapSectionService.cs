using MEngineDataContracts;
using MongoDB.Bson;
using MSetGeneratorPrototype;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
		#region Private Fields

		private static int _sectionCntr;

		private const int ESCASPE_VELOCITY_VALUE_SIZE = 2;
		private const int VALUE_SIZE = 4;

		private DtoMapper _dtoMapper;
		private readonly SizeInt _blockSize;
		private readonly IMapSectionGenerator _generator;

		private readonly Dictionary<string, CancellationTokenSource> _activeServiceRequests;

		private readonly object _stateLock = new object();

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

			_dtoMapper = new DtoMapper();
			_generator = new MapSectionGeneratorDepthFirst(defaultLimbCount, _blockSize);

			_activeServiceRequests = new Dictionary<string, CancellationTokenSource>();
		}

		#endregion

		#region Public Properties

		//public MSetGenerationStrategy MSetGenerationStrategy { get; init; }

		//public string EndPointAddress { get; init; }

		#endregion

		#region Public Methods

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionServiceRequest, CallContext context = default)
		{
			var key = GetKey(mapSectionServiceRequest.MapLoaderJobNumber, mapSectionServiceRequest.RequestNumber);
			var cts = new CancellationTokenSource();

			lock (_stateLock)
			{
				_activeServiceRequests.Add(key, cts);
			}

			try
			{
				var mapSectionRequest = MapFrom(mapSectionServiceRequest);
				mapSectionRequest.CancellationTokenSource = cts;

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
			finally
			{
				lock (_stateLock)
				{
					_activeServiceRequests.Remove(mapSectionServiceRequest.JobId);
				}
			}
		}

		public CancelResponse CancelGeneration(CancelRequest cancelRequest, CallContext context = default)
		{
			Debug.Assert(cancelRequest.MapLoaderJobNumber != -1, "The Cancel Request's MapLoaderJobNumber is -1.");
			Debug.Assert(cancelRequest.RequestNumber != -1, "The Cancel Request's RequestNumber is -1.");

			var key = GetKey(cancelRequest.MapLoaderJobNumber, cancelRequest.RequestNumber);

			CancellationTokenSource? cts;

			lock (_stateLock)
			{
				if (_activeServiceRequests.TryGetValue(key, out var result))
				{
					cts = result;
				}
				else
				{
					cts = null;
				}
			}

			if (cts != null)
			{
				cts.Cancel();
				return new CancelResponse { RequestWasCancelled = true, ErrorMessage = "" };

			}
			else
			{
				return new CancelResponse { RequestWasCancelled = false, ErrorMessage = $"No active request matching {cancelRequest} found." };
			}
		}

		#endregion

		#region Private Methods

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			try
			{
				if (mapSectionRequest.MapSectionVectors2 == null)
				{
					var mapSectionVectors2 = new MapSectionVectors2(mapSectionRequest.BlockSize);
					mapSectionRequest.MapSectionVectors2 = mapSectionVectors2;
				}

				if (mapSectionRequest.MapCalcSettings.SaveTheZValues && mapSectionRequest.MapSectionZVectors == null)
				{
					mapSectionRequest.MapSectionZVectors = new MapSectionZVectors(mapSectionRequest.BlockSize, mapSectionRequest.LimbCount);
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

		private MapSectionRequest MapFrom(MapSectionServiceRequest req)
		{
			var mapPosition = _dtoMapper.MapFrom(req.MapPosition);

			var mapSectionRequest = new MapSectionRequest(
				msrJob: GetMsrJob(req),
				requestNumber: req.RequestNumber, 
				screenPosition: req.ScreenPosition,
				screenPositionRelativeToCenter: new VectorInt(),
				sectionBlockOffset: req.BlockPosition,
				mapPosition: mapPosition,
				isInverted: req.IsInverted
			);

			mapSectionRequest.MapSectionVectors2 = GetVectors2(req);
			mapSectionRequest.MapSectionZVectors = GetZVectors(req);

			return mapSectionRequest;
		}

		private MsrJob GetMsrJob(MapSectionServiceRequest req)
		{
			var samplePointDelta = _dtoMapper.MapFrom(req.SamplePointDelta);
			var subdivision = new Subdivision(samplePointDelta, new BigVector());

			var result = new MsrJob(
				req.MapLoaderJobNumber, 
				JobType.FullScale, 
				req.JobId, 
				req.OwnerType, 
				subdivision,
				originalSourceSubdivisionId: "",
				jobBlockOffset: new VectorLong(), 
				req.Precision, 
				req.LimbCount, 
				req.MapCalcSettings,
				crossesXZero: false			// TODO: Provide a real value for crossesXZero  
				);

			return result;
		}

		private MapSectionVectors2? GetVectors2(MapSectionServiceRequest req)
		{
			if (req.Counts == null || req.Counts.Length == 0)
			{
				return null;
			}

			var escapeVelocities = (req.EscapeVelocities == null || req.EscapeVelocities.Length == 0) 
				? new byte[req.BlockSize.NumberOfCells * ESCASPE_VELOCITY_VALUE_SIZE] 
				: req.EscapeVelocities;

			var mapSectionVectors2 = new MapSectionVectors2(_blockSize, req.Counts, escapeVelocities);

			return mapSectionVectors2;
		}

		private MapSectionZVectors? GetZVectors(MapSectionServiceRequest req)
		{
			// Layout parameters
			var valueCount = req.BlockSize.NumberOfCells;
			var rowCount = req.BlockSize.Height;
			var totalBytesForFlags = valueCount * VALUE_SIZE;           // ValueCount * VALUE_SIZE
			var totalByteCount = totalBytesForFlags * req.LimbCount;    // ValueCount * VALUE_SIZE * LimbCount;

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

			if (mapSectionZVectors != null && !mapSectionResponse.AllRowsHaveEscaped)
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

		private string GetKey(int jobNumber, int requestNumber)
		{
			var result = $"{jobNumber}/{requestNumber}";
			return result;
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
				BlockPosition = new VectorLong(),
				MapPosition = new RPointDto(),
				IsInverted = false,
				BlockSize = _blockSize,
				SamplePointDelta = new RSizeDto(),
				MapCalcSettings = new MapCalcSettings(),
				Precision = RMapConstants.DEFAULT_PRECISION,
				LimbCount = 1,
				IncreasingIterations = false,
				MapLoaderJobNumber = 1,
				RequestNumber = 1
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

		// ---- Old Private Properties
		// TODO: Have the MapSectionService get the MongoDb connection string from the appsettings.json file.
		//private const string MONGO_DB_SERVER = "desktop-bau7fe6";
		//private const int MONGO_DB_PORT = 27017;

		//private static readonly IMapSectionAdapter _mapSectionAdapter;
		//private static readonly MapSectionPersistProcessor _mapSectionPersistProcessor;
		
		// ---- Old Constructor Code
		//var repositoryAdapters = new RepositoryAdapters(MONGO_DB_SERVER, MONGO_DB_PORT, "MandelbrotProjects");
		//_mapSectionAdapter = repositoryAdapters.MapSectionAdapter;
		//_mapSectionPersistProcessor = new MapSectionPersistProcessor(_mapSectionAdapter, _mapSectionVectorProvider);

		//Console.WriteLine($"The MapSection Persist Processor has started. Server: {MONGO_DB_SERVER}, Port: {MONGO_DB_PORT}.");


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
