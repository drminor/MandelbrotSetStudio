using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MEngineDataContracts
{
    [DataContract]
    public class HelloReply1
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }

    [DataContract]
    public class HelloRequest1
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [ServiceContract]
    public interface IMEngineService
    {
        [OperationContract]
        Task<HelloReply1> SayHelloAsync(HelloRequest1 request, CallContext context = default);
    }

    // 1877 469 7793
}
