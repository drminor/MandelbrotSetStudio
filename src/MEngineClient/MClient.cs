﻿using Grpc.Net.Client;
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

				var isCancelled = ct.IsCancellationRequested | mapSectionServiceResponse.RequestCancelled;

				MapSectionResponse result;

				if (isCancelled)
				{
					result = new MapSectionResponse(req, mapSectionServiceResponse.RequestCompleted, mapSectionServiceResponse.AllRowsHaveEscaped, req.MapSectionVectors, mapSectionZVectors: null, requestCancelled: true);
					req.MapSectionVectors = null;
				}
				else
				{
					req.GenerationDuration = TimeSpan.FromMilliseconds(mapSectionServiceResponse.TimeToGenerate);

					result = new MapSectionResponse(req, mapSectionServiceResponse.RequestCompleted, mapSectionServiceResponse.AllRowsHaveEscaped, req.MapSectionVectors);
					req.MapSectionVectors = null;

					if (mapSectionServiceResponse.MathOpCounts != null)
					{
						result.MathOpCounts = mapSectionServiceResponse.MathOpCounts.Clone();
					}

					if (result.MapSectionVectors != null )
					{
						if (mapSectionServiceResponse.Counts.Length == result.MapSectionVectors.Counts.Length)
						{
							Array.Copy(mapSectionServiceResponse.Counts, result.MapSectionVectors.Counts, mapSectionServiceResponse.Counts.Length);
						}

						if (req.MapCalcSettings.UseEscapeVelocities && mapSectionServiceResponse.EscapeVelocities.Length == result.MapSectionVectors.EscapeVelocities.Length)
						{
							Array.Copy(mapSectionServiceResponse.EscapeVelocities, result.MapSectionVectors.EscapeVelocities, mapSectionServiceResponse.EscapeVelocities.Length);
						}
					}
				}

				return result;
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
