using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;

namespace MSetRepo
{
	public class MapSectionAdapter
	{
		private readonly DbProvider _dbProvider;
		private readonly MSetRecordMapper _mSetRecordMapper;

		public MapSectionAdapter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
			_mSetRecordMapper = new MSetRecordMapper();
		}

		public Job GetMapSectionWriter(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobRecord = jobReaderWriter.Get(jobId);
			Debug.WriteLine($"The JobId is {jobRecord.Id}.");

			var job = _mSetRecordMapper.MapFrom(jobRecord);

			return job;
		}

		public ObjectId CreateJob(Project project, SizeInt canvasSize, RRectangle coords, MSetInfo mSetInfo, bool overwrite)
		{
			ObjectId result;

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobIds = jobReaderWriter.GetJobIds(project.Id);

			if (jobIds.Length > 0)
			{
				JobRecord jobRecord = CreateFirstJob(project.Id, canvasSize, coords, mSetInfo);
				result = jobReaderWriter.Insert(jobRecord);
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

					JobRecord jobRecord = CreateFirstJob(project.Id, canvasSize, coords, mSetInfo);
					result = jobReaderWriter.Insert(jobRecord);
				}
			}

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

		private JobRecord CreateFirstJob(ObjectId projectId, SizeInt canvasSize, RRectangle coords, MSetInfo mSetInfo)
		{
			RRectangleRecord coordsDto = CoordsHelper.BuildCoords(coords);
			JobRecord jobRecord = new JobRecord(projectId, canvasSize, coordsDto, mSetInfo.MaxIterations, mSetInfo.Threshold, mSetInfo.InterationsPerStep,
				mSetInfo.ColorMap.ColorMapEntries, mSetInfo.HighColorCss);

			return jobRecord;
		}
	}
}
