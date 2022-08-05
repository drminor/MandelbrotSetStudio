using MongoDB.Bson;
using MSS.Common;
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

		#region Constructor

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
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


		public void WarmUp()
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var x = projectReaderWriter.Collection.Indexes;
			_ = x.List();
		}

		#endregion

		#region Project

		//public bool TryGetProject(ObjectId projectId, [MaybeNullWhen(false)] out Project project)
		//{
		//	//Debug.WriteLine($"Retrieving Project object for Project with name: {name}.");

		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

		//	if (projectReaderWriter.TryGet(projectId, out var projectRecord))
		//	{
		//		var colorBandSets = GetColorBandSetsForProject(projectRecord.Id);
		//		var jobs = GetAllJobsForProject(projectRecord.Id, colorBandSets);
		//		//colorBandSets = GetColorBandSetsForProject(projectRecord.Id); // TODO: Remove this 

		//		project = AssembleProject(projectRecord, jobs, colorBandSets, projectRecord.LastSavedUtc);
		//		return project != null;
		//	}
		//	else
		//	{
		//		project = null;
		//		return false;
		//	}
		//}

		public bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project)
		{
			//Debug.WriteLine($"Retrieving Project object for Project with name: {name}.");

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			if (projectReaderWriter.TryGet(name, out var projectRecord))
			{
				var colorBandSets = GetColorBandSetsForProject(projectRecord.Id);
				var jobs = GetAllJobsForProject(projectRecord.Id, colorBandSets);
				//colorBandSets = GetColorBandSetsForProject(projectRecord.Id); // TODO: Remove this 

				project = AssembleProject(projectRecord, jobs, colorBandSets, projectRecord.LastSavedUtc, projectRecord.LastAccessedUtc);
				return project != null;
			}
			else
			{
				project = null;
				return false;
			}
		}

		//public Project? CreateProject(string name, string? description, ObjectId currentJobId)
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

		//	if (!projectReaderWriter.ExistsWithName(name))
		//	{
		//		var projectRecord = new ProjectRecord(name, description, currentJobId, DateTime.UtcNow);

		//		var projectId = projectReaderWriter.Insert(projectRecord);
		//		projectRecord = projectReaderWriter.Get(projectId);

		//		if (projectRecord != null)
		//		{
		//			var colorBandSets = GetColorBandSetsForProject(projectRecord.Id);
		//			var jobs = GetAllJobsForProject(projectRecord.Id, colorBandSets);
		//			//colorBandSets = GetColorBandSetsForProject(projectRecord.Id); // TODO: Remove this 

		//			var result = AssembleProject(projectRecord, jobs, colorBandSets, DateTime.MinValue);

		//			return result;
		//		}
		//		else
		//		{
		//			throw new InvalidOperationException($"Could not retrieve newly created project record with id: {projectId}.");
		//		}
		//	}
		//	else
		//	{
		//		throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
		//	}
		//}

		public Project? CreateProject(string name, string? description, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			if (!projectReaderWriter.ProjectExists(name))
			{
				var projectRecord = new ProjectRecord(name, description, jobs.First().Id, DateTime.UtcNow);

				var projectId = projectReaderWriter.Insert(projectRecord);
				projectRecord = projectReaderWriter.Get(projectId);

				foreach (var job in jobs)
				{
					job.ProjectId = projectId;
				}

				foreach (var cbs in colorBandSets)
				{
					cbs.ProjectId = projectId;
				}

				var result = AssembleProject(projectRecord, jobs, colorBandSets, DateTime.MinValue, DateTime.MinValue);

				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}
		}

		private Project? AssembleProject(ProjectRecord? projectRecord, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Project? result;
			if (projectRecord == null || jobs.Count == 0 || !colorBandSets.Any())
			{
				result = null;
			}
			else
			{
				result = new Project(projectRecord.Id, projectRecord.Name, projectRecord.Description, jobs, colorBandSets, projectRecord.CurrentJobId, lastSavedUtc, lastAccessedUtc);

			}
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

		public bool DeleteProject(ObjectId projectId)
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

			var numberDeleted = projectReaderWriter.Delete(projectId);

			return numberDeleted == 1;
		}

		public bool ProjectExists(string name)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var result = projectReaderWriter.ProjectExists(name);

			return result;
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
			var dateCreated = projectRec.DateCreated.ToLocalTime();

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

				var numberOfFirstLevelChildJobs = jobInfos.Count(x => !x.ParentJobId.HasValue);

				var jobCount = numberOfFirstLevelChildJobs > 1 ? jobInfos.Count() : -1 * jobInfos.Count();

				result = new ProjectInfo(projectRec.Id, dateCreated, projectRec.Name, projectRec.Description, lastUpdated, jobCount, minMapCoordsExponent, minSamplePointDeltaExponent);
			}
			else
			{
				result = new ProjectInfo(projectRec.Id, dateCreated, projectRec.Name, projectRec.Description, DateTime.MinValue, 0, 0, 0);
			}

			return result;
		}

		#endregion

		#region ColorBandSet 

		public ColorBandSet? GetColorBandSet(string id)
		{
			var result = GetColorBandSet(new ObjectId(id), new ColorBandSetReaderWriter(_dbProvider));
			return result;
		}

		private ColorBandSet? GetColorBandSet(ObjectId id, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			Debug.WriteLine($"Retrieving ColorBandSet with Id: {id}.");

			var colorBandSetRecord = colorBandSetReaderWriter.Get(id);

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);
			return result;
		}

		public void InsertColorBandSet(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			InsertCbs(colorBandSet, colorBandSetReaderWriter);
		}

		private void InsertCbs(ColorBandSet colorBandSet, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);

			_ = colorBandSetReaderWriter.Insert(colorBandSetRecord);
			colorBandSet.LastSavedUtc = DateTime.UtcNow;
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

		public IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = GetColorBandSetsForProject(projectId, colorBandSetReaderWriter);

			return result;
		}

		private IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var result = new List<ColorBandSet>();

			var colorBandSetRecords = colorBandSetReaderWriter.GetColorBandSetsForProject(projectId);

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

		#endregion

		#region Job

		public List<ObjectId> GetAllJobIdsForPoster(ObjectId posterId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetJobIds(posterId).ToList();

			return result;
		}

		public List<Job> GetAllJobsForPoster(ObjectId poster, IEnumerable<ColorBandSet> colorBandSets)
		{
			var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(colorBandSets.Select(x => new KeyValuePair<ObjectId, ColorBandSet>(x.Id, x)));
			var result = GetAllJobsForProject(poster, colorBandSetCache);

			return result;
		}

		public List<ObjectId> GetAllJobIdsForProject(ObjectId projectId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var result = jobReaderWriter.GetJobIds(projectId).ToList();

			return result;
		}

		public List<Job> GetAllJobsForProject(ObjectId projectId, IEnumerable<ColorBandSet> colorBandSets)
		{
			var colorBandSetCache = new Dictionary<ObjectId, ColorBandSet>(colorBandSets.Select(x => new KeyValuePair<ObjectId, ColorBandSet>(x.Id, x)));
			var result = GetAllJobsForProject(projectId, colorBandSetCache);

			return result;
		}

		private List<Job> GetAllJobsForProject(ObjectId projectId, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache)
		{
			var result = new List<Job>();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var jobCache = new Dictionary<ObjectId, Job>();

			var ids = jobReaderWriter.GetJobIds(projectId);
			foreach (var jobId in ids)
			{
				var job = GetJob(jobId, jobReaderWriter, colorBandSetReaderWriter, jobCache, colorBandSetCache);
				result.Add(job);
			}

			return result;
		}

		public Job GetJob(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var job = GetJob(jobId, jobReaderWriter, colorBandSetReaderWriter, jobCache: null, colorBandSetCache: null);

			return job;
		}

		private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter, ColorBandSetReaderWriter colorBandSetReaderWriter,
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

			var job = new Job(
				id: jobId,
				parentJobId: jobRecord.ParentJobId,
				//isAlternatePathHead: jobRecord.IsAlternatePathHead,
				projectId: jobRecord.ProjectId,
				label: jobRecord.Label,
				transformType: _mSetRecordMapper.MapFromTransformType(jobRecord.TransformType),
				newArea: new RectangleInt(_mSetRecordMapper.MapFrom(jobRecord.NewAreaPosition), _mSetRecordMapper.MapFrom(jobRecord.NewAreaSize)),

				mapAreaInfo: _mSetRecordMapper.MapFrom(jobRecord.MapAreaInfoRecord),
				canvasSizeInBlocks: _mSetRecordMapper.MapFrom(jobRecord.CanvasSizeInBlocks),
				colorBandSetId: jobRecord.ColorBandSetId,

				mapCalcSettings: jobRecord.MapCalcSettings,
				lastSaved: jobRecord.LastSaved
				)
			{
				LastAccessedUtc = jobRecord.LastAccessedUtc,
				IterationUpdates = jobRecord.IterationUpdates,
				ColorMapUpdates = jobRecord.ColorMapUpdates
			};

			var colorBandSet = GetColorBandSet(job, colorBandSetReaderWriter, colorBandSetCache, out var isCacheHit);

			ObjectId cbsId;
			if (colorBandSet != null)
			{
				cbsId = colorBandSet.Id;

				if (!isCacheHit)
				{
					colorBandSetCache?.Add(colorBandSet.Id, colorBandSet);
					jobReaderWriter.UpdateJobsColorBandSet(jobId, colorBandSet.HighCutoff, cbsId);
				}

				if (cbsId != jobRecord.ColorBandSetId)
				{
					jobReaderWriter.UpdateJobsColorBandSet(jobId, colorBandSet.HighCutoff, cbsId);
				}
			}
			else
			{
				cbsId = jobRecord.ColorBandSetId;
			}

			job.ColorBandSetId = cbsId;

			jobCache?.Add(job.Id, job);

			return job;
		}

		private ColorBandSet? GetColorBandSet(Job job, ColorBandSetReaderWriter colorBandSetReaderWriter, IDictionary<ObjectId, ColorBandSet>? colorBandSetCache, out bool isCacheHit)
		{
			ColorBandSet? result;

			var colorBandSetId = job.ColorBandSetId;

			if (colorBandSetCache != null && colorBandSetCache.TryGetValue(colorBandSetId, out var colorBandSet))
			{
				isCacheHit = true;

				result = colorBandSet;
			}
			else
			{
				isCacheHit = false;
				var colorBandSetRecord = colorBandSetReaderWriter.Get(job.ColorBandSetId);
				if (colorBandSetRecord == null)
				{
					Debug.WriteLine($"The colorBandSetRecord is null for the CbsId: {colorBandSetId}, job : {job.Id}, of Project: {job.ProjectId} .");
				}

				if (colorBandSetRecord == null)
				{
					result = null;
				}
				else
				{
					colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);

					if (colorBandSet.ProjectId != job.ProjectId)
					{
						result = GetUpdatedCbsForProject(colorBandSet, job.ProjectId, colorBandSetReaderWriter);
					}
					else
					{
						result = colorBandSet;
					}
				}
			}

			//if (result != null && colorBandSetCache != null && result.HighCutoff != job.MapCalcSettings.TargetIterations)
			//{
			//	var targetIterations = job.MapCalcSettings.TargetIterations;
			//	var averageTargetIterations = GetIterationsForJob(result, job);
			//	var newTargetIterations = (int)Math.Round(averageTargetIterations);

			//	Debug.WriteLine($"WARNING: Job's ColorMap HighCutoff: {result.HighCutoff} doesn't match the TargetIterations: {targetIterations}. Job has an average TargetIteration of {averageTargetIterations}.");

			//	result = GetUpdatedCbsWithTargetIteration(result, newTargetIterations, colorBandSetCache.Values, colorBandSetReaderWriter);
			//}

			return result;
		}

		private double GetIterationsForJob(ColorBandSet colorBandSet, Job job)
		{
			var targetIterations = job.MapCalcSettings.TargetIterations;
			var highCutoff = colorBandSet.HighCutoff;

			var jobHelper = new MapSectionHelper();

			var mapSectionRequests = jobHelper.CreateSectionRequests(job.Id.ToString(), JobOwnerType.Project, job.MapAreaInfo, job.MapCalcSettings);

			// TODO: Retrieve each MapSection record from the database and use the value of the Target Iterations, actually computed.

			var averageTargetIterations = mapSectionRequests.Average(x => x.MapCalcSettings.TargetIterations);

			return averageTargetIterations;
		}

		private ColorBandSet GetUpdatedCbsWithTargetIteration(ColorBandSet colorBandSet, int targetIterations, IEnumerable<ColorBandSet> cacheValues, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var updatedCbs = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations, cacheValues);
			InsertCbs(updatedCbs, colorBandSetReaderWriter);

			return updatedCbs;
		}

		private ColorBandSet GetUpdatedCbsForProject(ColorBandSet colorBandSet, ObjectId projectId, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var updatedCbs = colorBandSet.CreateNewCopy();
			updatedCbs.ProjectId = projectId;
			InsertCbs(updatedCbs, colorBandSetReaderWriter);

			return updatedCbs;
		}

		public void InsertJob(Job job)
		{
			var dt = job.DateCreated;

			job.LastSavedUtc = DateTime.UtcNow;
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);

			jobRecord.LastSaved = DateTime.UtcNow;
			_ = jobReaderWriter.Insert(jobRecord);
			job.LastSavedUtc = DateTime.UtcNow;

			var dt2 = job.DateCreated;

			var dur = dt2 - dt;

			Debug.WriteLine($"The job date created has changeed by {dur}.");
		}

		public void UpdateJobDetails(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);
			jobReaderWriter.UpdateJobDetails(jobRecord);
			job.LastSavedUtc = DateTime.UtcNow;
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

		#region Poster

		public List<Poster> GetAllPosters()
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var posterRecords = posterReaderWriter.GetAll();

			var result = posterRecords.Select(x => BuildPoster(x, colorBandSetReaderWriter)).ToList();

			return result;
		}

		public bool TryGetPoster(ObjectId posterId, [MaybeNullWhen(false)] out Poster poster)
		{
			//Debug.WriteLine($"Retrieving Poster object with Id: {posterId}.");
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (posterReaderWriter.TryGet(posterId, out var posterRecord))
			{
				var colorBandSets = GetColorBandSetsForProject(posterId);
				var jobs = GetAllJobsForProject(posterId, colorBandSets);

				poster = AssemblePoster(posterRecord, jobs, colorBandSets, posterRecord.LastSavedUtc);
			}
			else
			{
				poster = null;
			}

			return poster != null;
		}

		public bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster)
		{
			//Debug.WriteLine($"Retrieving Poster object with name: {name}.");
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (posterReaderWriter.TryGet(name, out var posterRecord))
			{
				var posterId = posterRecord.Id;

				var colorBandSets = GetColorBandSetsForProject(posterId);
				var jobs = GetAllJobsForProject(posterId, colorBandSets);

				poster = AssemblePoster(posterRecord, jobs, colorBandSets, posterRecord.LastSavedUtc);
			}
			else
			{
				poster = null;
			}

			return poster != null;
		}

		private Poster BuildPoster(PosterRecord target, ColorBandSetReaderWriter colorBandSetReaderWriter)
		{
			var posterId = target.Id;
			var colorBandSets = GetColorBandSetsForProject(posterId, colorBandSetReaderWriter);
			var jobs = GetAllJobsForPoster(posterId, colorBandSets);

			var result = new Poster(
				id: target.Id,
				name: target.Name,
				description: target.Description,
				sourceJobId: target.SourceJobId,
				jobs: jobs,
				colorBandSets: colorBandSets,
				currentJobId: target.CurrentJobId,
				displayPosition: _mSetRecordMapper.MapFrom(target.DisplayPosition),
				displayZoom: target.DisplayZoom,
				dateCreatedUtc: target.DateCreatedUtc,
				lastSavedUtc: target.LastSavedUtc,
				lastAccessedUtc: target.LastAccessedUtc

				);

			return result;
		}

		public Poster? CreatePoster(string name, string? description, ObjectId sourceJobId, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (!posterReaderWriter.PosterExists(name))
			{
				var posterRecord = new PosterRecord(name, description, sourceJobId, jobs.First().Id, 
					DisplayPosition: new VectorIntRecord(0,0), 
					DisplayZoom: 0,
					DateCreatedUtc: DateTime.UtcNow,
					LastSavedUtc: DateTime.UtcNow,
					LastAccessedUtc: DateTime.UtcNow);

				var posterId = posterReaderWriter.Insert(posterRecord);

				foreach (var job in jobs)
				{
					job.ProjectId = posterId;
				}

				foreach (var cbs in colorBandSets)
				{
					cbs.ProjectId = posterId;
				}

				var result = AssemblePoster(posterRecord, jobs, colorBandSets, DateTime.MinValue);

				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}
		}

		private Poster? AssemblePoster(PosterRecord? posterRecord, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, DateTime lastSavedUtc)
		{
			Poster? result;
			if (posterRecord == null || jobs.Count == 0 || !colorBandSets.Any())
			{
				result = null;
			}
			else
			{
				var displayPosition = _mSetRecordMapper.MapFrom(posterRecord.DisplayPosition);
				result = new Poster(posterRecord.Id, posterRecord.Name, posterRecord.Description, posterRecord.SourceJobId, jobs, colorBandSets,
					posterRecord.CurrentJobId, displayPosition, posterRecord.DisplayZoom, DateTime.UtcNow, lastSavedUtc, DateTime.MinValue);

			}
			return result;
		}

		public void CreatePoster(Poster poster)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);

			if (!posterReaderWriter.PosterExists(poster.Name))
			{
				var posterRecord = _mSetRecordMapper.MapTo(poster);
				var posterRecordId = posterReaderWriter.Insert(posterRecord);

				Debug.Assert(poster.Id == posterRecordId);
			}
			else
			{
				throw new InvalidOperationException($"Cannot create poster with name: {poster.Name}, a poster with that name already exists.");
			}
		}

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

		public bool DeletePoster(ObjectId posterId)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetJobIds(posterId);

			foreach (var jobId in jobIds)
			{
				_ = DeleteJob(jobId, jobReaderWriter);
			}

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var cbsIds = colorBandSetReaderWriter.GetColorBandSetIdsForProject(posterId);

			foreach (var colorBandSetId in cbsIds)
			{
				_ = colorBandSetReaderWriter.Delete(colorBandSetId);
			}

			var numberDeleted = posterReaderWriter.Delete(posterId);

			return numberDeleted == 1;
		}

		public bool PosterExists(string name)
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var result = posterReaderWriter.PosterExists(name);

			return result;
		}

		#endregion

		#region PosterInfo

		public IEnumerable<IPosterInfo> GetAllPosterInfos()
		{
			var posterReaderWriter = new PosterReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var allPosterRecords = posterReaderWriter.GetAll();

			var result = allPosterRecords.Select(x => GetPosterInfoInternal(x, jobReaderWriter));

			return result;
		}

		private IPosterInfo GetPosterInfoInternal(PosterRecord posterRec, JobReaderWriter jobReaderWriter)
		{
			Debug.WriteLine($"Retrieving PosterInfo. Poster: {posterRec.Id}, Current Job: {posterRec.CurrentJobId}");
			var jobRec = jobReaderWriter.Get(posterRec.CurrentJobId);
			var lastSavedUtc = jobRec != null ? jobRec.LastSaved > posterRec.LastSavedUtc ? jobRec.LastSaved : posterRec.LastSavedUtc : posterRec.LastSavedUtc;
			var size = jobRec != null ? _mSetRecordMapper.MapFrom(jobRec.MapAreaInfoRecord.CanvasSize) : new SizeInt();

			var result = new PosterInfo(posterRec.Id, posterRec.Name, posterRec.Description, posterRec.CurrentJobId, size, posterRec.DateCreatedUtc, lastSavedUtc, posterRec.LastAccessedUtc);
			return result;
		}

		#endregion

		#region Active Job Schema Updates

		public void DoSchemaUpdates()
		{

		}

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

		public int DeleteUnusedColorBandSets()
		{
			var result = 0;

			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var colorBandSetIdsRefByJob = jobReaderWriter.GetAllReferencedColorBandSetIds().ToList();

			//Debug.WriteLine($"\nRef by a Job\n");
			//foreach (var x in colorBandSetIdsRefByJob)
			//{
			//	Debug.WriteLine($"{x}");
			//}

			var colorBandSetIds = new List<ObjectId>(colorBandSetIdsRefByJob);

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			//var referencedByProjectNotByJob = 0;

			var referencedByProjectNotByJob = new List<ObjectId>();

			var projectRecords = projectReaderWriter.GetAll();
			foreach (var projectRec in projectRecords)
			{
				var colorBandSetRecords = colorBandSetReaderWriter.GetColorBandSetsForProject(projectRec.Id);
				foreach (var colorBandSetRec in colorBandSetRecords)
				{
					if (!colorBandSetIds.Contains(colorBandSetRec.Id))
					{
						//colorBandSetIds.Add(colorBandSetRec.Id);
						referencedByProjectNotByJob.Add(colorBandSetRec.Id);
					}
				}
			}

			referencedByProjectNotByJob = referencedByProjectNotByJob.Distinct().ToList();

			//Debug.WriteLine($"\nRef by Project, but not by job\n");
			//foreach (var x in referencedByProjectNotByJob)
			//{
			//	Debug.WriteLine($"{x}");
			//}

			var allCbsIds = colorBandSetReaderWriter.GetAll().Select(x => x.Id);

			var unReferenced = new List<ObjectId>();

			foreach (var cbsId in allCbsIds)
			{
				if (!colorBandSetIds.Contains(cbsId))
				{
					unReferenced.Add(cbsId);
				}
			}

			Debug.WriteLine($"\nUnreferenced\n");
			foreach (var x in referencedByProjectNotByJob)
			{
				//Debug.WriteLine($"{x}");
				_ = colorBandSetReaderWriter.Delete(x);
				result++;
			}

			return result;
		}

		#endregion

		#region Old Schema Updates

		//public long UpdateAllJobsToUseMapAreaInfoRec1()
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


		//public long UpdateAllJobsToUseMapAreaInfoRec2()
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
