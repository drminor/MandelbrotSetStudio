using FileDictionaryLib;

namespace MapSectionRepo
{
	public class CountsRepoReaderHiRes : ICountsRepoReader
	{
		public bool IsHighRes => true;
		readonly int _blockWidth;

		readonly ValueRecords<KPoint, SubJobResult> _countsRepo;
		readonly SubJobResult _workResult;

		public CountsRepoReaderHiRes(string repofilename, int blockWidth, int blockHeight)
		{
			_blockWidth = blockWidth;
			int blockLength = blockWidth * blockHeight;

			_countsRepo = new ValueRecords<KPoint, SubJobResult>(repofilename, useHiRezFolder: IsHighRes);
			_workResult = SubJobResult.GetEmptySubJobResult(blockLength, "0", false);
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
			int[] result = new int[_blockWidth];
			int srcPtr = lPtr * _blockWidth;

			for (int i = 0; i < result.Length; i++)
				result[i] = (int)counts[srcPtr++];

			return result;
		}
	}
}
