using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{
		public static IProjectAdapter GetProjectAdapter(string server, int port)
		{
			var dbProvider = new DbProvider(server, port);
			var mSetRecordMapper = GetMSetRecordMapper();
			var projectAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper);

			return projectAdapter;
		}

		public static IMapSectionAdapter GetMapSectionAdapter(string server, int port)
		{
			var dbProvider = new DbProvider(server, port);
			var mSetRecordMapper = GetMSetRecordMapper();
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper);

			return mapSectionAdapter;
		}

		public static SharedColorBandSetAdapter GetSharedColorBandSetAdapter(string server, int port)
		{
			var dbProvider = new DbProvider(server, port);
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
