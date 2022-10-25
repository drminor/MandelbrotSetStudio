using MapSectionProviderLib;
using MEngineDataContracts;
using MSetRepo;
using MSS.Common;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapSectionGeneratorLib
{
	public class MClientInProcess : IMEngineClient
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private static int _sectionCntr;

		public MClientInProcess(IMapSectionAdapter mapSectionAdapter, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;
			_sectionCntr = 0;
		}

		public string EndPointAddress => "InProcessServer";

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
			var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest, _mapSectionAdapter);

			var idStr = string.IsNullOrEmpty(mapSectionResponse.MapSectionId) ? "new" : mapSectionResponse.MapSectionId;

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"Adding MapSectionResponse with ID: {idStr} to the MapSection Persist Processor. Generated {_sectionCntr} Map Sections.");
			}

			_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}


	}
}
