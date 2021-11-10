using Grpc.Net.Client;
using MEngineDataContracts;
using ProtoBuf.Grpc.Client;
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

		public async Task<HelloReply1> SendHelloAsync()
		{
			IMEngineService mEngineService = GetMEngineService();

			var reply = await mEngineService.SayHelloAsync(new HelloRequest1 { Name = "GreeterClient" });
			Debug.WriteLine($"Greeting: {reply.Message}");

			return reply;
		}

		private IMEngineService GetMEngineService()
		{
			var client = Channel.CreateGrpcService<IMEngineService>();
			return client;
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
