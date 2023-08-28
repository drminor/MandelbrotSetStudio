using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class IterationStateDepthFirstNoZ : IIterationState
	{
		private readonly FP31Val[] _samplePointsY;

		private List<int> _inPlayBackingList;
		#region Constructor

		public IterationStateDepthFirstNoZ(FP31Val[] samplePointsX, FP31Val[] samplePointsY,
			MapSectionVectors mapSectionVectors, int targetIterations)
		{
			_samplePointsY = samplePointsY;
			MapSectionVectors = mapSectionVectors;

			TargetIterations = targetIterations;
			TargetIterationsVector = Vector256.Create(targetIterations);

			ValueCount = samplePointsX.Length * samplePointsY.Length;
			LimbCount = samplePointsX[0].LimbCount;
			RowCount = samplePointsY.Length;
			ValuesPerRow = samplePointsX.Length;
			VectorsPerZValueRow = -1;
			VectorsPerRow = ValuesPerRow / Vector256<uint>.Count;

			RowNumber = null;
			CrsRowVArray = new FP31ValArray(samplePointsX);
			CiLimbSet = new Vector256<uint>[LimbCount];

			CountsRowV = new Vector256<int>[VectorsPerRow];
			EscapeVelocities = new ushort[ValuesPerRow];

			RowHasEscaped = new bool[0];
			RowUsedCalcs = new long[RowCount];
			RowUnusedCalcs = new long[RowCount];

			HasEscapedFlagsRowV = new Vector256<int>[0];

			DoneFlags = new Vector256<int>[VectorsPerRow];
			UsedCalcs = new Vector256<int>[VectorsPerRow];
			UnusedCalcs = new Vector256<int>[VectorsPerRow];

			InPlayList = Enumerable.Range(0, VectorsPerRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
		}

		#endregion

		#region Public Properties

		public bool HaveZValues => false;
		public MapSectionVectors MapSectionVectors { get; init; }
		public MapSectionZVectors? MapSectionZVectors => null;

		public bool IncreasingIterations => false;
		public int TargetIterations { get; private set; }	
		public Vector256<int> TargetIterationsVector { get; private set; }

		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int RowCount { get; init; }
		public int VectorsPerZValueRow { get; init; }
		public int VectorsPerRow { get; init; }
		public int ValuesPerRow { get; init; }

		public int? RowNumber { get; private set; }

		public FP31ValArray CrsRowVArray { get; private set; }
		public Vector256<uint>[] CiLimbSet { get; private set; }

		public Vector256<int>[] CountsRowV { get; private set; }
		public ushort[] EscapeVelocities { get; private set; }

		public bool[] RowHasEscaped { get; init; }
		public long[] RowUnusedCalcs { get; init; }
		public long[] RowUsedCalcs { get; init; }

		public Vector256<int>[] HasEscapedFlagsRowV { get; private set; }

		public Vector256<int>[] DoneFlags { get; private set; }
		public Vector256<int>[] UsedCalcs { get; private set; }
		public Vector256<int>[] UnusedCalcs { get; private set; }

		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		#endregion

		#region Public Methods

		public void SetRowNumber(int rowNumber)
		{
			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
				//MapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);
				MapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				MapSectionVectors.UpdateFromEscapeVelocitiesRow(RowNumber.Value, EscapeVelocities);
			}

			UpdateUsedAndUnusedCalcs(RowNumber);

			Array.Clear(HasEscapedFlagsRowV);
			Array.Clear(CountsRowV);
			Array.Clear(DoneFlags);

			if (rowNumber < RowCount)
			{
				RowNumber = rowNumber;

				FillCiLimbSetForRow(rowNumber, CiLimbSet);
			}
			else
			{
				RowNumber = null;
			}
		}

		// Returns the next row number, or null, if all rows have been visited.
		public int? GetNextRowNumber()
		{
			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
				//MapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);
				MapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
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
						//MapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

						MapSectionVectors.FillCountsRow(rowNumber, CountsRowV);
						RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(CountsRowV, _inPlayBackingList, DoneFlags);
						allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;

						if (allSamplesForThisRowAreDone)
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
					//Array.Clear(HasEscapedFlagsRowV);
					Array.Clear(CountsRowV);
					Array.Clear(DoneFlags);

					_inPlayBackingList.Clear();
					for (var i = 0; i < VectorsPerRow; i++)
					{
						_inPlayBackingList.Add(i);
					}
				}
			}

			if (rowNumber < RowCount)
			{
				RowNumber = rowNumber;

				FillCiLimbSetForRow(rowNumber, CiLimbSet);

				InPlayList = _inPlayBackingList.ToArray();
				InPlayListNarrow = BuildNarowInPlayList(InPlayList);
			}
			else
			{
				RowNumber = null;
			}

			return RowNumber;
		}

		#endregion

		#region Public Methods - Fill / Update Limb Sets

		public void FillCrLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = CrsRowVArray.Mantissas[vecPtr++];
			}
		}

		public void FillCiLimbSetForRow(int rowNumber, Vector256<uint>[] limbSet)
		{
			var fp31Val = _samplePointsY[rowNumber];

			for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				limbSet[limbPtr] = Vector256.Create(fp31Val.Mantissa[limbPtr]);
			}
		}

		public void FillZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
			}
		}

		public void FillZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
			}
		}

		public void UpdateZrLimbSet(int rowNumber, int vectorIndex, Vector256<uint>[] limbSet)
		{
			throw new NotImplementedException();
		}

		public void UpdateZiLimbSet(int rowNumber, int vectorIndex, Vector256<uint>[] limbSet)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private Methods

		private bool BuildTheInPlayBackingList(Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags)
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags);

			var allHaveEscaped = false;

			for (var i = 0; i < VectorsPerRow; i++)
			{

				// Compare the new Counts with the TargetIterations
				var targetReachedCompVec = Avx2.CompareGreaterThan(countsRow[i], TargetIterationsVector);

				// Update the DoneFlag, only if the just updatedHaveEscapedFlagsV is true or targetIterations was reached.
				//doneFlags[i] = Avx2.Or(hasEscapedFlagsRow[i], targetReachedCompVec);
				doneFlags[i] = targetReachedCompVec;

				var compositeIsDone = Avx2.MoveMask(doneFlags[i].AsByte());
				if (compositeIsDone != -1)
				{
					inPlayBackingList.Add(i);
				}
				else
				{
					Debug.WriteLine("Done Already? Vec not escaped, but all have reached new target iterations.");
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

		private long GetUsedCalcs(Vector256<int>[] usedCalcsV, Vector256<int>[] unusedCalcsV, out long unusedCalcs)
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

		#endregion
	}
}

