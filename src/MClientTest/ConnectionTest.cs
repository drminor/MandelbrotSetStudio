using Grpc.Net.Client;
using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.ServiceModel;

namespace MClientTest
{
	public class ConnectionTest
	{
		//"applicationUrl": "http://localhost:5000;https://localhost:5001"
		//"applicationUrl": "http://192.168.2.106:5000"

		//private const string appUrl = "http://localhost:5000";

		private const string appUrl = "http://192.168.2.100:5000";

		[Fact]
		public void Connect_Succeeds()
		{
			var mapSectionVectorProvider = CreateMapSectionVectorProvider();

			var grpChannel = GrpcChannel.ForAddress(appUrl);
			var mClient = new MClient(clientNumber: 0, grpChannel, appUrl, mapSectionVectorProvider);

			var response = mClient.GenerateMapSectionTest();

			Debug.WriteLine($"Got a response. The MapSectionId is {response.MapSectionId}.");
		}

		private MapSectionVectorProvider CreateMapSectionVectorProvider()
		{
			var blockSize = new SizeInt(128);
			var defaultLimbCount = 2;
			var initialPoolSize = 10;

			var mapSectionVectorsPool = new MapSectionVectorsPool(blockSize, initialPoolSize);
			var mapSectionZVectorsPool = new MapSectionZVectorsPool(blockSize, defaultLimbCount, initialPoolSize);
			var mapSectionVectorProvider = new MapSectionVectorProvider(mapSectionVectorsPool, mapSectionZVectorsPool);

			return mapSectionVectorProvider;
		}
	}
}