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

		public async Task<MapSectionResponse?> GetMapSectionAsync(ObjectId subdivisionId, BigVectorDto blockPosition, CancellationToken ct, Func<MapSectionValues> allocateMsvBuf)
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);

			try
			{
				//if (includeZValues)
				//{
				//	var mapSectionRecord = await mapSectionReaderWriter.GetAsync(subdivisionId, blockPosition, ct);
				//	if (mapSectionRecord != null)
				//	{
				//		var mapSectionValues = allocateMsvBuf();
				//		var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionValues, mapSectionRecord);
				//		return mapSectionResponse;
				//	}
				//	else
				//	{
				//		return null;
				//	}
				//}
				//else
				//{
				//	var mapSectionRecordCountsOnly = await mapSectionReaderWriter.GetJustCountsAsync(subdivisionId, blockPosition, ct);
				//	if (mapSectionRecordCountsOnly != null /*&& mapSectionRecordCountsOnly.Counts.Length == 32768*/)
				//	{
				//		var mapSectionValues = allocateMsvBuf();
				//		var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionValues, mapSectionRecordCountsOnly);
				//		return mapSectionResponse;
				//	}
				//	else
				//	{
				//		return null;
				//	}
				//}

				var mapSectionRecordCountsOnly = await mapSectionReaderWriter.GetJustCountsAsync(subdivisionId, blockPosition, ct);
				if (mapSectionRecordCountsOnly != null /*&& mapSectionRecordCountsOnly.Counts.Length == 32768*/)
				{
					var mapSectionValues = allocateMsvBuf();
					var mapSectionResponse = _mSetRecordMapper.MapFrom(mapSectionValues, mapSectionRecordCountsOnly);
					return mapSectionResponse;
				}
				else
				{
					return null;
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

			//var mapSectionRecord = new MapSectionRecord()

			var mapSectionId = await mapSectionReaderWriter.InsertAsync(mapSectionRecord);

			return mapSectionId;
		}

		//public async Task<long?> UpdateMapSectionZValuesAsync(MapSectionResponse mapSectionResponse)
		//{
		//	var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
		//	var mapSectionRecord = _mSetRecordMapper.MapTo(mapSectionResponse);

		//	var result = await mapSectionReaderWriter.UpdateZValuesAync(mapSectionRecord);

		//	return result;
		//}

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
				throw new ArgumentNullException(nameof(MapSectionServiceResponse.MapSectionId), "The MapSectionId cannot be null.");
			}

			var subdivisionIdStr = mapSectionResponse.SubdivisionId;
			if (string.IsNullOrEmpty(subdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionServiceResponse.SubdivisionId), "The SubdivisionId cannot be null.");
			}

			var ownerIdStr = mapSectionResponse.OwnerId;
			if (string.IsNullOrEmpty(ownerIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionIdStr), "The OwnerId cannot be null.");
			}

			var result = await SaveJobMapSectionAsync(new ObjectId(mapSectionIdStr), new ObjectId(subdivisionIdStr), new ObjectId(ownerIdStr), mapSectionResponse.JobOwnerType);
			return result;
		}

		private async Task<ObjectId?> SaveJobMapSectionAsync(ObjectId mapSectionId, ObjectId subdivisionId, ObjectId ownerId, JobOwnerType jobOwnerType)
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var existingRecord = await jobMapSectionReaderWriter.GetByMapAndOwnerIdAsync(mapSectionId, ownerId, jobOwnerType);
			if (existingRecord == null)
			{
				var jobMapSectionRecord = new JobMapSectionRecord(mapSectionId, subdivisionId, ownerId, jobOwnerType);
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
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var jobMapSectionRecords = jobMapSectionReaderWriter.GetByOwnerId(ownerId, jobOwnerType);

			foreach (var jmsr in jobMapSectionRecords)
			{
				var newJmsr = new JobMapSectionRecord(jmsr.MapSectionId, jmsr.SubdivisionId, newOwnerId, jmsr.OwnerType);
				_ = jobMapSectionReaderWriter.Insert(newJmsr);
			}

			var result = jobMapSectionRecords.Count;

			return result;
		}

		public string GetJobMapSectionsReferenceReport()
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var mapSectionIds = mapSectionReaderWriter.GetAllMapSectionIds();
			var dict = new SortedDictionary<ObjectId, int>();
			foreach (var msIdRef in mapSectionIds)
			{
				dict.Add(msIdRef, 0);
			}

			var mapSectionIdReferences = jobMapSectionReaderWriter.GetAllMapSectionIdsFromJobMapSections();

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
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetAllJobIds();

			var dict = new SortedDictionary<ObjectId, int>();

			foreach(var jobId in jobIds)
			{
				dict.Add(jobId, 0);
			}

			var jobIdsNotFound = new List<ObjectId>();

			var msJobIds = jobMapSectionReaderWriter.GetDistinctJobIdsFromJobMapSections(JobOwnerType.Project);

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
				var numberDeleted = jobMapSectionReaderWriter.DeleteJobMapSections(jobId, JobOwnerType.Project);
				result.Add(new Tuple<string, long?>(jobId.ToString(), numberDeleted));
			}

			return result;
		}

		//public void AddSubdivisionId()
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
