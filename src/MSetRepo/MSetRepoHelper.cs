using MSS.Common;
using MSS.Common.DataTransferObjects;
using ProjectRepo;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{
		public static ProjectAdapter GetProjectAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mapSectionAdapter = new ProjectAdapter(dbProvider, GetMSetRecordMapper());

			return mapSectionAdapter;
		}

		public static IMapSectionRepo GetMapSectionRepo(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mapSectionRepo = new MapSectionRepo(dbProvider, GetMSetRecordMapper());

			return mapSectionRepo;
		}

		public static MSetRecordMapper GetMSetRecordMapper()
		{
			var dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var result = new MSetRecordMapper(dtoMapper, coordsHelper);

			return result;
		}

	}
}
