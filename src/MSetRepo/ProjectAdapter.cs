using MongoDB.Bson;
using MongoDB.Driver;
//using MSetRepo.Storage;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSetRepo
{
	public class ProjectAdapter : IProjectAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		private readonly ProjectReaderWriter _projectReaderWriter;
		private readonly PosterReaderWriter _posterReaderWriter;
		private readonly JobReaderWriter _jobReaderWriter;
		private readonly ColorBandSetReaderWriter _colorBandSetReaderWriter;

		//private readonly bool _useDetailedDebug = false;

		#region Constructor

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;

			_projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			_posterReaderWriter = new PosterReaderWriter(_dbProvider);
			_jobReaderWriter = new JobReaderWriter(_dbProvider);
			_colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		}

		#endregion

		#region Collections

		public void CreateCollections()
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.CreateCollection();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.CreateCollection();

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.CreateCollection();

			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			posterReaderWriter.CreateCollection();
		}

		//public void DropCollections()
		//{
		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		//	colorBandSetReaderWriter.DropCollection();

		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	jobReaderWriter.DropCollection();

		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	projectReaderWriter.DropCollection();
		//}


		public bool ProjectCollectionIsEmpty()
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var x = projectReaderWriter.GetAllIds();
			var t = x.FirstOrDefault();

			return t == ObjectId.Empty;
			
			//var x = projectReaderWriter.Collection.Indexes;
			//_ = x.List();
		}

		public void CreateIndexes()
		{
			_colorBandSetReaderWriter.CreateOwnerNameIterationsAndVersionIndex();
		}

		#endregion

		#region Project

		public bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project)
		{
			//Debug.WriteLine($"Retrieving Project object for Project with name: {name}.");

			if (_projectReaderWriter.TryGet(name, out var projectRecord))
			{
				var colorBandSets = GetColorBandSetsForOwner(projectRecord.Id).ToList();

				if (colorBandSets.Count == 0)
				{
					var newCbs = new ColorBandSet(projectRecord.Name ?? projectRecord.ProjectNameTemporary, null, RMapConstants.DEFAULT_TARGET_ITERATIONS, Guid.NewGuid());
					newCbs.OwnerId = projectRecord.Id;
					colorBandSets.Add(newCbs);
				}
				else
				{
					foreach(var cbs in colorBandSets)
					{
						if (cbs.Name == null)
						{
							cbs.Name = projectRecord.Name ?? projectRecord.ProjectNameTemporary;
						}
					}
				}

				var jobs = GetAllJobsForOwner(projectRecord.Id);

				var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, projectRecord.TargetIterationColorMapRecords, "as the project is being retrieved", out var updateWasMade);
				project = AssembleProject(projectRecord, jobs, colorBandSets, lookupColorBandSetByTargetIteration, projectRecord.LastSavedUtc, projectRecord.LastAccessedUtc);

				if (project != null && updateWasMade)
				{
					project.MarkAsDirty();
				}

				return project != null;
			}
			else
			{
				project = null;
				return false;
			}
		}

		public Project? CreateProject(string name, string? description, List<Job> jobs, List<ColorBandSet> colorBandSets, IEnumerable<TargetIterationColorMapRecord>? targetIterationColorMapRecords)
		{
			if (!_projectReaderWriter.ProjectExists(name, out var projectId))
			{
				var projectRecord = new ProjectRecord(name, description, jobs.First().Id, DateTime.UtcNow);

				projectId = _projectReaderWriter.Insert(projectRecord);

				projectRecord = _projectReaderWriter.Get(projectId);

				foreach (var job in jobs)
				{
					job.OwnerId = projectId;
				}

				foreach (var cbs in colorBandSets)
				{
					cbs.OwnerId = projectId;
					cbs.AssignNewSerialNumber();
					if (cbs.Name == RMapConstants.NAME_FOR_NEW_PROJECTS) cbs.Name = name;
				}

				var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, targetIterationColorMapRecords, "as the project is being created", out _);
				var result = AssembleProject(projectRecord, jobs, colorBandSets, lookupColorBandSetByTargetIteration, DateTime.MinValue, DateTime.MinValue);

				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project (with Id:{projectId}) already exists with that name.");
			}
		}

		private Project? AssembleProject(ProjectRecord? projectRecord, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Project? result;
			if (projectRecord == null || jobs.Count == 0 || !colorBandSets.Any())
			{
				result = null;
			}
			else
			{
				result = new Project(projectRecord.Id, projectRecord.Name ?? projectRecord.ProjectNameTemporary, projectRecord.Description, jobs, 
					colorBandSets, lookupColorMapByTargetIteration, projectRecord.ColorBandSetResolutionStrategy, 
					projectRecord.CurrentJobId, projectRecord.DateCreatedUtc, lastSavedUtc, lastAccessedUtc);
			}

			return result;
		}

		public void UpdateProjectName(ObjectId projectId, string name)
		{
			_projectReaderWriter.UpdateName(projectId, name);
		}

		public void UpdateProjectDescription(ObjectId projectId, string? description)
		{
			_projectReaderWriter.UpdateDescription(projectId, description);
		}

		public void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId)
		{
			_projectReaderWriter.UpdateCurrentJobId(projectId, currentJobId);
		}

		public void UpdateProjectTargetIterationMap(Project project)
		{
			//ObjectId projectId, DateTime lastAccessedUtc, TargetIterationColorMapRecord[] targetIterationColorMapRecords

			_projectReaderWriter.UpdateTargetIterationMap(project.Id, project.LastAccessedUtc, project.GetTargetIterationColorMapRecords().ToArray(), project.ColorBandSetResolutionStrategy);
		}

		public bool DeleteProject(ObjectId projectId)
		{
			var jobIds = _jobReaderWriter.GetJobIdsByOwner(projectId);

			foreach (var jobId in jobIds)
			{
				_ = DeleteJob(jobId, _jobReaderWriter);
			}

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			_ = colorBandSetReaderWriter.DeleteColorBandSetsForOwner(projectId);
			var numberDeleted = _projectReaderWriter.Delete(projectId);

			return numberDeleted == 1;
		}

		public bool ProjectExists(string name, [MaybeNullWhen(false)] out ObjectId projectId)
		{
			var result = _projectReaderWriter.ProjectExists(name, out projectId);

			return result;
		}

		public bool ProjectExists(ObjectId projectId)
		{
			var result = _projectReaderWriter.Get(projectId);

			return result != null;
		}

		public IEnumerable<ObjectId> GetAllProjectIds()
		{
			var result = _projectReaderWriter.GetAllIds();
			return result;
		}

		#endregion

		#region ProjectInfo

		public bool TryGetProjectInfo(string name, [NotNullWhen(true)] out IProjectInfo? projectInfo)
		{
			if (_projectReaderWriter.TryGet(name, out var projectRec))
			{
				var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
				var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

				if (TryGetProjectInfoInternal(projectRec, subdivisionReaderWriter, jobMapSectionReaderWriter, out projectInfo))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				projectInfo = null;
				return false;
			}
		}

		public IEnumerable<IProjectInfo> GetAllProjectInfos()
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var allProjectRecords = _projectReaderWriter.GetAll();
			var result = allProjectRecords.Select(x => GetProjectInfoInternal(x, subdivisionReaderWriter, jobMapSectionReaderWriter));

			return result;
		}

		public IEnumerable<IProjectInfo> GetAllProjectInfos(DateTime lastAccessedAfterDate)
		{
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var allProjectRecords = _projectReaderWriter.GetAll();

			//var filteredRecs = allProjectRecords.Where(x => x.LastAccessedUtc > lastAccessedAfterDate);

			//if (filteredRecs.Count() == 0)
			//{
			//	filteredRecs = allProjectRecords.Where(x => x.LastSavedUtc > lastAccessedAfterDate);
			//}

			IEnumerable<ProjectRecord> filteredRecs;

			if (lastAccessedAfterDate != DateTime.MinValue)
			{
				// TODO: Make sure that all Project Records have a valid value for the LastAccessedUtc property.
				// Currently we are using LastSavedUtc property instead.
				filteredRecs = allProjectRecords.Where(x => x.LastSavedUtc > lastAccessedAfterDate);
			}
			else
			{
				filteredRecs = allProjectRecords;
			}

			var result = filteredRecs.Select(x => GetProjectInfoInternal(x, subdivisionReaderWriter, jobMapSectionReaderWriter));

			return result;
		}

		private  IProjectInfo GetProjectInfoInternal(ProjectRecord projectRec, SubdivisonReaderWriter subdivisionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter)
		{
			if (TryGetProjectInfoInternal(projectRec, subdivisionReaderWriter, jobMapSectionReaderWriter, out var projectInfo))
			{
				return projectInfo;
			}
			else
			{
				var result = new ProjectInfo(projectRec.Id, projectRec.Name ?? projectRec.ProjectNameTemporary, projectRec.Description, projectRec.CurrentJobId, 0, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, 0, 0, 0);
				return result;
			}
		}

		private bool TryGetProjectInfoInternal(ProjectRecord projectRec, SubdivisonReaderWriter subdivisionReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter, [NotNullWhen(true)] out IProjectInfo? projectInfo)
		{
			var dateCreated = projectRec.DateCreated.ToLocalTime();
			var lastAccessed = projectRec.LastAccessedUtc;
			var currentJobId = projectRec.CurrentJobId;

			var jobInfos = _jobReaderWriter.GetJobSubdivisionInfosForOwner(projectRec.Id);

			if (jobInfos.Any())
			{
				//var subdivisionIds = jobInfos.Select(j => j.SubdivisionId).Distinct();
				//var minMapCoordsExponent = jobInfos.Min(x => x.MapCoordExponent);
				//var minSamplePointDeltaExponent = subdivisionReaderWriter.GetMinExponent(subdivisionIds);

				//var subdivisionIds = jobInfos.Select(j => j.SubdivisionId).Distinct();
				var minMapCoordsExponent = jobInfos.Min(x => x.MapCoordExponent);
				var minSamplePointDeltaExponent = 0;


				// Greater of the date of the last updated job and the date when the project was last updated.
				var lastSavedUtc = jobInfos.Max(x => x.DateCreatedUtc);
				var lastUpdatedUtc = projectRec.LastSavedUtc;

				if (lastSavedUtc > lastUpdatedUtc)
				{
					lastUpdatedUtc = lastSavedUtc;
				}

				//var numberOfFirstLevelChildJobs = jobInfos.Count(x => !x.ParentJobId.HasValue);
				//var jobCount = numberOfFirstLevelChildJobs > 1 ? jobInfos.Count() : -1 * jobInfos.Count();
				var jobCount = jobInfos.Count();

				//var jobIds = jobInfos.Select(x => x.Id).ToList();
				//var bytes = GetBytes(jobIds, jobMapSectionReaderWriter);
				var bytes = 0;

				projectInfo = new ProjectInfo(projectRec.Id, projectRec.Name ?? projectRec.ProjectNameTemporary, projectRec.Description, currentJobId, bytes, dateCreated, lastUpdatedUtc, lastSavedUtc, jobCount, minMapCoordsExponent, minSamplePointDeltaExponent);
				return true;
			}
			else
			{
				projectInfo = null;
				return false;
			}
		}

		#endregion

		#region ColorBandSet 

		//public ColorBandSet? GetColorBandSet(string id)
		//{
		//	var result = GetColorBandSet(new ObjectId(id), new ColorBandSetReaderWriter(_dbProvider));
		//	return result;
		//}

		private ColorBandSet? GetColorBandSet(ObjectId id)
		{
			var colorBandSetRecord = _colorBandSetReaderWriter.Get(id);

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);
			return result;
		}

		//public bool TryGetColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		//{
		//	Debug.WriteLine($"ProjectAdapter. Retrieving ColorBandSet with Id: {colorBandSetId}.");

		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

		//	if (colorBandSetReaderWriter.TryGet(colorBandSetId, out var colorBandSetRecord))
		//	{
		//		colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);
		//		return true;
		//	}
		//	else
		//	{
		//		colorBandSet = null;
		//		return false;
		//	}
		//}

		public void InsertColorBandSet(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			InsertCbs(colorBandSet, colorBandSetReaderWriter);
		}

		private ObjectId InsertCbs(ColorBandSet colorBandSet, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);

			var newRecordId = colorBandSetReaderWriter.Insert(colorBandSetRecord);
			colorBandSet.LastSavedUtc = DateTime.UtcNow;

			return newRecordId;
		}

		public void UpdateColorBandSetName(ObjectId colorBandSetId, string? name)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.UpdateName(colorBandSetId, name);
		}

		public void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.UpdateDescription(colorBandSetId, description);
		}

		public void UpdateColorBandSetDetails(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.UpdateDetails(colorBandSet);
		}

		public void UpdateColorBandSetBands(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.UpdateBands(colorBandSet);
		}

		public IEnumerable<ColorBandSet> GetColorBandSetsForOwner(ObjectId ownerId)
		{
			var result = new List<ColorBandSet>();

			var colorBandSetRecords = _colorBandSetReaderWriter.GetColorBandSetsForOwner(ownerId);

			foreach (var colorBandSetRecord in colorBandSetRecords)
			{
				var colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);
				result.Add(colorBandSet);
			}

			return result;
		}

		public bool DeleteColorBandSet(ObjectId colorBandSetId)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = colorBandSetReaderWriter.Delete(colorBandSetId);

			return result > 0;
		}

		public long DeletePriorColorBandSetsByVersion(ObjectId ownerId, string name, int targetIterations, int numberToKeep)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = colorBandSetReaderWriter.DeleteColorBandSetsByVersion(ownerId, name, targetIterations, numberToKeep);

			return result;
		}

		public bool ColorBandSetExists(string name)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = colorBandSetReaderWriter.Exists(name);

			return result;
		}

		#endregion

		#region ColorBandSetInfo

		public IEnumerable<ColorBandSetInfo> GetAllColorBandSetInfosForProject(ObjectId projectId)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var colorBandSetRecords = colorBandSetReaderWriter.GetColorBandSetsForOwner(projectId).ToList();

			//var result = colorBandSetRecords.Select((x,i) => new ColorBandSetInfo(x.Id, GetColorBandSetName(x.Name, i), x.Version, x.TargetIterations == 0 ? x.ColorBandRecords.Max(y => y.CutOff) : x.TargetIterations, x.Description, x.DateLastUsedUtc, x.ColorBandsSerialNumber,
			//	x.ColorBandRecords.Length, numberOfJobs: 0));

			var result = colorBandSetRecords.Select((x, i) => new ColorBandSetInfo(x.Id, GetColorBandSetName(x.Name, i), x.Version, x.TargetIterations, x.Description, x.DateLastUsedUtc, x.ColorBandsSerialNumber,
				x.ColorBandRecords.Length, numberOfJobs: 0));

			return result;
		}

		private string GetColorBandSetName(string? name, int position)
		{
			var result = name ?? position.ToString();
			return result;
		}

		public ColorBandSetInfo? GetColorBandSetInfo(ObjectId id)
		{
			var colorsReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var cbsRecord = colorsReaderWriter.Get(id);

			if (cbsRecord != null)
			{
				//var targetIterations = cbsRecord.TargetIterations == 0 ? cbsRecord.ColorBandRecords.Max(y => y.CutOff) : cbsRecord.TargetIterations;
				var result = new ColorBandSetInfo(cbsRecord.Id, cbsRecord.Name ?? cbsRecord.ColorBandsSerialNumber.ToString(), cbsRecord.Version, cbsRecord.TargetIterations, cbsRecord.Description, cbsRecord.DateLastUsedUtc, cbsRecord.ColorBandsSerialNumber, 
					cbsRecord.ColorBandRecords.Length, numberOfJobs: 0);
				return result;
			}
			else
			{
				return null;
			}
		}

		#endregion

		#region Job

		public IEnumerable<ValueTuple<ObjectId, ObjectId, OwnerType>> GetJobAndOwnerIdsWithJobOwnerType()
		{
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = _jobReaderWriter.GetJobAndOwnerIdsWithJobOwnerType();

			return result;
		}

		public IEnumerable<ObjectId> GetAllJobIdsForPoster(ObjectId posterId)
		{
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = _jobReaderWriter.GetJobIdsByOwner(posterId);

			return result;
		}

		public List<Job> GetAllJobsForPoster(ObjectId posterId)
		{
			var result = GetAllJobsForOwner(posterId);

			return result;
		}

		public List<ObjectId> GetAllJobIdsForProject(ObjectId projectId)
		{
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = _jobReaderWriter.GetJobIdsByOwner(projectId).ToList();

			return result;
		}

		public IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsForOwner(ObjectId projectId)
		{
			//IEnumerable<ValueTuple<ObjectId, ObjectId>> GetJobAndSubdivisionIdsByOwner(ObjectId ownerId)
			//var result = 

			//var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = _jobReaderWriter.GetJobAndSubdivisionIdsForOwner(projectId);

			return result;
		}

		public List<Job> GetAllJobsForOwner(ObjectId ownerId)
		{
			var result = new List<Job>();

			var jobCache = new Dictionary<ObjectId, Job>();

			var ids = _jobReaderWriter.GetJobIdsByOwner(ownerId);
			foreach (var jobId in ids)
			{
				var job = GetJob(jobId, jobCache);
				result.Add(job);
			}

			return result;
		}

		public Job GetJob(ObjectId jobId)
		{
			var job = GetJob(jobId, jobCache: null);

			return job;
		}

		//private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter/*, ColorBandSetReaderWriter colorBandSetReaderWriter*/, IDictionary<ObjectId, Job>? jobCache/*, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache*/)
		private Job GetJob(ObjectId jobId, IDictionary<ObjectId, Job>? jobCache)
		{
			if (jobCache != null && jobCache.TryGetValue(jobId, out var qJob))
			{
				//Debug.WriteLine($"Retrieving Job object for JobId: {jobId} from the cache.");
				return qJob;
			}

			//Debug.WriteLine($"Retrieving Job object for JobId: {jobId} from the data base.");
			var jobRecord = _jobReaderWriter.Get(jobId);

			if (jobRecord is null)
			{
				throw new KeyNotFoundException($"Could not find a job with jobId = {jobId}.");
			}

			//if (jobRecord.MapAreaInfoRecord.SubdivisionRecord.BaseMapPosition == null)
			//{
			//	jobRecord.MapAreaInfoRecord.SubdivisionRecord.BaseMapPosition = new BigVectorRecord();
			//}

			//DateTime lastSaved = DateTime.UtcNow;

			//if (jobRecord.LastSavedUtc.HasValue)
			//{
			//	lastSaved = jobRecord.LastSavedUtc.Value;
			//}
			//else if (jobRecord.LastSaved.HasValue)
			//{
			//	lastSaved = jobRecord.LastSaved.Value;
			//}
			//else
			//{
			//	lastSaved = DateTime.UtcNow;
			//}

			var job = new Job(
				id: jobId,
				ownerId: jobRecord.OwnerId,
				jobOwnerType: jobRecord.OwnerType, // ?? JobOwnerType.Undetermined,
				parentJobId: jobRecord.ParentJobId,
				label: jobRecord.Label,
				transformType: _mSetRecordMapper.MapFromTransformType(jobRecord.TransformType),
				newArea: new RectangleInt(_mSetRecordMapper.MapFrom(jobRecord.NewAreaPosition), _mSetRecordMapper.MapFrom(jobRecord.NewAreaSize)),


				mapAreaInfo: _mSetRecordMapper.MapFrom(jobRecord.MapCenterAndDeltaRecord),
				//colorBandSetId: jobRecord.ColorBandSetId,
				colorBandSetName: jobRecord.ColorBandSetName,
				colorBandSetVersion: jobRecord.ColorBandSetVersion,

				mapCalcSettings: jobRecord.MapCalcSettings,
				dateCreatedUtc: jobRecord.DateCreatedUtc,
				lastSavedUtc: jobRecord.LastSavedUtc
				)
			{
				//ColorBandSetId = jobRecord.ColorBandSetId,
				LastAccessedUtc = jobRecord.LastAccessedUtc
			};

			//var colorBandSet = GetColorBandSet(ownerName:null, jobRecord, colorBandSetReaderWriter, colorBandSetCache, out var isCacheHit);

			//ObjectId cbsId;
			//if (colorBandSet != null)
			//{
			//	cbsId = colorBandSet.Id;

			//	if (!isCacheHit)
			//	{
			//		if (!IsColorBandSetUnique(colorBandSet.Name, colorBandSet.TargetIterations, colorBandSetCache))
			//		{
			//			colorBandSet.Name = Guid.NewGuid().ToString();
			//		}

			//		colorBandSetCache?.Add(colorBandSet.Id, colorBandSet);
			//		jobReaderWriter.UpdateColorBandSet(jobId, colorBandSet.HighCutoff, cbsId);
			//	}

			//	if (cbsId != jobRecord.ColorBandSetId)
			//	{
			//		jobReaderWriter.UpdateColorBandSet(jobId, colorBandSet.HighCutoff, cbsId);
			//	}
			//}
			//else
			//{
			//	cbsId = jobRecord.ColorBandSetId;
			//}

			//job.ColorBandSetId = cbsId;

			//jobCache?.Add(job.Id, job);

			return job;
		}

		private bool IsColorBandSetUnique(string name, int targetIterations, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache)
		{
			if (colorBandSetCache == null) return false;


			var foundOne = colorBandSetCache.Values.Any(x => x.TargetIterations == targetIterations && x.Name == name);

			var isUnique = !foundOne;

			return isUnique;
		}

		//public (ObjectId, MapCenterAndDelta)? GetSubdivisionIdAndMapAreaInfo(ObjectId jobId)
		//{
		//	try
		//	{
		//		var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//		var subAndMap = jobReaderWriter.GetSubdivisionIdAndMapCenterAndDelta(jobId);

		//		if (subAndMap.HasValue)
		//		{
		//			var (subdivisionId, mapCenterAndDeltaRecord) = subAndMap.Value;
		//			var mapCenterAndDelta = _mSetRecordMapper.MapFrom(mapCenterAndDeltaRecord);

		//			return (subdivisionId, mapCenterAndDelta);
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		Debug.WriteLine($"While GetSubdivisionId from a JobId, got exception: {e}.");
		//		return null;
		//	}
		//}

		public ObjectId? GetSubdivisionId(ObjectId jobId)
		{
			try
			{
				var jobReaderWriter = new JobReaderWriter(_dbProvider);
				var subdivisionId = jobReaderWriter.GetSubdivisionId(jobId);
				return subdivisionId;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"While GetSubdivisionId from a JobId, got exception: {e}.");
				return null;
			}
		}

		//// TODO: Remove ownerName from signature and don't attempt to get the ColorBandSet by id.
		//private ColorBandSet? GetColorBandSet(string? ownerName, JobRecord jobRec, ColorBandSetReaderWriter colorBandSetReaderWriter, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache, out bool isCacheHit)
		//{
		//	ColorBandSet? result;

		//	var colorBandSetId = jobRec.ColorBandSetId;

		//	if (colorBandSetCache != null && colorBandSetCache.TryGetValue(colorBandSetId, out var colorBandSet))
		//	{
		//		isCacheHit = true;

		//		result = colorBandSet;
		//	}
		//	else
		//	{
		//		isCacheHit = false;
		//		var colorBandSetRecord = colorBandSetReaderWriter.Get(jobRec.ColorBandSetId);
		//		if (colorBandSetRecord == null)
		//		{
		//			Debug.WriteLine($"The colorBandSetRecord is null for the CbsId: {colorBandSetId}, job : {jobRec.Id}, of Project: {jobRec.OwnerId} .");
		//		}

		//		if (colorBandSetRecord == null)
		//		{
		//			result = null;
		//		}
		//		else
		//		{
		//			colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);

		//			if (colorBandSet.OwnerId != jobRec.OwnerId)
		//			{
		//				try
		//				{
		//					result = GetUpdatedCbsForProject(colorBandSet, jobRec.OwnerId, ownerName ?? Guid.NewGuid().ToString(), colorBandSetReaderWriter);
		//				}
		//				catch
		//				{
		//					result = null;
		//					// Ignore the error and return null;
		//				}
		//			}
		//			else
		//			{
		//				result = colorBandSet;
		//			}
		//		}
		//	}

		//	//if (result != null && colorBandSetCache != null && result.HighCutoff != job.MapCalcSettings.TargetIterations)
		//	//{
		//	//	var targetIterations = job.MapCalcSettings.TargetIterations;
		//	//	var averageTargetIterations = GetIterationsForJob(result, job);
		//	//	var newTargetIterations = (int)Math.Round(averageTargetIterations);

		//	//	Debug.WriteLine($"WARNING: Job's ColorMap HighCutoff: {result.HighCutoff} doesn't match the TargetIterations: {targetIterations}. Job has an average TargetIteration of {averageTargetIterations}.");

		//	//	result = GetUpdatedCbsWithTargetIteration(result, newTargetIterations, colorBandSetCache.Values, colorBandSetReaderWriter);
		//	//}

		//	return result;
		//}

		//private double GetIterationsForJob(ColorBandSet colorBandSet, Job job)
		//{
		//	var targetIterations = job.MapCalcSettings.TargetIterations;
		//	var highCutoff = colorBandSet.HighCutoff;

		//	var mapSectionHelper = new MapSectionBuilder();

		//	//var mapSectionRequests = mapSectionHelper.CreateSectionRequests(job.Id.ToString(), JobOwnerType.Project, job.MapAreaInfo, job.MapCalcSettings);
		//	// TODO: Retrieve each MapSection record from the database and use the value of the Target Iterations, actually computed.
		//	//var averageTargetIterations = mapSectionRequests.Average(x => x.MapCalcSettings.TargetIterations);
		//	var averageTargetIterations = job.MapCalcSettings.TargetIterations;

		//	return averageTargetIterations;
		//}

		//private ColorBandSet GetUpdatedCbsWithTargetIteration(ColorBandSet colorBandSet, int targetIterations, IEnumerable<ColorBandSet> cacheValues, ColorBandSetReaderWriter colorBandSetReaderWriter)
		//{
		//	var updatedCbs = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations, cacheValues);
		//	InsertCbs(updatedCbs, colorBandSetReaderWriter);

		//	return updatedCbs;
		//}

		private ColorBandSet GetUpdatedCbsForProject(ColorBandSet colorBandSet, ObjectId projectId, string ownerName, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var updatedCbs = colorBandSet.CreateNewCopy(ObjectId.GenerateNewId());

			if (string.IsNullOrEmpty(updatedCbs.Name))
			{
				updatedCbs.Name = ownerName;
			}

			updatedCbs.OwnerId = projectId;
			var newId = InsertCbs(updatedCbs, colorBandSetReaderWriter);

			Debug.Assert(newId == updatedCbs.Id, "The ColorBandSet Id was updated as it was inserted into the collection.");

			return updatedCbs;
		}

		public ObjectId InsertJob(Job job)
		{
			job.LastSavedUtc = DateTime.UtcNow;
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);

			//jobRecord.LastSavedUtc = DateTime.UtcNow;
			var newJobId = jobReaderWriter.Insert(jobRecord);

			return newJobId;
		}

		public void UpdateJobDetails(Job job)
		{
			var jobRecord = _mSetRecordMapper.MapTo(job);

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.UpdateJobDetails(jobRecord);
			job.LastSavedUtc = DateTime.UtcNow;
		}

		public void UpdateJobOwnerType(ObjectId jobId, OwnerType jobOwnerType)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			jobReaderWriter.UpdateJobOwnerType(jobId, jobOwnerType);
		}

		public bool DeleteJob(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = DeleteJob(jobId, jobReaderWriter);
			return result > 0;
		}

		private long DeleteJob(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			var result = jobReaderWriter.Delete(jobId);
			return result ?? 0;
		}

		#endregion

		#region JobInfo

		public IEnumerable<JobInfo> GetJobInfosForOwner(ObjectId ownerId, ObjectId currentJobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetJobInfosForOwner(ownerId).ToList();

			if (result.Count > 0)
			{
				var foundTheCurrentJob = false;
				for (var i = 0; i < result.Count; i++)
				{
					if (result[i].Id == currentJobId)
					{
						result[i].IsCurrentOnOwner = true;
						foundTheCurrentJob = true;
						break;
					}
				}

				if (!foundTheCurrentJob)
				{
					result[0].IsCurrentOnOwner = true;
				}
			}

			return result;
		}

		#endregion

		#region Poster

		public List<Poster> GetAllPosters()
		{
			var posterRecords = _posterReaderWriter.GetAll();
			var result = posterRecords.Select(x => BuildPoster(x)).ToList();

			return result;
		}

		public IEnumerable<ObjectId> GetAllPosterIds()
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var result = posterReaderWriter.GetAllIds();
			return result;
		}

		public bool TryGetPoster(ObjectId posterId, [NotNullWhen(true)] out Poster? poster)
		{
			//Debug.WriteLine($"Retrieving Poster object with Id: {posterId}.");
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (posterReaderWriter.TryGet(posterId, out var posterRecord))
			{
				var colorBandSets = GetColorBandSetsForOwner(posterId).ToList();
				var jobs = GetAllJobsForOwner(posterId);

				if (colorBandSets.Count == 0)
				{
					throw new InvalidOperationException($"The Poster: {posterId}/{posterRecord.Name} has no ColorBandSets.");
				}

				var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, posterRecord.TargetIterationColorMapRecords, "as the poster is being retrieved", out var updateWasMade);
				poster = AssemblePoster(posterRecord, jobs, colorBandSets, lookupColorBandSetByTargetIteration, posterRecord.LastSavedUtc);

				if (poster != null && updateWasMade)
				{
					poster.MarkAsDirty();
				}

				return poster != null;
			}
			else
			{
				poster = null;
				return false;
			}
		}

		public bool TryGetPoster(string name, [NotNullWhen(true)] out Poster? poster)
		{
			//Debug.WriteLine($"Retrieving Poster object with name: {name}.");
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (posterReaderWriter.TryGet(name, out var posterRecord))
			{
				var posterId = posterRecord.Id;

				var colorBandSets = GetColorBandSetsForOwner(posterId).ToList();
				var jobs = GetAllJobsForOwner(posterId);

				if (colorBandSets.Count == 0)
				{
					throw new InvalidOperationException($"The Poster: {posterId}/{posterRecord.Name} has no ColorBandSets.");
				}

				var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, posterRecord.TargetIterationColorMapRecords, "as the poster is being retrieved", out var updateWasMade);
				poster = AssemblePoster(posterRecord, jobs, colorBandSets, lookupColorBandSetByTargetIteration, posterRecord.LastSavedUtc);

				if (poster != null && updateWasMade)
				{
					poster.MarkAsDirty();
				}

				return poster != null;
			}
			else
			{
				poster = null;
				return false;
			}
		}

		private Poster BuildPoster(PosterRecord target)
		{
			var posterId = target.Id;
			var colorBandSets = GetColorBandSetsForOwner(posterId).ToList();
			var jobs = GetAllJobsForPoster(posterId);

			//var lookupColorMapByTargetIteration = new Dictionary<int, TargetIterationColorMapRecord>();

			var targetIterationColorMapRecords = new List<TargetIterationColorMapRecord>();

			var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, targetIterationColorMapRecords, "as the poster is being built", out var updateWasMade);


			var result = new Poster(
				id: target.Id,
				name: target.Name,
				description: target.Description,
				sourceJobId: target.SourceJobId,
				jobs: jobs,
				colorBandSets: colorBandSets,
				lookupColorBandSetByTargetIteration,
				colorBandSetResolutionStrategy: target.ColorBandSetResolutionStrategy,
				currentJobId: target.CurrentJobId,
				posterSize: target.PosterSize,
				displayPosition: _mSetRecordMapper.MapFrom(target.DisplayPosition),
				displayZoom: target.DisplayZoom,
				dateCreatedUtc: target.DateCreatedUtc,
				lastSavedUtc: target.LastSavedUtc,
				lastAccessedUtc: target.LastAccessedUtc

				);

			return result;
		}

		public Poster? CreatePoster(string name, string? description, SizeDbl posterSize, ObjectId sourceJobId, List<Job> jobs, List<ColorBandSet> colorBandSets, IEnumerable<TargetIterationColorMapRecord> targetIterationColorMapRecords)
		{
			if (jobs.Count == 0)
			{
				throw new InvalidOperationException($"Cannot create a poster with name: {name}. No jobs were provided.");
			}

			if (!colorBandSets.Any())
			{
				throw new InvalidOperationException($"Cannot create a poster with name: {name}. No ColorBandSets were provided.");
			}

			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (posterReaderWriter.PosterExists(name, out var posterId))
			{
				throw new InvalidOperationException($"Cannot create a poster with name: {name}, a poster: {posterId} with that name already exists.");
			}


			var posterSizeRounded = posterSize.Round(MidpointRounding.AwayFromZero);

			// TODO: Update all PosterRecords to use double instead of int for the Width and Height
			var posterRecord = new PosterRecord(name, description, sourceJobId, jobs.First().Id,
					DisplayPosition: new VectorDblRecord(0, 0),
					DisplayZoom: RMapConstants.DEFAULT_POSTER_DISPLAY_ZOOM,
					DateCreatedUtc: DateTime.UtcNow,
					LastSavedUtc: DateTime.UtcNow,
					LastAccessedUtc: DateTime.UtcNow)
			{
				Width = posterSizeRounded.Width,
				Height = posterSizeRounded.Height,
				TargetIterationColorMapRecords = targetIterationColorMapRecords.ToArray()
			};

			Debug.WriteLine($"Creating new Poster with name: {name}.");

			posterId = posterReaderWriter.Insert(posterRecord);

			foreach (var job in jobs)
			{
				job.OwnerId = posterId;
			}

			foreach (var cbs in colorBandSets)
			{
				cbs.OwnerId = posterId;
				cbs.AssignNewSerialNumber();
				if (cbs.Name == RMapConstants.NAME_FOR_NEW_PROJECTS)
					cbs.Name = name;

				cbs.MarkAsDirty();
			}

			//JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, lookupColorMapByTargetIteration, "as the poster is being created");
			//var result = AssemblePoster(posterRecord, jobs, colorBandSets, lookupColorMapByTargetIteration, DateTime.MinValue);

			var lookupColorBandSetByTargetIteration = JobOwnerHelper.PopulateColorBandSetsByTargetIteration(jobs, colorBandSets, posterRecord.TargetIterationColorMapRecords, "as the poster is being created", out _);
			var result = AssemblePoster(posterRecord, jobs, colorBandSets, lookupColorBandSetByTargetIteration, posterRecord.LastSavedUtc);

			if (result != null)
			{
				result.MarkAsDirty();
			}

			return result;
		}

		private Poster? AssemblePoster(PosterRecord posterRecord, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, DateTime lastSavedUtc)
		{
			Poster? result;
			if (jobs.Count == 0 || !colorBandSets.Any())
			{
				result = null;
			}
			else
			{
				var displayPosition = _mSetRecordMapper.MapFrom(posterRecord.DisplayPosition);

				result = new Poster(posterRecord.Id, posterRecord.Name, posterRecord.Description, 
					posterRecord.SourceJobId, jobs, 
					colorBandSets, lookupColorMapByTargetIteration, posterRecord.ColorBandSetResolutionStrategy, posterRecord.CurrentJobId, 
					
					posterSize: posterRecord.PosterSize, displayPosition, posterRecord.DisplayZoom, 
					dateCreatedUtc: posterRecord.DateCreatedUtc, lastSavedUtc: lastSavedUtc, lastAccessedUtc: DateTime.MinValue);


				var ts = DateTime.Now.ToLongTimeString();
				Debug.WriteLine($"AssemblePoster completed. Name: {result.Name}, CurrentJobId: {result.CurrentJobId}, DisplayPosition: {result.DisplayPosition}, DisplayZoom: {result.DisplayZoom}. At ts={ts}.");
			}

			return result;
		}

		//public void CreatePoster(Poster poster)
		//{
		//	var posterReaderWriter = new PosterReaderWriter(_dbProvider);

		//	if (!posterReaderWriter.PosterExists(poster.Name, out var posterId))
		//	{
		//		var posterRecord = _mSetRecordMapper.MapTo(poster);
		//		var posterRecordId = posterReaderWriter.Insert(posterRecord);

		//		Debug.Assert(poster.Id == posterRecordId);
		//	}
		//	else
		//	{
		//		throw new InvalidOperationException($"Cannot create poster with name: {poster.Name}, a poster: {posterId} with that name already exists.");
		//	}
		//}

		public void UpdatePosterCurrentJobId(ObjectId posterId, ObjectId? currentJobId)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			posterReaderWriter.UpdateCurrentJobId(posterId, currentJobId);
		}

		public void UpdatePosterName(ObjectId posterId, string name)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			posterReaderWriter.UpdateName(posterId, name);
		}

		public void UpdatePosterDescription(ObjectId posterId, string? description)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			posterReaderWriter.UpdateDescription(posterId, description);
		}

		public void UpdatePosterMapArea(Poster poster)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var posterRecord = _mSetRecordMapper.MapTo(poster);

			posterReaderWriter.UpdateMapArea(posterRecord);
		}

		public void UpdatePosterDisplayPositionAndZoom(Poster poster)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var posterRecord = _mSetRecordMapper.MapTo(poster);

			posterReaderWriter.UpdateDisplayPositionAndZoom(posterRecord);
		}

		public void UpdatePosterTargetIterationMap(Poster poster)
		{
			_posterReaderWriter.UpdateTargetIterationMap(poster.Id, poster.LastAccessedUtc, poster.GetTargetIterationColorMapRecords().ToArray(), poster.ColorBandSetResolutionStrategy);
		}

		public bool DeletePoster(ObjectId posterId)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetJobIdsByOwner(posterId);

			foreach (var jobId in jobIds)
			{
				_ = DeleteJob(jobId, jobReaderWriter);
			}

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			_ = colorBandSetReaderWriter.DeleteColorBandSetsForOwner(posterId);
			var numberDeleted = posterReaderWriter.Delete(posterId);

			return numberDeleted == 1;
		}

		public bool PosterExists(string name, [MaybeNullWhen(false)] out ObjectId posterId)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var result = posterReaderWriter.PosterExists(name, out posterId);

			return result;
		}

		public bool PosterExists(ObjectId posterId)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var result = posterReaderWriter.Get(posterId);

			return result != null;
		}

		#endregion

		#region PosterInfo

		public IEnumerable<IPosterInfo> GetAllPosterInfos()
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobMapSectionReaderWriter = new JobMapSectionReaderWriter(_dbProvider);

			var allPosterRecords = posterReaderWriter.GetAll();

			var result = allPosterRecords.Select(x => GetPosterInfoInternal(x, jobReaderWriter, jobMapSectionReaderWriter));

			return result;
		}

		private IPosterInfo GetPosterInfoInternal(PosterRecord posterRecord, JobReaderWriter jobReaderWriter, JobMapSectionReaderWriter jobMapSectionReaderWriter)
		{
			//Debug.WriteLine($"Retrieving PosterInfo. Poster: {posterRec.Id}, Current Job: {posterRec.CurrentJobId}");

			var jobRec = jobReaderWriter.Get(posterRecord.CurrentJobId);

			PosterInfo result;

			DateTime lastSavedUtc;

			if (jobRec == null)
			{
				//throw new InvalidOperationException($"Poster with ID: {posterRec.CurrentJobId} could not be found in the repository.");

				Debug.WriteLine($"WARNING: Could not find Job with Id: {posterRecord.CurrentJobId} for Poster Name: {posterRecord.Name} and ID: {posterRecord.Id}.");

				lastSavedUtc = posterRecord.LastSavedUtc;
			}
			else
			{
				lastSavedUtc = jobRec.LastSavedUtc;
			}

			if (posterRecord.Name == "Art3-13-4")
			{
				Debug.WriteLine("Here at Art3-13-4");
			}

			var jobIds = jobReaderWriter.GetJobIdsByOwner(posterRecord.Id).ToList();
			var bytes = GetBytes(jobIds, jobMapSectionReaderWriter);

			result = new PosterInfo(posterRecord.Id, posterRecord.Name, posterRecord.Description, posterRecord.CurrentJobId, posterRecord.PosterSize, bytes, posterRecord.DateCreatedUtc, lastSavedUtc, posterRecord.LastAccessedUtc);
			return result;
		}

		private int GetBytes(List<ObjectId> jobIds, JobMapSectionReaderWriter jobMapSectionReaderWriter)
		{
			var numberOfMapSections = 0;

			foreach (var jobId in jobIds)
			{
				var numberOfMapSectionsForJob = jobMapSectionReaderWriter.GetCountOfMapSectionsByJobId(jobId);
				numberOfMapSections += numberOfMapSectionsForJob;
			}

			var result = numberOfMapSections * 64300;

			return result;
		}

		#endregion

		#region Active Job Schema Updates

		public void DoSchemaUpdates()
		{
			//UpdateAllColorBandSets();

			//DeleteUnusedColorBandSets();

			//UpdateAllJobsWithMapCenterAndDelta();

			//UpdateColorBandSetSchema();

			//UpdateAllJobsToUseColorBandSetName();

			//RemoveUseEscapVelocitiesFromAllJobs();
		}

		public long RemoveUseEscapVelocitiesFromAllJobs()
		{
			//var result = _jobReaderWriter.RemoveUseEscapVelocitiesFromAllJobs();
			var result = 0;
			return result;
		}

		public long UpdateAllJobsToUseColorBandSetName()
		{
			var result = 0L;
			var potentialResult = 0L;
			var colorBandSetsNotFound = 0L;

			var allProjectIds = _projectReaderWriter.GetAllIds().ToList();
			foreach (var ownerId in allProjectIds)
			{
				var projectRec = _projectReaderWriter.Get(ownerId);
				var projectName = projectRec?.Name;
				if (projectName == null)
				{
					throw new InvalidOperationException($"Can't get a ProjectRecord for OwnerId: {ownerId} or the Project Record's name is null.");
				}

				UpdateJobRecs(ownerId, ref result, ref potentialResult, ref colorBandSetsNotFound);
			}

			var allPosterIds = _posterReaderWriter.GetAllIds();

			foreach (var ownerId in allPosterIds)
			{
				var projectRec = _posterReaderWriter.Get(ownerId);
				var projectName = projectRec?.Name;
				if (projectName == null)
				{
					throw new InvalidOperationException($"Can't get a PosterRec for OwnerId: {ownerId} or the Poster Record's name is null.");
				}

				UpdateJobRecs(ownerId, ref result, ref potentialResult, ref colorBandSetsNotFound);
			}

			return result;
		}

		//public long UpdateAllJobsToUseColorBandSetName()
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var posterReaderWriter = new PosterReaderWriter(_dbProvider);


		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	var result = 0L;
		//	var potentialResult = 0L;
		//	var colorBandSetsNotFound = 0L;

		//	//var allProjectIds =  projectReaderWriter.GetAllIds();

		//	var allProjectIds = posterReaderWriter.GetAllIds();

		//	foreach (var projectId in allProjectIds)
		//	{
		//		//var projectRec = _projectReaderWriter.Get(projectId);
		//		var projectRec = posterReaderWriter.Get(projectId);

		//		var projectName = projectRec?.Name;

		//		if (projectName == null)
		//		{
		//			throw new InvalidOperationException($"Can't get a ProjectRecord for ProjectId: {projectId} or the ProjectRecord's name is null.");
		//		}

		//		var colorBandSets = GetColorBandSetsForOwner(projectId).ToList();
		//		var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(colorBandSets.Select(x => new KeyValuePair<ObjectId, ColorBandSet>(x.Id, x)));

		//		var allJobsIds = jobReaderWriter.GetJobIdsByOwner(projectId).ToList();

		//		potentialResult += allJobsIds.Count;

		//		foreach (var jobId in allJobsIds)
		//		{
		//			var jobRecord = jobReaderWriter.Get(jobId);

		//			if (jobRecord != null)
		//			{
		//				jobRecord.OwnerType = jobRecord.JobOwnerType;

		//				if (jobRecord.DateCreatedUtc == DateTime.MinValue)
		//				{
		//					jobRecord.DateCreatedUtc = jobRecord.Id.CreationTime;
		//				}

		//				jobRecord.MapCenterAndDeltaRecord = jobRecord.MapAreaInfo2Record!;

		//				var colorBandSet = GetColorBandSet(ownerName: projectName, jobRecord, _colorBandSetReaderWriter, colorBandSetCache, out var isCacheHit);

		//				if (colorBandSet != null)
		//				{
		//					jobRecord.ColorBandSetName = colorBandSet.Name;
		//					jobRecord.ColorBandSetVersion = colorBandSet.Version;
		//				}
		//				else
		//				{
		//					jobRecord.ColorBandSetName = projectName;
		//					jobRecord.ColorBandSetVersion = null;
		//					colorBandSetsNotFound++;
		//				}

		//				_jobReaderWriter.UpdateSchema(jobRecord);

		//				result += 1;
		//			}
		//			else
		//			{
		//				Debug.WriteLine($"Could not get Job with Id: {jobId}.");
		//			}
		//		}
		//	}

		//	return result;
		//}

		private void UpdateJobRecs(ObjectId ownerId/*, string ownerName*/, ref long result, ref long potentialResult, ref long colorBandSetsNotFound)
		{
			var colorBandSets = GetColorBandSetsForOwner(ownerId).ToList();
			var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(colorBandSets.Select(x => new KeyValuePair<ObjectId, ColorBandSet>(x.Id, x)));

			var allJobsIds = _jobReaderWriter.GetJobIdsByOwner(ownerId).ToList();

			potentialResult += allJobsIds.Count;

			foreach (var jobId in allJobsIds)
			{
				var jobRecord = _jobReaderWriter.Get(jobId);

				if (jobRecord != null)
				{
					var colorBandSet = GetColorBandSet(jobRecord.ColorBandSetId);

					if (colorBandSet != null)
					{
						//jobRecord.ColorBandSetName = colorBandSet.Name;
						//jobRecord.ColorBandSetVersion = colorBandSet.Version;
					}
					else
					{
						//jobRecord.ColorBandSetName = projectName;
						//jobRecord.ColorBandSetVersion = null;
						colorBandSetsNotFound++;
					}

					//_jobReaderWriter.UpdateSchema(jobRecord);

					result += 1;
				}
				else
				{
					Debug.WriteLine($"Could not get Job with Id: {jobId}.");
				}
			}
		}

		//public void UpdateAllJobsWithMapCenterAndDelta()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	jobReaderWriter.UpdateJobsToUseMapCenterAndDelta();
		//}

		//public void RemoveEscapeVels()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	jobReaderWriter.RemoveEscapeVelsFromAllJobs();
		//}

		//public long RemoveColorBandSetIdFromProject()
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var result = projectReaderWriter.RemoveCurrentColorBandSetProp();
		//	return result;
		//}

		//// Update all Job Records to use MapAreaInfo
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = UpdateAllJobsToUseMapPostionSizeAndDelta();
		//	return numUpdated;
		//}


		//// Update all Job Records to have a MapCalcSettings
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = UpdateAllJobsToHaveMapCalcSettings();
		//	return numUpdated;
		//}

		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = RemoveFetchZValuesPropFromAllJobs();
		//	numUpdated += RemoveFetchZValuesPropFromAllJobs2();
		//	return numUpdated;
		//}

		// Remove the old properties that are now part of the MapAreaInfo record
		//public long? DoSchemaUpdates()
		//{
		//	var numUpdated = RemoveOldMapAreaPropsFromAllJobs();
		//	return numUpdated;
		//}

		//public int UpdateColorBandSetSchema()
		//{
		//	var result = _colorBandSetReaderWriter.UpdateColorBandSetSchema();

		//	return result;
		//}

		//public int UpdateAllColorBandSets()
		//{
		//	var result = 0;

		//	var lookupCbsByTargetIterations = new Dictionary<int, ColorBandSetRecord>();

		//	var cbsToDelete = new List<ObjectId>();

		//	var projectRecords = _posterReaderWriter.GetAll();
		//	foreach (var projectRec in projectRecords)
		//	{
		//		var colorBandSetRecords = _colorBandSetReaderWriter.GetColorBandSetsForOwner(projectRec.Id);
		//		foreach (var rec in colorBandSetRecords)
		//		{
		//			//rec.OwnerId = rec.ProjectId;
		//			var dateCreated = rec.DateCreatedUtc;

		//			if (dateCreated == DateTime.MinValue)
		//			{
		//				dateCreated = rec.Id.CreationTime;
		//				rec.DateCreatedUtc = dateCreated;
		//			}

		//			var targetIterations = rec.TargetIterations;
		//			if (targetIterations == 0)
		//			{
		//				targetIterations = rec.ColorBandRecords.Max(x => x.CutOff);
		//				rec.TargetIterations = targetIterations;
		//			}

		//			if (lookupCbsByTargetIterations.TryGetValue(targetIterations, out var existingRec))
		//			{
		//				if (existingRec.DateCreatedUtc <= dateCreated)
		//				{
		//					cbsToDelete.Add(existingRec.Id);
		//					lookupCbsByTargetIterations[targetIterations] = rec;
		//				}
		//			}
		//			else
		//			{
		//				lookupCbsByTargetIterations.Add(targetIterations, rec);
		//			}
		//		}

		//		foreach (var id in cbsToDelete)
		//		{
		//			_colorBandSetReaderWriter.Delete(id);
		//		}

		//		foreach (var kvp in lookupCbsByTargetIterations)
		//		{
		//			var cbs = _mSetRecordMapper.MapFrom(kvp.Value);
		//			_colorBandSetReaderWriter.UpdateDetails(cbs);
		//		}

		//		cbsToDelete.Clear();
		//		lookupCbsByTargetIterations.Clear();

		//	}


		//	return result;
		//}

		public int DeleteUnusedColorBandSets()
		{
			var result = 0;

			//var colorBandSetIdsRefByJob = _jobReaderWriter.GetAllReferencedColorBandSetIds().ToList();

			////Debug.WriteLine($"\nRef by a Job\n");
			////foreach (var x in colorBandSetIdsRefByJob)
			////{
			////	Debug.WriteLine($"{x}");
			////}

			//var colorBandSetIds = new List<ObjectId>(colorBandSetIdsRefByJob);

			//var referencedByProjectNotByJob = new List<ObjectId>();

			//var projectRecords = _projectReaderWriter.GetAll();
			//foreach (var projectRec in projectRecords)
			//{
			//	var colorBandSetRecords = _colorBandSetReaderWriter.GetColorBandSetsForOwner(projectRec.Id);
			//	foreach (var colorBandSetRec in colorBandSetRecords)
			//	{
			//		if (!colorBandSetIds.Contains(colorBandSetRec.Id))
			//		{
			//			referencedByProjectNotByJob.Add(colorBandSetRec.Id);
			//		}
			//	}
			//}

			//referencedByProjectNotByJob = referencedByProjectNotByJob.Distinct().ToList();

			////Debug.WriteLine($"\nRef by Project, but not by job\n");
			////foreach (var x in referencedByProjectNotByJob)
			////{
			////	Debug.WriteLine($"{x}");
			////}

			//var allCbsIds = _colorBandSetReaderWriter.GetAll().Select(x => x.Id);

			//var unReferenced = new List<ObjectId>();

			//foreach (var cbsId in allCbsIds)
			//{
			//	if (!colorBandSetIds.Contains(cbsId))
			//	{
			//		unReferenced.Add(cbsId);
			//	}
			//}

			//Debug.WriteLine($"\nUnreferenced\n");
			//foreach (var x in referencedByProjectNotByJob)
			//{
			//	Debug.WriteLine($"{x}");

			//	// Uncomment this next line!!
			//	//_ = _colorBandSetReaderWriter.Delete(x);
			//	result++;
			//}

			return result;
		}

		#endregion

		#region Old Schema Updates

		[Conditional("DEBUG2")]
		private void CompareMapAreaV1AfterRoundTrip(MapPositionSizeAndDelta previousValue, MapPositionSizeAndDelta newValue, MapCenterAndDelta middleValue)
		{
			Debug.WriteLine($"MapDisplay is RoundTripping MapAreaInfoV1" +
				$"\nPrevious Scale: {previousValue.SamplePointDelta.Width}. Pos: {previousValue.Coords}. MapOffset: {previousValue.MapBlockOffset}. ImageOffset: {previousValue.CanvasControlOffset} Size: {previousValue.CanvasSize} " +
				$"\nNew Scale     : {newValue.SamplePointDelta.Width}. Pos: {newValue.Coords}. MapOffset: {newValue.MapBlockOffset}. ImageOffset: {newValue.CanvasControlOffset} Size: {newValue.CanvasSize}" +
				$"\nIntermediate     : {middleValue.SamplePointDelta.Width}. Pos: {middleValue.MapCenter}. MapOffset: {middleValue.MapBlockOffset}. ImageOffset: {middleValue.CanvasControlOffset}");
		}

		//public long UpdateAllJobsToUseMapPositionSizeAndDelta()
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);

		//	var result = 0L;

		//	var allProjectIds = projectReaderWriter.GetAllIds();

		//	foreach(var projectId in allProjectIds)
		//	{
		//		var allJobsIds = jobReaderWriter.GetJobIds(projectId);

		//		foreach(var jobId in allJobsIds)
		//		{
		//			var jobRecord = jobReaderWriter.Get(jobId);
		//			var subdivisionRecord = subdivisonReaderWriter.Get(jobRecord.SubDivisionId);
		//			var canvasSizeRec = jobRecord.MapAreaInfoRecord.CanvasSize ?? new SizeIntRecord(1024, 1024);
		//			var newMapAreaInfoRec = new MapAreaInfoRecord(jobRecord.MapAreaInfoRecord.CoordsRecord, canvasSizeRec, subdivisionRecord, jobRecord.MapAreaInfoRecord.MapBlockOffset, jobRecord.MapAreaInfoRecord.CanvasControlOffset);

		//			var transformType = Enum.Parse<TransformType>(jobRecord.TransformType.ToString());

		//			var transformType2 = Enum.GetName(transformType) ?? "None";

		//			var numberUpdated = jobReaderWriter.ConvertToMapAreaRecord(jobId, newMapAreaInfoRec, transformType2);
		//			result += numberUpdated;
		//		}

		//		//break;
		//	}

		//	return result;
		//}

		//public long UpdateAllJobsToHaveMapCalcSettings()
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	var result = 0L;

		//	var allProjectIds = projectReaderWriter.GetAllIds();

		//	foreach (var projectId in allProjectIds)
		//	{
		//		var allJobsIds = jobReaderWriter.GetJobIds(projectId);

		//		foreach (var jobId in allJobsIds)
		//		{
		//			var jobRecord = jobReaderWriter.Get(jobId);

		//			var mapCalcSettings = jobRecord.MapCalcSettings;
		//			var numberUpdated = jobReaderWriter.AddMapCalcSettingsField(jobId, mapCalcSettings);
		//			result += numberUpdated;
		//		}

		//		//break;
		//	}

		//	return result;
		//}


		//public long UpdateAllJobsToUseMapCenterAndDelta()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var result = jobReaderWriter.RemoveJobsWithNoProject();
		//	return result;
		//}

		//public long RemoveFetchZValuesPropFromAllJobs()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var result = jobReaderWriter.RemoveFetchZValuesProperty();
		//	return result;
		//}


		//public long RemoveOldMapAreaPropsFromAllJobs()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var result = jobReaderWriter.RemoveOldMapAreaProperties();
		//	return result;
		//}




		//		public int[] FixAllJobRels()
		//		{
		//			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//			var allProjectRecs = projectReaderWriter.GetAll();

		//			var result = new int[allProjectRecs.Count()];

		//			var ptr = 0;
		//			foreach (var projectRecord in allProjectRecs)
		//			{
		//				result[ptr++] = FixJobRels(projectRecord.Id);
		//			}

		//			return result;
		//		}

		//		public int FixJobRels(ObjectId projectId)
		//		{
		//			var result = 0;
		//			var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//			var lastId = ObjectId.GenerateNewId();

		//			var jobs = GetAllJobsForProject(projectId);
		//			var jobsWithAParent = jobs.Where(x => x.ParentJobId != null).OrderBy(f => f.ParentJobId).ThenByDescending(f => f.Id);

		//			foreach (var job in jobsWithAParent)
		//			{
		//				if (job.ParentJobId == lastId)
		//				{
		//					job.IsPreferredChild = false;
		//					jobReaderWriter.UpdateJobsParent(job.Id, job.ParentJobId, job.IsPreferredChild);
		//					result++;
		//				}
		//				else
		//				{
		//					job.IsPreferredChild = true;
		//#pragma warning disable CS8629 // Nullable value type may be null.
		//					lastId = (ObjectId)job.ParentJobId;
		//#pragma warning restore CS8629 // Nullable value type may be null.
		//				}
		//			}

		//			return result;
		//		}

		//		#endregion

		//		#region Archived Job Schema Updates

		//		public string FixAllJobRels2()
		//		{
		//			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//			var allProjectRecs = projectReaderWriter.GetAll();

		//			var sb = new StringBuilder();

		//			foreach (var projectRecord in allProjectRecs)
		//			{
		//				sb.AppendLine($"Project: {projectRecord.Id}, {projectRecord.DateCreated}");

		//				var ts = FixJobRels2(projectRecord.Id);

		//				foreach (var t in ts)
		//				{
		//					var t1 = t.Item1;
		//					var t2 = t.Item2;

		//					var s1 = $"{t1.Id}\t {t1.DateCreated}\t{t1.TransformType}\t\t{t1.Subdivision.SamplePointDelta.Exponent}\t{t1.ParentJobId}";
		//					var s2 = $"{t2?.Id}\t {t2?.DateCreated}\t {t2?.TransformType}\t\t{t2?.Subdivision.SamplePointDelta.Exponent}\t{t2?.ParentJobId}";

		//					sb.Append(s1).Append("\t\t").AppendLine(s2);
		//				}
		//			}

		//			return sb.ToString();
		//		}

		//		//public int FixAllJobRels3()
		//		//{
		//		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//		//	var allProjectRecs = projectReaderWriter.GetAll();

		//		//	var result = 0;

		//		//	foreach (var projectRecord in allProjectRecs)
		//		//	{
		//		//		var ts = FixJobRels2(projectRecord.Id);

		//		//		foreach (var t in ts)
		//		//		{
		//		//			jobReaderWriter.UpdateJobsParent(t.Item1.Id, t.Item2.Id, t.Item1.IsPreferredChild);
		//		//		}
		//		//	}

		//		//	return result;
		//		//}

		//		public IList<Tuple<Job, Job>> FixJobRels2(ObjectId projectId)
		//		{
		//			var result = new List<Tuple<Job, Job>>();

		//			var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//			var lastId = ObjectId.GenerateNewId();


		//			var jobs = GetAllJobsForProject(projectId);
		//			var jobsWithAParent = jobs.Where(x => x.ParentJobId != null).OrderBy(f => f.ParentJobId).ThenBy(f => f.Id);

		//			var lookupList = jobs.OrderByDescending(f => f.Id).Select(x => new ValueTuple<string, Job>(x.Id.ToString(), x));


		//			foreach (var job in jobsWithAParent)
		//			{
		//				var parentJob = jobs.FirstOrDefault(x => x.Id == job.ParentJobId);

		//				if (parentJob == null)
		//				{
		//					ValueTuple<string, Job> newParentIdAndJob;
		//					//parentJob = jobs.FirstOrDefault(x => x.DateCreated <= job.DateCreated && x.Id.Increment < job.Id.Increment);
		//					if (job.TransformType == TransformType.ZoomIn)
		//					{
		//						newParentIdAndJob = lookupList.FirstOrDefault(x => string.Compare(x.Item1, job.Id.ToString()) < 0 && x.Item2.Subdivision.SamplePointDelta.Exponent > job.Subdivision.SamplePointDelta.Exponent);
		//					}
		//					else
		//					{
		//						newParentIdAndJob = lookupList.FirstOrDefault(x => string.Compare(x.Item1, job.Id.ToString()) < 0);
		//					}

		//					if (newParentIdAndJob != default(ValueTuple<string, Job>))
		//					{
		//						var jobAndNewParent = new Tuple<Job, Job>(job, newParentIdAndJob.Item2);
		//						result.Add(jobAndNewParent);
		//					}

		//				}
		//			}

		//			return result;
		//		}

		//public void AddIsIsAlternatePathHeadToAllJobs()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	jobReaderWriter.AddIsIsAlternatePathHeadToAllJobs();
		//}

		#endregion
	}
}
