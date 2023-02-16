using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateDepthFirst
	{
		//private readonly FP31Val[] _samplePointsX;
		private readonly FP31Val[] _samplePointsY;
		
		private readonly MapSectionVectors _mapSectionVectors;
		//private readonly byte[] _counts;
		private readonly int _bytesPerFlagRow;

		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;
		private readonly Vector256<int> ALL_BITS_SET;


		#region Constructor

		public IterationStateDepthFirst(FP31Val[] samplePointsX, FP31Val[] samplePointsY, MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors,
			bool increasingIterations, Vector256<int> targetIterationsVector)
		{
			//_samplePointsX = samplePointsX;
			_samplePointsY = samplePointsY;	

			_mapSectionVectors = mapSectionVectors;

			_mapSectionZVectors = mapSectionZVectors;
			_bytesPerFlagRow = mapSectionZVectors.BytesPerFlagRow;

			IncreasingIterations = increasingIterations;
			TargetIterationsVector = targetIterationsVector;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			RowCount = mapSectionZVectors.BlockSize.Height;
			ValuesPerRow = mapSectionZVectors.ValuesPerRow;
			VectorsPerRow = mapSectionZVectors.VectorsPerRow;
			VectorsPerFlagRow = mapSectionZVectors.VectorsPerFlagRow;

			RowNumber = null;

			CrsRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CrsRow.UpdateFrom(samplePointsX);

			CisRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CisColVec = new Vector256<uint>[LimbCount];

			//CountsRow = mapSectionVectors.GetCountsRow(0);
			CountsRowV = new Vector256<int>[VectorsPerFlagRow];

			RowHasEscaped = _mapSectionZVectors.GetRowHasEscaped();
			RowUsedCalcs = new long[RowCount];
			RowUnusedCalcs = new long[RowCount];

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
			UsedCalcs = new Vector256<int>[VectorsPerFlagRow];
			UnusedCalcs = new Vector256<int>[VectorsPerFlagRow];

			InPlayList = Enumerable.Range(0, VectorsPerFlagRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
			ALL_BITS_SET = Vector256<int>.AllBitsSet;
		}

		#endregion

		#region Public Properties

		public bool IncreasingIterations { get; private set; }
		public Vector256<int> TargetIterationsVector { get; private set; }

		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int RowCount { get; init; }
		public int VectorsPerRow { get; init; }
		public int VectorsPerFlagRow { get; init; }
		public int ValuesPerRow { get; init; }

		public int? RowNumber { get; private set; }

		public FP31ValArray CrsRow { get; set; }
		public FP31ValArray CisRow { get; set; }
		public Vector256<uint>[] CisColVec { get; private set; }

		//public Span<Vector256<int>> CountsRow { get; private set; }
		public Vector256<int>[] CountsRowV { get; private set; }
		public Span<bool> RowHasEscaped { get; init; }
		public long[] RowUnusedCalcs { get; init; }
		public long[] RowUsedCalcs { get; init; }

		//public Span<Vector256<byte>> HasEscapedFlagsRow { get; private set; }
		//public Span<Vector256<uint>> ZrsRow { get; private set; }
		//public Span<Vector256<uint>> ZisRow { get; private set; }

		public Vector256<int>[] HasEscapedFlagsRowV { get; private set; }
		public Vector256<uint>[] ZrsRowV { get; private set; }
		public Vector256<uint>[] ZisRowV { get; private set; }


		public Vector256<int>[] DoneFlags { get; private set; }
		public Vector256<int>[] UsedCalcs { get; private set; }
		public Vector256<int>[] UnusedCalcs { get; private set; }

		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		#endregion

		#region Public Methods

		// Returns true if all samples for this row have escaped or reached the target number of iterations.
		public int? GetNextRowNumber()
		{
			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
				_mapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);
				_mapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				_mapSectionZVectors.UpdateFromZrsRow(RowNumber.Value, ZrsRowV);
				_mapSectionZVectors.UpdateFromZisRow(RowNumber.Value, ZisRowV);
			}

			UpdateUsedAndUnusedCalcs(RowNumber);

			var rowNumber = RowNumber.HasValue ? RowNumber.Value : -1;

			if (IncreasingIterations)
			{
				var allSamplesForThisRowAreDone = true;

				while (allSamplesForThisRowAreDone && ++rowNumber < RowCount)
				{
					if (!RowHasEscaped[rowNumber])
					{
						//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
						_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

						//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
						_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

						RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(HasEscapedFlagsRowV, CountsRowV, _inPlayBackingList, DoneFlags);
						allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;

						if (!allSamplesForThisRowAreDone)
						{
							_mapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
							_mapSectionZVectors.FillZisRow(rowNumber, ZisRowV);
						}
						else
						{
							Debug.WriteLine($"WARNING: Row has not escaped, but the row is done, even with the new Target Iterations. New Target: {TargetIterationsVector.GetElement(0)}");
						}
					}
				}
			}
			else
			{
				// Starting fresh: all ZValues are zero.
				rowNumber++;
				if (rowNumber < RowCount)
				{
					Array.Clear(HasEscapedFlagsRowV);
					Array.Clear(CountsRowV);

					_inPlayBackingList.Clear();
					for (var i = 0; i < VectorsPerFlagRow; i++)
					{
						_inPlayBackingList.Add(i);
					}

					Array.Clear(DoneFlags);
				}
			}

			if (rowNumber < RowCount)
			{
				RowNumber = rowNumber;

				var yPoint = _samplePointsY[rowNumber];
				CisRow.UpdateFrom(yPoint);
				//RepeatInto(yPoint, CisColVec);

				InPlayList = _inPlayBackingList.ToArray();
				InPlayListNarrow = BuildNarowInPlayList(InPlayList);
			}
			else
			{
				RowNumber = null;
			}

			return RowNumber;
		}

		//private void RepeatInto(FP31Val val, Vector256<uint>[] destination)
		//{
		//	for (var i = 0; i < destination.Length; i++)
		//	{
		//		destination[i] = Vector256.Create(val.Mantissa[i]);
		//	}
		//}

		[Conditional("PERF")]
		private void UpdateUsedAndUnusedCalcs(int? rowNumber)
		{
			if (rowNumber.HasValue)
			{
				RowUsedCalcs[rowNumber.Value] = GetUsedCalcs(UsedCalcs, UnusedCalcs, out var unusedCalcs);
				RowUnusedCalcs[rowNumber.Value] = unusedCalcs;
			}

			Array.Clear(UnusedCalcs);
			Array.Clear(UsedCalcs);
		}

		public long GetUsedCalcs(Vector256<int>[] usedCalcsV, Vector256<int>[] unusedCalcsV, out long unusedCalcs)
		{
			unusedCalcs = 0;
			var result = 0L;

			var unusedSourceBack = MemoryMarshal.Cast<Vector256<int>, int>(unusedCalcsV);
			var usedSourceBack = MemoryMarshal.Cast<Vector256<int>, int>(usedCalcsV);

			for (var valuePtr = 0; valuePtr < unusedSourceBack.Length; valuePtr++)
			{
				unusedCalcs += unusedSourceBack[valuePtr];
				result += usedSourceBack[valuePtr];
			}

			return result;
		}

		public void FillCrLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			CrsRow.FillLimbSet(valueIndex, limbSet);
		}

		public void FillCiLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			CisRow.FillLimbSet(valueIndex, limbSet);

			//if (RowNumber.HasValue)
			//{
			//	var yPoint = _samplePointsY[RowNumber.Value];

			//	for (var i = 0; i < limbSet.Length; i++)
			//	{
			//		limbSet[i] = Vector256.Create(yPoint.Mantissa[i]);
			//	}
			//}
		}

		public void FillZrLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			if (!IncreasingIterations)
			{
				// Clear instead of copying form source
				for (var i = 0; i < LimbCount; i++)
				{
					limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
				}
			}
			else
			{
				var vecPtr = valueIndex * LimbCount;

				for (var i = 0; i < LimbCount; i++)
				{
					limbSet[i] = ZrsRowV[vecPtr++];
				}
			}
		}

		public void FillZiLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			if (!IncreasingIterations)
			{
				// Clear instead of copying form source
				for (var i = 0; i < LimbCount; i++)
				{
					limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
				}
			}
			else
			{
				var vecPtr = valueIndex * LimbCount;

				for (var i = 0; i < LimbCount; i++)
				{
					limbSet[i] = ZisRowV[vecPtr++];
				}
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


		//private void FillCountsRow(int rowNumber, Vector256<int>[] dest)
		//{
		//	var destBack = MemoryMarshal.Cast<Vector256<int>, byte>(dest);

		//	var startIndex = _bytesPerFlagRow * rowNumber;

		//	for (var i = 0; i < _bytesPerFlagRow; i++)
		//	{
		//		destBack[i] = _counts[startIndex + i];
		//	}
		//}

		//private void UpdateFromCountsRow(int rowNumber, Vector256<int>[] source)
		//{
		//	var sourceBack = MemoryMarshal.Cast<Vector256<int>, byte>(source);

		//	var startIndex = _bytesPerFlagRow * rowNumber;

		//	for (var i = 0; i < _bytesPerFlagRow; i++)
		//	{
		//		_counts[startIndex + i] = sourceBack[i];
		//	}
		//}

		#endregion

		#region Private Methods

		private bool BuildTheInPlayBackingList(Vector256<int>[] hasEscapedFlagsRow, Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags) 
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags);

			var allHaveEscaped = true;

			for (var i = 0; i < VectorsPerFlagRow; i++)
			{
				var compositeHasEscapedFlags = Avx2.MoveMask(hasEscapedFlagsRow[i].AsByte());
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
				else
				{
					// TODO: Is this required.
					doneFlags[i] = ALL_BITS_SET;
				}
			}

			return allHaveEscaped;
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

