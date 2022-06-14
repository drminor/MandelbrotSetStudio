using MapSectionProviderLib;
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

			IMapSectionAdapter forLookup = useMapSectionRepository ? mapSectionAdapter : null;

			// Force LookUps to find nothing
			//forLookup = null;

			var mapSectionRequestProcessor = new MapSectionRequestProcessor(forLookup, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
