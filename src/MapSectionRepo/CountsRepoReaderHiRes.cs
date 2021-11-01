using FileDictionaryLib;
using FSTypes;

namespace MapSectionRepo
{
	public class CountsRepoReaderHiRes : IMapSectionReader
	{
		public bool IsHighRes => true;
		readonly SizeInt _blockSize;

		readonly ValueRecords<KPoint, SubJobResult> _countsRepo;
		readonly SubJobResult _workResult;

		public CountsRepoReaderHiRes(string repofilename, SizeInt blockSize)
		{
			_blockSize = blockSize;

			_countsRepo = new ValueRecords<KPoint, SubJobResult>(repofilename, useHiRezFolder: IsHighRes);
			_workResult = SubJobResult.GetEmptySubJobResult(_blockSize.NumberOfCells, "0", false);
		}

		public int[] GetCounts(KPoint key, int linePtr)
		{
			if (_countsRepo.ReadParts(key, _workResult))
			{
				return GetOneLineFromCountsBlock(_workResult.Counts, linePtr);
			}
			else
			{
				return null;
			}
		}

		public bool ContainsKey(KPoint key) => _countsRepo.ContainsKey(key);

		private int[] GetOneLineFromCountsBlock(uint[] counts, int lPtr)
		{
			int[] result = new int[_blockSize.Width];
			int srcPtr = lPtr * _blockSize.Width;

			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (int)counts[srcPtr++];
			}

			return result;
		}

		public SizeInt GetImageSizeInBlocks()
		{
			int w = 10;
			int h = 0;

			KPoint key = new KPoint(w, h);
			bool foundMax = !ContainsKey(key);

			if (foundMax) return new SizeInt(0, 0);

			// Find max value where w and h are equal.
			while (!foundMax)
			{
				w++;
				h++;
				key = new KPoint(w, h);
				foundMax = !ContainsKey(key);
			}

			w--;
			h--;

			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				h++;
				key = new KPoint(w, h);
				foundMax = !ContainsKey(key);
			}

			h--;

			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				w++;
				key = new KPoint(w, h);
				foundMax = !ContainsKey(key);
			}

			//w--;

			return new SizeInt(w, ++h);
		}
	}
}
