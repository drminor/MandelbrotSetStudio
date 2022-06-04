using MapSectionProviderLib;
using MSS.Common;
using MSS.Common.MSetRepo;

namespace MEngineClient
{
	public static class MapSectionRequestProcessorProvider
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(IMEngineClient[] mEngineClients, IMapSectionAdapter mapSectionRepo, bool useMapSectionRepository)
		{
			var mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionRepo) : null;
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClients, mapSectionPersistProcessor);

			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionRepo, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
