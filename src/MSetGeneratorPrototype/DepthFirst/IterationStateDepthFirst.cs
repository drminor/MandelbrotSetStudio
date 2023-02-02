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
		private readonly bool _updatingIterationsCount;

		private List<int> _inPlayBackingList;
		private readonly Vector256<byte> ALL_BITS_SET;

		#region Constructor

		public IterationStateDepthFirst(MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors, bool updatingIterationCount)
		{
			_mapSectionVectors = mapSectionVectors;
			_mapSectionZVectors = mapSectionZVectors;
			_updatingIterationsCount = updatingIterationCount;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			VectorCount = mapSectionVectors.VectorsPerRow;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;

			RowNumber = -1;

			CrsRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CisRow = new FP31ValArray(LimbCount, ValuesPerRow);
			//Zrs = new FP31ValArray(LimbCount, ValuesPerRow);
			//Zis = new FP31ValArray(LimbCount, ValuesPerRow);

			ZValuesAreZero = !updatingIterationCount;

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

		public SizeInt BlockSize => _mapSectionVectors.BlockSize;
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int VectorCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int RowNumber { get; private set; }

		public FP31ValArray CrsRow { get; set; }
		public FP31ValArray CisRow { get; set; }
		//public FP31ValArray Zrs { get; set; }
		//public FP31ValArray Zis { get; set; }

		public bool ZValuesAreZero { get; set; }

		public Span<Vector256<int>> HasEscapedFlagsRow { get; private set; }

		//public Span<Vector256<int>> Counts { get; private set; }
		public Span<Vector256<int>> CountsRow { get; private set; }

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
		public bool SetRowNumber(int rowNumber, Vector256<int> targetIterationsVector)
		{
			RowNumber = rowNumber;

			HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);

			CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);

			var allSamplesForThisRowHaveEscaped = true;

			_inPlayBackingList.Clear();
			for (var i = 0; i < VectorCount; i++)
			{
				var compositeAllEscaped = Avx2.MoveMask(HasEscapedFlagsRow[i].AsByte());
				if (compositeAllEscaped != -1)
				{
					allSamplesForThisRowHaveEscaped = false;
					// Compare the new Counts with the TargetIterations
					var targetReachedCompVec = Avx2.CompareGreaterThan(CountsRow[i], targetIterationsVector).AsByte();

					// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
					var escapedOrReachedVec = Avx2.Or(HasEscapedFlagsRow[i].AsByte(), targetReachedCompVec).AsByte().AsInt32();

					DoneFlags[i] = escapedOrReachedVec; // Avx2.BlendVariable(DoneFlags[i].AsByte(), ALL_BITS_SET, escapedOrReachedVec.AsByte()).AsInt32();
					var compositeIsDone = Avx2.MoveMask(DoneFlags[i].AsByte());
					if (compositeIsDone != -1)
					{
						_inPlayBackingList.Add(i);
					}
				}
			}

			RowHasEscaped[rowNumber] = allSamplesForThisRowHaveEscaped;

			if (_inPlayBackingList.Count > 0)
			{
				ZrsRow = _mapSectionZVectors.GetZrsRow(rowNumber);
				ZisRow = _mapSectionZVectors.GetZisRow(rowNumber);

				ZValuesAreZero = !_updatingIterationsCount;

				Array.Clear(DoneFlags, 0, DoneFlags.Length);
				Array.Clear(UnusedCalcs, 0, UnusedCalcs.Length);

				InPlayList = _inPlayBackingList.ToArray();
				InPlayListNarrow = BuildNarowInPlayList(InPlayList);
				return false;
			}
			else
			{
				return true;
			}
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

