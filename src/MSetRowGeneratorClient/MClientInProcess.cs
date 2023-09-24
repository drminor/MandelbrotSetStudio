//using MapSectionProviderLib;
//using MEngineDataContracts;

using MSS.Common;
using MSS.Types.MSet;
using System.Diagnostics;

namespace MSetRowGeneratorClient
{
	public class MClientInProcess : IMEngineClient
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;
		//private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private static int _sectionCntr;

		//MClientInProcess()
		//{
		//	_sectionCntr = 0;
		//	_mapSectionAdapter = new MapSection
		//}

		public MClientInProcess(IMapSectionAdapter mapSectionAdapter/*, MapSectionPersistProcessor mapSectionPersistProcessor*/)
		{
			_mapSectionAdapter = mapSectionAdapter;
			//_mapSectionPersistProcessor = mapSectionPersistProcessor;
		}

		public string EndPointAddress => "C++_InProcessServer";
		public bool IsLocal => true;

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			await Task.Delay(100);
			throw new NotImplementedException();
		}

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				return new MapSectionResponse(mapSectionRequest, isCancelled: true);
			}

			mapSectionRequest.ClientEndPointAddress = EndPointAddress;

			var stopWatch = Stopwatch.StartNew();
			var mapSectionResponse = GenerateMapSectionInternal(mapSectionRequest, ct);
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;

			//Debug.Assert(mapSectionResponse.ZValues == null && mapSectionResponse.ZValuesForLocalStorage == null, "The MapSectionResponse includes ZValues.");

			return mapSectionResponse;
		}

		private MapSectionResponse GenerateMapSectionInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			//var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest, , ct);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
			}

			////mapSectionResponse.IncludeZValues = false;

			//return mapSectionResponse;

			throw new NotImplementedException();


		}
	}
}
