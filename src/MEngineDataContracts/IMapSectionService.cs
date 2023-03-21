using ProtoBuf.Grpc;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MEngineDataContracts
{
	[ServiceContract]
    public interface IMapSectionService
    {
		//[OperationContract]
		//Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default);

		//[OperationContract]
		//ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest, CallContext context = default);

		[OperationContract]
		Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest, CallContext context = default);

		[OperationContract]
		MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest, CallContext context = default);


	}

}
