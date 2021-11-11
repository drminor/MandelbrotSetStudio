using MEngineDataContracts;
using ProtoBuf.Grpc;
using System.Threading.Tasks;

namespace MEngineService.Services
{
	public class MapSectionService : IMapSectionService
    {
        public Task<HelloReply1> SayHelloAsync(HelloRequest1 request, CallContext context = default)
        {
            return Task.FromResult(
                   new HelloReply1
                   {
                       Message = $"Hello {request.Name} from the new MEngine."
                   });
        }

	}
}
