﻿using FSTypes;
using MapSectionRepo;
using MongoDB.Bson;
using ProjectRepo;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ImageBuilder
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
			var project = InsertProject(projectData);

			var jobId = CreateJob(project, mSetInfo, overwrite);

			GetMapSectionWriter(jobId);

			SizeInt imageSizeInBlocks = mapSectionReader.GetImageSizeInBlocks();

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

		private void GetMapSectionWriter(ObjectId jobId)
		{
			Debug.WriteLine($"The JobId is {jobId}.");
		}

		private ObjectId CreateJob(Project project, MSetInfo mSetInfo, bool overwrite)
		{
			var jobReaderWriter = new JobReaderWriter(_dbProvider);
			var jobId = jobReaderWriter.GetJobId(project.Id);

			if (jobId == ObjectId.Empty)
			{
				Job job = CreateFirstJob(project.Id, project.BaseCoords, mSetInfo);
				jobId = jobReaderWriter.Insert(job);
			}
			else
			{
				if (!overwrite)
				{
					throw new InvalidOperationException($"Overwrite is false and Project: {project.Name} already has at least one job.");
				}
				else
				{
					//throw new NotSupportedException($"Project: {project.Name} already has at least one job. Overwriting an existing job is not yet supported.");
					Tuple<long, long> dResult = DeleteJobAndChildMapSections(jobId, jobReaderWriter);
					Debug.WriteLine($"Deleted {dResult.Item1} jobs, {dResult.Item2} map sections.");

					Job job = CreateFirstJob(project.Id, project.BaseCoords, mSetInfo);
					jobId = jobReaderWriter.Insert(job);
				}
			}

			return jobId;
		}

		private Tuple<long, long> DeleteJobAndChildMapSections(ObjectId jobId, JobReaderWriter jobReaderWriter)
		{
			long? deleteCount = jobReaderWriter.Delete(jobId);
			Tuple<long, long> result = new Tuple<long, long>(deleteCount ?? 0, 0);
			return result;
		}

		//private void UpdateJob(Project project, ObjectId jobId)
		//{

		//}

		/// <summary>
		/// Inserts the project record if it does not exist on the database.
		/// </summary>
		/// <param name="project"></param>
		private Project InsertProject(Project project)
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

			return result;
		}

		private Job CreateFirstJob(ObjectId projectId, Coords coords, MSetInfo mSetInfo)
		{
			//Job job = new Job(projectId, saved:true, coords, mSetInfo.MaxIterations, mSetInfo.Threshold, mSetInfo.InterationsPerStep,
			//	new List<ColorMapEntry>(mSetInfo.ColorMap.ColorMapEntries), mSetInfo.ColorMap.HighColorEntry.StartColor.CssColor);

			Job job = new Job(projectId, saved: true, coords, mSetInfo.MaxIterations, mSetInfo.Threshold, mSetInfo.InterationsPerStep,
				mSetInfo.ColorMap.ColorMapEntries, mSetInfo.ColorMap.HighColorEntry.StartColor.CssColor);

			return job;
		}

	}
}
