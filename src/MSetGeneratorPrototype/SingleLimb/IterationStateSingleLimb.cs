using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateSingleLimb
	{
		//private readonly double[] _samplePointsX;
		private readonly double[] _samplePointsY;
		private readonly MapSectionVectors _mapSectionVectors;
		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;

		#region Constructor

		public IterationStateSingleLimb(double[] samplePointsX, double[] samplePointsY, MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors,
			bool increasingIterations, int targetIterations)
		{
			//_samplePointsX = samplePointsX;
			_samplePointsY = samplePointsY;
			_mapSectionVectors = mapSectionVectors;
			_mapSectionZVectors = mapSectionZVectors;

			IncreasingIterations = increasingIterations;
			TargetIterations = targetIterations;

			ValueCount = mapSectionZVectors.ValueCount;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;

			RowNumber = null;

			CrsRow = samplePointsX;
			CisRow = Enumerable.Repeat(samplePointsY[0], ValuesPerRow).ToArray();

			CountsRowV = new int[ValuesPerRow];

			// No rows have escaped -- not supporting IncreasingIterations.
			RowHasEscaped = new bool[mapSectionVectors.BlockSize.Height]; // _mapSectionZVectors.GetRowHasEscaped();

			HasEscapedFlagsRowV = new bool[ValuesPerRow];
			ZrsRowV = new double[ValuesPerRow];
			ZisRowV = new double[ValuesPerRow];

			//_mapSectionZVectors.FillHasEscapedFlagsRow(0, HasEscapedFlagsRowV);
			//_mapSectionZVectors.FillZrsRow(0, ZrsRowV);
			//_mapSectionZVectors.FillZisRow(0, ZisRowV);

			DoneFlags = new bool[ValuesPerRow];
			UnusedCalcs = new int[ValuesPerRow];

			InPlayList = Enumerable.Range(0, ValuesPerRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
		}

		#endregion

		#region Public Properties

		public bool IncreasingIterations { get; private set; }
		public int TargetIterations { get; private set; }

		public SizeInt BlockSize => _mapSectionVectors.BlockSize;
		public int ValueCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int? RowNumber { get; private set; }

		public double[] CrsRow { get; set; }
		public double[] CisRow { get; set; }

		public int[] CountsRowV { get; private set; }
		public bool[] RowHasEscaped { get; init; }

		public bool[] HasEscapedFlagsRowV { get; private set; }
		public double[] ZrsRowV { get; private set; }
		public double[] ZisRowV { get; private set; }


		public bool[] DoneFlags { get; private set; }
		public int[] UnusedCalcs { get; private set; }

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
				_mapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV.Cast<int>().ToArray());
				_mapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				//_mapSectionZVectors.UpdateFromZrsRow(RowNumber.Value, ZrsRowV);
				//_mapSectionZVectors.UpdateFromZisRow(RowNumber.Value, ZisRowV);
			}

			var rowNumber = RowNumber.HasValue ? RowNumber.Value : -1;

			if (IncreasingIterations)
			{
				throw new NotSupportedException("IncreasingIterations for the SingleLimb Generator.");
				//var allSamplesForThisRowAreDone = true;

				//while (allSamplesForThisRowAreDone && ++rowNumber < BlockSize.Height)
				//{
				//	if (!RowHasEscaped[rowNumber])
				//	{
				//		//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
				//		_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

				//		//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
				//		_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

				//		RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(HasEscapedFlagsRowV, CountsRowV, _inPlayBackingList, DoneFlags);
				//		allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;
				//	}
				//}
			}
			else
			{
				// Starting fresh: all ZValues are zero.

				rowNumber++;
				if (rowNumber < BlockSize.Height)
				{
					//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);

					var temp = new int[ValuesPerRow];
					_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, temp);
					HasEscapedFlagsRowV = temp.Cast<bool>().ToArray();

					//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
					_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

					Array.Clear(DoneFlags, 0, DoneFlags.Length);

					_inPlayBackingList.Clear();
					for (var i = 0; i < ValuesPerRow; i++)
					{
						_inPlayBackingList.Add(i);
					}
				}
			}

			if (rowNumber < BlockSize.Height)
			{
				RowNumber = rowNumber;

				CisRow = Enumerable.Repeat(_samplePointsY[rowNumber], ValuesPerRow).ToArray();

				// Not Loading ZValues -- Increasing iterations is not supported.
				//_mapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
				//_mapSectionZVectors.FillZisRow(rowNumber, ZisRowV);

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

		private bool BuildTheInPlayBackingList(Vector256<int>[] hasEscapedFlagsRow, Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags) 
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags, 0, doneFlags.Length);

			var allHaveEscaped = true;

			//for (var i = 0; i < VectorsPerFlagRow; i++)
			//{
			//	var compositeHasEscapedFlags = Avx2.MoveMask(HasEscapedFlagsRowV[i].AsByte());
			//	if (compositeHasEscapedFlags != -1)
			//	{
			//		allHaveEscaped = false;

			//		// Compare the new Counts with the TargetIterations
			//		var targetReachedCompVec = Avx2.CompareGreaterThan(countsRow[i], TargetIterationsVector);

			//		// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
			//		doneFlags[i] = Avx2.Or(hasEscapedFlagsRow[i], targetReachedCompVec);

			//		var compositeIsDone = Avx2.MoveMask(doneFlags[i].AsByte());
			//		if (compositeIsDone != -1)
			//		{
			//			inPlayBackingList.Add(i);
			//		}
			//	}
			//}

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

