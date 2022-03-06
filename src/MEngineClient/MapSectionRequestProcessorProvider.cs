using MapSectionProviderLib;
using MSetRepo;

namespace MEngineClient
{
	public static class MapSectionRequestProcessorProvider
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(string clientAddress, string mongoDbConnectionString, bool useMapSectionRepository)
		{
			var mEngineClient = new MClient(clientAddress);
			var mapSectionRepo = MSetRepoHelper.GetMapSectionAdapter(mongoDbConnectionString);

			var mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionRepo) : null;
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClient, mapSectionPersistProcessor);

			var mapSectionResponseProcessor = new MapSectionResponseProcessor();
			var mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionRepo, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
