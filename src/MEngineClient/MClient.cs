using Grpc.Net.Client;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using MSS.Common.DataTransferObjects;

namespace MEngineClient
{
	public class MClient : IMEngineClient
	{
		#region Private Fields

		private DtoMapper _dtoMapper;
		private GrpcChannel? _grpcChannel;

		private static int _sectionCntr;

		#endregion

		#region Constructor

		static MClient()
		{
			_sectionCntr = 0;
		}

		public MClient(MSetGenerationStrategy mSetGenerationStrategy, string endPointAddress)
		{
			MSetGenerationStrategy = mSetGenerationStrategy;
			_dtoMapper = new DtoMapper();
			EndPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		#endregion

		#region Public Properties

		public MSetGenerationStrategy MSetGenerationStrategy { get; init; }

		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		#endregion

		#region Public Methods

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				Debug.WriteLine($"The MClient is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.");
				return new MapSectionResponse(mapSectionRequest, isCancelled: true);
			}

			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			//Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest req, CancellationToken ct)
		{
			var blockPosition = _dtoMapper.MapTo(req.BlockPosition);
			var mapPosition = _dtoMapper.MapTo(req.MapPosition);

			var samplePointDelta = _dtoMapper.MapTo(req.SamplePointDelta);

			try
			{
				var mapSectionServiceRequest = new MapSectionServiceRequest()
				{
					MapSectionId = req.MapSectionId,
					JobId = req.JobId,
					OwnerType = req.OwnerType,
					SubdivisionId = req.SubdivisionId,
					ScreenPosition = req.ScreenPosition,
					BlockPosition = blockPosition,
					MapPosition = mapPosition,
					BlockSize = req.BlockSize,
					SamplePointDelta = samplePointDelta,
					MapCalcSettings = req.MapCalcSettings,
					Precision = req.Precision,
					LimbCount = req.LimbCount,
					IncreasingIterations = req.IncreasingIterations
				};

				var mEngineService = GetMapSectionService();

				var mapSectionServiceResponse = mEngineService.GenerateMapSection(mapSectionServiceRequest);

				if (++_sectionCntr % 10 == 0)
				{
					Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
				}

				var isCancelled = ct.IsCancellationRequested;
				var mapSectionResponse = new MapSectionResponse(req, isCancelled);

				if (!isCancelled)
				{
					mapSectionResponse.RequestCompleted = mapSectionServiceResponse.RequestCompleted;
					req.TimeToCompleteGenRequest = TimeSpan.FromMilliseconds(mapSectionServiceResponse.TimeToGenerate);
					mapSectionResponse.MathOpCounts = mapSectionServiceResponse.MathOpCounts;

					var mapSectionVectors = new MapSectionVectors(RMapConstants.BLOCK_SIZE);

					Array.Copy(mapSectionServiceResponse.Counts, mapSectionVectors.Counts, mapSectionServiceResponse.Counts.Length);
					Array.Copy(mapSectionServiceResponse.EscapeVelocities, mapSectionVectors.EscapeVelocities, mapSectionServiceResponse.EscapeVelocities.Length);

					mapSectionResponse.MapSectionVectors = mapSectionVectors;
				}

				return mapSectionResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}



		public MapSectionServiceResponse GenerateMapSectionTest()
		{
			var mEngineService = GetMapSectionService();

			var stopWatch = Stopwatch.StartNew();
			var mapSectionServiceResponse = mEngineService.GenerateMapSectionTest("dummy");

			stopWatch.Stop();

			var elapsed = stopWatch.Elapsed;

			Debug.WriteLine($"The test call took: {elapsed.TotalMilliseconds}ms.");

			return mapSectionServiceResponse;
		}

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		//{
		//	var mEngineService = GetMapSectionService();
		//	var reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
		//	return reply;
		//}

		#endregion

		#region Depreciated - Service Request / Service Response

		//public async Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest, CancellationToken ct)
		//{
		//	var mEngineService = GetMapSectionService();
		//	mapSectionRequest.ClientEndPointAddress = EndPointAddress;

		//	var stopWatch = Stopwatch.StartNew();
		//	var mapSectionResponse = await mEngineService.GenerateMapSectionAsync(mapSectionRequest, ct);
		//	mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

		//	Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

		//	return mapSectionResponse;
		//}

		//public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest, CancellationToken ct)
		//{
		//	if (ct.IsCancellationRequested)
		//	{
		//		return new MapSectionServiceResponse(mapSectionRequest)
		//		{
		//			RequestCancelled = true
		//		};
		//	}

		//	var mEngineService = GetMapSectionService();
		//	mapSectionRequest.ClientEndPointAddress = EndPointAddress;

		//	var stopWatch = Stopwatch.StartNew();
		//	var reply = mEngineService.GenerateMapSection(mapSectionRequest);
		//	mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

		//	return reply;
		//}

		#endregion

		#region Service and Channel Support

		private IMapSectionService GetMapSectionService()
		{
			try
			{
				var client = Channel.CreateGrpcService<IMapSectionService>();
				return client;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exc: {e.GetType()}:{e.Message}");
				throw;
			}
		}

		private GrpcChannel Channel
		{
			get
			{
				if (_grpcChannel == null)
				{
					_grpcChannel = GrpcChannel.ForAddress(EndPointAddress);
				}

				return _grpcChannel;
			}
		}

		#endregion
	}
}
