using MongoDB.Bson;
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






    [DataContract]
    public class SubmitJobRequest
	{
        [DataMember(Order = 1)]
        string JobId { get; set; }
    
        [DataMember(Order = 2)]
        string ParentJobId { get; set; }

        [DataMember(Order = 3)]
        int TransformType { get; set; }





    }

    [ServiceContract]
    public interface IMapSectionService
    {
        [OperationContract]
        Task<HelloReply1> SayHelloAsync(HelloRequest1 request, CallContext context = default);

        [OperationContract]
        ValueTask<bool> SendJobTest(SubmitJobRequest submitJobRequest, CallContext context = default);
    }

    // 1877 469 7793
}
