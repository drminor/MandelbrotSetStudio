using MSS.Common.APValues;
using MSS.Types;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateDepthFirst
	{
		//private readonly FP31Val[] _samplePointsX;
		private readonly FP31Val[] _samplePointsY;
		private readonly MapSectionVectors _mapSectionVectors;
		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;

		#region Constructor

		public IterationStateDepthFirst(FP31Val[] samplePointsX, FP31Val[] samplePointsY, MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors, 
			bool increasingIterations, Vector256<int> targetIterationsVector)
		{
			//_samplePointsX = samplePointsX;
			_samplePointsY = samplePointsY;	
			_mapSectionVectors = mapSectionVectors;
			_mapSectionZVectors = mapSectionZVectors;

			IncreasingIterations = increasingIterations;
			TargetIterationsVector = targetIterationsVector;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;
			VectorsPerRow = mapSectionZVectors.VectorsPerRow;
			VectorsPerFlagRow = mapSectionZVectors.VectorsPerFlagRow;

			RowNumber = null;

			CrsRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CrsRow.UpdateFrom(samplePointsX);

			CisRow = new FP31ValArray(LimbCount, ValuesPerRow);

			//CountsRow = mapSectionVectors.GetCountsRow(0);
			CountsRowV = new Vector256<int>[VectorsPerRow];

			RowHasEscaped = _mapSectionZVectors.GetRowHasEscaped();

			//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(0);
			//ZrsRow = _mapSectionZVectors.GetZrsRow(0);
			//ZisRow = _mapSectionZVectors.GetZisRow(0);

			HasEscapedFlagsRowV = new Vector256<int>[VectorsPerFlagRow];
			ZrsRowV = new Vector256<uint>[VectorsPerRow];
			ZisRowV = new Vector256<uint>[VectorsPerRow];

			_mapSectionZVectors.FillHasEscapedFlagsRow(0, HasEscapedFlagsRowV);
			_mapSectionZVectors.FillZrsRow(0, ZrsRowV);
			_mapSectionZVectors.FillZisRow(0, ZisRowV);

			DoneFlags = new Vector256<int>[VectorsPerFlagRow];
			UnusedCalcs = new Vector256<int>[VectorsPerFlagRow];

			InPlayList = Enumerable.Range(0, VectorsPerFlagRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
		}

		#endregion

		#region Public Properties

		public bool IncreasingIterations { get; private set; }
		public Vector256<int> TargetIterationsVector { get; private set; }

		public SizeInt BlockSize => _mapSectionVectors.BlockSize;
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int VectorsPerRow { get; init; }
		public int VectorsPerFlagRow { get; init; }
		public int ValuesPerRow { get; init; }

		public int? RowNumber { get; private set; }

		public FP31ValArray CrsRow { get; set; }
		public FP31ValArray CisRow { get; set; }

		//public Span<Vector256<int>> CountsRow { get; private set; }
		public Vector256<int>[] CountsRowV { get; private set; }
		public Span<bool> RowHasEscaped { get; init; }

		//public Span<Vector256<byte>> HasEscapedFlagsRow { get; private set; }
		//public Span<Vector256<uint>> ZrsRow { get; private set; }
		//public Span<Vector256<uint>> ZisRow { get; private set; }

		public Vector256<int>[] HasEscapedFlagsRowV { get; private set; }
		public Vector256<uint>[] ZrsRowV { get; private set; }
		public Vector256<uint>[] ZisRowV { get; private set; }


		public Vector256<int>[] DoneFlags { get; private set; }
		public Vector256<int>[] UnusedCalcs { get; private set; }

		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		#endregion

		#region Public Methods

		// Returns true if all samples for this row have escaped or reached the target number of iterations.
		public int? GetNextRowNumber()
		{
			Array.Clear(UnusedCalcs, 0, UnusedCalcs.Length);

			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
				_mapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);
				_mapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				_mapSectionZVectors.UpdateFromZrsRow(RowNumber.Value, ZrsRowV);
				_mapSectionZVectors.UpdateFromZisRow(RowNumber.Value, ZisRowV);
			}

			var rowNumber = RowNumber.HasValue ? RowNumber.Value : -1;

			if (IncreasingIterations)
			{
				var allSamplesForThisRowAreDone = true;

				while (allSamplesForThisRowAreDone && ++rowNumber < BlockSize.Height)
				{
					if (!RowHasEscaped[rowNumber])
					{
						//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
						_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

						//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
						_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

						RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(HasEscapedFlagsRowV, CountsRowV, _inPlayBackingList, DoneFlags);
						allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;
					}
				}
			}
			else
			{
				// Starting fresh: all ZValues are zero.
				rowNumber++;
				if (rowNumber < BlockSize.Height)
				{
					//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
					_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);
					//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
					_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

					Array.Clear(DoneFlags, 0, DoneFlags.Length);

					_inPlayBackingList.Clear();
					for (var i = 0; i < VectorsPerFlagRow; i++)
					{
						_inPlayBackingList.Add(i);
					}
				}
			}

			if (rowNumber < BlockSize.Height)
			{
				RowNumber = rowNumber;

				var yPoint = _samplePointsY[rowNumber];
				CisRow.UpdateFrom(yPoint);

				_mapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
				_mapSectionZVectors.FillZisRow(rowNumber, ZisRowV);

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

		public void FillCrLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			CrsRow.FillLimbSet(valueIndex, limbSet);
		}

		public void FillCiLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			CisRow.FillLimbSet(valueIndex, limbSet);
		}

		public void FillZrLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = ZrsRowV[vecPtr++];
			}
		}

		public void FillZiLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = ZisRowV[vecPtr++];
			}
		}

		public void UpdateZrLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				ZrsRowV[vecPtr++] = limbSet[i];
			}
		}

		public void UpdateZiLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				ZisRowV[vecPtr++] = limbSet[i];
			}
		}

		#endregion

		#region Private Methods

		private bool BuildTheInPlayBackingList(Vector256<int>[] hasEscapedFlagsRow, Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags) 
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags, 0, doneFlags.Length);

			var allHaveEscaped = true;

			for (var i = 0; i < VectorsPerFlagRow; i++)
			{
				var compositeHasEscapedFlags = Avx2.MoveMask(HasEscapedFlagsRowV[i].AsByte());
				if (compositeHasEscapedFlags != -1)
				{
					allHaveEscaped = false;

					// Compare the new Counts with the TargetIterations
					var targetReachedCompVec = Avx2.CompareGreaterThan(countsRow[i], TargetIterationsVector);

					// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
					doneFlags[i] = Avx2.Or(hasEscapedFlagsRow[i], targetReachedCompVec);

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

