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
		private readonly SizeInt _blockSize;

		public ProjectAdapter(DbProvider dbProvider, MSetRecordMapper mSetRecordMapper, SizeInt blockSize)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = mSetRecordMapper;
			_blockSize = blockSize;
		}

		public Job GetMapSectionWriter(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = jobReaderWriter.Get(jobId);
			Debug.WriteLine($"The JobId is {jobRecord.Id}.");

			var job = _mSetRecordMapper.MapFrom(jobRecord);

			return job;
		}

		public ObjectId CreateJob(Project project, MSetInfo mSetInfo, bool overwrite)
		{
			ObjectId result;

			var subdivisionReaderWriter = new SubdivisonReaderWriter(_dbProvider);

			var subdivisionId = GetSubdivision(mSetInfo.Coords.Exponent, subdivisionReaderWriter);

			if (!subdivisionId.HasValue)
			{
				subdivisionId = CreateAndInsertSubdivision(mSetInfo.CanvasSize, mSetInfo.Coords, subdivisionReaderWriter);
			}
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobIds = jobReaderWriter.GetJobIds(project.Id);

			if (jobIds.Length == 0)
			{
				result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, jobReaderWriter);
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already has at least one job.");
				}
				else
				{
					foreach (ObjectId jobId in jobIds)
					{
						Tuple<long, long> dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					result = CreateAndInsertFirstJob(project.Id, subdivisionId.Value, mSetInfo, jobReaderWriter);
				}
			}

			return result;
		}

		private ObjectId? GetSubdivision(int scale, SubdivisonReaderWriter subdivisionReaderWriter)
		{
			var result = subdivisionReaderWriter.Get(scale);

			return result.FirstOrDefault()?.Id;
		}

		private ObjectId CreateAndInsertSubdivision(SizeInt canvasSize, RRectangle coords, SubdivisonReaderWriter subdivisionReaderWriter)
		{
			var subdivision = JobHelper.CreateSubdivision(canvasSize, _blockSize, coords);
			var subdivisionRecord = _mSetRecordMapper.MapTo(subdivision);
			var result = subdivisionReaderWriter.Insert(subdivisionRecord);

			return result;
		}

		private ObjectId CreateAndInsertFirstJob(ObjectId projectId, ObjectId subdivisionId, MSetInfo mSetInfo, JobReaderWriter jobReaderWriter)
		{
			JobRecord jobRecord = CreateJob(null, projectId, subdivisionId, ROOT_JOB_LABEL, mSetInfo);
			var result = jobReaderWriter.Insert(jobRecord);

			return result;
		}

		public Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			long? deleteCount = jobReaderWriter.Delete(jobId);
			Tuple<long, long> result = new Tuple<long, long>(deleteCount ?? 0, 0);
			return result;
		}

		/// <summary>
		/// Inserts the project record if it does not exist on the database.
		/// </summary>
		/// <param name="project"></param>
		public Project InsertProject(Project project, bool overwrite)
		{
			ProjectRecord projectRecord;

			string projectName = project.Name;
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			projectRecord = projectReaderWriter.Get(projectName);

			if (projectRecord == null)
			{
				projectRecord = _mSetRecordMapper.MapTo(project);
				projectReaderWriter.Insert(projectRecord);
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

					ObjectId[] jobIds = jobReaderWriter.GetJobIds(projectId);

					foreach (ObjectId jobId in jobIds)
					{
						Tuple<long, long> dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					projectReaderWriter.Delete(projectId);

					projectRecord = _mSetRecordMapper.MapTo(project);
					projectReaderWriter.Insert(projectRecord);
				}
			}

			project = _mSetRecordMapper.MapFrom(projectRecord);

			return project;
		}

		private JobRecord CreateJob(ObjectId? parentJobId, ObjectId projectId, ObjectId subdivisionId, string label, MSetInfo mSetInfo)
		{
			var mSetInfoRecord = _mSetRecordMapper.MapTo(mSetInfo);

			JobRecord jobRecord = new JobRecord(
				ParentJobId: parentJobId,
				ProjectId: projectId,
				SubDivisionId: subdivisionId,
				Label: label,
				MSetInfo: mSetInfoRecord
				);

			return jobRecord;
		}
	}
}
