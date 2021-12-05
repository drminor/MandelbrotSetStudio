using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using ProjectRepo;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{
		public static ProjectAdapter GetProjectAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);

			var dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, coordsHelper);

			var mapSectionAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper);

			return mapSectionAdapter;
		}

		public static IMapSectionRepo GetMapSectionRepo(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);

			var dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, coordsHelper);

			var mapSectionRepo = new MapSectionRepo(dbProvider, mSetRecordMapper);


			return mapSectionRepo;
		}


	}
}
