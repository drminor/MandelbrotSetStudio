using MSS.Types;
using System.Runtime.Intrinsics;
using static MongoDB.Driver.WriteConcern;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateLimbFirst
	{
		private readonly MapSectionVectors2 _mapSectionVectors2;
		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;

		#region Constructor

		public IterationStateLimbFirst(MapSectionVectors2 mapSectionVectors2, MapSectionZVectors mapSectionZVectors, bool updatingIterationCount, Vector256<int> targetIterationsVector)
		{
			_mapSectionVectors2 = mapSectionVectors2;
			_mapSectionZVectors = mapSectionZVectors;

			UpdatingIterationsCount = updatingIterationCount;
			TargetIterationsVector = targetIterationsVector;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			RowCount = mapSectionZVectors.BlockSize.Height;

			ValuesPerRow = mapSectionVectors2.ValuesPerRow;
			VectorsPerRow = mapSectionZVectors.VectorsPerZValueRow;
			VectorsPerFlagRow = mapSectionZVectors.VectorsPerRow;

			RowNumber = -1;

			CountsRow = new Vector256<int>[VectorsPerFlagRow];
			mapSectionVectors2.FillCountsRow(0, CountsRow);

			RowHasEscaped = _mapSectionZVectors.RowHasEscaped;
			RowUsedCalcs = new long[RowCount];
			RowUnusedCalcs = new long[RowCount];

			RowIterationsFull = new long[RowCount];
			RowIterationsPartial = new long[RowCount];

			HasEscapedFlags = new Vector256<int>[_mapSectionZVectors.ValuesPerRow];
			_mapSectionZVectors.FillHasEscapedFlagsRow(0, HasEscapedFlags);
			
			DoneFlags = new Vector256<int>[VectorsPerFlagRow];
			Calcs = new long[VectorsPerFlagRow];
			UnusedCalcs = new Vector256<int>[VectorsPerFlagRow];

			InPlayList = Enumerable.Range(0, VectorsPerFlagRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
		}

		#endregion

		#region Public Properties

		public bool UpdatingIterationsCount { get; private set; }
		public Vector256<int> TargetIterationsVector { get; private set; }

		public SizeInt BlockSize => _mapSectionVectors2.BlockSize;
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int RowCount { get; init; }

		public int VectorsPerRow { get; init; }
		public int VectorsPerFlagRow { get; init; }
		public int ValuesPerRow { get; init; }

		public int RowNumber { get; private set; }

		public Vector256<int>[] CountsRow { get; private set; }
		public Span<bool> RowHasEscaped { get; init; }
		public long[] RowUnusedCalcs { get; init; }
		public long[] RowUsedCalcs { get; init; }

		public long[] RowIterationsFull { get; init; }
		public long[] RowIterationsPartial { get; init; }

		public Vector256<int>[] HasEscapedFlags { get; private set; }

		public Vector256<int>[] DoneFlags { get; private set; }
		public long[] Calcs { get; private set; }
		public Vector256<int>[] UnusedCalcs { get; private set; }

		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		#endregion

		#region Public Methods

		public void SetRowNumber(int rowNumber)
		{
			if (RowNumber != -1)
			{
				UpdateTheHasEscapedFlagsSource(RowNumber);
				UpdateTheCountsSource(RowNumber);
			}

			RowNumber = rowNumber;

			// Get the HasEscapedFlags
			_mapSectionZVectors.FillHasEscapedFlagsRow(RowNumber, HasEscapedFlags);

			// Get the counts
			_mapSectionVectors2.FillCountsRow(RowNumber, CountsRow);

			Array.Clear(DoneFlags, 0, DoneFlags.Length);
			Array.Clear(UnusedCalcs, 0, UnusedCalcs.Length);

			_inPlayBackingList.Clear();
			for(var i = 0; i < VectorsPerFlagRow; i++)
			{
				_inPlayBackingList.Add(i);
			}

			InPlayList = _inPlayBackingList.ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);
		}


		public void UpdateTheHasEscapedFlagsSource(int rowNumber)
		{
			_mapSectionZVectors.UpdateFromHasEscapedFlagsRow(rowNumber, HasEscapedFlags);
		}

		public void UpdateTheCountsSource(int rowNumber)
		{
			_mapSectionVectors2.UpdateFromCountsRow(rowNumber, CountsRow);
		}

		public long GetTotalUnusedCalcs()
		{
			return 0L;
		}

		#endregion

		#region Private Methods

		public int[] UpdateTheInPlayList(List<int> vectorsNoLongerInPlay)
		{
			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				_inPlayBackingList.Remove(vectorIndex);
			}

			InPlayList = _inPlayBackingList.ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			return InPlayList;
		}

		private static int[] BuildNarowInPlayList(int[] inPlayList)
		{
			var result = new int[inPlayList.Length * 2];

			var resultIdxPtr = 0;

			for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++, resultIdxPtr += 2)
			{
				var resultIdx = inPlayList[idxPtr] * 2;

				result[resultIdxPtr] = resultIdx;
				result[resultIdxPtr + 1] = resultIdx + 1;
			}

			return result;
		}

		private int[] BuildTheInplayList(Span<bool> hasEscapedFlags, Span<ushort> counts, int targetIterations, out bool[] doneFlags)
		{
			var lanes = Vector256<uint>.Count;
			var vectorCount = hasEscapedFlags.Length / lanes;

			doneFlags = new bool[hasEscapedFlags.Length];

			for (int i = 0; i < hasEscapedFlags.Length; i++)
			{
				if (hasEscapedFlags[i] | counts[i] >= targetIterations)
				{
					doneFlags[i] = true;
				}
			}

			var result = Enumerable.Range(0, vectorCount).ToList();

			for (int j = 0; j < vectorCount; j++)
			{
				var arrayPtr = j * lanes;

				var allDone = true;

				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (!doneFlags[arrayPtr + lanePtr])
					{
						allDone = false;
						break;
					}
				}

				if (allDone)
				{
					result.Remove(j);
				}
			}

			return result.ToArray();
		}

		#endregion
	}
}

