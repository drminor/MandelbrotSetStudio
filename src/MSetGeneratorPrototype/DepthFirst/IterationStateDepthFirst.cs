using MSS.Common.APValues;
using MSS.Types;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateDepthFirst
	{
		private readonly MapSectionVectors _mapSectionVectors;
		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;
		private readonly Vector256<byte> ALL_BITS_SET;

		#region Constructor

		public IterationStateDepthFirst(MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors, bool updatingIterationCount, Vector256<int> targetIterationsVector)
		{
			_mapSectionVectors = mapSectionVectors;
			_mapSectionZVectors = mapSectionZVectors;
			UpdatingIterationsCount = updatingIterationCount;
			TargetIterationsVector = targetIterationsVector;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			VectorCount = mapSectionVectors.VectorsPerRow;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;

			RowNumber = null;

			CrsRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CisRow = new FP31ValArray(LimbCount, ValuesPerRow);
			//Zrs = new FP31ValArray(LimbCount, ValuesPerRow);
			//Zis = new FP31ValArray(LimbCount, ValuesPerRow);

			HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(0);
			RowHasEscaped = _mapSectionZVectors.GetRowHasEscaped();

			CountsRow = mapSectionVectors.GetCountsRow(0);

			ZrsRow = _mapSectionZVectors.GetZrsRow(0);
			ZisRow = _mapSectionZVectors.GetZisRow(0);

			DoneFlags = new Vector256<int>[VectorCount];
			UnusedCalcs = new Vector256<int>[VectorCount];

			InPlayList = Enumerable.Range(0, VectorCount).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();

			ALL_BITS_SET = Vector256<byte>.AllBitsSet;
		}

		#endregion

		#region Public Properties

		public bool UpdatingIterationsCount { get; private set; }
		public Vector256<int> TargetIterationsVector { get; private set; }

		public SizeInt BlockSize => _mapSectionVectors.BlockSize;
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int VectorCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int? RowNumber { get; private set; }

		public Span<Vector256<int>> HasEscapedFlagsRow { get; private set; }

		public Span<Vector256<int>> CountsRow { get; private set; }

		public FP31ValArray CrsRow { get; set; }
		public FP31ValArray CisRow { get; set; }
		public Span<Vector256<uint>> ZrsRow { get; private set; }
		public Span<Vector256<uint>> ZisRow { get; private set; }

		public Vector256<int>[] DoneFlags { get; private set; }
		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		public Span<bool> RowHasEscaped { get; init; }

		public Vector256<int>[] UnusedCalcs { get; private set; }

		#endregion

		#region Public Methods

		// Returns true if all samples for this row have escaped or reached the target number of iterations.
		public int? GetNextRowNumber()
		{
			Array.Clear(UnusedCalcs, 0, UnusedCalcs.Length);

			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
			}

			var rowNumber = RowNumber.HasValue ? RowNumber.Value : -1;

			if (UpdatingIterationsCount)
			{
				var allSamplesForThisRowAreDone = true;

				while (allSamplesForThisRowAreDone && ++rowNumber < BlockSize.Height)
				{
					if (!RowHasEscaped[rowNumber])
					{
						HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
						CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);

						RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(HasEscapedFlagsRow, CountsRow, _inPlayBackingList, DoneFlags);
						allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;
					}
				}
			}
			else
			{
				rowNumber++;
				if (rowNumber < BlockSize.Height)
				{
					HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
					CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);

					Array.Clear(DoneFlags, 0, DoneFlags.Length);

					_inPlayBackingList.Clear();
					for (var i = 0; i < VectorCount; i++)
					{
						_inPlayBackingList.Add(i);
					}
				}
			}

			if (rowNumber < BlockSize.Height)
			{
				RowNumber = rowNumber;
				ZrsRow = _mapSectionZVectors.GetZrsRow(rowNumber);
				ZisRow = _mapSectionZVectors.GetZisRow(rowNumber);

				InPlayList = _inPlayBackingList.ToArray();
				InPlayListNarrow = BuildNarowInPlayList(InPlayList);
			}
			else
			{
				RowNumber = null;
			}

			return RowNumber;
		}

		public long GetTotalUnusedCalcs()
		{
			return 0L;
		}

		#endregion

		#region Private Methods

		private bool BuildTheInPlayBackingList(Span<Vector256<int>> hasEscapedFlagsRow, Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags) 
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags, 0, doneFlags.Length);

			var allHaveEscaped = true;

			for (var i = 0; i < VectorCount; i++)
			{
				var compositeHasEscapedFlags = Avx2.MoveMask(HasEscapedFlagsRow[i].AsByte());
				if (compositeHasEscapedFlags != -1)
				{
					allHaveEscaped = false;

					// Compare the new Counts with the TargetIterations
					var targetReachedCompVec = Avx2.CompareGreaterThan(countsRow[i], TargetIterationsVector).AsByte();

					// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
					var escapedOrReachedVec = Avx2.Or(hasEscapedFlagsRow[i].AsByte(), targetReachedCompVec).AsByte().AsInt32();
					doneFlags[i] = escapedOrReachedVec;

					var compositeIsDone = Avx2.MoveMask(doneFlags[i].AsByte());
					if (compositeIsDone != -1)
					{
						inPlayBackingList.Add(i);
					}
				}
			}

			return allHaveEscaped;
		}

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

		#endregion
	}
}

