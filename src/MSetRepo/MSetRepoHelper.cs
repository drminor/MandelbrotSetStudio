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

		public static IMapSectionAdapter GetMapSectionAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, GetMSetRecordMapper());

			return mapSectionAdapter;
		}

		public static MSetRecordMapper GetMSetRecordMapper()
		{
			var dtoMapper = new DtoMapper();
			var result = new MSetRecordMapper(dtoMapper);

			return result;
		}

	}
}
