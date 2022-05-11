using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Types.DataTransferObjects;
using ProjectRepo;
using System.Threading.Tasks;

namespace MSetRepo
{
	public class MapSectionAdapter : IMapSectionAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		public MapSectionAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
		}


		public async Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = await mapSectionReaderWriter.GetAsync(new ObjectId(subdivisionId), blockPosition);

			if (mapSectionRecord == null)
			{
				return null;
			}
			else
			{
				var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);
				return mapSectionResponse;
			}
		}

		//public async Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId)
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var mapSectionRecord = await mapSectionReaderWriter.GetAsync(new ObjectId(mapSectionId));
		//	var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);

		//	return mapSectionResponse;
		//}

		//public MapSectionResponse? GetMapSection(string mapSectionId)
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var mapSectionRecord = mapSectionReaderWriter.Get(new ObjectId(mapSectionId));
		//	var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);

		//	return mapSectionResponse;
		//}

		public async Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var mapSectionId = await mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			return mapSectionId;
		}

		public async Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = await mapSectionReaderWriter.UpdateZValuesAync(new ObjectId(mapSectionResponse.MapSectionId), mapSectionResponse.MapCalcSettings.TargetIterations, mapSectionResponse.Counts, mapSectionResponse.DoneFlags, mapSectionResponse.ZValues);

			return result;
		}

		public long? ClearMapSections(string subdivisionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteAllWithSubId(new ObjectId(subdivisionId));

			return result;
		}

		//public void AddCreatedDateToAllMapSections()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.AddCreatedDateToAllRecords();
		//}

	}
}
