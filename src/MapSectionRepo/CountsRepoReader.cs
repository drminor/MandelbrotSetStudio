using CountsRepo;
using System;

namespace MapSectionRepo
{
	public class CountsRepoReader
	{
		readonly bool _hiRez;
		readonly int _blockWidth;

		readonly ValueRecords<KPoint, MapSectionWorkResult> _countsRepo;
		readonly MapSectionWorkResult _workResult;

		readonly ValueRecords<KPoint, SubJobResult> _countsRepoHiRez;
		readonly SubJobResult _workResultHiRez;

		public CountsRepoReader(string repofilename, bool hiRez, int blockWidth, int blockHeight)
		{
			_hiRez = hiRez;
			_blockWidth = blockWidth;
			int blockLength = blockWidth * blockHeight;

			if(_hiRez)
			{
				_countsRepoHiRez = new ValueRecords<KPoint, SubJobResult>(repofilename, useHiRezFolder: _hiRez);
				_workResultHiRez = SubJobResult.GetEmptySubJobResult(blockLength, "0", false);
			}
			else
			{
				_countsRepo = new ValueRecords<KPoint, MapSectionWorkResult>(repofilename, useHiRezFolder: _hiRez);
				_workResult = new MapSectionWorkResult(blockLength, hiRez: hiRez, includeZValuesOnRead: false);
			}
		}

		public int[] GetCounts(KPoint key, int linePtr)
		{
			if(_hiRez)
			{
				if (_countsRepoHiRez.ReadParts(key, _workResultHiRez))
				{
					return GetOneLineFromCountsBlock(_workResultHiRez.Counts, linePtr);
				}
				else
				{
					return null;
				}
			}
			else
			{
				if(_countsRepo.ReadParts(key, _workResult))
				{
					return GetOneLineFromCountsBlock(_workResult.Counts, linePtr);
				}
				else
				{
					return null;
				}
			}
		}

		public bool ContainsKey(KPoint key)
		{
			if (_hiRez)
			{
				return _countsRepoHiRez.ContainsKey(key);
			}
			else
			{
				return _countsRepo.ContainsKey(key);
			}
		}

		private int[] GetOneLineFromCountsBlock(int[] counts, int lPtr)
		{
			int[] result = new int[_blockWidth];

			Array.Copy(counts, lPtr * _blockWidth, result, 0, _blockWidth);
			return result;
		}

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
