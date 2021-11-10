using MSS.Types;
using MongoDB.Bson;
using ProjectRepo;
using System;
using System.Diagnostics;
using MSS.Common;
using MSS.Types.MSetDatabase;
using MSetRepo;

namespace MSetRepo
{
	public class MapSectionAdapter
	{
		private readonly DbProvider _dbProvider;

		public MapSectionAdapter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

		public Job GetMapSectionWriter(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var job = jobReaderWriter.Get(jobId);
			Debug.WriteLine($"The JobId is {job.Id}.");

			return job;
		}

		public ObjectId CreateJob(Project project, MSetInfo mSetInfo, bool overwrite)
		{
			ObjectId result;

			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobIds = jobReaderWriter.GetJobIds(project.Id);

			if (jobIds.Length > 0)
			{
				Job job = CreateFirstJob(project.Id, project.BaseCoords, mSetInfo);
				result = jobReaderWriter.Insert(job);
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

					Job job = CreateFirstJob(project.Id, project.BaseCoords, mSetInfo);
					result = jobReaderWriter.Insert(job);
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
			Project result;

			string projectName = project.Name;
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			result = projectReaderWriter.Get(projectName);

			if (result == null)
			{
				projectReaderWriter.Insert(project);
				result = project;
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already exists.");
				}
				else
				{
					var projectId = result.Id;

					var jobReaderWriter = new JobReaderWriter(_dbProvider);

					ObjectId[] jobIds = jobReaderWriter.GetJobIds(projectId);

					foreach (ObjectId jobId in jobIds)
					{
						Tuple<long, long> dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
						Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");
					}

					projectReaderWriter.Delete(projectId);

					projectReaderWriter.Insert(project);
					result = project;
				}
			}

			return result;
		}

		private Job CreateFirstJob(ObjectId projectId, Coords coords, MSetInfo mSetInfo)
		{
			Job job = new Job(projectId, saved: true, coords, mSetInfo.MaxIterations, mSetInfo.Threshold, mSetInfo.InterationsPerStep,
				mSetInfo.ColorMap.ColorMapEntries, mSetInfo.HighColorCss);

			return job;
		}
	}
}
