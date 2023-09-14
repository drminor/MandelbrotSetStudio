using Grpc.Net.Client;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
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

		private DtoMapper _dtoMapper;
		private GrpcChannel? _grpcChannel;

		private readonly MapSectionVectorProvider _mapSectionVectorProvider;

		private static int _sectionCntr;

		#endregion

		#region Constructor

		static MClient()
		{
			_sectionCntr = 0;
		}

		public MClient(MSetGenerationStrategy mSetGenerationStrategy, string endPointAddress, MapSectionVectorProvider mapSectionVectorProvider)
		{
			MSetGenerationStrategy = mSetGenerationStrategy;
			_dtoMapper = new DtoMapper();
			EndPointAddress = endPointAddress;
			_grpcChannel = null;
			_mapSectionVectorProvider = mapSectionVectorProvider;
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

			var mapSectionServiceRequest = MapTo(mapSectionRequest);

			var stopWatch = Stopwatch.StartNew();
			var mapSectionServiceResponse = GenerateMapSectionInternal(mapSectionServiceRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			var isCancelled = ct.IsCancellationRequested | mapSectionServiceResponse.RequestCancelled;

			var mapSectionResponse = MapFrom(mapSectionServiceResponse, mapSectionRequest);
			mapSectionRequest.GenerationDuration = TimeSpan.FromMilliseconds(mapSectionServiceResponse.TimeToGenerate);
			mapSectionResponse.RequestCancelled = isCancelled;

			if (mapSectionResponse.MapSectionVectors != null)
			{
				if (!isCancelled)
				{
					var mapSectionVectors = mapSectionResponse.MapSectionVectors;

					//if (mapSectionServiceResponse.Counts.Length == mapSectionVectors.Counts.Length)
					//{
					//	Array.Copy(mapSectionServiceResponse.Counts, mapSectionVectors.Counts, mapSectionServiceResponse.Counts.Length);
					//}

					//if (mapSectionServiceResponse.EscapeVelocities.Length == mapSectionVectors.EscapeVelocities.Length)
					//{
					//	Array.Copy(mapSectionServiceResponse.EscapeVelocities, mapSectionVectors.EscapeVelocities, mapSectionServiceResponse.EscapeVelocities.Length);
					//}

					if (mapSectionServiceResponse.Counts.Length > 0)
					{
						mapSectionVectors.LoadCounts(mapSectionServiceResponse.Counts);
					}

					if (mapSectionServiceResponse.EscapeVelocities.Length > 0)
					{
						mapSectionVectors.LoadEscapeVelocities(mapSectionServiceResponse.EscapeVelocities);
					}

					// Z Vectors
					var mapSectionZVectors = mapSectionResponse.MapSectionZVectors;

					if (mapSectionZVectors != null)
					{
						Array.Copy(mapSectionServiceResponse.Zrs, mapSectionZVectors.Zrs, mapSectionServiceResponse.Zrs.Length);
						Array.Copy(mapSectionServiceResponse.Zis, mapSectionZVectors.Zis, mapSectionServiceResponse.Zis.Length);
						Array.Copy(mapSectionServiceResponse.HasEscapedFlags, mapSectionZVectors.HasEscapedFlags, mapSectionServiceResponse.HasEscapedFlags.Length);

						mapSectionZVectors.FillRowHasEscaped(mapSectionServiceResponse.RowHasEscaped, mapSectionZVectors.RowHasEscaped);
					}
				}
				else
				{
					mapSectionResponse.MapSectionVectors.ResetObject();
					mapSectionResponse.MapSectionZVectors?.ResetObject();
				}
			}

			return mapSectionResponse;
		}

		private MapSectionServiceResponse GenerateMapSectionInternal(MapSectionServiceRequest req, CancellationToken ct)
		{
			try
			{ 
				var mEngineService = GetMapSectionService();

				var mapSectionServiceResponse = mEngineService.GenerateMapSection(req);

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

		#endregion

		#region Private Methods

		private MapSectionServiceRequest MapTo(MapSectionRequest req)
		{
			var blockPosition = _dtoMapper.MapTo(req.BlockPosition);
			var mapPosition = _dtoMapper.MapTo(req.MapPosition);
			var samplePointDelta = _dtoMapper.MapTo(req.SamplePointDelta);

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
				IncreasingIterations = req.IncreasingIterations,
			};

			if (req.IncreasingIterations)
			{
				var mapSectionVectors = req.MapSectionVectors;

				if (mapSectionVectors == null)
				{
					throw new InvalidOperationException("MClient received a MapSectionRequest with a null value for the MapSectionVectors property.");
				}

				mapSectionServiceRequest.Counts = mapSectionVectors.GetSerializedCounts();
				mapSectionServiceRequest.EscapeVelocities = mapSectionVectors.GetSerializedEscapeVelocities();
			}
			//else
			//{
			//	mapSectionServiceRequest.Counts = Array.Empty<byte>();
			//	mapSectionServiceRequest.EscapeVelocities = Array.Empty<byte>();
			//}

			var mapSectionZVectors = req.MapSectionZVectors;
			if (mapSectionZVectors != null)
			{
				mapSectionServiceRequest.Zrs = mapSectionZVectors.Zrs;
				mapSectionServiceRequest.Zis = mapSectionZVectors.Zis;
				mapSectionServiceRequest.HasEscapedFlags = mapSectionZVectors.HasEscapedFlags;
				mapSectionServiceRequest.RowHasEscaped = mapSectionZVectors.GetBytesForRowHasEscaped();
			}
			else
			{
				mapSectionServiceRequest.Zrs = Array.Empty<byte>();
				mapSectionServiceRequest.Zis = Array.Empty<byte>();
				mapSectionServiceRequest.HasEscapedFlags = Array.Empty<byte>();
				mapSectionServiceRequest.RowHasEscaped = Array.Empty<byte>();
			}

			return mapSectionServiceRequest;
		}

		private MapSectionResponse MapFrom(MapSectionServiceResponse serviceResponse, MapSectionRequest mapSectionRequest)
		{
			var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();

			var mapSectionResponse = new MapSectionResponse(mapSectionRequest, serviceResponse.RequestCompleted, serviceResponse.AllRowsHaveEscaped, msv, mszv);
			mapSectionResponse.MathOpCounts = serviceResponse.MathOpCounts?.Clone();

			return mapSectionResponse;
		}

		#endregion

		#region Test Support

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
