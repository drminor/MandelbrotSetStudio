using MEngineDataContracts;
using MSS.Common;
using System.Diagnostics;

namespace MSetGenP
{
	public class MClientLocal : IMEngineClient
	{
		private static int _sectionCntr;

		public MClientLocal()
		{
			_sectionCntr = 0;
		}

		public string EndPointAddress => "LocalCSharpServer-Serial";

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;
			var stopWatch = Stopwatch.StartNew();

			var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest);
			
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;
			return mapSectionResponse;
		}

		private async Task<MapSectionResponse> GenerateMapSectionAsyncInternal(MapSectionRequest mapSectionRequest)
		{
			if (DateTime.Now > DateTime.Today.AddDays(1d))
			{
				await Task.Delay(100);
			}

			var mapSectionResponse = MapSectionGeneratorSerial.GenerateMapSection(mapSectionRequest);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {++_sectionCntr} requests.");
			}

			mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}

	}
}
