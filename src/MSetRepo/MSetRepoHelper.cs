using MSS.Common.MSetRepo;
using MSS.Common.DataTransferObjects;
using ProjectRepo;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{
		public static ProjectAdapter GetProjectAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mSetRecordMapper = GetMSetRecordMapper();
			var projectAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper);

			return projectAdapter;
		}

		public static IMapSectionAdapter GetMapSectionAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mSetRecordMapper = GetMSetRecordMapper();
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper);

			return mapSectionAdapter;
		}

		public static SharedColorBandSetAdapter GetSharedColorBandSetAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mSetRecordMapper = GetMSetRecordMapper();
			var sharedColorBandSetAdapter = new SharedColorBandSetAdapter(dbProvider, mSetRecordMapper);

			return sharedColorBandSetAdapter;
		}

		private static MSetRecordMapper GetMSetRecordMapper()
		{
			var dtoMapper = new DtoMapper();
			var result = new MSetRecordMapper(dtoMapper);

			return result;
		}

	}
}
