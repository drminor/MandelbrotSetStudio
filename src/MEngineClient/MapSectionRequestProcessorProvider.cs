using MapSectionProviderLib;
using MSetRepo;

namespace MEngineClient
{
	public static class MapSectionRequestProcessorProvider
	{
		public static MapSectionRequestProcessor CreateMapSectionRequestProcessor(string clientAddress, string mongoDbConnectionString, bool useMapSectionRepository)
		{
			var mEngineClient = new MClient(clientAddress);
			var mapSectionRepo = MSetRepoHelper.GetMapSectionRepo(mongoDbConnectionString);

			MapSectionPersistProcessor _mapSectionPersistProcessor = useMapSectionRepository ? new MapSectionPersistProcessor(mapSectionRepo) : null;
			var mapSectionGeneratorProcessor = new MapSectionGeneratorProcessor(mEngineClient, _mapSectionPersistProcessor);

			MapSectionResponseProcessor mapSectionResponseProcessor = new MapSectionResponseProcessor();
			MapSectionRequestProcessor mapSectionRequestProcessor = new MapSectionRequestProcessor(mapSectionRepo, mapSectionGeneratorProcessor, mapSectionResponseProcessor);

			return mapSectionRequestProcessor;
		}

	}
}
