using MSS.Types;
using MongoDB.Bson;
using ProjectRepo;
using System;
using System.Diagnostics;
using MSS.Common;
using MSS.Types.MSetDatabase;

namespace MSetDatabaseClient
{
	public class MongoDbImporter
	{
		private readonly DbProvider _dbProvider;

		public MongoDbImporter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

		public void Import(IMapSectionReader mapSectionReader, Project projectData, MSetInfo mSetInfo, bool overwrite)
		{
			// Make sure the project record has been written.
			var project = InsertProject(projectData, overwrite);

			var jobId = CreateJob(project, mSetInfo, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = GetMapSectionWriter(jobId);
			CopyBlocks(mapSectionReader, job);
		}

		private void CopyBlocks(IMapSectionReader mapSectionReader, Job job)
		{
			var imageSizeInBlocks = mapSectionReader.GetImageSizeInBlocks();
			var jobId = job.Id;

			//int numHorizBlocks = imageSizeInBlocks.W;
			//int numVertBlocks = imageSizeInBlocks.H;

			//var key = new KPoint(0, 0);

			//for (int vBPtr = 0; vBPtr < numVertBlocks; vBPtr++)
			//{
			//	key.Y = vBPtr;
			//	for (int lPtr = 0; lPtr < 100; lPtr++)
			//	{
			//		for (int hBPtr = 0; hBPtr < numHorizBlocks; hBPtr++)
			//		{
			//			key.X = hBPtr;

			//			int[] countsForThisLine = mapSectionReader.GetCounts(key, lPtr);
			//			if (countsForThisLine != null)
			//			{
			//				Debug.WriteLine($"Read Block. V={vBPtr}, HB={hBPtr}.");
			//			}
			//			else
			//			{
			//				Debug.WriteLine($"No Block. V={vBPtr}, HB={hBPtr}.");
			//			}
			//		}

			//	}
			//}
		}

		public void DoZoomTest1(Project projectData, MSetInfo mSetInfo, bool overwrite)
		{
			// Make sure the project record has been written.
			var project = InsertProject(projectData, overwrite);

			var jobId = CreateJob(project, mSetInfo, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = GetMapSectionWriter(jobId);

			ZoomUntil(job, 100);
		}

		private void ZoomUntil(Job job, int numZooms)
		{
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);

			for (int zCntr = 0; zCntr < numZooms; zCntr++)
			{
				Job zJob = JobHelper.ZoomIn(job);
				Debug.WriteLine($"Zoom: {zCntr}, Coords: {zJob.Coords.Display}.");

				//jobReaderWriter.Insert(zJob);

				job = zJob;
			}
		}

		private Job GetMapSectionWriter(ObjectId jobId)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var job = jobReaderWriter.Get(jobId);
			Debug.WriteLine($"The JobId is {job.Id}.");

			return job;
		}

		private ObjectId CreateJob(Project project, MSetInfo mSetInfo, bool overwrite)
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

		private Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			long? deleteCount = jobReaderWriter.Delete(jobId);
			Tuple<long, long> result = new Tuple<long, long>(deleteCount ?? 0, 0);
			return result;
		}

		/// <summary>
		/// Inserts the project record if it does not exist on the database.
		/// </summary>
		/// <param name="project"></param>
		private Project InsertProject(Project project, bool overwrite)
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

					foreach(ObjectId jobId in jobIds)
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

		private Job CreateFirstJob(ObjectId projectId, BCoords coords, MSetInfo mSetInfo)
		{
			Job job = new Job(projectId, saved: true, coords, mSetInfo.MaxIterations, mSetInfo.Threshold, mSetInfo.InterationsPerStep,
				mSetInfo.ColorMap.ColorMapEntries, mSetInfo.HighColorCss);

			return job;
		}

	}
}
