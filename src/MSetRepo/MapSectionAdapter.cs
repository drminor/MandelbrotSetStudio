using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MSetRepo
{
	public class MapSectionAdapter : IMapSectionAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;
		private readonly DtoMapper _dtoMapper;

		public MapSectionAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
			_dtoMapper = new DtoMapper();
		}

		#region MapSection

		public async Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, BigVectorDto blockPosition, bool includeZValues)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var subObjectId = new ObjectId(subdivisionId);

			if (true == await IsMapSectionAV1Async(subObjectId, blockPosition, mapSectionReaderWriter))
			{
				var mapSectionResponse = await GetMapSectionV1Async(subdivisionId, blockPosition);
				if (mapSectionResponse != null)
				{
					_ = await UpdateToNewV(mapSectionResponse, mapSectionReaderWriter);

					if (!includeZValues)
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
					if (includeZValues)
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
					else
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
				}
				catch (Exception e)
				{
					Debug.WriteLine($"While reading JustCounts, got exception: {e}.");
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

		public async Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = await mapSectionReaderWriter.GetZValuesAsync(mapSectionId);

			return result;
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

		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard);

			return result;
		}


		//public void AddCreatedDateToAllMapSections()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.AddCreatedDateToAllRecords();
		//}
		#endregion

		#region Subdivision

		public bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var samplePointDeltaReduced = Reducer.Reduce(samplePointDelta);
			var samplePointDeltaDto = _dtoMapper.MapTo(samplePointDeltaReduced);

			var matches = subdivisionReaderWriter.Get(samplePointDeltaDto, blockSize);

			if (matches.Count > 1)
			{
				throw new InvalidOperationException($"Found more than one subdivision was found matching: {samplePointDelta}.");
			}

			bool result;

			if (matches.Count < 1)
			{
				subdivision = null;
				result = false;
			}
			else
			{
				var subdivisionRecord = matches[0];
				subdivision = _mSetRecordMapper.MapFrom(subdivisionRecord);
				result = true;
			}

			return result;
		}

		public void InsertSubdivision(Subdivision subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			_ = subdivisionReaderWriter.Insert(subdivisionRecord);
		}

		//public bool DeleteSubdivision(Subdivision subdivision)
		//{
		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
		//	var subsDeleted = subdivisionReaderWriter.Delete(subdivision.Id);

		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	_ = mapSectionReaderWriter.DeleteAllWithSubId(subdivision.Id);

		//	return subsDeleted.HasValue && subsDeleted.Value > 0;
		//}

		//public Subdivision[] GetAllSubdivions()
		//{
		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

		//	var allRecs = subdivisionReaderWriter.GetAll();

		//	var result = allRecs.Select(x => _mSetRecordMapper.MapFrom(x)).ToArray();

		//	return result;
		//}

		//public SubdivisionInfo[] GetAllSubdivisionInfos()
		//{
		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

		//	var allRecs = subdivisionReaderWriter.GetAll();

		//	var result = allRecs
		//		.Select(x => _mSetRecordMapper.MapFrom(x))
		//		.Select(x => new SubdivisionInfo(x.Id, x.SamplePointDelta.Width))
		//		.ToArray();

		//	return result;
		//}


		#endregion
	}
}
