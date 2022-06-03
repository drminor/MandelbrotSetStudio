using Grpc.Net.Client;
using MEngineDataContracts;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClient : IMEngineClient
	{
		private GrpcChannel _grpcChannel;

		public MClient(string endPointAddress)
		{
			EndPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		public string EndPointAddress { get; init; }

		//public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		//{
		//	var mEngineService = GetMapSectionService();
		//	var reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
		//	return reply;
		//}

		public async ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest)
		{
			var mEngineService = GetMapSectionService();
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var reply = await mEngineService.GenerateMapSectionAsyncR(mapSectionRequest);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			return reply;
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mEngineService = GetMapSectionService();
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var reply = mEngineService.GenerateMapSectionAsyncR(mapSectionRequest);
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
	}
}
