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

		MClientInProcess()
		{
			_sectionCntr = 0;
		}

		public MClientInProcess(IMapSectionAdapter mapSectionAdapter, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;
		}

		public string EndPointAddress => "C++_InProcessServer";
		public bool IsLocal => true;

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

			//var idStr = string.IsNullOrEmpty(mapSectionResponse.MapSectionId) ? "new" : mapSectionResponse.MapSectionId;
			//if (++_sectionCntr % 10 == 0)
			//{
			//	Debug.WriteLine($"Adding MapSectionResponse with ID: {idStr} to the MapSection Persist Processor. Generated {_sectionCntr} Map Sections.");
			//}
			//_mapSectionPersistProcessor.AddWork(mapSectionResponse);

			if (++_sectionCntr % 10 == 0)
			{
				Debug.WriteLine($"Generated {_sectionCntr} Map Sections.");
			}

			mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
		}


	}
}
