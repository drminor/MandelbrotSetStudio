using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types.MSet;
using ProjectRepo;
using ProjectRepo.Entities;
using System.Diagnostics;

namespace MSetDatabaseClient
{
	public class MongoDbImporter
	{
		private readonly ProjectAdapter _projectAdapter;

		public MongoDbImporter(ProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;
		}

		public void Import(/*IMapSectionReader mapSectionReader, */Project project, MSetInfo mSetInfo, bool overwrite)
		{
			var jobId = _projectAdapter.CreateJob(project, mSetInfo, RMapConstants.BLOCK_SIZE, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = _projectAdapter.GetJob(jobId);
			CopyBlocks(/*mapSectionReader, */job);
		}

		private void CopyBlocks(/*IMapSectionReader mapSectionReader, */Job job)
		{
			//var imageSizeInBlocks = mapSectionReader.GetImageSizeInBlocks();
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

		public void DoZoomTest1(Project project, MSetInfo mSetInfo, bool overwrite)
		{
			var jobId = _projectAdapter.CreateJob(project, mSetInfo, RMapConstants.BLOCK_SIZE, overwrite);

			// TODO: using a job object, temporarily, update to a MapSectionReaderWriter.
			var job = _projectAdapter.GetJob(jobId);

			ZoomUntil(job, 100);
		}

		private void ZoomUntil(Job job, int numZooms)
		{
			var dtoMapper = new DtoMapper();
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper, new CoordsHelper(dtoMapper));
			//var jobReaderWriter = new JobReaderWriter(_dbProvider);

			for (var zCntr = 0; zCntr < numZooms; zCntr++)
			{
				var zJob = JobHelper.ZoomIn(job);

				var jobRecord = mSetRecordMapper.MapTo(zJob);

				Debug.WriteLine($"Zoom: {zCntr}, Coords: {jobRecord.MSetInfo.CoordsRecord.Display}.");

				//jobReaderWriter.Insert(zJob);

				job = zJob;
			}
		}



	}
}
