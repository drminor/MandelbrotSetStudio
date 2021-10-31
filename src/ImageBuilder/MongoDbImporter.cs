using FSTypes;
using MapSectionRepo;
using MSetInfoRepo;
using ProjectRepo;

using System.Diagnostics;

namespace ImageBuilder
{
	public class MongoDbImporter
	{
		private readonly int _blockWidth;
		private readonly int _blockHeight;

		public MongoDbImporter(int blockWidth, int blockHeigth)
		{
			_blockWidth = blockWidth;
			_blockHeight = blockHeigth;
		}

		public void Import(string mFilePath)
		{
			var mSetInfo = MSetInfoReaderWriter.Read(mFilePath);
			bool isHighRes = mSetInfo.IsHighRes;
			var repofilename = mSetInfo.Name;

			ICountsRepoReader countsRepoReader = GetReader(repofilename, isHighRes);

			var jobReaderWriter = new JobReaderWriter();

			Project test = jobReaderWriter.GetProject("test");


			//SizeInt imageSizeInBlocks = GetImageSizeInBlocks(countsRepoReader);

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

		private ICountsRepoReader GetReader(string fn, bool isHighRes)
		{
			if (isHighRes)
			{
				return new CountsRepoReaderHiRes(fn, _blockWidth, _blockHeight);
			}
			else
			{
				return new CountsRepoReader(fn, _blockWidth, _blockHeight);
			}
		}

		//private SizeInt GetImageSizeInBlocks(ICountsRepoReader countsRepo)
		//{
		//	int w = 10;
		//	int h = 0;

		//	KPoint key = new KPoint(w, h);
		//	bool foundMax = !countsRepo.ContainsKey(key);

		//	if (foundMax) return new SizeInt(0, 0);

		//	// Find max value where w and h are equal.
		//	while (!foundMax)
		//	{
		//		w++;
		//		h++;
		//		key = new KPoint(w, h);
		//		foundMax = !countsRepo.ContainsKey(key);
		//	}

		//	w--;
		//	h--;
		
		//	foundMax = false;
		//	// Find max value of h
		//	while (!foundMax)
		//	{
		//		h++;
		//		key = new KPoint(w, h);
		//		foundMax = !countsRepo.ContainsKey(key);
		//	}

		//	h--;

		//	foundMax = false;
		//	// Find max value of h
		//	while (!foundMax)
		//	{
		//		w++;
		//		key = new KPoint(w, h);
		//		foundMax = !countsRepo.ContainsKey(key);
		//	}

		//	//w--;

		//	return new SizeInt(w, ++h);
		//}

	}
}
