using MapSectionProviderLib;
using MEngineDataContracts;

using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using ProtoBuf.Grpc.Client;
using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace MSetGeneratorLib
{
	public class MClientInProcess : IMEngineClient
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapSectionPersistProcessor _mapSectionPersistProcessor;

		private static int _sectionCntr;

		//MClientInProcess()
		//{
		//	_sectionCntr = 0;
		//	_mapSectionAdapter = new MapSection
		//}

		public MClientInProcess(IMapSectionAdapter mapSectionAdapter, MapSectionPersistProcessor mapSectionPersistProcessor)
		{
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionPersistProcessor = mapSectionPersistProcessor;
		}

		public string EndPointAddress => "C++_InProcessServer";
		public bool IsLocal => true;

		public async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			mapSectionRequest.ClientEndPointAddress = EndPointAddress;
			var stopWatch = Stopwatch.StartNew();

			var mapSectionResponse = await GenerateMapSectionAsyncInternal(mapSectionRequest, ct);
			
			mapSectionRequest.TimeToCompleteGenRequest = stopWatch.Elapsed;
			return mapSectionResponse;
		}

		private async Task<MapSectionResponse> GenerateMapSectionAsyncInternal(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var mapSectionResponse = await MapSectionGenerator.GenerateMapSectionAsync(mapSectionRequest, _mapSectionAdapter, ct);

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

			//mapSectionResponse.IncludeZValues = false;

			return mapSectionResponse;
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
			//var mapSectionResponse = MapSectionGenerator.GenerateMapSection(mapSectionRequest, ct);

			//if (++_sectionCntr % 10 == 0)
			//{
			//	Debug.WriteLine($"The MEngineClient, {EndPointAddress} has processed {_sectionCntr} requests.");
			//}

			////mapSectionResponse.IncludeZValues = false;

			//return mapSectionResponse;

			throw new NotImplementedException();
		}
	}
}
