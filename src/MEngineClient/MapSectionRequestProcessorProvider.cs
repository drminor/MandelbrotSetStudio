﻿using MapSectionProviderLib;
using MSS.Common;
using MSS.Common.MSetRepo;

namespace MEngineClient
{
	public static class MapSectionRequestProcessorProvider
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionAdapter, bool useMapSectionRepository)
		{
			var mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionAdapter) : null;

			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients, mapSectionPersistProcessor);

			var mapSectionResponseProcessor = new MapSectionResponseProcessor();

			IMapSectionAdapter? mapSectionAdapterForLookup = useMapSectionRepository ? mapSectionAdapter : null;

			// Force LookUps to find nothing
			//forLookup = null;

			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionAdapterForLookup, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
