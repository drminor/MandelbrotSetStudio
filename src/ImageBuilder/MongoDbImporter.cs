using FSTypes;
using MapSectionRepo;
using MongoDB.Bson;
using ProjectRepo;
using System;

namespace ImageBuilder
{
	public class MongoDbImporter
	{
		private readonly DbProvider _dbProvider;

		public MongoDbImporter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

		// TODO: 
		public void Import(IMapSectionReader mapSectionReader, Project project)
		{
			GetMapSectionWriter(project);

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

			//			int[] countsForThisLine = countsRepoReader.GetCounts(key, lPtr);
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

		private void GetMapSectionWriter(Project project)
		{
			string projectName = project.Name;
			var projectReaderWriter = new ProjectReaderWriter(_dbProvider);

			ObjectId projectId = projectReaderWriter.GetProjectId(projectName);

			if (projectId == ObjectId.Empty)
			{
				projectReaderWriter.Insert(project);
				projectId = projectReaderWriter.GetProjectId(projectName);
			}

		}

	}
}
