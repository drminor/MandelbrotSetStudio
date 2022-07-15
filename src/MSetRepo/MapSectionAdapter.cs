using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
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

		#region Collections

		public void CreateCollections()
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			jobMapSectionReaderWriter.CreateCollection();

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.CreateCollection();
			mapSectionReaderWriter.CreateSubAndPosIndex();

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.CreateCollection();
		}

		//public void DropCollections()
		//{
		//	var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
		//	jobMapSectionReaderWriter.DropCollection();

		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	mapSectionReaderWriter.DropCollection();

		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
		//	subdivisionReaderWriter.DropCollection();
		//}

		//public void DropSubdivisionsAndMapSectionsCollections()
		//{
		//	var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
		//	jobMapSectionReaderWriter.DropCollection();

		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	mapSectionReaderWriter.DropCollection();

		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
		//	subdivisionReaderWriter.DropCollection();
		//}

		#endregion

		#region MapSection

		public async Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, bool includeZValues)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

			try
			{
				if (includeZValues)
				{
					var mapSectionRecord = await mapSectionReaderWriter.GetAsync(subdivisionId, blockPosition);
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
					var mapSectionRecordCountsOnly = await mapSectionReaderWriter.GetJustCountsAsync(subdivisionId, blockPosition);
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
				var id = await mapSectionReaderWriter.GetId(subdivisionId, blockPosition);
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

		public async Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = await mapSectionReaderWriter.GetZValuesAsync(mapSectionId);

			return result;
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

		public long? ClearMapSections(ObjectId subdivisionId)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteAllWithSubId(subdivisionId);

			return result;
		}

		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard);

			return result;
		}

		public long? DeleteMapSectionsSince(DateTime lastSaved)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var deleteCnt = mapSectionReaderWriter.DeleteMapSectionsSince(lastSaved);

			return deleteCnt;
		}

		//public long? RemoveFetchZValuesProp()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var numberUpdated = mapSectionReaderWriter.RemoveFetchZValuesProp();
		//	return numberUpdated;
		//}

		//public void AddCreatedDateToAllMapSections()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.AddCreatedDateToAllRecords();
		//}

		#endregion

		#region JobMapSection

		public async Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var result = await SaveJobMapSectionAsync(mapSectionResponse.MapSectionId, mapSectionResponse.OwnerId, mapSectionResponse.JobOwnerType);
			return result;
		}

		public async Task<ObjectId?> SaveJobMapSectionAsync(string mapSectionIdStr, string ownerIdStr, int jobOwnerType)
		{
			if (string.IsNullOrEmpty(mapSectionIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionIdStr), "The mapSectionIdStr argument must have a non-null value.");
			}

			if (string.IsNullOrEmpty(ownerIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionIdStr), "The ownerIdStr argument must have a non-null value.");
			}

			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var mapSectionId = new ObjectId(mapSectionIdStr);
			var ownerId = new ObjectId(ownerIdStr);
			var ownerType = jobOwnerType;
			var jobMapSectionRecord = new JobMapSectionRecord(mapSectionId, ownerId, ownerType);

			var jobMapSectionId = await jobMapSectionReaderWriter.InsertAsync(jobMapSectionRecord);

			return jobMapSectionId;
		}



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
