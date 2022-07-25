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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetRepo
{
	public class MapSectionAdapter : IMapSectionAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;
		private readonly DtoMapper _dtoMapper;

		#region Constructor

		public MapSectionAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
			_dtoMapper = new DtoMapper();
		}

		#endregion

		#region Collections

		public void CreateCollections()
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			jobMapSectionReaderWriter.CreateCollection();

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			if (mapSectionReaderWriter.CreateCollection())
			{
				mapSectionReaderWriter.CreateSubAndPosIndex();
			}

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			_ = subdivisionReaderWriter.CreateCollection();
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

		public async Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, bool includeZValues, CancellationToken ct)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

			try
			{
				if (includeZValues)
				{
					var mapSectionRecord = await mapSectionReaderWriter.GetAsync(subdivisionId, blockPosition, ct);
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
					var mapSectionRecordCountsOnly = await mapSectionReaderWriter.GetJustCountsAsync(subdivisionId, blockPosition, ct);
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

		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = mapSectionReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard);

			return result;
		}

		#endregion

		#region JobMapSection

		public async Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionIdStr = mapSectionResponse.MapSectionId;
			if (string.IsNullOrEmpty(mapSectionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.MapSectionId), "The MapSectionId cannot be null.");
			}

			var ownerIdStr = mapSectionResponse.OwnerId;
			if (string.IsNullOrEmpty(ownerIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionIdStr), "The OwnerId cannot be null.");
			}

			var result = await SaveJobMapSectionAsync(new ObjectId(mapSectionIdStr), new ObjectId(ownerIdStr), mapSectionResponse.JobOwnerType);
			return result;
		}

		private async Task<ObjectId?> SaveJobMapSectionAsync(ObjectId mapSectionId, ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var existingRecord = await jobMapSectionReaderWriter.GetByMapAndOwnerIdAsync(mapSectionId, ownerId, jobOwnerType);
			if (existingRecord == null)
			{
				var jobMapSectionRecord = new JobMapSectionRecord(mapSectionId, ownerId, jobOwnerType);
				var jobMapSectionId = await jobMapSectionReaderWriter.InsertAsync(jobMapSectionRecord);
				return jobMapSectionId;
			}
			else
			{
				return existingRecord.Id;
			}
		}

		public long? DeleteMapSectionsForMany(IEnumerable<ObjectId> ownerIds, JobOwnerType jobOwnerType)
		{
			var result = 0L;

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			foreach (var ownerId in ownerIds)
			{
				Debug.WriteLine($"Removing MapSections and JobMapSections for {jobOwnerType}: {ownerId}.");
				var singleResult = DeleteMapSectionsForJobInternal(ownerId, jobOwnerType, mapSectionReaderWriter, jobMapSectionReaderWriter, out var numberJobMapSectionsDeleted);
				Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {singleResult} MapSections.");

				if (singleResult.HasValue)
				{
					result += singleResult.Value;
				}
			}

			return result;
		}

		public long? DeleteMapSectionsForJob(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			Debug.WriteLine($"Removing MapSections and JobMapSections for {jobOwnerType}: {ownerId}.");
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var result = DeleteMapSectionsForJobInternal(ownerId, jobOwnerType, mapSectionReaderWriter, jobMapSectionReaderWriter, out var numberJobMapSectionsDeleted);

			Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {result} MapSections.");

			return result;
		}

		private long? DeleteMapSectionsForJobInternalNew(ObjectId ownerId, JobOwnerType jobOwnerType, MapSectionReaderWriter mapSectionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter, out long? numberJobMapSectionsDeleted)
		{
			var jobMapSectionRecords = jobMapSectionReaderWriter.GetByOwnerId(ownerId, jobOwnerType);
			var jobMapSectionRecIds = jobMapSectionRecords.Select(x => x.Id).ToList();

			numberJobMapSectionsDeleted = jobMapSectionReaderWriter.DeleteJobMapSections(ownerId, jobOwnerType);

			var jobMapSecRecIdsExtant = jobMapSectionReaderWriter.DoJobMapSectionRecordsExist(jobMapSectionRecIds).ToArray();

			var toBeRemoved = new List<ObjectId>();

			foreach(var recId in jobMapSectionRecIds)
			{
				if (!jobMapSecRecIdsExtant.Contains(recId))
				{
					toBeRemoved.Add(recId);
				}
			}

			var numberDeleted = mapSectionReaderWriter.Delete(toBeRemoved);

			return numberDeleted;
		}

		private long? DeleteMapSectionsForJobInternal(ObjectId ownerId, JobOwnerType jobOwnerType, MapSectionReaderWriter mapSectionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter, out long? numberJobMapSectionsDeleted)
		{
			var jobMapSectionRecords = jobMapSectionReaderWriter.GetByOwnerId(ownerId, jobOwnerType);

			//var beforeCnt = 0L;
			//foreach (var jobMapSectionRecord in jobMapSectionRecords)
			//{
			//	var jobMapSectionRecordsBefore = jobMapSectionReaderWriter.GetByMapSectionId(jobMapSectionRecord.MapSectionId);

			//	beforeCnt += jobMapSectionRecordsBefore.Count;
			//}

			numberJobMapSectionsDeleted = jobMapSectionReaderWriter.DeleteJobMapSections(ownerId, jobOwnerType);

			var jobMapSectionRecIds = jobMapSectionRecords.Select(x => x.Id);
			var jobMapSecRecIdsExtant = jobMapSectionReaderWriter.DoJobMapSectionRecordsExist(jobMapSectionRecIds);

			var result = 0L;
			//var afterCnt = 0L;
			var jobMapSectionFound = 0L;
			foreach (var jobMapSectionRecord in jobMapSectionRecords)
			{
				//var jobMapSectionRecordsAfter = jobMapSectionReaderWriter.GetByMapSectionId(jobMapSectionRecord.MapSectionId);
				//afterCnt += jobMapSectionRecordsAfter.Count;

				if (!jobMapSectionReaderWriter.DoesJobMapSectionRecordExist(jobMapSectionRecord.MapSectionId))
				{
					var numberDeleted = mapSectionReaderWriter.Delete(jobMapSectionRecord.MapSectionId);
					if (numberDeleted.HasValue)
					{
						result += numberDeleted.Value;
					}
				}
				else
				{
					jobMapSectionFound++;
				}
			}

			if (jobMapSecRecIdsExtant.Count() !=  jobMapSectionFound)
			{
				Debug.WriteLine("Hi there.");
			}

			return result;
		}

		public long? DuplicateJobMapSections(ObjectId ownerId, JobOwnerType jobOwnerType, ObjectId newOwnerId)
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var jobMapSectionRecords = jobMapSectionReaderWriter.GetByOwnerId(ownerId, jobOwnerType);

			foreach (var jmsr in jobMapSectionRecords)
			{
				var newJmsr = new JobMapSectionRecord(jmsr.MapSectionId, newOwnerId, jmsr.OwnerType);
				_ = jobMapSectionReaderWriter.Insert(newJmsr);
			}

			var result = jobMapSectionRecords.Count;

			return result;
		}

		//public async Task<long?> DeleteMapSectionsForJobAsync(ObjectId ownerId, JobOwnerType jobOwnerType)
		//{
		//	Debug.WriteLine($"Removing MapSections and JobMapSections for {jobOwnerType}: {ownerId}. (Async)");
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

		//	var jobMapSectionRecords = await jobMapSectionReaderWriter.GetByOwnerIdAsync(ownerId, jobOwnerType);
		//	var numberJobMapSectionsDeleted = await jobMapSectionReaderWriter.DeleteJobMapSectionsAsync(ownerId, jobOwnerType);

		//	var result = 0L;

		//	// TODO: Update to use Async methods
		//	foreach (var jobMapSectionRecord in jobMapSectionRecords)
		//	{
		//		if (!jobMapSectionReaderWriter.DoesJobMapSectionRecordExist(jobMapSectionRecord.MapSectionId))
		//		{
		//			var numberDeleted = mapSectionReaderWriter.Delete(jobMapSectionRecord.MapSectionId);
		//			if (numberDeleted.HasValue)
		//			{
		//				result += numberDeleted.Value;
		//			}
		//		}
		//	}

		//	Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {result} MapSections. (Async)");
		//	return result;
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
