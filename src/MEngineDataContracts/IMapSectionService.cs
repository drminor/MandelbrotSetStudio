using ProtoBuf.Grpc;
using System.ServiceModel;

namespace MEngineDataContracts
{
	[ServiceContract]
    public interface IMapSectionService
    {
		[OperationContract]
		MapSectionServiceResponse GenerateMapSectionTest(string dummy, CallContext context = default);

		[OperationContract]
		MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest, CallContext context = default);

		//[OperationContract]
		//MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CallContext context = default);

		//[OperationContract]
		//Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CallContext context = default);

		//[OperationContract]
		//ValueTask<MapSectionResponse> GenerateMapSectionAsyncR(MapSectionRequest mapSectionRequest, CallContext context = default);

		//[OperationContract]
		//Task<MapSectionServiceResponse> GenerateMapSectionAsync(MapSectionServiceRequest mapSectionRequest, CallContext context = default);
	}
}
