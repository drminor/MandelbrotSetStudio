using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using ProjectRepo;
using System;
using System.Collections.Generic;

namespace MSetRepo
{
	public static class MSetRepoHelper
	{
		public static ProjectAdapter GetProjectAdapter(string dbProviderConnString, ProjectInfoCreator projectInfoCreator)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mSetRecordMapper = GetMSetRecordMapper();
			var projectAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper, projectInfoCreator);

			return projectAdapter;
		}

		// TODO: Create a separate IMapper Implementation just for the MapSectionAdapter.
		public static IMapSectionAdapter GetMapSectionAdapter(string dbProviderConnString)
		{
			var dbProvider = new DbProvider(dbProviderConnString);
			var mSetRecordMapper = GetMSetRecordMapper();
			var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper);

			return mapSectionAdapter;
		}

		public static MSetRecordMapper GetMSetRecordMapper()
		{
			var dtoMapper = new DtoMapper();
			var colorBandSetCache = new Dictionary<Guid, ColorBandSetW>(); 
			var result = new MSetRecordMapper(dtoMapper, colorBandSetCache);

			return result;
		}

	}
}
