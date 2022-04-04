using MongoDB.Bson;
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

		private static MSetRecordMapper GetMSetRecordMapper()
		{
			var dtoMapper = new DtoMapper();
			var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(); 
			var result = new MSetRecordMapper(dtoMapper, colorBandSetCache);

			return result;
		}

	}
}
