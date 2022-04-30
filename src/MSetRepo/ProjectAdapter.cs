using MongoDB.Bson;
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

namespace MSetRepo
{
	public class ProjectAdapter : IProjectAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;
		private readonly DtoMapper _dtoMapper;

		#region Constructor

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
			_dtoMapper = new DtoMapper();
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

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.CreateCollection();

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.CreateCollection();

			mapSectionReaderWriter.CreateSubAndPosIndex();
		}

		public void DropCollections()
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.DropCollection();

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.DropCollection();

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.DropCollection();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.DropCollection();

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.DropCollection();
		}

		public void DropSubdivisionsAndMapSectionsCollections()
		{
			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.DropCollection();

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.DropCollection();
		}

		#endregion

		#region Project

		//public Project GetProject(ObjectId projectId)
		//{
		//	//Debug.WriteLine($"Retrieving Project object for ProjectId: {projectId}.");

		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var projectRecord = projectReaderWriter.Get(projectId);
		//	var project = _mSetRecordMapper.MapFrom(projectRecord);

		//	return project;
		//}

		public bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project)
		{
			//Debug.WriteLine($"Retrieving Project object for Project with name: {name}.");

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			if (projectReaderWriter.TryGet(name, out var projectRecord))
			{
				var colorBandSets = GetColorBandSetsForProject(projectRecord.Id);
				var jobs = GetAllJobsForProject(projectRecord.Id, colorBandSets);

				project = AssembleProject(projectRecord, jobs, colorBandSets);
				return true;
			}
			else
			{
				project = null;
				return false;
			}
		}

		public Project CreateProject(string name, string? description, ObjectId currentJobId, ObjectId currentColorBandSetId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(name);
			if (projectRecord is null)
			{
				//var project = new Project(name, description, currentJobId);
				//projectRecord = _mSetRecordMapper.MapTo(project);

				projectRecord = new ProjectRecord(name, description, currentJobId, DateTime.UtcNow);

				var projectId = projectReaderWriter.Insert(projectRecord);
				projectRecord = projectReaderWriter.Get(projectId);

				var colorBandSets = GetColorBandSetsForProject(projectRecord.Id);
				var jobs = GetAllJobsForProject(projectRecord.Id, colorBandSets);
				var result = AssembleProject(projectRecord, jobs, colorBandSets);

				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}
		}

		public Project CreateNewProject(string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(name);
			if (projectRecord is null)
			{
				projectRecord = new ProjectRecord(name, description, jobs.First().Id, DateTime.UtcNow);

				var projectId = projectReaderWriter.Insert(projectRecord);
				projectRecord = projectReaderWriter.Get(projectId);

				foreach(var job in jobs)
				{
					job.ProjectId = projectId;
				}

				foreach(var cbs in colorBandSets)
				{
					cbs.ProjectId = projectId;
				}

				var result = AssembleProject(projectRecord, jobs, colorBandSets);

				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}
		}

		private Project AssembleProject(ProjectRecord projectRecord, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets)
		{
			var result = new Project(projectRecord.Id, projectRecord.Name, projectRecord.Description, jobs, colorBandSets, projectRecord.CurrentJobId, projectRecord.LastSavedUtc);
			return result;
		}


		public void UpdateProjectName(ObjectId projectId, string name)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.UpdateName(projectId, name);
		}

		public void UpdateProjectDescription(ObjectId projectId, string? description)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.UpdateDescription(projectId, description);
		}

		public void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.UpdateCurrentJobId(projectId, currentJobId);
		}

		public void DeleteProject(ObjectId projectId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetJobIds(projectId);

			foreach (var jobId in jobIds)
			{
				_ = DeleteJob(jobId, jobReaderWriter);
			}

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var cbsIds = colorBandSetReaderWriter.GetColorBandSetIdsForProject(projectId);

			foreach (var colorBandSetId in cbsIds)
			{
				_ = colorBandSetReaderWriter.Delete(colorBandSetId);
			}

			_ = projectReaderWriter.Delete(projectId);
		}

		#endregion

		#region ProjectInfo

		public IEnumerable<IProjectInfo> GetAllProjectInfos()
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var allProjectRecords = projectReaderWriter.GetAll();
			var result = allProjectRecords.Select(x => GetProjectInfoInternal(x, jobReaderWriter, subdivisionReaderWriter));

			return result;
		}

		//public IProjectInfo GetProjectInfo(Project project)
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);
		//	var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
		//	return GetProjectInfoInternal(project, jobReaderWriter, subdivisionReaderWriter);
		//}

		private IProjectInfo GetProjectInfoInternal(ProjectRecord projectRec, JobReaderWriter jobReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter)
		{
			IProjectInfo result;

			var jobInfos = jobReaderWriter.GetJobInfos(projectRec.Id);

			if (jobInfos.Any())
			{
				var subdivisionIds = jobInfos.Select(j => j.SubDivisionId).Distinct();
				var minMapCoordsExponent = jobInfos.Min(x => x.MapCoordExponent);
				var minSamplePointDeltaExponent = subdivisonReaderWriter.GetMinExponent(subdivisionIds);

				// Greater of the date of the last updated job and the date when the project was last updated.
				var lastSaved = jobInfos.Max(x => x.DateCreated);
				var lastUpdated = projectRec.LastSavedUtc;
				if (lastSaved > lastUpdated)
				{
					lastUpdated = lastSaved;
				}

				var dateCreated = projectRec.DateCreated.ToLocalTime();

				result = new ProjectInfo(projectRec.Id, dateCreated, projectRec.Name, projectRec.Description, lastUpdated, jobInfos.Count(), minMapCoordsExponent, minSamplePointDeltaExponent);
			}
			else
			{
				result = new ProjectInfo(ObjectId.Empty, DateTime.MinValue, "name", null, DateTime.MinValue, 0, 0, 0);
			}

			return result;
		}

		#endregion

		#region ColorBandSet 

		public ColorBandSet? GetColorBandSet(string id)
		{
			Debug.WriteLine($"Retrieving ColorBandSet with Id: {id}.");

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = colorBandSetReaderWriter.Get(new ObjectId(id));

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);
			return result;
		}

		public ColorBandSet CreateColorBandSet(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);
			var id = colorBandSetReaderWriter.Insert(colorBandSetRecord);
			colorBandSetRecord = colorBandSetReaderWriter.Get(id);

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);

			Debug.Assert(id == result?.Id, "ColorBandSet result has Id different from the one on file.");

			return result;
		}

		//public void UpdateColorBandSetParentId(ObjectId colorBandSetId, ObjectId? parentId)
		//{
		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		//	colorBandSetReaderWriter.UpdateParentId(colorBandSetId, parentId);
		//}

		//public void UpdateColorBandSetProjectId(ObjectId colorBandSetId, ObjectId projectId)
		//{
		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		//	colorBandSetReaderWriter.UpdateProjectId(colorBandSetId, projectId);
		//}

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

		public IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId)
		{
			var result = new List<ColorBandSet>();

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecords = colorBandSetReaderWriter.GetColorBandSetsForProject(projectId);

			foreach (var colorBandSetRecord in colorBandSetRecords)
			{
				var colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);
				result.Add(colorBandSet);
			}

			return result;
		}

		//public DateTime GetProjectCbSetsLastSaveTime(ObjectId projectId)
		//{
		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		//	var result = colorBandSetReaderWriter.GetLastSaveTime(projectId);

		//	return result;
		//}

		#endregion

		#region Job

		public Job GetJob(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var job = GetJob(jobId, jobReaderWriter, subdivisonReaderWriter, colorBandSetReaderWriter, jobCache: null, colorBandSetCache: null);

			return job;
		}

		private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter, ColorBandSetReaderWriter colorBandSetReaderWriter,
			IDictionary<ObjectId, Job>? jobCache, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache)
		{
			if (jobCache != null && jobCache.TryGetValue(jobId, out var qJob))
			{
				//Debug.WriteLine($"Retrieving Job object for JobId: {jobId} from the cache.");
				return qJob;
			}

			//Debug.WriteLine($"Retrieving Job object for JobId: {jobId} from the data base.");
			var jobRecord = jobReaderWriter.Get(jobId);

			if (jobRecord is null)
			{
				throw new KeyNotFoundException($"Could not find a job with jobId = {jobId}.");
			}

			var subdivisionRecord = subdivisonReaderWriter.Get(jobRecord.SubDivisionId);
			var colorBandSet = GetColorBandSet(jobRecord, colorBandSetReaderWriter, colorBandSetCache, out var isCacheHit);

			if (jobRecord.MSetInfo.MapCalcSettings.TargetIterations != colorBandSet.HighCutOff)
			{
				Debug.WriteLine($"WARNING: Job's ColorMap HighCutOff doesn't match the TargetIterations.");
			}

			var job = new Job(
				id: jobId,
				parentJobId: jobRecord.ParentJobId,
				isPreferredChild: jobRecord.IsPreferredChild,
				projectId: jobRecord.ProjectId,
				subdivision: _mSetRecordMapper.MapFrom(subdivisionRecord),
				label: jobRecord.Label,
				transformType: _mSetRecordMapper.MapFromTransformType(jobRecord.TransformType),
				newArea: new RectangleInt(_mSetRecordMapper.MapFrom(jobRecord.NewAreaPosition), _mSetRecordMapper.MapFrom(jobRecord.NewAreaSize)),
				mSetInfo: _mSetRecordMapper.MapFrom(jobRecord.MSetInfo),
				colorBandSet: colorBandSet,
				canvasSizeInBlocks: _mSetRecordMapper.MapFrom(jobRecord.CanvasSizeInBlocks),
				mapBlockOffset: _mSetRecordMapper.MapFrom(jobRecord.MapBlockOffset),
				canvasControlOffset: _mSetRecordMapper.MapFrom(jobRecord.CanvasControlOffset),
				jobRecord.LastSaved
				);

			jobCache?.Add(job.Id, job);

			if (!isCacheHit)
			{
				colorBandSetCache?.Add(colorBandSet.Id, colorBandSet);
			}

			return job;
		}

		private ColorBandSet GetColorBandSet(JobRecord jobRecord, ColorBandSetReaderWriter colorBandSetReaderWriter, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache, out bool isCacheHit)
		{
			if (colorBandSetCache != null && colorBandSetCache.TryGetValue(jobRecord.ColorBandSetId, out var colorBandSet))
			{
				isCacheHit = true;
				return colorBandSet;
			}

			isCacheHit = false;
			var colorBandSetRecord = colorBandSetReaderWriter.Get(jobRecord.ColorBandSetId);
			if (colorBandSetRecord == null)
			{
				Debug.WriteLine($"The colorBandSetRecord is null for the CbsId: {jobRecord.ColorBandSetId}, job : {jobRecord.Id}, of Project: {jobRecord.ProjectId} .");
			}

			colorBandSet = colorBandSetRecord == null ? new ColorBandSet() : _mSetRecordMapper.MapFrom(colorBandSetRecord);

			return colorBandSet;
		}

		public Job InsertJob(Job job)
		{
			job.LastSavedUtc = DateTime.UtcNow;
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);

			jobRecord.LastSaved = DateTime.UtcNow;
			var id = jobReaderWriter.Insert(jobRecord);

			var updatedJob = GetJob(id);

			return updatedJob;
		}

		public void UpdateJobsParent(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.UpdateJobsParent(job.Id, job.ParentJobId, job.IsPreferredChild);
			job.LastSavedUtc = DateTime.UtcNow;
		}

		public void UpdateJobDetails(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);
			jobReaderWriter.UpdateJobDetails(jobRecord);
			job.LastSavedUtc = DateTime.UtcNow;
		}

		public void UpdateJobsProject(ObjectId jobId, ObjectId projectId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.UpdateJobsProject(jobId, projectId);
		}

		public long DeleteJob(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			var result = jobReaderWriter.Delete(jobId);
			return result ?? 0;
		}

		public DateTime GetProjectJobsLastSaveTime(ObjectId projectId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetLastSaveTime(projectId);

			return result;
		}

		public IEnumerable<Job> GetAllJobsForProject(ObjectId projectId)
		{
			return GetAllJobsForProject(projectId, colorBandSetCache: null);
		}

		public IEnumerable<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets)
		{
			var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(colorBandSets.Select(x => new KeyValuePair<ObjectId, ColorBandSet>(x.Id, x)));
			var result = GetAllJobsForProject(projectId, colorBandSetCache);

			return result;
		}

		public IEnumerable<Job> GetAllJobsForProject(ObjectId projectId, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache)
		{
			var result = new List<Job>();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var ids = jobReaderWriter.GetJobIds(projectId);

			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var jobCache = new Dictionary<ObjectId, Job>();

			foreach (var jobId in ids)
			{
				var job = GetJob(jobId, jobReaderWriter, subdivisonReaderWriter, colorBandSetReaderWriter, jobCache, colorBandSetCache);
				result.Add(job);
			}

			return result;
		}

		#endregion

		#region Active Job Schema Updates

		//		public int DeleteUnusedColorBandSets()
		//		{
		//			var result = 0;

		//			var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//			var colorBandSetIdsRefByJob = jobReaderWriter.GetAllReferencedColorBandSetIds().ToList();

		//			Debug.WriteLine($"\nRef by a Job\n");
		//			foreach (var x in colorBandSetIdsRefByJob)
		//			{
		//				Debug.WriteLine($"{x}");
		//			}

		//			var colorBandSetIds = new List<ObjectId>(colorBandSetIdsRefByJob);

		//			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

		//			//var referencedByProjectNotByJob = 0;

		//			var referencedByProjectNotByJob = new List<ObjectId>();

		//			var projectRecords = projectReaderWriter.GetAll();
		//			foreach(var projectRec in projectRecords)
		//			{
		//				var colorBandSetRecords = colorBandSetReaderWriter.GetColorBandSetsForProject(projectRec.Id);
		//				foreach (var colorBandSetRec in colorBandSetRecords)
		//				{
		//					if (!colorBandSetIds.Contains(colorBandSetRec.Id))
		//					{
		//						colorBandSetIds.Add(colorBandSetRec.Id);
		//						referencedByProjectNotByJob.Add(colorBandSetRec.Id);
		//					}
		//				}
		//			}

		//			referencedByProjectNotByJob = referencedByProjectNotByJob.Distinct().ToList();

		//			Debug.WriteLine($"\nRef by Project, but not by job\n");
		//			foreach (var x in referencedByProjectNotByJob)
		//			{
		//				Debug.WriteLine($"{x}");
		//			}

		//			var allCbsIds = colorBandSetReaderWriter.GetAll().Select(x => x.Id);

		//			var unReferenced = new List<ObjectId>();

		//			foreach (var cbsId in allCbsIds)
		//			{
		//				if (!colorBandSetIds.Contains(cbsId))
		//				{
		//					unReferenced.Add(cbsId);
		//				}
		//			}

		//			Debug.WriteLine($"\nUnreferenced\n");
		//			foreach (var x in unReferenced)
		//			{
		//				Debug.WriteLine($"{x}");
		//				//_ = colorBandSetReaderWriter.Delete(x);
		//			}


		//			return result;
		//		}

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

		//public void AddColorBandSetIdToAllJobs()
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	var allProjectRecs = projectReaderWriter.GetAll();

		//	foreach (var projectRecord in allProjectRecs)
		//	{
		//		jobReaderWriter.AddColorBandSetIdByProject(projectRecord.Id, projectRecord.CurrentColorBandSetId);
		//	}
		//}

		//public void AddIsPreferredChildToAllJobs()
		//{
		//	var jobReaderWriter = new JobReaderWriter(_dbProvider);

		//	jobReaderWriter.AddIsPreferredChildToAllJobs();
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

		public Subdivision InsertSubdivision(Subdivision subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var subId = subdivisionReaderWriter.Insert(subdivisionRecord);

			subdivisionRecord = subdivisionReaderWriter.Get(subId);
			var result = _mSetRecordMapper.MapFrom(subdivisionRecord);

			return result;
		}

		public bool DeleteSubdivision(Subdivision subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			var subsDeleted = subdivisionReaderWriter.Delete(subdivision.Id);

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			_ = mapSectionReaderWriter.DeleteAllWithSubId(subdivision.Id);

			return subsDeleted.HasValue && subsDeleted.Value > 0;
		}

		#endregion
	}
}
