using Grpc.Net.Client;
using MEngineDataContracts;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MEngineClient
{
	public class MClient
	{
		private readonly string _endPointAddress;
		private GrpcChannel _grpcChannel;

		public MClient(string endPointAddress)
		{
			_endPointAddress = endPointAddress;
			_grpcChannel = null;
		}

		//public async Task<HelloReply1> SendHelloAsync()
		//{
		//	IMapSectionService mEngineService = GetMapSectionService();

		//	var reply = await mEngineService.SayHelloAsync(new HelloRequest1 { Name = "GreeterClient" });
		//	Debug.WriteLine($"Greeting: {reply.Message}");

		//	return reply;
		//}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			IMapSectionService mEngineService = GetMapSectionService();

			MapSectionResponse reply = null;

			for (int i = 1; i < 5; i++)
			{
				reply = await mEngineService.GenerateMapSectionAsync(mapSectionRequest);
				Debug.WriteLine($"Call #{i} to Submit MapSectionRequest returned: {reply.Status}");
			}

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
