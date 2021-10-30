using CountsRepo;
using System;

namespace MapSectionRepo
{
	public class CountsRepoReader : ICountsRepoReader
	{
		public bool IsHighRes => false;

		readonly int _blockWidth;

		readonly ValueRecords<KPoint, MapSectionWorkResult> _countsRepo;
		readonly MapSectionWorkResult _workResult;

		public CountsRepoReader(string repofilename, int blockWidth, int blockHeight)
		{
			_blockWidth = blockWidth;
			int blockLength = blockWidth * blockHeight;

			_countsRepo = new ValueRecords<KPoint, MapSectionWorkResult>(repofilename, useHiRezFolder: IsHighRes);
			_workResult = new MapSectionWorkResult(blockLength, highRes: IsHighRes, includeZValuesOnRead: false);
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

		private int[] GetOneLineFromCountsBlock(int[] counts, int lPtr)
		{
			int[] result = new int[_blockWidth];

			Array.Copy(counts, lPtr * _blockWidth, result, 0, _blockWidth);
			return result;
		}
	}
}
