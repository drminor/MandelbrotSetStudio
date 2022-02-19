using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetRepo
{
	public class ProjectAdapter
	{
		public const string ROOT_JOB_LABEL = "Root";

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

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subdivisionReaderWriter.CreateCollection();

			var mapSectionReaderWriter = new MapSectionReaderWriter(_dbProvider);
			mapSectionReaderWriter.CreateCollection();
		}

		#endregion

		#region Project

		public Project GetProject(ObjectId projectId)
		{
			Debug.WriteLine($"Retrieving Project object for ProjectId: {projectId}.");

			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(projectId);

			var project = _mSetRecordMapper.MapFrom(projectRecord);

			return project;
		}

		public Project GetOrCreateProject(string name)
		{
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			var projectRecord = projectReaderWriter.Get(name);
			if (projectRecord is null)
			{
				var projectId = projectReaderWriter.Insert(new ProjectRecord(name));
				projectRecord = projectReaderWriter.Get(projectId);
			}

			var result = _mSetRecordMapper.MapFrom(projectRecord);

			return result;
		}

		/// <summary>
		/// Inserts the project record if it does not exist on the database.
		/// </summary>
		/// <param name="project"></param>
		public Project InsertProject(Project project, bool overwrite)
		{
			ProjectRecord projectRecord;

			var projectName = project.Name;
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			projectRecord = projectReaderWriter.Get(projectName);

			if (projectRecord == null)
			{
				projectRecord = _mSetRecordMapper.MapTo(project);
				_ = projectReaderWriter.Insert(projectRecord);
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already exists.");
				}
				else
				{
					var projectId = projectRecord.Id;

					var jobReaderWriter = new JobReaderWriter(_dbProvider);

					var jobIds = jobReaderWriter.GetJobIds(projectId);

					foreach (var jobId in jobIds)
					{
						var dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					_ = projectReaderWriter.Delete(projectId);

					projectRecord = _mSetRecordMapper.MapTo(project);
					_ = projectReaderWriter.Insert(projectRecord);
				}
			}

			project = _mSetRecordMapper.MapFrom(projectRecord);

			return project;
		}

		#endregion

		#region Job

		public Job GetJob(ObjectId jobId)
		{
			Debug.WriteLine($"Retrieving Job object for JobId: {jobId}.");
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);
			var subdivisonReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var job = GetJob(jobId, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter);

			return job;
		}

		private Job GetJob(ObjectId jobId, JobReaderWriter jobReaderWriter, ProjectReaderWriter projectReaderWriter, SubdivisonReaderWriter subdivisonReaderWriter)
		{
			var jobRecord = jobReaderWriter.Get(jobId);

			if (jobRecord is null)
			{
				throw new KeyNotFoundException($"Could not find a job with jobId = {jobId}.");
			}

			Job? parentJob;

			if (jobRecord.ParentJobId.HasValue)
			{
				Debug.WriteLine($"Retrieving Job object for parent JobId: {jobRecord.ParentJobId}.");
				parentJob = GetJob(jobRecord.ParentJobId.Value, jobReaderWriter, projectReaderWriter, subdivisonReaderWriter);
			}
			else
			{
				parentJob = null;
			}

			var projectRecord = projectReaderWriter.Get(jobRecord.ProjectId);
			var project = _mSetRecordMapper.MapFrom(projectRecord);

			var subdivisionRecord = subdivisonReaderWriter.Get(jobRecord.SubDivisionId);
			var subdivision = _mSetRecordMapper.MapFrom(subdivisionRecord);

			var mSetInfo = _mSetRecordMapper.MapFrom(jobRecord.MSetInfo);

			var job = new Job(
				id: jobId, 
				parentJob: parentJob, 
				project: project, 
				subdivision: subdivision, 
				label: jobRecord.Label,
				transformType: Enum.Parse<TransformType>(jobRecord.TransformType.ToString()),
				newArea: new SizeInt(jobRecord.NewAreaWidth, jobRecord.NewAreaHeight),
				mSetInfo: mSetInfo, 
				canvasSizeInBlocks: new SizeInt(jobRecord.CanvasSizeInBlocksWidth, jobRecord.CanvasSizeInBlocksHeight), 
				mapBlockOffset: new SizeInt(jobRecord.MapBlockOffsetWidth, jobRecord.MapBlockOffsetHeight), 
				canvasControlOffset: new SizeDbl(jobRecord.CanvasControlOffsetWidth, jobRecord.CanvasControlOffsetHeight)
				);

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

		public Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			var deleteCount = jobReaderWriter.Delete(jobId);
			var result = new Tuple<long, long>(deleteCount ?? 0, 0);
			return result;
		}

		public DateTime GetProjectLastSaveTime(ObjectId projectId)
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

			foreach(var id in ids)
			{
				var job = GetJob(id);
				result.Add(job);
			}

			return result;
		}

		#endregion

		#region Subdivision

		public bool TryGetSubdivision(RPoint position, RSize samplePointDelta, SizeInt blockSize, out Subdivision? subdivision)
		{
			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var posReduced = Reducer.Reduce(position);
			var posDto = _dtoMapper.MapTo(posReduced);

			var samplePointDeltaReduced = Reducer.Reduce(samplePointDelta);
			var samplePointDeltaDto = _dtoMapper.MapTo(samplePointDeltaReduced);

			var matches = subdivisionReaderWriter.Get(posDto, samplePointDeltaDto, blockSize);

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

		//public Subdivision GetOrCreateSubdivision(RPoint position, RSize samplePointDelta, SizeInt blockSize, out bool created)
		//{
		//	if (TryGetSubdivision(position, samplePointDelta, blockSize, out var subdivision))
		//	{
		//		created = false;
		//		return subdivision ?? throw new InvalidOperationException("Subdivision is null.");
		//	}
		//	else
		//	{
		//		var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

		//		var subdivisionNotSaved = new Subdivision(ObjectId.GenerateNewId(), position, samplePointDelta, blockSize);
		//		subdivision = InsertSubdivision(subdivisionNotSaved, subdivisionReaderWriter);

		//		created = true;
		//		return subdivision;
		//	}
		//}

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
