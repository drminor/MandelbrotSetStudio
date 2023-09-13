using MEngineClient;
using MSS.Common;
using System.Diagnostics;

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
			var mClient = new MClient(MSetGenerationStrategy.DepthFirst, appUrl);

			var response = mClient.GenerateMapSectionTest();

			Debug.WriteLine($"Got a response. The MapSectionId is {response.MapSectionId}.");
		}
	}
}