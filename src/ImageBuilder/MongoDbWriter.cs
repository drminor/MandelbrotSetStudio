using FSTypes;
using MapSectionRepo;
using MFile;
using System.Diagnostics;

namespace ImageBuilder
{
	public class MongoDbWriter
	{
		private const int BlockWidth = 100;
		private const int BlockHeight = 100;

		public static void Build(string mFilePath)
		{
			MFileInfo mFileInfo = ReadFromJson(mFilePath);

			var mSetInfo = MFileHelper.GetMSetInfo(mFileInfo);
			bool isHighRes = mSetInfo.IsHighRes;

			var repofilename = mFileInfo.Name;

			ICountsRepoReader countsRepoReader = GetReader(repofilename, isHighRes);
			SizeInt imageSizeInBlocks = GetImageSizeInBlocks(countsRepoReader);

			int numHorizBlocks = imageSizeInBlocks.W;
			int numVertBlocks = imageSizeInBlocks.H;

			var key = new KPoint(0, 0);

			for (int vBPtr = 0; vBPtr < numVertBlocks; vBPtr++)
			{
				key.Y = vBPtr;
				for (int lPtr = 0; lPtr < 100; lPtr++)
				{
					for (int hBPtr = 0; hBPtr < numHorizBlocks; hBPtr++)
					{
						key.X = hBPtr;

						int[] countsForThisLine = countsRepoReader.GetCounts(key, lPtr);
						if (countsForThisLine != null)
						{
							Debug.WriteLine($"Read Block. V={vBPtr}, HB={hBPtr}.");
						}
						else
						{
							Debug.WriteLine($"No Block. V={vBPtr}, HB={hBPtr}.");
						}
					}

				}
			}
		}

		private static ICountsRepoReader GetReader(string fn, bool isHighRes)
		{
			if (isHighRes)
			{
				return new CountsRepoReaderHiRes(fn, BlockWidth, BlockHeight);
			}
			else
			{
				return new CountsRepoReader(fn, BlockWidth, BlockHeight);
			}
		}

		private static MFileInfo ReadFromJson(string mFilePath)
		{
			var mFileReaderWriter = new MFileReaderWriter();
			MFileInfo mFileInfo = mFileReaderWriter.Read(mFilePath);
			return mFileInfo;
		}

		private static SizeInt GetImageSizeInBlocks(ICountsRepoReader countsRepo)
		{
			int w = 10;
			int h = 0;

			KPoint key = new KPoint(w, h);
			bool foundMax = !countsRepo.ContainsKey(key);

			if (foundMax) return new SizeInt(0, 0);

			// Find max value where w and h are equal.
			while (!foundMax)
			{
				w++;
				h++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			w--;
			h--;
		
			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				h++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			h--;

			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				w++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			//w--;

			return new SizeInt(w, ++h);
		}

	}
}
