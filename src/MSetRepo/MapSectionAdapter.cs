using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using ProjectRepo;
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

		public MapSectionResponse? GetMapSection(string mapSectionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = mapSectionReaderWriter.Get(new ObjectId(mapSectionId));
			var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);

			return mapSectionResponse;
		}

		public async Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, PointInt blockPosition)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = await mapSectionReaderWriter.GetAsync(new ObjectId(subdivisionId), blockPosition);
			var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);

			return mapSectionResponse;
		}

		public async Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = await mapSectionReaderWriter.GetAsync(new ObjectId(mapSectionId));
			var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);

			return mapSectionResponse;
		}

		public async Task<string> SaveMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			if (mapSectionRecord is null) return string.Empty;

			var mapSectionId = await mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			return mapSectionId.ToString();
		}
	}
}
