using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using ProjectRepo;
using System;
using System.Threading.Tasks;

namespace MSetRepo
{
	public class MapSectionRepo : IMapSectionRepo
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		public MapSectionRepo(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
		}

		public async Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, PointInt blockPosition)
		{
			await Task.Delay(50);

			return null;

			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		}

		public Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId)
		{
			throw new NotImplementedException();
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		}

		public void SaveMapSection(MapSectionResponse mapSectionResponse)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		}
	}
}
