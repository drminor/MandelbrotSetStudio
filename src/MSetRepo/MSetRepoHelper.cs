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
			var mapSectionAdapter = new ProjectAdapter(dbProvider, GetMSetRecordMapper(), projectInfoCreator);

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
			var colorBandSetCache = new Dictionary<Guid, ColorBandSet>(); 
			var result = new MSetRecordMapper(dtoMapper, colorBandSetCache);

			return result;
		}

	}
}
