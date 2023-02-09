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
			RowCount = mapSectionZVectors.BlockSize.Height;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;
			VectorsPerRow = mapSectionZVectors.VectorsPerRow;
			VectorsPerFlagRow = mapSectionZVectors.VectorsPerFlagRow;

			RowNumber = null;

			CrsRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CrsRow.UpdateFrom(samplePointsX);

			CisRow = new FP31ValArray(LimbCount, ValuesPerRow);
			CisColVec = new Vector256<uint>[LimbCount];

			//CountsRow = mapSectionVectors.GetCountsRow(0);
			CountsRowV = new Vector256<int>[VectorsPerRow];

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
			Calcs = new long[VectorsPerFlagRow];
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
		public long[] Calcs { get; private set; }
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

				while (allSamplesForThisRowAreDone && ++rowNumber < BlockSize.Height)
				{
					if (!RowHasEscaped[rowNumber])
					{
						//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);
						_mapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

						//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
						_mapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

						_mapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
						_mapSectionZVectors.FillZisRow(rowNumber, ZisRowV);

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

					// TODO: Is this required.
					//Array.Clear(ZrsRowV);
					//Array.Clear(ZisRowV);

					//_mapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
					//_mapSectionZVectors.FillZisRow(rowNumber, ZisRowV);

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

				Array.Clear(DoneFlags, 0, DoneFlags.Length);

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

		private void RepeatInto(FP31Val val, Vector256<uint>[] destination)
		{
			for (var i = 0; i < destination.Length; i++)
			{
				destination[i] = Vector256.Create(val.Mantissa[i]);
			}
		}

		[Conditional("PERF")]
		private void UpdateUsedAndUnusedCalcs(int? rowNumber)
		{
			if (rowNumber.HasValue)
			{
				RowUsedCalcs[rowNumber.Value] = GetUsedCalcs(Calcs, UnusedCalcs, out var unusedCalcs);
				RowUnusedCalcs[rowNumber.Value] = unusedCalcs;
			}

			Array.Clear(UnusedCalcs);
			Array.Clear(Calcs);
		}

		public long GetUsedCalcs(long[] calcs, Vector256<int>[] unusedCalcsV, out long unusedCalcs)
		{
			var lanes = Vector256<int>.Count;

			unusedCalcs = 0;
			var sourceBack = MemoryMarshal.Cast<Vector256<int>, int>(unusedCalcsV);

			var result = 0L;

			for (var vecPtr = 0; vecPtr < VectorsPerFlagRow; vecPtr++)
			{
				var unusedForThisVec = 0L;

				var laneOffset = vecPtr * lanes;
				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					unusedForThisVec += sourceBack[laneOffset + lanePtr];
				}

				unusedCalcs += unusedForThisVec;
				result += (calcs[vecPtr] * lanes) - unusedForThisVec;
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

			//var vecPtr = valueIndex * LimbCount;

			//for (var i = 0; i < LimbCount; i++)
			//{
			//	limbSet[i] = ZrsRowV[vecPtr++];
			//}
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

			//var vecPtr = valueIndex * LimbCount;

			//for (var i = 0; i < LimbCount; i++)
			//{
			//	limbSet[i] = ZisRowV[vecPtr++];
			//}
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

