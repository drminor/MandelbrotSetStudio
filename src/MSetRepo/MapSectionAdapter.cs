using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSetRepo
{
	public class MapSectionAdapter : IMapSectionAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionReaderWriter _mapSectionReaderWriter;
		private readonly MapSectionZValuesReaderWriter _mapSectionZValuesReaderWriter;
		private readonly JobMapSectionReaderWriter _jobMapSectionReaderWriter;
		private readonly SubdivisonReaderWriter _subdivisionReaderWriter;

		#region Constructor

		public MapSectionAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
			_dtoMapper = new DtoMapper();

			_mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			_jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			_subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			//BsonSerializer.RegisterSerializer(new ZValuesSerializer());

			//BsonClassMap.RegisterClassMap<ZValues>(cm => {
			//	cm.AutoMap();
			//	cm.GetMemberMap(c => c.Zrs).SetSerializer(new ZValuesArraySerializer());
			//	cm.GetMemberMap(c => c.Zis).SetSerializer(new ZValuesArraySerializer());
			//});
		}

		#endregion

		#region Collections

		public void CreateCollections()
		{
			if (_jobMapSectionReaderWriter.CreateCollection())
			{
				_jobMapSectionReaderWriter.CreateOwnerAndTypeIndex();
				_jobMapSectionReaderWriter.CreateMapSectionIdIndex();
			}

			if (_mapSectionReaderWriter.CreateCollection())
			{
				_mapSectionReaderWriter.CreateSubAndPosIndex();
			}

			_ = _subdivisionReaderWriter.CreateCollection();

			if (_mapSectionZValuesReaderWriter.CreateCollection())
			{
				_mapSectionZValuesReaderWriter.CreateSectionIdIndex();
			}
		}

		public void CreateIndexes()
		{
			_jobMapSectionReaderWriter.CreateOwnerAndTypeIndex();
			_jobMapSectionReaderWriter.CreateMapSectionIdIndex();

			_mapSectionReaderWriter.CreateSubAndPosIndex();

			_mapSectionZValuesReaderWriter.CreateSectionIdIndex();
		}

		public void DropMapSections()
		{
			_jobMapSectionReaderWriter.DropCollection();

			_mapSectionZValuesReaderWriter.DropCollection();

			_mapSectionReaderWriter.DropCollection();
		}

		public void DropMapSectionsAndSubdivisions()
		{
			_jobMapSectionReaderWriter.DropCollection();

			_mapSectionZValuesReaderWriter.DropCollection();

			_mapSectionReaderWriter.DropCollection();

			_subdivisionReaderWriter.DropCollection();
		}

		public long GetSizeOfCollectionInMB()
		{
			var result = _mapSectionReaderWriter.GetSizeOfCollectionInMB();
			return result;
		}

		public int GetSizeOfDocZero()
		{
			var result = _mapSectionReaderWriter.GetSizeOfDocZero();
			return result;
		}

		#endregion

		#region MapSection

		public MapSectionBytes? GetMapSectionBytes(ObjectId subdivisionId, VectorLong blockPosition)
		{
			try
			{
				var mapSectionRecord = _mapSectionReaderWriter.Get(subdivisionId, blockPosition);
				if (mapSectionRecord != null)
				{
					var result = _mSetRecordMapper.MapFrom(mapSectionRecord);

					return result;
				}
				else
				{
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GetMapSectionBytes: While fetching a MapSectionRecord from Subdivision and BlockPosition (Async), got exception: {e}.");

				return null;
			}
		}

		public async Task<MapSectionBytes?> GetMapSectionBytesAsync(ObjectId subdivisionId, VectorLong blockPosition, CancellationToken ct)
		{
			try
			{
				var mapSectionRecord = await _mapSectionReaderWriter.GetAsync(subdivisionId, blockPosition, ct);

				if (mapSectionRecord != null)
				{
					var result = _mSetRecordMapper.MapFrom(mapSectionRecord);

					return result;
				}
				else
				{
					return null;
				}
			}
			catch (OperationCanceledException)
			{
				// Ignore
				return null;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"GetMapSectionBytesAsync: While fetching a MapSectionRecord from Subdivision and BlockPosition (Async), got exception: {e}.");
				throw;
			}
		}

		public ObjectId? GetMapSectionId(ObjectId subdivisionId, VectorLong blockPosition)
		{
			try
			{
				var mapSectionRecord = _mapSectionReaderWriter.Get(subdivisionId, blockPosition);

				if (mapSectionRecord != null)
				{
					return mapSectionRecord.Id;
				}
				else
				{
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While fetching a MapSectionId from Subdivision and BlockPosition (synchronous), got exception: {e}.");
				return null;
			}
		}

		public ObjectId? SaveMapSection(MapSectionResponse mapSectionResponse)
		{
			var originalId = mapSectionResponse.MapSectionId;
			Debug.Assert(originalId == null, "MapSectionId is not null on call to SaveMapSectionAsync.");

			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var mapSectionId = _mapSectionReaderWriter.Insert(mapSectionRecord);

			if (mapSectionRecord.Id != ObjectId.Empty && mapSectionId != mapSectionRecord.Id)
			{
				mapSectionResponse.MapSectionId = mapSectionRecord.Id.ToString();
			}

			return mapSectionId;
		}

		public async Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var originalId = mapSectionResponse.MapSectionId;
			Debug.Assert(originalId == null, "MapSectionId is not null on call to SaveMapSectionAsync.");

			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var mapSectionId = await _mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			if (mapSectionRecord.Id != ObjectId.Empty && mapSectionId != mapSectionRecord.Id)
			{
				mapSectionResponse.MapSectionId = mapSectionRecord.Id.ToString();
			}

			return mapSectionId;
		}

		public long? UpdateCountValues(MapSectionResponse mapSectionResponse)
		{
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var result = _mapSectionReaderWriter.UpdateCountValues(mapSectionRecord, mapSectionResponse.RequestCompleted);

			return result;
		}

		public async Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var result = await _mapSectionReaderWriter.UpdateCountValuesAync(mapSectionRecord, mapSectionResponse.RequestCompleted);

			return result;
		}

		public void UpdateJobMapSectionSubdivisionIds(ObjectId jobMapSectionId, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId)
		{
			_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
		}


		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			var result = _mapSectionReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard) ?? 0;
			var result2 = _mapSectionZValuesReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard) ?? 0;
			result += result2;

			return result;
		}

		public IEnumerable<ObjectId> GetSubdivisionIdsForAllMapSections()
		{
			var result = _mapSectionReaderWriter.GetAllSubdivisionIds();

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, DateTime, ObjectId>> GetMapSectionCreationDatesAndSubIds(IEnumerable<ObjectId> mapSectionIds)
		{
			var result = _mapSectionReaderWriter.GetCreationDatesAndSubIds(mapSectionIds);

			return result;
		}
		#endregion

		#region MapSection ZValues

		public bool DoesMapSectionZValuesExist(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = _mapSectionZValuesReaderWriter.RecordExists(mapSectionId, ct);

			return result;
		}

		public async Task<bool> DoesMapSectionZValuesExistAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionZValuesReaderWriter.RecordExistsAsync(mapSectionId, ct);

			return result;
		}

		public async Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionZValuesReaderWriter.GetBySectionIdAsync(mapSectionId, ct);

			return result?.ZValues;
		}

		public ZValues? GetMapSectionZValues(ObjectId mapSectionId)
		{
			var result = _mapSectionZValuesReaderWriter.GetBySectionId(mapSectionId);

			return result?.ZValues;
		}

		public ObjectId? SaveMapSectionZValues(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var mapSectionZValuesId = _mapSectionZValuesReaderWriter.Insert(mapSectionZValuesRecord);

			return mapSectionZValuesId;
		}

		public async Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var mapSectionZValuesId = await _mapSectionZValuesReaderWriter.InsertAsync(mapSectionZValuesRecord);

			return mapSectionZValuesId;
		}

		public long? UpdateZValues(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var result = _mapSectionZValuesReaderWriter.UpdateZValuesByMapSectionId(mapSectionZValuesRecord, mapSectionId);

			return result;
		}

		public async Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var result = await _mapSectionZValuesReaderWriter.UpdateZValuesByMapSectionIdAync(mapSectionZValuesRecord, mapSectionId);

			return result;
		}

		private MapSectionZValuesRecord GetZValues(MapSectionResponse source, ObjectId mapSectionId)
		{
			if (source.MapSectionId == null)
			{
				throw new InvalidOperationException("The MapSectionResponse has a null MapSectionId.");
			}

			var zVectors = source.MapSectionZVectors;

			if (zVectors == null)
			{
				throw new InvalidOperationException("The MapSectionResponse has a null MapSectionZVectors.");
			}

			var zValues = new ZValues(zVectors.BlockSize, zVectors.LimbCount, zVectors.Zrs, zVectors.Zis, zVectors.HasEscapedFlags, zVectors.GetBytesForRowHasEscaped());

			var result = new MapSectionZValuesRecord
				(
				MapSectionId: mapSectionId,
				DateCreatedUtc: DateTime.UtcNow,
				ZValues: zValues
				)
			{
				Id = ObjectId.GenerateNewId(),
				LastSavedUtc = DateTime.UtcNow,
				LastAccessedUtc = DateTime.UtcNow
			};

			return result;
		}

		public long? DeleteZValues(ObjectId mapSectionId)
		{
			var result = _mapSectionZValuesReaderWriter.Delete(mapSectionId);

			return result;
		}

		public async Task<long?> DeleteZValuesAync(ObjectId mapSectionId)
		{
			var result = await _mapSectionZValuesReaderWriter.DeleteAsync(mapSectionId);

			return result;
		}

		#endregion

		#region JobMapSection

		public ObjectId? SaveJobMapSection(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType)
		{
			var existingRecord = _jobMapSectionReaderWriter.GetByMapSectionIdJobIdAndJobType(mapSectionId, jobId, jobType);
			if (existingRecord == null)
			{
				var blockIndexRec = _mSetRecordMapper.MapTo(blockIndex);

				var jobMapSectionRecord = new JobMapSectionRecord(jobType, jobId, mapSectionId, blockIndexRec, isInverted, DateCreatedUtc: DateTime.UtcNow, LastSavedUtc: DateTime.UtcNow,
					mapSectionSubdivisionId, jobSubdivisionId, ownerType);

				try
				{
					var jobMapSectionId = _jobMapSectionReaderWriter.Insert(jobMapSectionRecord);
					return jobMapSectionId;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Got exception: {e}.");
					throw;
				}
			}
			else
			{
				var jobMapSectionId = existingRecord.Id;
				if (existingRecord.JobSubdivisionId != jobSubdivisionId)
				{
					Debug.WriteLine($"The JobSubdivisionId on the existing JobMapSectionRecord: {existingRecord.JobSubdivisionId} does not match the Job's SubdivisionId: {jobSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				if (existingRecord.MapSectionSubdivisionId != mapSectionSubdivisionId)
				{
					Debug.WriteLine($"The MapSubdivisionId on the existing JobMapSectionRecord: {existingRecord.MapSectionSubdivisionId} does not match the MapSection's SubdivisionId: {mapSectionSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				return jobMapSectionId;
			}
		}

		public async Task<ObjectId?> SaveJobMapSectionAsync(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType)
		{
			var existingRecord = await _jobMapSectionReaderWriter.GetByMapSectionIdJobIdAndJobTypeAsync(mapSectionId, jobId, jobType);
			if (existingRecord == null)
			{
				var blockIndexRec = _mSetRecordMapper.MapTo(blockIndex);

				var jobMapSectionRecord = new JobMapSectionRecord(jobType, jobId, mapSectionId, blockIndexRec, isInverted, DateCreatedUtc: DateTime.UtcNow, LastSavedUtc: DateTime.UtcNow,
					mapSectionSubdivisionId, jobSubdivisionId, ownerType);

				try
				{
					var jobMapSectionId = await _jobMapSectionReaderWriter.InsertAsync(jobMapSectionRecord);
					return jobMapSectionId;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Got exception: {e}.");
					throw;
				}
			}
			else
			{
				var jobMapSectionId = existingRecord.Id;
				if (existingRecord.JobSubdivisionId != jobSubdivisionId)
				{
					Debug.WriteLine($"The JobSubdivisionId on the existing JobMapSectionRecord: {existingRecord.JobSubdivisionId} does not match the Job's SubdivisionId: {jobSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					await _jobMapSectionReaderWriter.SetSubdivisionIdAsync(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				if (existingRecord.MapSectionSubdivisionId != mapSectionSubdivisionId)
				{
					Debug.WriteLine($"The MapSubdivisionId on the existing JobMapSectionRecord: {existingRecord.MapSectionSubdivisionId} does not match the MapSection's SubdivisionId: {mapSectionSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					await _jobMapSectionReaderWriter.SetSubdivisionIdAsync(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				return jobMapSectionId;
			}
		}

		public bool InsertIfNotFoundJobMapSection(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType, out ObjectId jobMapSectionId)
		{
			var existingRecord = _jobMapSectionReaderWriter.GetByMapSectionIdJobIdAndJobType(mapSectionId, jobId, jobType);
			if (existingRecord == null)
			{
				var blockIndexRecord = _mSetRecordMapper.MapTo(blockIndex);

				var jobMapSectionRecord = new JobMapSectionRecord(jobType, jobId, mapSectionId, blockIndexRecord, isInverted, DateCreatedUtc: DateTime.UtcNow, LastSavedUtc: DateTime.UtcNow,
					mapSectionSubdivisionId, jobSubdivisionId, ownerType);

				try
				{
					jobMapSectionId = _jobMapSectionReaderWriter.Insert(jobMapSectionRecord);
					return true;
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Got exception: {e}.");
					throw;
				}
			}
			else
			{
				jobMapSectionId = existingRecord.Id;

				if (existingRecord.JobSubdivisionId != jobSubdivisionId)
				{
					Debug.WriteLine($"The JobSubdivisionId on the existing JobMapSectionRecord: {existingRecord.JobSubdivisionId} does not match the Job's SubdivisionId: {jobSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				if (existingRecord.MapSectionSubdivisionId != mapSectionSubdivisionId)
				{
					Debug.WriteLine($"The MapSubdivisionId on the existing JobMapSectionRecord: {existingRecord.MapSectionSubdivisionId} does not match the MapSection's SubdivisionId: {mapSectionSubdivisionId}. JobMapSectionId: {jobMapSectionId}, JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				return false;
			}
		}

		public List<JobMapSectionRecord> GetByJobId(ObjectId jobId)
		{
			var jobMapSectionRecords = _jobMapSectionReaderWriter.GetByJobId(jobId);
			return jobMapSectionRecords;
		}

		public IList<ObjectId> GetMapSectionIds(ObjectId jobId)
		{
			var mapSectionIds = _jobMapSectionReaderWriter.GetMapSectionIdsByJobId(jobId);

			return mapSectionIds;
		}

		public long DeleteJobMapSectionsInList(IEnumerable<ObjectId> jobMapSectionIds)
		{
			var result = _jobMapSectionReaderWriter.DeleteJobMapSectionsInList(jobMapSectionIds);

			return result ?? 0;
		}

		public IEnumerable<ObjectId> GetAllMapSectionIds()
		{
			var result = _mapSectionReaderWriter.GetAllMapSectionIds();
			return result;
		}

		public long? DeleteMapSectionsForManyJobs(IEnumerable<ObjectId> jobIds)
		{
			var result = 0L;

			foreach (var jobId in jobIds)
			{
				Debug.WriteLine($"Removing MapSections and JobMapSections for job: {jobId}.");
				var singleResult = DeleteMapSectionsForJobInternal(jobId, out var numberJobMapSectionsDeleted, out var numberOfZValueRecordsDeleted);
				Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords, {singleResult} MapSections and {numberOfZValueRecordsDeleted} MapSectionZValueRecords.");

				if (singleResult.HasValue)
				{
					result += singleResult.Value;
				}
			}

			return result;
		}

		public long? DeleteMapSectionsForJob(ObjectId jobId)
		{
			Debug.WriteLine($"Removing MapSections and JobMapSections for Job: {jobId}.");

			var result = DeleteMapSectionsForJobInternal(jobId, out var numberJobMapSectionsDeleted, out var numberOfZValueRecordsDeleted);
			Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords, {result} MapSections and {numberOfZValueRecordsDeleted} MapSectionZValueRecords.");
			return result;
		}

		public long? DeleteMapSectionsForJobHavingJobTypes(ObjectId jobId, JobType[] jobTypes)
		{
			Debug.WriteLine($"Removing MapSections and JobMapSections for Job: {jobId} Having JobTypes: {string.Join(";", jobTypes)}.");

			var result = DeleteMapSectionsForJobHavingJobTypesInternal(jobId, jobTypes, out var numberJobMapSectionsDeleted);

			Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {result} MapSections.");

			return result;
		}

		private long? DeleteMapSectionsForJobInternal(ObjectId jobId, out long? numberJobMapSectionsDeleted, out long? numberOfZValueRecordsDeleted)
		{
			var mapSectionIds = _jobMapSectionReaderWriter.GetMapSectionIdsByJobId(jobId);

			numberJobMapSectionsDeleted = _jobMapSectionReaderWriter.DeleteJobMapSections(jobId);

			var foundMapSectionRefs = _jobMapSectionReaderWriter.GetJobMapSectionIds(mapSectionIds).ToArray();
			var mapSectionsNotReferenced = mapSectionIds.Where(x => !foundMapSectionRefs.Contains(x)).ToList();

			var numberDeleted = _mapSectionReaderWriter.Delete(mapSectionsNotReferenced);

			numberOfZValueRecordsDeleted = _mapSectionZValuesReaderWriter.Delete(mapSectionsNotReferenced);

			return numberDeleted;
		}

		private long? DeleteMapSectionsForJobHavingJobTypesInternal(ObjectId jobId, JobType[] jobTypes, out long? numberJobMapSectionsDeleted)
		{
			var mapSectionIds = _jobMapSectionReaderWriter.GetMapSectionIdsByJobIdAndJobTypes(jobId, jobTypes);

			numberJobMapSectionsDeleted = _jobMapSectionReaderWriter.DeleteJobMapSectionsByJobIdAndJobTypes(jobId, jobTypes);

			var foundMapSectionRefs = _jobMapSectionReaderWriter.GetJobMapSectionIds(mapSectionIds).ToArray();
			var mapSectionsNotReferenced = mapSectionIds.Where(x => !foundMapSectionRefs.Contains(x)).ToList();

			var numberDeleted = _mapSectionReaderWriter.Delete(mapSectionsNotReferenced);

			return numberDeleted;
		}

		public long? DeleteMapSectionsWithJobType(IList<ObjectId> mapSectionIds, OwnerType jobOwnerType)
		{
			var numberOfMapSectionRefsDeleted = _jobMapSectionReaderWriter.DeleteJobMapSectionsByMapSectionId(mapSectionIds, jobOwnerType);

			var foundMapSectionRefs = _jobMapSectionReaderWriter.GetJobMapSectionIds(mapSectionIds).ToArray();

			var mapSectionsNotReferenced = mapSectionIds.Where(x => !foundMapSectionRefs.Contains(x)).ToList();

			var numberDeleted = _mapSectionReaderWriter.Delete(mapSectionsNotReferenced);

			Debug.WriteLine($"DeleteMapSectionsWithJobType removed {numberOfMapSectionRefsDeleted} JobMapSection records and deleted {numberDeleted} MapSectionRecoreds.");

			return numberDeleted;
		}
		
		public long? DeleteJobMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			var result = _jobMapSectionReaderWriter.DeleteJobMapSectionsSince(dateCreatedUtc, overrideRecentGuard) ?? 0;

			return result;
		}
		
		public long? DuplicateJobMapSections(ObjectId jobId, OwnerType jobOwnerType, ObjectId newJobId)
		{
			var jobMapSectionRecords = _jobMapSectionReaderWriter.GetByJobId(jobId);

			foreach (var jmsr in jobMapSectionRecords)
			{
				var newJmsr = new JobMapSectionRecord(jmsr.JobType, newJobId, jmsr.MapSectionId, jmsr.BlockIndex, jmsr.IsInverted, DateCreatedUtc: DateTime.UtcNow, LastSavedUtc: DateTime.UtcNow,
					jmsr.MapSectionSubdivisionId, jmsr.JobSubdivisionId, jmsr.OwnerType);

				_ = _jobMapSectionReaderWriter.Insert(newJmsr);
			}

			var result = jobMapSectionRecords.Count;

			return result;
		}

		public string GetJobMapSectionsToMapSectionReferenceReport()
		{
			var mapSectionIds = _mapSectionReaderWriter.GetAllMapSectionIds();
			var dict = new SortedDictionary<ObjectId, int>();
			foreach (var msIdRef in mapSectionIds)
			{
				dict.Add(msIdRef, 0);
			}

			var mapSectionIdReferences = _jobMapSectionReaderWriter.GetMapSectionIdsFromAllJobMapSections();

			foreach(var msIdRef in mapSectionIdReferences)
			{
				if (dict.TryGetValue(msIdRef.Item2, out var cnt))
				{
					dict[msIdRef.Item2] = cnt + 1;
				}
			}

			var sb = new StringBuilder();

			var runningCnt = 0;

			foreach(var kvp in dict)
			{
				sb.AppendLine($"{kvp.Key}\t{kvp.Value}");
				runningCnt += kvp.Value;
			}

			var headLine = $"\nThere are {dict.Count} MapSections and {runningCnt} references.";
			
			headLine += $"\nMapSectionId\tCount Refs\n";

			return headLine + sb.ToString();
		}

		public IEnumerable<JobMapSectionRecord> GetAllJobMapSections()
		{
			var result = _jobMapSectionReaderWriter.GetAll();
			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetMapSectionAndSubdivisionIdsForAllJobMapSections()
		{
			var result = _jobMapSectionReaderWriter.GetMapSectionAndSubdivisionIdsForAllJobMapSections();	

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId, ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobMapSections()
		{
			var result = _jobMapSectionReaderWriter.GetJobAndSubdivisionIdsForAllJobMapSections();

			return result;
		}

		public ObjectId? GetSubdivisionId(ObjectId mapSectionId)
		{
			try
			{
				var mapSectionRecord = _mapSectionReaderWriter.Get(mapSectionId);
				if (mapSectionRecord != null)
				{
					return mapSectionRecord.SubdivisionId;
				}
				else
				{
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While GetSubdivisionId from a MapSectionId, got exception: {e}.");
				return null;
			}
		}


		public IEnumerable<ObjectId> GetJobMapSectionIds(IEnumerable<ObjectId> mapSectionIds)
		{
			var result = _jobMapSectionReaderWriter.GetJobMapSectionIds(mapSectionIds);

			return result;
		}

		public long DeleteMapSectionsInList(IList<ObjectId> mapSectionIds)
		{
			var numberDeleted = _mapSectionReaderWriter.Delete(mapSectionIds);

			return numberDeleted;
		}

		#endregion

		#region Subdivision

		public bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [NotNullWhen(true)] out Subdivision? subdivision)
		{
			//var samplePointDeltaReduced = Reducer.Reduce(samplePointDelta); THIS CANNOT BE CHANGED

			var samplePointDeltaDto = _dtoMapper.MapTo(samplePointDelta);

			var baseMapPositionDto = _dtoMapper.MapTo(baseMapPosition);

			var matches = _subdivisionReaderWriter.Get(samplePointDeltaDto, baseMapPositionDto);

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

		public Subdivision InsertSubdivision(Subdivision subdivision)
		{
			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var id = _subdivisionReaderWriter.Insert(subdivisionRecord);

			var result = new Subdivision(id, subdivision.SamplePointDelta, subdivision.BaseMapPosition, subdivision.BlockSize, subdivisionRecord.DateCreatedUtc);

			return result;
		}

		//public bool DeleteSubdivision(Subdivision subdivision)
		//{
		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
		//	var subsDeleted = subdivisionReaderWriter.Delete(subdivision.Id);

		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	_ = mapSectionReaderWriter.DeleteAllWithSubId(subdivision.Id);

		//	return subsDeleted.HasValue && subsDeleted.Value > 0;
		//}

		public IEnumerable<Subdivision> GetAllSubdivisions()
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var allRecs = subdivisionReaderWriter.GetAll();

			var result = allRecs.Select(x => _mSetRecordMapper.MapFrom(x));

			return result;
		}

		public SubdivisionInfo[] GetAllSubdivisionInfos()
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var allRecs = subdivisionReaderWriter.GetAll();

			var result = allRecs
				.Select(x => _mSetRecordMapper.MapFrom(x))
				.Select(x => new SubdivisionInfo(x.Id, x.SamplePointDelta.Width, x.BaseMapPosition))
				.ToArray();

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForAllJobs()
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetJobAndSubdivisionIdsForAllJobs();
			return result;
		}

		public IEnumerable<ObjectId> GetSubdivisionIdsForAllJobs()
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetSubdivisionIdsForAllJobs();
			return result;
		}

		public long DeleteSubdivisionsInList(IList<ObjectId> subdivisionIds)
		{
			var numberDeleted = _subdivisionReaderWriter.Delete(subdivisionIds);
			return numberDeleted;
		}

		#endregion

		#region Active Map Section Schema Updates

		public void DoSchemaUpdates()
		{
			//RemoveEscapeVels();
			//ReplaceOwnerIdWithJobId();

			//AddJobTypeAndBlockIndex();
		}

		//// Remove FetchZValuesFromRepo property from MapSections
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = ((MapSectionAdapter)_mapSectionAdapter).RemoveFetchZValuesProp();
		//	return numUpdated;
		//}

		//public void AddJobTypeAndBlockIndex()
		//{
		//	_jobMapSectionReaderWriter.AddJobTypeAndBlockIndex();
		//}

		//public void ReplaceOwnerIdWithJobId()
		//{
		//	_jobMapSectionReaderWriter.ReplaceOwnerIdWithJobId();
		//}

		//public void RemoveEscapeVels()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.RemoveEscapeVelsFromMapSectionRecords();
		//}

		//public void AddSubdivisionId_ToAllJobMapSection_Records()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

		//	jobMapSectionReaderWriter.AddSubdivisionIdToAllRecords();

		//	var jobMapSections = jobMapSectionReaderWriter.GetAllJobMapSections();
		//	var mapSections = mapSectionReaderWriter.GetAllSubdivisionIds();

		//	var dict = new Dictionary<ObjectId, ObjectId>();

		//	foreach(var ms in mapSections)
		//	{
		//		dict.Add(ms.Item1, ms.Item2);
		//	}

		//	var notFound = 0;

		//	foreach (var jm in jobMapSections)
		//	{
		//		if (dict.TryGetValue(jm.MapSectionId, out var subId))
		//		{
		//			jobMapSectionReaderWriter.SetSubdivisionId(jm.MapSectionId, subId);
		//		}
		//		else
		//		{
		//			notFound++;
		//		}
		//	}

		//	Debug.WriteLine($"Could not find: {notFound} records.");

		//}


		#endregion
	}
}
