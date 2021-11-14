using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MEngineDataContracts
{
	[ServiceContract]
    public interface IMapSectionService
    {
        //[OperationContract]
        //Task<HelloReply1> SayHelloAsync(HelloRequest1 request, CallContext context = default);

		[OperationContract]
		Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default);
	}

}
