using MongoDB.Bson;
using MongoDB.Driver;
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

		public async Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVector blockPosition, MapSectionVectors mapSectionVectors, CancellationToken ct)
		{
			try
			{
				var mapSectionRecord = await _mapSectionReaderWriter.GetAsync(subdivisionId, blockPosition, ct);
				if (mapSectionRecord != null)
				{
					var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord, mapSectionVectors);

					return mapSectionResponse;
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
				Debug.WriteLine($"While fetching a MapSectionRecord from Subdivision and BlockPosition (Async), got exception: {e}.");

				var id = await _mapSectionReaderWriter.GetIdAsync(subdivisionId, blockPosition);
				if (id != null)
				{
					_mapSectionReaderWriter.Delete(id.Value);
				}
				else
				{
					throw new InvalidOperationException("Cannot delete the bad MapSectionRecord.");
				}

				return null;
			}
		}

		public MapSectionResponse? GetMapSection(ObjectId mapSectionId, MapSectionVectors mapSectionVectors)
		{
			try
			{
				var mapSectionRecord = _mapSectionReaderWriter.Get(mapSectionId);
				if (mapSectionRecord != null)
				{
					var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord, mapSectionVectors);

					return mapSectionResponse;
				}
				else
				{
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While fetching a MapSectionRecord from a MapSectionId, got exception: {e}.");
				return null;
			}
		}

		public MapSectionResponse? GetMapSection(ObjectId subdivisionId, BigVector blockPosition, MapSectionVectors mapSectionVectors)
		{
			try
			{
				var blockPositionRecord = _mSetRecordMapper.MapTo(blockPosition);

				var mapSectionRecord = _mapSectionReaderWriter.Get(subdivisionId, blockPosition);
				if (mapSectionRecord != null)
				{
					var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionRecord, mapSectionVectors);

					return mapSectionResponse;
				}
				else
				{
					//Debug.WriteLine($"MapSectionNotFound. SubdivisionId: {subdivisionId}, BlockPosition: {blockPosition}.");
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While fetching a MapSectionRecord from Subdivision and BlockPosition (synchronous), got exception: {e}.");

				return null;
			}
		}

		public ObjectId? GetMapSectionId(ObjectId subdivisionId, BigVector blockPosition)
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

		public async Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var mapSectionId = await _mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			return mapSectionId;
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

		public async Task<long?> DeleteZValuesAync(ObjectId mapSectionId)
		{
			var result = await _mapSectionReaderWriter.DeleteAsync(mapSectionId);

			return result;
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

		public IEnumerable<ValueTuple<ObjectId, DateTime, ObjectId>> GetMapSectionCreationDates(IEnumerable<ObjectId> mapSectionIds)
		{
			var result = _mapSectionReaderWriter.GetCreationDatesAndSubIds(mapSectionIds);

			return result;
		}
		#endregion

		#region MapSection ZValues

		public async Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			var result = await _mapSectionZValuesReaderWriter.GetBySectionIdAsync(mapSectionId, ct);

			return result?.ZValues;
		}

		public async Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var mapSectionZValuesId = await _mapSectionZValuesReaderWriter.InsertAsync(mapSectionZValuesRecord);

			return mapSectionZValuesId;
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
				throw new InvalidOperationException("The MapSectionResponse has a null MapSectionVectors.");
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


		#endregion

		#region JobMapSection

		public async Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse, string jobIdStr, JobType jobType, SizeInt blockIndex, bool isInverted, OwnerType ownerType, string jobSubdivisionIdStr)
		{
			var mapSectionIdStr = mapSectionResponse.MapSectionId;
			if (string.IsNullOrEmpty(mapSectionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.MapSectionId), "The MapSectionId cannot be null.");
			}

			var mapSubdivisionIdStr = mapSectionResponse.SubdivisionId;
			if (string.IsNullOrEmpty(mapSubdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.SubdivisionId), "The SubdivisionId cannot be null.");
			}

			//var jobSubdivisionIdStr = mapSectionResponse.OriginalSourceSubdivisionId;
			if (string.IsNullOrEmpty(jobSubdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(jobSubdivisionIdStr), "The OriginalSourceSubdivisionId cannot be null.");
			}

			//var jobIdStr = mapSectionResponse.JobId;
			if (string.IsNullOrEmpty(jobIdStr))
			{
				throw new ArgumentNullException(nameof(jobIdStr), "The OwnerId cannot be null.");
			}

			var result = await SaveJobMapSectionAsync(jobType, new ObjectId(jobIdStr), new ObjectId(mapSectionIdStr), blockIndex, isInverted, new ObjectId(mapSubdivisionIdStr), new ObjectId(jobSubdivisionIdStr), ownerType);
			return result;
		}

		private async Task<ObjectId?> SaveJobMapSectionAsync(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType)
		{
			var existingRecord = await _jobMapSectionReaderWriter.GetByMapAndJobIdAsync(mapSectionId, jobId, jobType);
			if (existingRecord == null)
			{
				//var blockPositionRec = _mSetRecordMapper.MapTo(blockPosition);

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

				if (existingRecord.MapSectionId != mapSectionSubdivisionId | existingRecord.JobSubdivisionId != jobSubdivisionId)
				{
					Debug.WriteLine($"The subdivisionId on the existing JobMapSectionRecord: {existingRecord.MapSectionId} does not match the MapSection's: {mapSectionSubdivisionId}. JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				//if (existingRecord.OriginalSourceSubdivisionId != originalSourceSubdivisionId)
				//{
				//	if (existingRecord.OriginalSourceSubdivisionId != ObjectId.Empty)
				//	{
				//		Debug.WriteLine($"The origSrcSubdivisionId on the existing JobMapSectionRecord: {existingRecord.OriginalSourceSubdivisionId} does not match the Job's origSrcSubdivisionId: {originalSourceSubdivisionId}. JobId: {jobId}, MapSectionId: {mapSectionId}.");
				//	}

				//	_jobMapSectionReaderWriter.SetOriginalSourceSubdivisionId(jobMapSectionId, originalSourceSubdivisionId);
				//}

				return existingRecord.Id;
			}
		}

		// TODO: Add JobType and BlockIndex arguments

		public bool InsertIfNotFoundJobMapSection(JobType jobType, ObjectId jobId, ObjectId mapSectionId, SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType, out ObjectId jobMapSectionId)
		{
			var existingRecord = _jobMapSectionReaderWriter.GetByMapAndJobId(mapSectionId, jobId, jobType);
			if (existingRecord == null)
			{
				//var jobMapSectionRecord = new JobMapSectionRecord(jobId, mapSectionId, subdivisionId, jobOwnerType, isInverted, DateTime.UtcNow, refIsHard)
				//{
				//	OriginalSourceSubdivisionId = jobSubdivisionId
				//};

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

				if (existingRecord.MapSectionSubdivisionId != mapSectionSubdivisionId | existingRecord.JobSubdivisionId != jobSubdivisionId)
				{
					Debug.WriteLine($"The subdivisionId on the existing JobMapSectionRecord: {existingRecord.MapSectionSubdivisionId} does not match the MapSection's SubdivisionId: {mapSectionSubdivisionId}. JobId: {jobId}, MapSectionId: {mapSectionId}.");
					_jobMapSectionReaderWriter.SetSubdivisionId(jobMapSectionId, mapSectionSubdivisionId, jobSubdivisionId);
				}

				//if (existingRecord.OriginalSourceSubdivisionId != originalSourceSubdivisionId)
				//{
				//	if (existingRecord.OriginalSourceSubdivisionId != ObjectId.Empty)
				//	{
				//		Debug.WriteLine($"The origSrcSubdivisionId on the existing JobMapSectionRecord: {existingRecord.OriginalSourceSubdivisionId} does not match the Job's origSrcSubdivisionId: {originalSourceSubdivisionId}. JobId: {jobId}, MapSectionId: {mapSectionId}.");
				//	}

				//	_jobMapSectionReaderWriter.SetOriginalSourceSubdivisionId(jobMapSectionId, originalSourceSubdivisionId);
				//}

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
				var singleResult = DeleteMapSectionsForJobInternalNew(jobId, out var numberJobMapSectionsDeleted);
				Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {singleResult} MapSections.");

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

			var result = DeleteMapSectionsForJobInternalNew(jobId, out var numberJobMapSectionsDeleted);

			Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {result} MapSections.");

			return result;
		}

		// TODO: Estimate how many MapSections will actually be removed.

		private long? DeleteMapSectionsForJobInternalNew(ObjectId jobId, out long? numberJobMapSectionsDeleted)
		{
			var mapSectionIds = _jobMapSectionReaderWriter.GetMapSectionIdsByJobId(jobId);

			numberJobMapSectionsDeleted = _jobMapSectionReaderWriter.DeleteJobMapSections(jobId);

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

		//private long? DeleteMapSectionsForJobInternal(ObjectId jobId, JobOwnerType jobOwnerType, out long? numberJobMapSectionsDeleted)
		//{
		//	var mapSectionIds = _jobMapSectionReaderWriter.GetMapSectionIdsByOwnerId(jobId, jobOwnerType);
		//	numberJobMapSectionsDeleted = _jobMapSectionReaderWriter.DeleteJobMapSections(jobId, jobOwnerType);

		//	var foundMapSectionRefs = _jobMapSectionReaderWriter.GetJobMapSectionIds(mapSectionIds);

		//	var result = 0L;
		//	foreach (var mapSectionId in mapSectionIds)
		//	{

		//		if (!_jobMapSectionReaderWriter.DoesJobMapSectionRecordExist(mapSectionId))
		//		{
		//			var numberDeleted = _mapSectionReaderWriter.Delete(mapSectionId);
		//			result += numberDeleted ?? 0;
		//		}
		//	}

		//	var numberOfNotFoundRefs = mapSectionIds.Count(x => !foundMapSectionRefs.Contains(x));

		//	if (numberOfNotFoundRefs != result)
		//	{
		//		Debug.WriteLine("The new DeleteMapSectionsForJob method is not finding the same number of JobMapSection Records.");
		//	}

		//	return result;
		//}
		
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

		//public List<Tuple<string, long?>> DeleteJobMapSectionsWithMissingJob(JobOwnerType jobOwnerType)
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	var jobIds = jobReaderWriter.GetAllJobIds();

		//	var dict = new SortedDictionary<ObjectId, int>();

		//	foreach(var jobId in jobIds)
		//	{
		//		dict.Add(jobId, 0);
		//	}

		//	var jobIdsNotFound = new List<ObjectId>();

		//	var msJobIds = _jobMapSectionReaderWriter.GetDistinctJobIdsFromJobMapSections(jobOwnerType);

		//	foreach (var jobId in msJobIds)
		//	{
		//		if (!dict.TryGetValue(jobId, out _))
		//		{
		//			jobIdsNotFound.Add(jobId);
		//		}
		//	}

		//	var result = new List<Tuple<string, long?>>();

		//	foreach(var jobId in jobIdsNotFound)
		//	{
		//		var numberDeleted = _jobMapSectionReaderWriter.DeleteJobMapSections(jobId, jobOwnerType);
		//		result.Add(new Tuple<string, long?>(jobId.ToString(), numberDeleted));
		//	}

		//	return result;
		//}

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
