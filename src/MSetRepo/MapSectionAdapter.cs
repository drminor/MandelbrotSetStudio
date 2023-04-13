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
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			if (_jobMapSectionReaderWriter.CreateCollection())
			{
				_jobMapSectionReaderWriter.CreateOwnerAndTypeIndex();
				_jobMapSectionReaderWriter.CreateMapSectionIdIndex();
			}

			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			if (_mapSectionReaderWriter.CreateCollection())
			{
				_mapSectionReaderWriter.CreateSubAndPosIndex();
			}

			//var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			_ = _subdivisionReaderWriter.CreateCollection();

			//var mapSectionZValuesReaderWriter  = new MapSectionZValuesReaderWriter(_dbProvider);
			if (_mapSectionZValuesReaderWriter.CreateCollection())
			{
				_mapSectionZValuesReaderWriter.CreateSectionIdIndex();
			}
		}

		public void DropMapSections()
		{
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			_jobMapSectionReaderWriter.DropCollection();

			//var mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			_mapSectionZValuesReaderWriter.DropCollection();

			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_mapSectionReaderWriter.DropCollection();
		}

		public void DropMapSectionsAndSubdivisions()
		{
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			_jobMapSectionReaderWriter.DropCollection();

			//var mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			_mapSectionZValuesReaderWriter.DropCollection();

			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_mapSectionReaderWriter.DropCollection();

			//var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			_subdivisionReaderWriter.DropCollection();
		}

		#endregion

		#region MapSection

		public async Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors, CancellationToken ct)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

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
			catch (Exception e)
			{
				Debug.WriteLine($"While fetching a MapSectionRecord from Subdivision and BlockPosition (Async), got exception: {e}.");

				var id = await _mapSectionReaderWriter.GetId(subdivisionId, blockPosition);
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
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

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

		public MapSectionResponse? GetMapSection(ObjectId subdivisionId, BigVectorDto blockPosition, MapSectionVectors mapSectionVectors)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

			try
			{
				var mapSectionRecord = _mapSectionReaderWriter.Get(subdivisionId, blockPosition);
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
				Debug.WriteLine($"While fetching a MapSectionRecord from Subdivision and BlockPosition (synchronous), got exception: {e}.");

				return null;
			}
		}

		public async Task<ObjectId?> SaveMapSectionAsync(MapSectionResponse mapSectionResponse)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var mapSectionId = await _mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			return mapSectionId;
		}

		public async Task<long?> UpdateCountValuesAync(MapSectionResponse mapSectionResponse)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

			var result = await _mapSectionReaderWriter.UpdateCountValuesAync(mapSectionRecord, mapSectionResponse.RequestCompleted);

			return result;
		}

		public async Task<long?> DeleteZValuesAync(ObjectId mapSectionId)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = await _mapSectionReaderWriter.DeleteAsync(mapSectionId);

			return result;
		}

		public long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false)
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var result = _mapSectionReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard) ?? 0;
			var result2 = _mapSectionZValuesReaderWriter.DeleteMapSectionsSince(dateCreatedUtc, overrideRecentGuard) ?? 0;
			result += result2;

			return result;
		}

		#endregion

		#region MapSection ZValues

		public async Task<ZValues?> GetMapSectionZValuesAsync(ObjectId mapSectionId, CancellationToken ct)
		{
			//var mapSectionZValsReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			var result = await _mapSectionZValuesReaderWriter.GetBySectionIdAsync(mapSectionId, ct);

			return result?.ZValues;
		}

		public async Task<ObjectId?> SaveMapSectionZValuesAsync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			//var mapSectionZValsReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
			var mapSectionZValuesRecord = GetZValues(mapSectionResponse, mapSectionId);

			var mapSectionZValuesId = await _mapSectionZValuesReaderWriter.InsertAsync(mapSectionZValuesRecord);

			return mapSectionZValuesId;
		}

		public async Task<long?> UpdateZValuesAync(MapSectionResponse mapSectionResponse, ObjectId mapSectionId)
		{
			//var mapSectionZValuesReaderWriter = new MapSectionZValuesReaderWriter(_dbProvider);
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
				DateCreatedUtc: DateTime.UtcNow,
				MapSectionId: mapSectionId,
				ZValues: zValues
				)
			{
				Id = ObjectId.GenerateNewId(),
				LastAccessed = DateTime.UtcNow
			};

			return result;
		}


		#endregion

		#region JobMapSection

		public async Task<ObjectId?> SaveJobMapSectionAsync(MapSectionResponse mapSectionResponse, BigVector blockPosition, bool isInverted)
		{
			var mapSectionIdStr = mapSectionResponse.MapSectionId;
			if (string.IsNullOrEmpty(mapSectionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.MapSectionId), "The MapSectionId cannot be null.");
			}

			var subdivisionIdStr = mapSectionResponse.SubdivisionId;
			if (string.IsNullOrEmpty(subdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.SubdivisionId), "The SubdivisionId cannot be null.");
			}

			var ownerIdStr = mapSectionResponse.OwnerId;
			if (string.IsNullOrEmpty(ownerIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.OwnerId), "The OwnerId cannot be null.");
			}

			var result = await SaveJobMapSectionAsync(new ObjectId(mapSectionIdStr), new ObjectId(subdivisionIdStr), new ObjectId(ownerIdStr), mapSectionResponse.JobOwnerType, blockPosition, isInverted);
			return result;
		}

		private async Task<ObjectId?> SaveJobMapSectionAsync(ObjectId mapSectionId, ObjectId subdivisionId, ObjectId ownerId, JobOwnerType jobOwnerType, BigVector blockPosition, bool isInverted)
		{
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var existingRecord = await _jobMapSectionReaderWriter.GetByMapAndOwnerIdAsync(mapSectionId, ownerId, jobOwnerType);
			if (existingRecord == null)
			{
				var blockPositionRec = _mSetRecordMapper.MapTo(blockPosition);
				var jobMapSectionRecord = new JobMapSectionRecord(mapSectionId, subdivisionId, ownerId, jobOwnerType, isInverted, blockPositionRec);

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
				return existingRecord.Id;
			}
		}

		public IList<ObjectId> GetMapSectionIds(ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var mapSectionIds = jobMapSectionReaderWriter.GetMapSectionIdsByOwnerId(ownerId, jobOwnerType);

			return mapSectionIds;
		}

		public long? DeleteMapSectionsForMany(IEnumerable<ObjectId> ownerIds, JobOwnerType jobOwnerType)
		{
			var result = 0L;

			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			foreach (var ownerId in ownerIds)
			{
				Debug.WriteLine($"Removing MapSections and JobMapSections for {jobOwnerType}: {ownerId}.");
				var singleResult = DeleteMapSectionsForJobInternal(ownerId, jobOwnerType, _mapSectionReaderWriter, jobMapSectionReaderWriter, out var numberJobMapSectionsDeleted);
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
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var result = DeleteMapSectionsForJobInternal(ownerId, jobOwnerType, mapSectionReaderWriter, _jobMapSectionReaderWriter, out var numberJobMapSectionsDeleted);

			Debug.WriteLine($"Removed {numberJobMapSectionsDeleted} JobMapSectionRecords and {result} MapSections.");

			return result;
		}

		private long? DeleteMapSectionsForJobInternalNew(ObjectId ownerId, JobOwnerType jobOwnerType, MapSectionReaderWriter mapSectionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter, out long? numberJobMapSectionsDeleted)
		{
			var mapSectionIds = jobMapSectionReaderWriter.GetMapSectionIdsByOwnerId(ownerId, jobOwnerType);

			numberJobMapSectionsDeleted = jobMapSectionReaderWriter.DeleteJobMapSections(ownerId, jobOwnerType);

			var foundMapSectionRefs = jobMapSectionReaderWriter.DoJobMapSectionRecordsExist(mapSectionIds).ToArray();
			var mapSectionsNotReferenced = mapSectionIds.Where(x => !foundMapSectionRefs.Contains(x)).ToList();

			var numberDeleted = mapSectionReaderWriter.Delete(mapSectionsNotReferenced);

			return numberDeleted;
		}

		private long? DeleteMapSectionsForJobInternal(ObjectId ownerId, JobOwnerType jobOwnerType, MapSectionReaderWriter mapSectionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter, out long? numberJobMapSectionsDeleted)
		{
			var mapSectionIds = jobMapSectionReaderWriter.GetMapSectionIdsByOwnerId(ownerId, jobOwnerType);
			numberJobMapSectionsDeleted = jobMapSectionReaderWriter.DeleteJobMapSections(ownerId, jobOwnerType);

			var foundMapSectionRefs = jobMapSectionReaderWriter.DoJobMapSectionRecordsExist(mapSectionIds);

			var result = 0L;
			foreach (var mapSectionId in mapSectionIds)
			{

				if (!jobMapSectionReaderWriter.DoesJobMapSectionRecordExist(mapSectionId))
				{
					var numberDeleted = mapSectionReaderWriter.Delete(mapSectionId);
					result += numberDeleted ?? 0;
				}
			}

			var numberOfNotFoundRefs = mapSectionIds.Count(x => !foundMapSectionRefs.Contains(x));

			if (numberOfNotFoundRefs != result)
			{
				Debug.WriteLine("The new DeleteMapSectionsForJob method is not finding the same number of JobMapSection Records.");
			}

			return result;
		}

		public long? DuplicateJobMapSections(ObjectId ownerId, JobOwnerType jobOwnerType, ObjectId newOwnerId)
		{
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var jobMapSectionRecords = _jobMapSectionReaderWriter.GetByOwnerId(ownerId, jobOwnerType);

			foreach (var jmsr in jobMapSectionRecords)
			{
				var newJmsr = new JobMapSectionRecord(jmsr.MapSectionId, jmsr.SubdivisionId, newOwnerId, jmsr.OwnerType, jmsr.IsInverted, jmsr.MapBlockOffset);
				_ = _jobMapSectionReaderWriter.Insert(newJmsr);
			}

			var result = jobMapSectionRecords.Count;

			return result;
		}

		public string GetJobMapSectionsReferenceReport()
		{
			//var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var mapSectionIds = _mapSectionReaderWriter.GetAllMapSectionIds();
			var dict = new SortedDictionary<ObjectId, int>();
			foreach (var msIdRef in mapSectionIds)
			{
				dict.Add(msIdRef, 0);
			}

			var mapSectionIdReferences = _jobMapSectionReaderWriter.GetAllMapSectionIdsFromJobMapSections();

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

			var headLine = $"\nThere are {dict.Count} MapSections and {runningCnt} references.\n";
			headLine += $"MapSectionId\tCount Refs";

			return headLine + sb.ToString();
		}

		public List<Tuple<string, long?>> DeleteNonExtantJobsReferenced()
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			//var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetAllJobIds();

			var dict = new SortedDictionary<ObjectId, int>();

			foreach(var jobId in jobIds)
			{
				dict.Add(jobId, 0);
			}

			var jobIdsNotFound = new List<ObjectId>();

			var msJobIds = _jobMapSectionReaderWriter.GetDistinctJobIdsFromJobMapSections(JobOwnerType.Project);

			foreach (var jobId in msJobIds)
			{
				if (!dict.TryGetValue(jobId, out _))
				{
					jobIdsNotFound.Add(jobId);
				}
			}

			var result = new List<Tuple<string, long?>>();

			foreach(var jobId in jobIdsNotFound)
			{
				var numberDeleted = _jobMapSectionReaderWriter.DeleteJobMapSections(jobId, JobOwnerType.Project);
				result.Add(new Tuple<string, long?>(jobId.ToString(), numberDeleted));
			}

			return result;
		}

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

		#region Subdivision

		public bool TryGetSubdivision(RSize samplePointDelta, BigVector baseMapPosition, [MaybeNullWhen(false)] out Subdivision subdivision)
		{
			//var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var samplePointDeltaReduced = Reducer.Reduce(samplePointDelta);
			var samplePointDeltaDto = _dtoMapper.MapTo(samplePointDeltaReduced);

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
			//var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var id = _subdivisionReaderWriter.Insert(subdivisionRecord);

			var result = new Subdivision(id, subdivision.SamplePointDelta, subdivision.BaseMapPosition, subdivision.BlockSize);

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

		#region Active Map Section Schema Updates

		public void DoSchemaUpdates()
		{
			//RemoveEscapeVels();
		}

		//public void RemoveEscapeVels()
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

		//	mapSectionReaderWriter.RemoveEscapeVelsFromMapSectionRecords();
		//}

		#endregion
	}
}
