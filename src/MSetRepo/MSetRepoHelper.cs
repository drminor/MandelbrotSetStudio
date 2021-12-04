using MSS.Common.DataTransferObjects;
using MSS.Types;
using ProjectRepo;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{

		public static ProjectAdapter GetMapSectionAdapter(string dbProviderConnString, SizeInt blockSize)
		{
			var dbProvider = new DbProvider(dbProviderConnString);

			var dtoMapper = new DtoMapper();
			var coordsHelper = new CoordsHelper(dtoMapper);
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, coordsHelper);

			var mapSectionAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper, blockSize);

			return mapSectionAdapter;
		}
	}
}
