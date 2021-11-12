using FileDictionaryLib;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSetOld;
using System;

namespace MapSectionRepo
{
	public class MapSectionReader : IMapSectionReader
	{
		public bool IsHighRes => false;
		readonly SizeInt _blockSize;

		readonly ValueRecords<KPoint, MapSectionWorkResult> _countsRepo;
		readonly MapSectionWorkResult _workResult;

		public MapSectionReader(string repofilename, SizeInt blockSize)
		{
			_blockSize = blockSize;

			_countsRepo = new ValueRecords<KPoint, MapSectionWorkResult>(repofilename, useHiRezFolder: IsHighRes);
			_workResult = new MapSectionWorkResult(_blockSize.NumberOfCells, highRes: IsHighRes, includeZValuesOnRead: false);
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
			int[] result = new int[_blockSize.Width];

			Array.Copy(counts, lPtr * _blockSize.Width, result, 0, _blockSize.Width);
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
