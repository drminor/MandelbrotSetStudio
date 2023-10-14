using Grpc.Net.Client;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace MEngineClient
{
	public class MClient : IMEngineClient
	{
		#region Private Fields

		private static int _sectionCntr;

		private DtoMapper _dtoMapper;
		private GrpcChannel? _grpcChannel;
		private IMapSectionService? _mapSectionService;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		static MClient()
		{
			_sectionCntr = 0;
		}

		public MClient(int clientNumber, string endPointAddress, MapSectionVectorProvider mapSectionVectorProvider)
		{
			ClientNumber = clientNumber;
			EndPointAddress = endPointAddress;

			_mapSectionVectorProvider = mapSectionVectorProvider;

			_dtoMapper = new DtoMapper();

			_grpcChannel = null;
			_mapSectionService = null;
		}

		#endregion

		#region Public Properties

		public int ClientNumber {get; init;}
		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		#endregion

		#region Public Methods

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			MapSectionResponse result;

			if (ct.IsCancellationRequested)
			{
				mapSectionRequest.Cancelled = true;
				Debug.WriteLineIf(_useDetailedDebug, $"MClient. Request: JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber} is cancelled.");
				result = MapSectionResponse.CreateCancelledResponseWithVectors(mapSectionRequest);
			}
			else
			{
				mapSectionRequest.ClientEndPointAddress = EndPointAddress;

				var mapSectionServiceRequest = MapTo(mapSectionRequest);

				var stopWatch = Stopwatch.StartNew();
				var mapSectionServiceResponse = GenerateMapSectionInternal(mapSectionServiceRequest, ct);
				stopWatch.Stop();

				result = MapFrom(mapSectionServiceResponse, mapSectionRequest);

				mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;
				mapSectionRequest.GenerationDuration = TimeSpan.FromMilliseconds(mapSectionServiceResponse.TimeToGenerateMs);

				if (ct.IsCancellationRequested)
				{
					mapSectionRequest.Cancelled = true;
				}
			}

			return result;
		}

		#endregion

		#region Private Methods

		private MapSectionServiceResponse GenerateMapSectionInternal(MapSectionServiceRequest req, CancellationToken ct)
		{
			try
			{
				var mapSectionService = MapSectionService;

				var registration = ct.Register(CancelGeneration, req);

				Debug.WriteLineIf(_useDetailedDebug, $"MEngineClient #{ClientNumber} is starting the call to Generate MapSection: {req.ScreenPosition}.");
				var mapSectionServiceResponse = mapSectionService.GenerateMapSection(req);
				Debug.WriteLineIf(_useDetailedDebug, $"MEngineClient #{ClientNumber} is completing the call to Generate MapSection: {req.ScreenPosition}. Request is Cancelled = {ct.IsCancellationRequested}.");

				registration.Unregister();

				if (ct.IsCancellationRequested)
				{
					mapSectionServiceResponse.RequestCancelled = true;
				}

				if (++_sectionCntr % 10 == 0)
				{
					Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
				}

				return mapSectionServiceResponse;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GenerateMapSectionInternal raised Exception: {e}.");
				throw;
			}
		}

		private void CancelGeneration(object? state, CancellationToken ct)
		{
			if (state is MapSectionServiceRequest req)
			{
				var cancelRequest = new CancelRequest
				{
					MapLoaderJobNumber = req.MapLoaderJobNumber,
					RequestNumber = req.RequestNumber
				};

				var mapSectionService = MapSectionService;
				_ = mapSectionService.CancelGeneration(cancelRequest);
			}
			else
			{
				var stateType = state == null ? "null" : state.GetType().ToString();
				Debug.WriteLine($"WARNING: MClient: CancelGeneration Callback was given a state with type: {stateType} different than {typeof(MapSectionServiceRequest)}.");
			}
		}

		private MapSectionServiceRequest MapTo(MapSectionRequest req)
		{
			var mapPosition = _dtoMapper.MapTo(req.MapPosition);
			var samplePointDelta = _dtoMapper.MapTo(req.SamplePointDelta);

			var mapSectionServiceRequest = new MapSectionServiceRequest()
			{
				MapSectionId = req.MapSectionId,
				JobId = req.JobId,
				OwnerType = req.OwnerType,
				SubdivisionId = req.Subdivision.Id.ToString(),
				ScreenPosition = req.ScreenPosition,
				BlockPosition = req.SectionBlockOffset,
				IsInverted = req.IsInverted,
				MapPosition = mapPosition,
				BlockSize = req.BlockSize,
				SamplePointDelta = samplePointDelta,
				MapCalcSettings = req.MapCalcSettings,
				Precision = req.Precision,
				LimbCount = req.LimbCount,
				IncreasingIterations = req.IncreasingIterations,
				MapLoaderJobNumber = req.MapLoaderJobNumber,
				RequestNumber = req.RequestNumber
			};

			var mapSectionVectors2 = req.MapSectionVectors2;
			var mapSectionZVectors = req.MapSectionZVectors;

			if (req.IncreasingIterations && mapSectionVectors2 != null && mapSectionZVectors != null)
			{
				mapSectionServiceRequest.Counts = mapSectionVectors2.Counts;
				mapSectionServiceRequest.EscapeVelocities = mapSectionVectors2.EscapeVelocities;

				mapSectionServiceRequest.Zrs = mapSectionZVectors.Zrs;
				mapSectionServiceRequest.Zis = mapSectionZVectors.Zis;
				mapSectionServiceRequest.HasEscapedFlags = mapSectionZVectors.HasEscapedFlags;
				mapSectionServiceRequest.RowHasEscaped = mapSectionZVectors.GetBytesForRowHasEscaped();
			}
			else
			{
				mapSectionServiceRequest.Counts = Array.Empty<byte>();
				mapSectionServiceRequest.EscapeVelocities = Array.Empty<byte>();

				mapSectionServiceRequest.Zrs = Array.Empty<byte>();
				mapSectionServiceRequest.Zis = Array.Empty<byte>();
				mapSectionServiceRequest.HasEscapedFlags = Array.Empty<byte>();
				mapSectionServiceRequest.RowHasEscaped = Array.Empty<byte>();
			}

			return mapSectionServiceRequest;
		}

		private MapSectionResponse MapFrom(MapSectionServiceResponse res, MapSectionRequest mapSectionRequest)
		{
			var (mapSectionVectors2, mapSectionZVectors) = mapSectionRequest.TransferMapVectorsOut2();

			if (!res.RequestCancelled)
			{
				if (mapSectionVectors2 != null)
				{
					if (res.Counts.Length > 0)
					{
						mapSectionVectors2.Counts = res.Counts;
					}

					if (res.EscapeVelocities.Length > 0)
					{
						mapSectionVectors2.EscapeVelocities = res.EscapeVelocities;
					}
				}
				else
				{
					mapSectionVectors2 = new MapSectionVectors2(mapSectionRequest.BlockSize, res.Counts, res.EscapeVelocities);
				}

				if (mapSectionRequest.MapCalcSettings.SaveTheZValues && !res.AllRowsHaveEscaped)
				{
					if (mapSectionZVectors == null)
					{
						mapSectionZVectors = _mapSectionVectorProvider.ObtainMapSectionZVectors(mapSectionRequest.LimbCount);
					}

					Array.Copy(res.Zrs, mapSectionZVectors.Zrs, res.Zrs.Length);
					Array.Copy(res.Zis, mapSectionZVectors.Zis, res.Zis.Length);
					Array.Copy(res.HasEscapedFlags, mapSectionZVectors.HasEscapedFlags, res.HasEscapedFlags.Length);

					mapSectionZVectors.FillRowHasEscaped(res.RowHasEscaped, mapSectionZVectors.RowHasEscaped);
				}
				else
				{
					if (mapSectionZVectors != null)
					{
						_mapSectionVectorProvider.ReturnMapSectionZVectors(mapSectionZVectors);
					}
				}
			}
			else
			{
				mapSectionVectors2?.ResetObject();
				mapSectionZVectors?.ResetObject();
			}

			var result = new MapSectionResponse(mapSectionRequest, res.RequestCompleted, res.AllRowsHaveEscaped, mapSectionVectors2, mapSectionZVectors, res.RequestCancelled);
			result.MathOpCounts = res.MathOpCounts?.Clone();

			return result;
		}

		#endregion

		#region Test Support

		public MapSectionServiceResponse GenerateMapSectionTest()
		{
			var mapSectionService = MapSectionService;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionServiceResponse = mapSectionService.GenerateMapSectionTest("dummy");

			stopWatch.Stop();

			var elapsed = stopWatch.Elapsed;

			Debug.WriteLine($"The test call took: {elapsed.TotalMilliseconds}ms.");

			return mapSectionServiceResponse;
		}

		#endregion

		#region Service and Channel Support

		private IMapSectionService MapSectionService
		{
			get
			{
				if (_mapSectionService == null)
				{
					_mapSectionService = GetMapSectionService();
				}

				return _mapSectionService;
			}
		}

		private IMapSectionService GetMapSectionService()
		{
			try
			{
				var client = Channel.CreateGrpcService<IMapSectionService>();
				return client;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While Creating the GrpcService<IMapSectionService>, received exception: {e.GetType()}:{e.Message}");
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

		#region Old Code

		//// Field Declartions
		//private int? _jobNumber;
		//private int? _requestNumber;

		//// Constructor
		//private object _cancellationLock = new object();
		//_jobNumber = null;
		//_requestNumber = null;

		//public bool CancelGeneration(MapSectionRequest mapSectionRequest, CancellationToken ct)
		//{
		//	if (_jobNumber != null && _requestNumber != null)
		//	{
		//		var jobNumber = mapSectionRequest.MapLoaderJobNumber;
		//		var requestNumber = mapSectionRequest.RequestNumber;

		//		if (jobNumber == _jobNumber && requestNumber == _requestNumber)
		//		{
		//			var mapSectionService = MapSectionService;
		//			//var mapSectionService = GetMapSectionService();

		//			var cancelRequest = new CancelRequest
		//			{
		//				MapLoaderJobNumber = jobNumber,
		//				RequestNumber = requestNumber
		//			};


		//			lock (_cancellationLock)
		//			{
		//				var result = mapSectionService.CancelGeneration(cancelRequest);
		//				return result.RequestWasCancelled;
		//			}
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}

		//	return false;
		//}

		#endregion
	}
}
