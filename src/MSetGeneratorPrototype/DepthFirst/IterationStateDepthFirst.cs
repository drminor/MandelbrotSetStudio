using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class IterationStateDepthFirst : IIterationState
	{
		private readonly FP31Val[] _samplePointsY;

		private List<int> _inPlayBackingList;
		private readonly Vector256<int> ALL_BITS_SET;

		#region Constructor

		public IterationStateDepthFirst(FP31Val[] samplePointsX, FP31Val[] samplePointsY,
			MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors,
			bool increasingIterations, Vector256<int> targetIterationsVector)
		{

			_samplePointsY = samplePointsY;
			MapSectionVectors = mapSectionVectors;
			MapSectionZVectors = mapSectionZVectors;

			IncreasingIterations = increasingIterations;
			TargetIterationsVector = targetIterationsVector;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			RowCount = mapSectionZVectors.BlockSize.Height;
			ValuesPerRow = mapSectionZVectors.ValuesPerRow;
			VectorsPerZValueRow = mapSectionZVectors.VectorsPerZValueRow;
			VectorsPerRow = mapSectionZVectors.VectorsPerRow;

			RowNumber = null;
			CrsRowVArray = new FP31ValArray(samplePointsX);
			CiLimbSet = new Vector256<uint>[LimbCount];

			//CountsRow = mapSectionVectors.GetCountsRow(0);
			CountsRowV = new Vector256<int>[VectorsPerRow];
			CountsRowVF = new Vector256<float>[VectorsPerRow];

			RowHasEscaped = MapSectionZVectors.RowHasEscaped;
			RowUsedCalcs = new long[RowCount];
			RowUnusedCalcs = new long[RowCount];

			//HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(0);
			//ZrsRow = _mapSectionZVectors.GetZrsRow(0);
			//ZisRow = _mapSectionZVectors.GetZisRow(0);

			HasEscapedFlagsRowV = new Vector256<int>[VectorsPerRow];
			ZrsRowV = new Vector256<uint>[VectorsPerZValueRow];
			ZisRowV = new Vector256<uint>[VectorsPerZValueRow];

			MapSectionZVectors.FillHasEscapedFlagsRow(0, HasEscapedFlagsRowV);
			MapSectionZVectors.FillZrsRow(0, ZrsRowV);
			MapSectionZVectors.FillZisRow(0, ZisRowV);

			DoneFlags = new Vector256<int>[VectorsPerRow];
			UsedCalcs = new Vector256<int>[VectorsPerRow];
			UnusedCalcs = new Vector256<int>[VectorsPerRow];

			InPlayList = Enumerable.Range(0, VectorsPerRow).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
			ALL_BITS_SET = Vector256<int>.AllBitsSet;
		}

		#endregion

		#region Public Properties

		public MapSectionVectors MapSectionVectors { get; init; } 
		public MapSectionZVectors MapSectionZVectors { get; init; }

		public bool IncreasingIterations { get; private set; }
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

		//public Span<Vector256<int>> CountsRow { get; private set; }
		public Vector256<int>[] CountsRowV { get; private set; }
		public Vector256<float>[] CountsRowVF { get; private set; }

		public bool[] RowHasEscaped { get; init; }
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

		public void SetRowNumber(int rowNumber)
		{
			if (RowNumber.HasValue)
			{
				// Update the _mapSectionVectors with the current row properties
				MapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);

				MapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				//MapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowVF);

				MapSectionZVectors.UpdateFromZrsRow(RowNumber.Value, ZrsRowV);
				MapSectionZVectors.UpdateFromZisRow(RowNumber.Value, ZisRowV);
			}

			UpdateUsedAndUnusedCalcs(RowNumber);

			Array.Clear(HasEscapedFlagsRowV);

			Array.Clear(CountsRowV);
			//Array.Clear(CountsRowVF);

			Array.Clear(DoneFlags);

			if (rowNumber < RowCount)
			{
				RowNumber = rowNumber;

				FillCiLimbSetForRow(rowNumber, CiLimbSet);

				//InPlayList = _inPlayBackingList.ToArray();
				//InPlayListNarrow = BuildNarowInPlayList(InPlayList);
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
				MapSectionZVectors.UpdateFromHasEscapedFlagsRow(RowNumber.Value, HasEscapedFlagsRowV);
				MapSectionVectors.UpdateFromCountsRow(RowNumber.Value, CountsRowV);
				MapSectionZVectors.UpdateFromZrsRow(RowNumber.Value, ZrsRowV);
				MapSectionZVectors.UpdateFromZisRow(RowNumber.Value, ZisRowV);
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
						MapSectionZVectors.FillHasEscapedFlagsRow(rowNumber, HasEscapedFlagsRowV);

						//CountsRow = _mapSectionVectors.GetCountsRow(rowNumber);
						MapSectionVectors.FillCountsRow(rowNumber, CountsRowV);

						RowHasEscaped[rowNumber] = BuildTheInPlayBackingList(HasEscapedFlagsRowV, CountsRowV, _inPlayBackingList, DoneFlags);
						allSamplesForThisRowAreDone = _inPlayBackingList.Count == 0;

						if (!allSamplesForThisRowAreDone)
						{
							MapSectionZVectors.FillZrsRow(rowNumber, ZrsRowV);
							MapSectionZVectors.FillZisRow(rowNumber, ZisRowV);
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
			//CrsRowVArray.FillLimbSet(vectorIndex, limbSet);

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
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = ZrsRowV[vecPtr++];
			}
		}

		public void FillZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = ZisRowV[vecPtr++];
			}
		}

		public void UpdateZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				ZrsRowV[vecPtr++] = limbSet[i];
			}
		}

		public void UpdateZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				ZisRowV[vecPtr++] = limbSet[i];
			}
		}

		#endregion

		//#region Public Methods - SamplePoints And Counts -- Used by the HpMSetRowClient

		//public void FillSamplePointsXBuffer(byte[] buffer)
		//{
		//	var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(CrsRowVArray.Mantissas);
		//	Debug.Assert(buffer.Length >= sourceBack.Length, $"The Buffer is too short for filling SamplePointsX. BufferLength: {buffer.Length}, SamplePointsX Length: {CrsRowVArray.Mantissas.Length}");

		//	//for (var i = 0; i < sourceBack.Length; i++)
		//	//{
		//	//	buffer[i] = sourceBack[i];
		//	//}

		//	sourceBack.CopyTo(buffer);
		//}

		//public void FillSamplePointYBuffer(byte[] buffer)
		//{
		//	var sourceBack = MemoryMarshal.Cast<Vector256<uint>, byte>(CiLimbSet);
		//	Debug.Assert(buffer.Length >= sourceBack.Length, $"The Buffer is too short for filling the SamplePointY Vector. BufferLength: {buffer.Length}, SamplePointY Vector Length: {CiLimbSet.Length}");

		//	for (var i = 0; i < sourceBack.Length; i++)
		//	{
		//		buffer[i] = sourceBack[i];
		//	}
		//}

		//// Used by the HpMSetRowClient
		//public void FillCountsRow(int rowNumber, byte[] dest)
		//{
		//	var countsRowVAsB = MemoryMarshal.Cast<Vector256<int>, byte>(CountsRowV);

		//	for (var i = 0; i < dest.Length; i++)
		//	{
		//		dest[i] = countsRowVAsB[i];
		//	}
		//}

		//public void UpdateFromCountsRow(int rowNumber, byte[] source)
		//{
		//	var countsRowVAsB = MemoryMarshal.Cast<Vector256<int>, byte>(CountsRowV);

		//	for (var i = 0; i < source.Length; i++)
		//	{
		//		countsRowVAsB[i] = source[i];
		//	}
		//}

		//#endregion

		#region Private Methods

		private bool BuildTheInPlayBackingList(Vector256<int>[] hasEscapedFlagsRow, Span<Vector256<int>> countsRow, List<int> inPlayBackingList, Vector256<int>[] doneFlags)
		{
			inPlayBackingList.Clear();
			Array.Clear(doneFlags);

			var allHaveEscaped = true;

			for (var i = 0; i < VectorsPerRow; i++)
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

