using MapSectionProviderLib;
using MSS.Common;
using MSS.Common.MSetRepo;

namespace MapSectionProviderLib
{
	public static class MapSectionRequestProcessorBuilder
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionAdapter, bool useMapSectionRepository, bool fetchZValues)
		{
			var mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionAdapter) : null;

			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients/*, mapSectionPersistProcessor*/);

			var mapSectionResponseProcessor = new MapSectionResponseProcessor();

			IMapSectionAdapter? mapSectionAdapterForLookup = useMapSectionRepository ? mapSectionAdapter : null;

			// Force LookUps to find nothing
			// mapSectionAdapterForLookup = null;
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionAdapterForLookup, mapSectionGeneratorProcessor, mapSectionResponseProcessor, fetchZValues);

			return mapSectionRequestProcessor;
		}

	}
}
