using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSetRepo
{
	public class ProjectAdapter
	{
		public const string ROOT_JOB_LABEL = "Root";

		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
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

			var job = new Job(jobId, parentJob, project, subdivision, jobRecord.Label, mSetInfo, new PointDbl(jobRecord.CanvasOffsetX, jobRecord.CanvasOffsetY));

			return job;
		}

		public ObjectId CreateJob(Project project, MSetInfo mSetInfo, SizeInt blockSize, bool overwrite)
		{
			ObjectId result;

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var subdivisionId = GetSubdivision(mSetInfo.Coords.Exponent, subdivisionReaderWriter);

			// TODO: calculat the canvasOffset
			var canvasOffset = new PointDbl(); 

			if (!subdivisionId.HasValue)
			{
				var subdivision = JobHelper.CreateSubdivision(blockSize, mSetInfo.Coords);
				subdivisionId = InsertSubdivision(subdivision, subdivisionReaderWriter);
			}

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobIds = jobReaderWriter.GetJobIds(project.Id);

			if (jobIds.Length == 0)
			{
				result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, canvasOffset, jobReaderWriter);
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already has at least one job.");
				}
				else
				{
					foreach (var jobId in jobIds)
					{
						var dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, canvasOffset, jobReaderWriter);
				}
			}

			return result;
		}

		public Subdivision GetOrCreateSubdivision(Subdivision subdivision)
		{
			ObjectId? subId;

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);
			subId = GetSubdivision(subdivision.SamplePointDelta.Exponent, subdivisionReaderWriter);

			if (!subId.HasValue)
			{
				subId = InsertSubdivision(subdivision, subdivisionReaderWriter);
			}

			return new Subdivision(subId.Value, new RPoint(subdivision.Position.Values, subdivision.Position.Exponent), subdivision.BlockSize, new RSize(subdivision.SamplePointDelta.Values, subdivision.SamplePointDelta.Exponent));
		}

		private ObjectId? GetSubdivision(int scale, SubdivisonReaderWriter subdivisionReaderWriter)
		{
			var result = subdivisionReaderWriter.Get(scale);

			return result.FirstOrDefault()?.Id;
		}

		private ObjectId InsertSubdivision(Subdivision subdivision, SubdivisonReaderWriter subdivisionReaderWriter)
		{
			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var result = subdivisionReaderWriter.Insert(subdivisionRecord);

			return result;
		}

		private ObjectId CreateAndInsertFirstJob(ObjectId projectId, ObjectId subdivisionId, MSetInfo mSetInfo, PointDbl canvasOffset, JobReaderWriter jobReaderWriter)
		{
			var jobRecord = CreateJob(null, projectId, subdivisionId, ROOT_JOB_LABEL, mSetInfo, canvasOffset);
			var result = jobReaderWriter.Insert(jobRecord);

			return result;
		}

		public Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			var deleteCount = jobReaderWriter.Delete(jobId);
			var result = new Tuple<long, long>(deleteCount ?? 0, 0);
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

		private JobRecord CreateJob(ObjectId? parentJobId, ObjectId projectId, ObjectId subdivisionId, string label, MSetInfo mSetInfo, PointDbl canvasOffset)
		{
			var mSetInfoRecord = _mSetRecordMapper.MapTo(mSetInfo);

			var jobRecord = new JobRecord(
				ParentJobId: parentJobId,
				ProjectId: projectId,
				SubDivisionId: subdivisionId,
				Label: label,
				MSetInfo: mSetInfoRecord,
				CanvasOffsetX: canvasOffset.X,
				CanvasOffsetY: canvasOffset.Y
				);

			return jobRecord;
		}
	}
}
