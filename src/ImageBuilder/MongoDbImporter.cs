using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using System.Diagnostics;

namespace MSetDatabaseClient
{
	public class MongoDbImporter
	{
		private readonly MapSectionAdapter _mapSectionAdapter;

		public MongoDbImporter(MapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
		}

		public void Import(IMapSectionReader mapSectionReader, Project projectData, MSetInfo mSetInfo, bool overwrite)
		{
			SizeInt canvasSize = new SizeInt(1280, 1280);
			RRectangle coords = RMapConstants.ENTIRE_SET_RECTANGLE;

			// Make sure the project record has been written.
			var project = _mapSectionAdapter.InsertProject(projectData, overwrite);

			var jobId = _mapSectionAdapter.CreateJob(project, canvasSize, coords, mSetInfo, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = _mapSectionAdapter.GetMapSectionWriter(jobId);
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
			SizeInt canvasSize = new SizeInt(1280, 1280);
			RRectangle coords = RMapConstants.ENTIRE_SET_RECTANGLE;

			// Make sure the project record has been written.
			var project = _mapSectionAdapter.InsertProject(projectData, overwrite);

			var jobId = _mapSectionAdapter.CreateJob(project, canvasSize, coords, mSetInfo, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = _mapSectionAdapter.GetMapSectionWriter(jobId);

			ZoomUntil(job, 100);
		}

		private void ZoomUntil(Job job, int numZooms)
		{
			var dtoMapper = new DtoMapper();
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, new CoordsHelper(dtoMapper));
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);

			for (int zCntr = 0; zCntr < numZooms; zCntr++)
			{
				var zJob = JobHelper.ZoomIn(job);

				var jobRecord = mSetRecordMapper.MapTo(zJob);

				Debug.WriteLine($"Zoom: {zCntr}, Coords: {jobRecord.CoordsRecord.Display}.");

				//jobReaderWriter.Insert(zJob);

				job = zJob;
			}
		}



	}
}
