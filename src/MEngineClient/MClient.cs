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
		private readonly string _endPointAddress;
		private GrpcChannel _grpcChannel;

		public MClient(string endPointAddress)
		{
			_endPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			var mEngineService = GetMapSectionService();
			var reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
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
					_grpcChannel = GrpcChannel.ForAddress(_endPointAddress);
				}

				return _grpcChannel;
			}
		}
	}
}
