using MSS.Common.APValSupport;
using MSS.Common.APValues;
using MSS.Types;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;

namespace MSetGeneratorPrototype
{
	public class VecMath9
	{
		#region Private Properties

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

		private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
		private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private static readonly int _lanes = Vector256<uint>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private Vector256<int> _thresholdVector;

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public VecMath9(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			ValueCount = valueCount;
			VecCount = Math.DivRem(ValueCount, _lanes, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException($"The valueCount must be an even multiple of {_lanes}.");
			}

			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, VecCount).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			// Initially, all values are 'In Play.'
			DoneFlags = new bool[ValueCount];

			BlockPosition = new BigVector();
			RowNumber = 0;

			ApFixedPointFormat = apFixedPointFormat;
			MaxIntegerValue = ScalarMathHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint, IsSigned);
			
			var thresholdMsl = FP31ValHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);
			_thresholdVector = Vector256.Create(thresholdMsl).AsInt32();

			_squareResult1Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);

			UnusedCalcs = new long[valueCount];
		}

		#endregion

		#region Mantissa Support

		private Memory<ulong>[] BuildMantissaMemoryArrayL(int limbCount, int valueCount)
		{
			var ba = BuildMantissaBackingArrayL(limbCount, valueCount);
			var result = BuildMantissaMemoryArrayL(ba);

			return result;
		}

		private ulong[][] BuildMantissaBackingArrayL(int limbCount, int valueCount)
		{
			var result = new ulong[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new ulong[valueCount];
			}

			return result;
		}

		private Memory<ulong>[] BuildMantissaMemoryArrayL(ulong[][] backingArray)
		{
			var result = new Memory<ulong>[backingArray.Length];

			for (var i = 0; i < backingArray.Length; i++)
			{
				result[i] = new Memory<ulong>(backingArray[i]);
			}

			return result;
		}

		private void ClearManatissMemsL(Memory<ulong>[] mantissaMems, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var indexes = InPlayListNarrow;

				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUL(mantissaMems[j]);

					for (var i = 0; i < indexes.Length; i++)
					{
						vectors[indexes[i]] = Vector256<ulong>.Zero;
					}
				}
			}
			else
			{
				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUL(mantissaMems[j]);

					for (var i = 0; i < vectors.Length; i++)
					{
						vectors[i] = Vector256<ulong>.Zero;
					}
				}
			}
		}

		private Span<Vector256<ulong>> GetLimbVectorsUL(Memory<ulong> memory)
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}

		private Span<Vector256<uint>> GetLimbVectorsUW(Memory<uint> memory)
		{
			Span<Vector256<uint>> result = MemoryMarshal.Cast<uint, Vector256<uint>>(memory.Span);
			return result;
		}

		private int[] BuildNarowInPlayList(int[] inPlayList)
		{
			var result = new int[inPlayList.Length * 2];

			var indexes = inPlayList;
			var resultIdxPtr = 0;

			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++, resultIdxPtr += 2)
			{
				var resultIdx = indexes[idxPtr] * 2;

				result[resultIdxPtr] = resultIdx;
				result[resultIdxPtr + 1] = resultIdx + 1;
			}

			return result;
		}

		#endregion

		#region Public Properties

		public bool IsSigned => true;

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public int[] InPlayList { get; set; }			// Vector-Level
		public int[] InPlayListNarrow { get; set; }		// Vector-Level for Squaring
		public bool[] DoneFlags { get; set; }			// Value-Level

		public BigVector BlockPosition { get; set; }
		public int RowNumber { get; set; }

		public uint MaxIntegerValue { get; init; }

		public int NumberOfMultiplications { get; private set; }
		public int NumberOfAdditions { get; private set; }
		public long NumberOfConversions { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		public long[] UnusedCalcs { get; init; }

		#endregion

		#region Multiply and Square

		// By Deck
		public void Square(FP31Deck a, FP31Deck result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			FPValues non2CFPValues = a.ConvertFrom2C(InPlayList);
			NumberOfConversions++;

			// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			SquareInternal(non2CFPValues, _squareResult1Mems);
			SumThePartials(_squareResult1Mems, _squareResult2Mems);

			ShiftAndTrim(_squareResult2Mems, non2CFPValues);

			result.UpdateFrom(non2CFPValues, InPlayList, _lanes);
		}

		// By Limb, By Vector
		private void SquareInternal(FPValues a, Memory<ulong>[] resultLimbs)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			ClearManatissMemsL(resultLimbs, onlyInPlayItems: false);

			var indexes = InPlayListNarrow;
			for (int j = 0; j < a.LimbCount; j++)
			{
				for (int i = j; i < a.LimbCount; i++)
				{
					var left = a.GetLimbVectorsUW(j);
					var right = a.GetLimbVectorsUW(i);

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectorsUL(resultLimbs[resultPtr]);
					var resultHighs = GetLimbVectorsUL(resultLimbs[resultPtr + 1]);

					for(var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var productVector = Avx2.Multiply(left[idx], right[idx]);
						NumberOfMultiplications++;

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var highs = Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB); // Create new ulong from bits 32 - 63.
						var lows = Avx2.And(productVector, HIGH33_MASK_VEC_L);						// Create new ulong from bits 0 - 31.
						NumberOfSplits++;

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
						NumberOfAdditions += 2;
					}
				}
			}
		}

		#endregion

		#region Multiply Post Processing

		// By Vector, By Limb
		private void SumThePartials(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var limbCnt = mantissaMems.Length;

			var indexes = InPlayListNarrow;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var limbPtr = 0;

				var pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
				var resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

				var carries = Avx2.ShiftRightLogical(pProductVectors[idx], EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
				resultVectors[idx] = Avx2.And(pProductVectors[idx], HIGH33_MASK_VEC_L);					// The low 31 bits of the sum is the result.
				NumberOfSplits++;

				for (; limbPtr < limbCnt; limbPtr++)
				{
					pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
					resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

					var withCarries = Avx2.Add(pProductVectors[idx], carries);

					carries = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);				// The high 31 bits of sum becomes the new carry.
					resultVectors[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);						// The low 31 bits of the sum is the result.
					NumberOfSplits++;
				}
			}
		}
		
		// By Limb, By Vector
		private void ShiftAndTrim(Memory<ulong>[] mantissaMems, FPValues result)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			Memory<ulong>[] resultLimbs = result.MantissaMemories;

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
			{
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[limbPtr]);

				if (sourceIndex > 0)
				{
					var limbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					var prevLimbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex - 1]);
					ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);
				}
				else
				{
					var limbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					ShiftAndCopyBits(limbVecs, resultLimbVecs);
				}
			}
		}

		// By Vector
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> prevSource, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayListNarrow;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC_L);

				// Take the top shiftAmount of bits from the previous limb
				var previousLimbVector = Avx2.And(prevSource[idx], HIGH33_MASK_VEC_L); // TODO: Combine this and the next operation.
				result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(previousLimbVector, (byte)(31 - shiftAmount)));

				NumberOfSplits += 2;
			}
		}

		// By Vector
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayListNarrow;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC_L);

				NumberOfSplits++;
			}
		}

		#endregion

		#region Add and Subtract

		// By Deck
		public void Sub(FP31Deck a, FP31Deck b, FP31Deck c)
		{
			NumberOfConversions++;

			var notB = b.Negate(InPlayList);
			Add(a, notB, c);
		}

		// By Vector, By Limb
		public void Add(FP31Deck a, FP31Deck b, FP31Deck c)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				//var resultPtr = idx * _lanes;

				var carryVector = Vector256<uint>.Zero;

				//var aCarryOccured = false;

				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					var limbVecsA = a.GetLimbVectorsUW(limbPtr);
					var limbVecsB = b.GetLimbVectorsUW(limbPtr);
					var resultLimbVecs = c.GetLimbVectorsUW(limbPtr);

					var va = Avx2.And(limbVecsA[idx], HIGH33_MASK_VEC);
					var vb = Avx2.And(limbVecsB[idx], HIGH33_MASK_VEC);

					var sumVector = Avx2.Add(va, vb);
					var newValuesVector = Avx2.Add(sumVector, carryVector);
					NumberOfAdditions += 2;

					var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);	// The high 31 bits of sum becomes the new carry.
					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);						// The low 31 bits of the sum is the result.
					resultLimbVecs[idx] = limbValues;
					NumberOfSplits++;


					//var (limbValues, newCarries) = GetResultWithCarry(newValuesVector, isMsl: (limbPtr == LimbCount - 1), out var aCarryOccured);
					//resultLimbVecs[idx] = limbValues;
					//NumberOfGetCarries++;

					if (USE_DET_DEBUG)
						ReportForAddition(limbPtr, va, vb, carryVector, newValuesVector, limbValues, newCarries);

					carryVector = newCarries;
				}

				//if (aCarryOccured)
				//{
				//	for (var i = 0; i < _lanes; i++)
				//	{
				//		if (!DoneFlags[resultPtr + i])
				//		{
				//			if (carryVector.GetElement(i) > 0)
				//			{
				//				DoneFlags[resultPtr + i] = true;
				//			}
				//		}
				//	}
				//}

			}
		}

		private void ReportForAddition(int step, Vector256<uint> left, Vector256<uint> right, Vector256<uint> carry, Vector256<uint> nv, Vector256<uint> lo, Vector256<uint> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var rightVal0 = right.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = FP31ValHelper.ConvertFrom2C(leftVal0);
			var rd = FP31ValHelper.ConvertFrom2C(rightVal0);
			var cd = FP31ValHelper.ConvertFrom2C(carryVal0);
			var nvd = FP31ValHelper.ConvertFrom2C(nvVal0);
			var hid = FP31ValHelper.ConvertFrom2C(newCarryVal0);
			var lod = FP31ValHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0;
			var unSNv = leftVal0 + rightVal0 + carryVal0;


			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {leftVal0:X4}, {rightVal0:X4} wc:{carryVal0:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		#endregion

		#region Comparison

		//// By Deck
		//public void IsGreaterOrEqThanThreshold(FP31Deck a, bool[] results)
		//{
		//	var left = a.GetLimbVectorsUW(LimbCount - 1);
		//	var right = _thresholdVector;

		//	IsGreaterOrEqThan(left, right, results);
		//}

		//// By Vector
		//private void IsGreaterOrEqThan(Span<Vector256<uint>> left, Vector256<int> right, bool[] results)
		//{
		//	var indexes = InPlayList;
		//	for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
		//	{
		//		var idx = indexes[idxPtr];
		//		var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
		//		var resultVector = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);

		//		var x = Avx2.MoveMask(resultVector.AsByte());
		//		NumberOfGrtrThanOps++;

		//		var vectorPtr = idx * _lanes;

		//		for (var i = 0; i < _lanes; i++)
		//		{
		//			results[vectorPtr + i] = resultVector.GetElement(i) == -1;
		//		}
		//	}
		//}

		// By Deck
		public void IsGreaterOrEqThanThreshold(FP31Deck a, Memory<int> results)
		{
			var left = a.GetLimbVectorsUW(LimbCount - 1);
			var right = _thresholdVector;

			Span<Vector256<int>> resultVectors = MemoryMarshal.Cast<int, Vector256<int>>(results.Span);
			IsGreaterOrEqThan(left, right, resultVectors);
		}

		// By Vector
		private void IsGreaterOrEqThan(Span<Vector256<uint>> left, Vector256<int> right, Span<Vector256<int>> results)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				results[idx] = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);
				NumberOfGrtrThanOps++;
			}
		}

		#endregion

		#region Mantissa Support - Not Used

		private Memory<uint>[] BuildMantissaMemoryArray(int limbCount, int valueCount)
		{
			var ba = BuildMantissaBackingArray(limbCount, valueCount);
			var result = BuildMantissaMemoryArray(ba);

			return result;
		}

		private uint[][] BuildMantissaBackingArray(int limbCount, int valueCount)
		{
			var result = new uint[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new uint[valueCount];
			}

			return result;
		}

		private Memory<uint>[] BuildMantissaMemoryArray(uint[][] backingArray)
		{
			var result = new Memory<uint>[backingArray.Length];

			for (var i = 0; i < backingArray.Length; i++)
			{
				result[i] = new Memory<uint>(backingArray[i]);
			}

			return result;
		}

		private void ClearManatissMems(Memory<uint>[] mantissaMems, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var indexes = InPlayList;

				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUW(mantissaMems[j]);

					for (var i = 0; i < indexes.Length; i++)
					{
						vectors[indexes[i]] = Vector256<uint>.Zero;
					}
				}
			}
			else
			{
				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUW(mantissaMems[j]);

					for (var i = 0; i < VecCount; i++)
					{
						vectors[i] = Vector256<uint>.Zero;
					}
				}
			}
		}

		private void ClearBackingArray(ulong[][] backingArray, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var template = new ulong[_lanes];

				var indexes = InPlayListNarrow;

				for (var j = 0; j < backingArray.Length; j++)
				{
					for (var i = 0; i < indexes.Length; i++)
					{
						Array.Copy(template, 0, backingArray[j], indexes[i] * _lanes, _lanes);
					}
				}
			}
			else
			{
				var vc = backingArray[0].Length;

				for (var j = 0; j < backingArray.Length; j++)
				{
					for (var i = 0; i < vc; i++)
					{
						backingArray[j][i] = 0;
					}
				}
			}
		}

		#endregion

	}
}
