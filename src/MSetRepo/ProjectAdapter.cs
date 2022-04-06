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
	public class ProjectAdapter
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

		public Project GetProject(ObjectId projectId)
		{
			//Debug.WriteLine($"Retrieving Project object for ProjectId: {projectId}.");

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var projectRecord = projectReaderWriter.Get(projectId);
			var project = _mSetRecordMapper.MapFrom(projectRecord);

			return project;
		}

		public bool TryGetProject(string name, [MaybeNullWhen(false)] out Project project)
		{
			//Debug.WriteLine($"Retrieving Project object for Project with name: {name}.");

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			if (projectReaderWriter.TryGet(name, out var projectRecord))
			{
				project = _mSetRecordMapper.MapFrom(projectRecord);
				return true;
			}
			else
			{
				project = null;
				return false;
			}
		}

		public Project CreateProject(string name, string? description, ObjectId currentColorBandSetId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(name);
			if (projectRecord is null)
			{
				var project = new Project(name, description, currentColorBandSetId);
				projectRecord = _mSetRecordMapper.MapTo(project);
				var projectId = projectReaderWriter.Insert(projectRecord);
				projectRecord = projectReaderWriter.Get(projectId);
			}
			else
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}

			var result = _mSetRecordMapper.MapFrom(projectRecord);

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

		public void UpdateProjectColorBandSetId(ObjectId projectId, ObjectId currentColorBandSetId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			projectReaderWriter.UpdateColorBandSetId(projectId, currentColorBandSetId);
		}

		//public void UpdateProjectColorBands(ObjectId projectId, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
		//{
		//	var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
		//	var colorBandSetIdsRaw = colorBandSetIds.Select(x => x.ToByteArray()).ToArray();
		//	var currentColorBandSetRecord = _mSetRecordMapper.MapTo(currentColorBandSet);
		//	projectReaderWriter.UpdateColorBands(projectId, colorBandSetIdsRaw, currentColorBandSetRecord);

		//	var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
		//	colorBandSetReaderWriter.UpdateName(currentColorBandSetRecord.Id, currentColorBandSetRecord.Name);
		//	colorBandSetReaderWriter.UpdateDescription(currentColorBandSetRecord.Id, currentColorBandSetRecord.Description);
		//	colorBandSetReaderWriter.UpdateVersionNumber(currentColorBandSetRecord.Id, currentColorBandSetRecord.VersionNumber);
		//	colorBandSetReaderWriter.UpdateColorBands(currentColorBandSetRecord.Id, currentColorBandSetRecord.ColorBandRecords);
		//}

		public void DeleteProject(ObjectId projectId)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var jobReaderWriter = new JobReaderWriter(_dbProvider);

			var jobIds = jobReaderWriter.GetJobIds(projectId);

			foreach (var jobId in jobIds)
			{
				_ = DeleteJob(jobId, jobReaderWriter);
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
			var result = allProjectRecords.Select(x => GetProjectInfoInternal(_mSetRecordMapper.MapFrom(x), jobReaderWriter, subdivisionReaderWriter));

			return result;
		}

		public IProjectInfo GetProjectInfo(Project project)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			return GetProjectInfoInternal(project, jobReaderWriter, subdivisionReaderWriter);
		}

		private IProjectInfo GetProjectInfoInternal(Project project, JobReaderWriter jobReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter)
		{
			IProjectInfo result;

			var jobInfos = jobReaderWriter.GetJobInfos(project.Id);

			if (jobInfos.Any())
			{
				var subdivisionIds = jobInfos.Select(j => j.SubDivisionId).Distinct();
				var lastSaved = jobInfos.Max(x => x.DateCreated);
				var minMapCoordsExponent = jobInfos.Min(x => x.MapCoordExponent);
				var minSamplePointDeltaExponent = subdivisonReaderWriter.GetMinExponent(subdivisionIds);

				result = new ProjectInfo(project, lastSaved, jobInfos.Count(), minMapCoordsExponent, minSamplePointDeltaExponent);
			}
			else
			{
				result = new ProjectInfo(project, DateTime.MinValue, 0, 0, 0);
			}

			return result;
		}

		#endregion

		#region ColorBandSet 

		public ColorBandSetRecord GetOrCreateColorBandSetRecord(ColorBandSet currentColorBandSet)
		{
			Debug.WriteLine($"Retrieving ColorBandSet with Id: {currentColorBandSet.Id}.");

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			if (!colorBandSetReaderWriter.TryGet(currentColorBandSet.Id, out var colorBandSetRecord))
			{
				colorBandSetRecord = _mSetRecordMapper.MapTo(currentColorBandSet);
				var id = colorBandSetReaderWriter.Insert(colorBandSetRecord);
				colorBandSetRecord = colorBandSetReaderWriter.Get(id);
			}

			return colorBandSetRecord;
		}

		public ColorBandSet? GetColorBandSet(string id)
		{
			Debug.WriteLine($"Retrieving ColorBandSet with Id: {id}.");

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = colorBandSetReaderWriter.Get(new ObjectId(id));

			var result = colorBandSetRecord == null ? null : _mSetRecordMapper.MapFrom(colorBandSetRecord);
			return result;
		}

		public bool TryGetColorBandSet(Guid colorBandSetSerialNumber, [MaybeNullWhen(false)] out ColorBandSet colorBandSet)
		{
			Debug.WriteLine($"Retrieving ColorBandSet object for Guid: {colorBandSetSerialNumber}.");

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = colorBandSetReaderWriter.TryGet(colorBandSetSerialNumber, out var colorBandSetRecord);

			colorBandSet = result ? _mSetRecordMapper.MapFrom(colorBandSetRecord) : null;

			return result;
		}

		public ColorBandSet CreateColorBandSet(ColorBandSet colorBandSet)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);
			var id = colorBandSetReaderWriter.Insert(colorBandSetRecord);
			colorBandSetRecord = colorBandSetReaderWriter.Get(id);

			var result = _mSetRecordMapper.MapFrom(colorBandSetRecord);

			return result;
		}

		public void UpdateColorBandSetParentId(ObjectId colorBandSetId, ObjectId? parentId)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			colorBandSetReaderWriter.UpdateParentId(colorBandSetId, parentId);

			//if (!_mSetRecordMapper.ColorBandSetCache.ContainsKey(colorBandSetId))
			//{
			//	_mSetRecordMapper.ColorBandSetCache.Remove(colorBandSetId);
			//}
		}

		public void UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			var colorBandSetRecord = _mSetRecordMapper.MapTo(colorBandSet);
			var id = colorBandSetRecord.Id;

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			//colorBandSetReaderWriter.UpdateName(id, colorBandSetRecord.Name);
			colorBandSetReaderWriter.UpdateDescription(id, colorBandSetRecord.Description);
			colorBandSetReaderWriter.UpdateColorBands(id, colorBandSetRecord.ColorBandRecords);

			//if (!_mSetRecordMapper.ColorBandSetCache.ContainsKey(id))
			//{
			//	_mSetRecordMapper.ColorBandSetCache.Add(id, colorBandSet);
			//}
			//else
			//{
			//	_mSetRecordMapper.ColorBandSetCache[id] = colorBandSet;
			//}
		}

		public IEnumerable<ColorBandSet> GetColorBandSetsForProject(ObjectId projectId)
		{
			var result = new List<ColorBandSet>();

			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var ids = colorBandSetReaderWriter.GetColorBandSetIds(projectId);

			foreach (var colorBandSetId in ids)
			{
				var colorBandSetRecord = colorBandSetReaderWriter.Get(colorBandSetId);
				var colorBandSet = _mSetRecordMapper.MapFrom(colorBandSetRecord);
				result.Add(colorBandSet);
			}

			return result;
		}

		public DateTime GetProjectCbSetsLastSaveTime(ObjectId projectId)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var result = colorBandSetReaderWriter.GetLastSaveTime(projectId);

			return result;
		}

		#endregion

		#region ColorBandSetInfo

		// TODO: Use Projection to reduce data retrieved when calling GetAllColorBandSetInfos.

		public IEnumerable<ColorBandSetInfo> GetAllColorBandSetInfos()
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);

			var allColorBandSetRecords = colorBandSetReaderWriter.GetAll();

			var result = allColorBandSetRecords.Select(x => new ColorBandSetInfo(x.Id, x.ParentId, x.DateCreated, x.ColorBandRecords.Length, x.Name, x.Description));

			return result;
		}

		public ColorBandSetInfo GetColorBandSetInfo(ObjectId id)
		{
			var colorBandSetReaderWriter = new ColorBandSetReaderWriter(_dbProvider);
			var cbsRecord = colorBandSetReaderWriter.Get(id);

			var result = new ColorBandSetInfo(cbsRecord.Id, cbsRecord.ParentId, cbsRecord.DateCreated, cbsRecord.ColorBandRecords.Length, cbsRecord.Name, cbsRecord.Description);

			return result;
		}

		#endregion

		#region Job

		public Job GetJob(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var job = GetJob(jobId, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter, jobCache: null);

			return job;
		}

		private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter, ProjectReaderWriter projectReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter, 
			IDictionary<ObjectId, Job>? jobCache)
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

			//Job? parentJob;

			//if (jobRecord.ParentJobId.HasValue)
			//{
			//	Debug.WriteLine($"Retrieving Job object for parent JobId: {jobRecord.ParentJobId}.");
			//	parentJob = GetJob(jobRecord.ParentJobId.Value, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter, jobCache);
			//}
			//else
			//{
			//	parentJob = null;
			//}

			var projectRecord = projectReaderWriter.Get(jobRecord.ProjectId);
			var project = _mSetRecordMapper.MapFrom(projectRecord);

			var subdivisionRecord = subdivisonReaderWriter.Get(jobRecord.SubDivisionId);
			var subdivision = _mSetRecordMapper.MapFrom(subdivisionRecord);

			var mSetInfo = _mSetRecordMapper.MapFrom(jobRecord.MSetInfo);

			var job = new Job(
				id: jobId,
				parentJobId: jobRecord.ParentJobId, 
				project: project, 
				subdivision: subdivision, 
				label: jobRecord.Label,
				transformType: Enum.Parse<TransformType>(jobRecord.TransformType.ToString()),
				newArea: new RectangleInt(_mSetRecordMapper.MapFrom(jobRecord.NewAreaPosition), _mSetRecordMapper.MapFrom(jobRecord.NewAreaSize)),
				mSetInfo: mSetInfo, 
				canvasSizeInBlocks: _mSetRecordMapper.MapFrom(jobRecord.CanvasSizeInBlocks), 
				mapBlockOffset: _mSetRecordMapper.MapFrom(jobRecord.MapBlockOffset), 
				canvasControlOffset: _mSetRecordMapper.MapFrom(jobRecord.CanvasControlOffset)
				);

			jobCache?.Add(job.Id, job);
			return job;
		}

		public Job InsertJob(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);
			var id = jobReaderWriter.Insert(jobRecord);

			var updatedJob = GetJob(id);

			return updatedJob;
		}

		public void UpdateJobsParent(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			jobReaderWriter.UpdateJobsParent(job.Id, job.ParentJobId);
		}

		public void UpdateJobDetalis(Job job)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = _mSetRecordMapper.MapTo(job);
			jobReaderWriter.UpdateJobDetails(jobRecord);
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

		public IEnumerable<Job> GetAllJobs(ObjectId projectId)
		{
			var result = new List<Job>();

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var ids = jobReaderWriter.GetJobIds(projectId);

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			var jobCache = new Dictionary<ObjectId, Job>();

			foreach (var jobId in ids)
			{
				var job = GetJob(jobId, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter, jobCache);
				result.Add(job);
			}

			return result;
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
