using MapSectionProviderLib;
using MSS.Common;

namespace MEngineClient
{
	public static class MapSectionRequestProcessorProvider
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(MClient mEngineClient, IMapSectionAdapter mapSectionRepo, bool useMapSectionRepository)
		{
			var mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionRepo) : null;
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClient, mapSectionPersistProcessor);

			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionRepo, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
