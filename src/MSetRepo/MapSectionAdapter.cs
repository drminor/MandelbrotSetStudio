using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common.MSetRepo;
using MSS.Types.DataTransferObjects;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
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

		public async Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition, bool excludeZValues = false)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var subObjectId = new ObjectId(subdivisionId);

			if (true == await IsMapSectionAV1Async(subObjectId, blockPosition, mapSectionReaderWriter))
			{
				var mapSectionResponse = await GetMapSectionV1Async(subdivisionId, blockPosition);
				if (mapSectionResponse != null)
				{
					_ = await UpdateToNewV(mapSectionResponse, mapSectionReaderWriter);

					if (excludeZValues)
					{
						mapSectionResponse.DoneFlags = new bool[0];
						mapSectionResponse.ZValues = new double[0];
					}
				}

				return mapSectionResponse;
			}
			else
			{
				try
				{
					if (excludeZValues)
					{
						var mapSectionRecordCountsOnly = await mapSectionReaderWriter.GetJustCountsAsync(subObjectId, blockPosition);
						if (mapSectionRecordCountsOnly != null)
						{
							var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecordCountsOnly);
							return mapSectionResponse;
						}
						else
						{
							return null;
						}
					}
					else
					{
						var mapSectionRecord = await mapSectionReaderWriter.GetAsync(subObjectId, blockPosition);
						if (mapSectionRecord != null)
						{
							var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);
							return mapSectionResponse;
						}
						else
						{
							return null;
						}
					}
				}
				catch
				{
					var id = await mapSectionReaderWriter.GetId(subObjectId, blockPosition);
					if (id != null)
					{
						mapSectionReaderWriter.Delete(id.Value);
					}
					else
					{
						throw new InvalidOperationException("Cannot delete the bad MapSectionRecord.");
					}

					return null;
				}
			}
		}

		private async Task<bool> UpdateToNewV(MapSectionResponse mapSectionResponse, MapSectionReaderWriter mapSectionReaderWriter)
		{
			var updatedMapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);
			var numberOfRecordsUpdated = await mapSectionReaderWriter.UpdateToNewVersionAsync(updatedMapSectionRecord);

			if (numberOfRecordsUpdated == 1)
			{
				Debug.WriteLine($"Updated MapSection: {updatedMapSectionRecord.Id}.");
				mapSectionResponse.JustNowUpdated = true;
				return true;
			}
			else
			{
				Debug.WriteLine($"WARNING: Could not update MapSection: {updatedMapSectionRecord.Id}.");
				return false;
			}
		}

		private async Task<bool?> IsMapSectionAV1Async(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionReaderWriter mapSectionReaderWriter)
		{
			var result = await mapSectionReaderWriter.GetIsV1Async(subdivisionId, blockPosition);

			return result;
		}


		private async Task<MapSectionResponse?> GetMapSectionV1Async(string subdivisionId, BigVectorDto blockPosition)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriterV1(_dbProvider);
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


		//public MapSectionResponse? GetMapSection(string subdivisionId, BigVectorDto blockPosition, bool returnOnlyCounts = false)
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	if (returnOnlyCounts)
		//	{
		//		var mapSectionRecordJustCounts = mapSectionReaderWriter.GetJustCounts(new ObjectId(subdivisionId), blockPosition);
		//		if (mapSectionRecordJustCounts == null)
		//		{
		//			return null;
		//		}
		//		else
		//		{
		//			var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecordJustCounts);
		//			return mapSectionResponse;
		//		}
		//	}
		//	else
		//	{
		//		var mapSectionRecord = mapSectionReaderWriter.Get(new ObjectId(subdivisionId), blockPosition);
		//		if (mapSectionRecord == null)
		//		{
		//			return null;
		//		}
		//		else
		//		{
		//			var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord);
		//			return mapSectionResponse;
		//		}
		//	}

		//}

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
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var result = await mapSectionReaderWriter.UpdateZValuesAync(mapSectionRecord);

			return result;
		}

		public long? ClearMapSections(string subdivisionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteAllWithSubId(new ObjectId(subdivisionId));

			return result;
		}

		public long? DeleteMapSectionsSince(DateTime lastSaved, bool overrideRecentGuard = false)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteMapSectionsSince(lastSaved, overrideRecentGuard);

			return result;
		}

		//public void AddCreatedDateToAllMapSections()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.AddCreatedDateToAllRecords();
		//}

	}
}
