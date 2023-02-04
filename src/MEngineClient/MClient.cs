﻿using Grpc.Net.Client;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types.MSet;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClient : IMEngineClient
	{
		private GrpcChannel? _grpcChannel;

		public MClient(string endPointAddress)
		{
			EndPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		public string EndPointAddress { get; init; }
		public bool IsLocal => true;

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		//{
		//	var mEngineService = GetMapSectionService();
		//	var reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
		//	return reply;
		//}

		public async Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest, CancellationToken ct)
		{
			var mEngineService = GetMapSectionService();
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = await mEngineService.GenerateMapSectionAsync(mapSectionRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		public MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest, CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				return new MapSectionServiceResponse(mapSectionRequest)
				{
					RequestCancelled = true
				};
			}

			var mEngineService = GetMapSectionService();
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var reply = mEngineService.GenerateMapSection(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			return reply;
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


		public Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			throw new NotImplementedException();
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			throw new NotImplementedException();
		}

	}
}
