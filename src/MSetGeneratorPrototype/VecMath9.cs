using MongoDB.Bson.Serialization.Options;
using MSS.Common;
using MSS.Common.APValSupport;
using MSS.Common.APValues;
using MSS.Types;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSetGeneratorPrototype
{
	public class VecMath9
	{
		#region Private Properties

		private static readonly int _lanes = Vector256<uint>.Count;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

		private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
		private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const uint RESERVED_BIT_MASK = 0x80000000;
		private static readonly Vector256<uint> RESERVED_BIT_MASK_VEC = Vector256.Create(RESERVED_BIT_MASK);

		private static readonly Vector256<uint> ALL_BITS_SET_VEC = Vector256<uint>.AllBitsSet;

		private static readonly Vector256<int> ZERO_VEC = Vector256<int>.Zero;
		private static readonly Vector256<uint> UINT_ZERO_VEC = Vector256<uint>.Zero;

		private const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
		private static readonly Vector256<int> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

		private ulong[][] _squareResult0Ba;
		private Memory<ulong>[] _squareResult0Mems;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private ulong[][] _squareResult3Ba;
		private Memory<ulong>[] _squareResult3Mems;

		private uint[][] _negationResultBa;
		private Memory<uint>[] _negationResultMems;

		private Vector256<uint>[] _ones;

		private Vector256<int> _thresholdVector;

		private static readonly bool USE_DET_DEBUG = false;

		private Vector256<byte> _lutLow;
		private Vector256<byte> _lutHigh;
		private Vector256<uint> _nibbleMask;
		private Vector256<uint> _byteOffset;

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
			MaxIntegerValue = FP31ValHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint, IsSigned);

			var thresholdMsl = FP31ValHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);
			_thresholdVector = Vector256.Create(thresholdMsl).AsInt32();

			MathOpCounts = new MathOpCounts();

			_squareResult0Ba = BuildMantissaBackingArrayL(LimbCount * 2, ValueCount);
			_squareResult0Mems = BuildMantissaMemoryArrayL(_squareResult0Ba);

			_squareResult1Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);

			_squareResult3Ba = BuildMantissaBackingArrayL(LimbCount, ValueCount);
			_squareResult3Mems = BuildMantissaMemoryArrayL(_squareResult3Ba);

			_negationResultBa = BuildMantissaBackingArray(LimbCount, ValueCount);
			_negationResultMems = BuildMantissaMemoryArray(_negationResultBa);

			var justOne = Vector256.Create(1u);
			_ones = Enumerable.Repeat(justOne, VecCount).ToArray();

			UnusedCalcs = new long[valueCount];

			_lutLow = Vector256.Create(0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0, 32, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0, 32).AsByte();
			_lutHigh = Vector256.Create(4, 5, 4, 6, 4, 5, 4, 7, 4, 5, 4, 6, 4, 5, 4, 32, 4, 5, 4, 6, 4, 5, 4, 7, 4, 5, 4, 6, 4, 5, 4, 32).AsByte();

			_nibbleMask = Vector256.Create(0x0F).AsUInt32();
			_byteOffset = Vector256.Create(0x18100800).AsUInt32();
		}

		#endregion

		#region Mantissa Support - Long

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

		private Span<Vector256<uint>> GetLimbVectorsUW(Memory<ulong> memory)
		{
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(memory.Span);

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

		#region Mantissa Support - Short

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

		#region Public Properties

		public bool IsSigned => true;

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public int[] InPlayList { get; set; }           // Vector-Level
		public int[] InPlayListNarrow { get; set; }     // Vector-Level for Squaring
		public bool[] DoneFlags { get; set; }           // Value-Level

		public BigVector BlockPosition { get; set; }
		public int RowNumber { get; set; }

		public uint MaxIntegerValue { get; init; }

		public MathOpCounts MathOpCounts { get; init; }

		//public int NumberOfMultiplications { get; private set; }
		//public int NumberOfAdditions { get; private set; }
		//public long NumberOfConversions { get; private set; }
		//public long NumberOfSplits { get; private set; }
		//public long NumberOfGetCarries { get; private set; }
		//public long NumberOfGrtrThanOps { get; private set; }

		public long[] UnusedCalcs { get; init; }

		#endregion

		#region Multiply and Square

		//// By Deck
		//public void SquareOld(FP31Deck a, FP31Deck result)
		//{
		//	// Convert back to standard, i.e., non two's compliment.
		//	// Our multiplication routines don't support 2's compliment.
		//	// The result of squaring is always positive,
		//	// so we don't have to convert them to 2's compliment afterwards.

		//	CheckReservedBitIsClear(a.MantissaMemories, "Squaring");

		//	FPValues non2CFPValues = a.ConvertFrom2C(InPlayList);
		//	MathOpCounts.NumberOfConversions++;

		//	// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
		//	InPlayListNarrow = BuildNarowInPlayList(InPlayList);

		//	SquareInternal(non2CFPValues.MantissaMemories, _squareResult1Mems);
		//	SumThePartials(_squareResult1Mems, _squareResult2Mems);
		//	ShiftAndTrim(_squareResult2Mems, non2CFPValues.MantissaMemories);

		//	result.UpdateFrom(non2CFPValues.Mantissas);
		//}

		// By Deck
		public void Square(FP31Deck a, FP31Deck result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			CheckReservedBitIsClear(a.MantissaMemories, "Squaring");

			ConvertFrom2C(a.MantissaMemories, a.Mantissas, _negationResultMems, _negationResultBa, InPlayList);
			FP31ValHelper.ExpandTo(_negationResultBa, _squareResult0Ba);
			MathOpCounts.NumberOfConversions++;

			// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			SquareInternal(_squareResult0Mems, _squareResult1Mems);
			SumThePartials(_squareResult1Mems, _squareResult2Mems);
			ShiftAndTrim(_squareResult2Mems, _squareResult3Mems);

			FP31ValHelper.PackTo(_squareResult3Ba, result.Mantissas);
		}


		// By Limb, By Vector
		private void SquareInternal(Memory<ulong>[] sourceLimbs, Memory<ulong>[] resultLimbs)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			ClearManatissMemsL(resultLimbs, onlyInPlayItems: false);

			var indexes = InPlayListNarrow;
			for (int j = 0; j < LimbCount; j++)
			{
				for (int i = j; i < LimbCount; i++)
				{
					var left = GetLimbVectorsUW(sourceLimbs[j]);
					var right = GetLimbVectorsUW(sourceLimbs[i]);

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectorsUL(resultLimbs[resultPtr]);
					var resultHighs = GetLimbVectorsUL(resultLimbs[resultPtr + 1]);

					for(var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var productVector = Avx2.Multiply(left[idx], right[idx]);
						MathOpCounts.NumberOfMultiplications++;

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var highs = Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB); // Create new ulong from bits 32 - 63.
						var lows = Avx2.And(productVector, HIGH33_MASK_VEC_L);                      // Create new ulong from bits 0 - 31.
						MathOpCounts.NumberOfSplits++;

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
						MathOpCounts.NumberOfAdditions += 2;
					}
				}
			}
		}

		#endregion

		#region Multiply Post Processing

		// By Limb, By Vector
		private void SumThePartials(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var carryVectors = Enumerable.Repeat(Vector256<ulong>.Zero, VecCount * 2).ToArray();
			var indexes = InPlayListNarrow;

			var limbCnt = mantissaMems.Length;

			for (int limbPtr = 0; limbPtr < limbCnt; limbPtr++)
			{
				var pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
				var resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					var withCarries = Avx2.Add(pProductVectors[idx], carryVectors[idx]);

					resultVectors[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);						// The low 31 bits of the sum is the result.
					carryVectors[idx] = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.
					MathOpCounts.NumberOfSplits++;
				}
			}
		}

		// By Limb, By Vector
		private void ShiftAndTrim(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var shiftAmount = BitsBeforeBP;
			byte inverseShiftAmount = (byte)(31 - shiftAmount);
			var indexes = InPlayListNarrow;

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
			{
				var result = GetLimbVectorsUL(resultLimbs[limbPtr]);

				if (sourceIndex > 0)
				{
					var source = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					var prevSource = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex - 1]);

					//ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);

					for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC_L);

						// Take the top shiftAmount of bits from the previous limb
						result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(Avx2.And(prevSource[idx], HIGH33_MASK_VEC_L), inverseShiftAmount));

						MathOpCounts.NumberOfSplits += 2;
					}
				}
				else
				{
					var source = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					//ShiftAndCopyBits(limbVecs, resultLimbVecs);
					
					for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC_L);

						MathOpCounts.NumberOfSplits++;
					}
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

				MathOpCounts.NumberOfSplits += 2;
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

				MathOpCounts.NumberOfSplits++;
			}
		}

		#endregion

		#region Add and Subtract

		// By Deck
		public void Sub(FP31Deck a, FP31Deck b, FP31Deck c)
		{
			MathOpCounts.NumberOfConversions++;

			//var sourceLimbsA = a.MantissaMemories;
			//CheckReservedBitIsClear(sourceLimbsA, "Negating A");

			var sourceLimbsB = b.MantissaMemories;
			CheckReservedBitIsClear(sourceLimbsB, "Negating B");

			//var notB = b.Negate(InPlayList);
			//AddInternal(a.MantissaMemories, notB.MantissaMemories, c.MantissaMemories);

			Negate(b.MantissaMemories, _negationResultMems, InPlayList);
			AddInternal(a.MantissaMemories, _negationResultMems, c.MantissaMemories);
		}

		public void Add(FP31Deck a, FP31Deck b, FP31Deck c)
		{
			AddInternal(a.MantissaMemories, b.MantissaMemories, c.MantissaMemories);
		}

		// By Limb, By Vector
		private void AddInternal(Memory<uint>[] a, Memory<uint>[] b, Memory<uint>[] c)
		{
			var carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, VecCount).ToArray();
			var indexes = InPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecsA = GetLimbVectorsUW(a[limbPtr]);
				var limbVecsB = GetLimbVectorsUW(b[limbPtr]);
				var resultLimbVecs = GetLimbVectorsUW(c[limbPtr]);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					//var va = Avx2.And(limbVecsA[idx], HIGH33_MASK_VEC);
					//var vb = Avx2.And(limbVecsB[idx], HIGH33_MASK_VEC);

					var left = limbVecsA[idx];
					var right = limbVecsB[idx];

					var sumVector = Avx2.Add(left, right);
					var newValuesVector = Avx2.Add(sumVector, carryVectors[idx]);
					MathOpCounts.NumberOfAdditions += 2;

					var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					resultLimbVecs[idx] = limbValues;
					MathOpCounts.NumberOfSplits++;

					if (USE_DET_DEBUG)
						ReportForAddition(limbPtr, left, right, carryVectors[idx], newValuesVector, limbValues, newCarries);

					carryVectors[idx] = newCarries;
				}
			}

			//for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			//{
			//	var idx = indexes[idxPtr];

			//	var cVec = carryVectors[idx];

			//	var resultPtr = idx * _lanes;

			//	for (var i = 0; i < _lanes; i++)
			//	{
			//		if (cVec.GetElement(i) > 1)
			//		{
			//			DoneFlags[resultPtr + i] = true;
			//		}
			//	}
			//}

		}

		// By Limb, By Vector
		private void Negate(Memory<uint>[] sourceLimbs, Memory<uint>[] resultLimbs, int[] inPlayList)
		{
			var carryVectors = _ones;
			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = GetLimbVectorsUW(sourceLimbs[limbPtr]);
				var resultLimbVecs = GetLimbVectorsUW(resultLimbs[limbPtr]);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					var left = limbVecs[idx];

					var notVector = Avx2.Xor(left, ALL_BITS_SET_VEC);
					var newValuesVector = Avx2.Add(notVector, carryVectors[idx]);
					MathOpCounts.NumberOfAdditions += 2;

					var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					resultLimbVecs[idx] = limbValues;
					MathOpCounts.NumberOfSplits++;

					if (USE_DET_DEBUG)
						ReportForNegation(limbPtr, left, carryVectors[idx], newValuesVector, limbValues, newCarries);

					carryVectors[idx] = newCarries;
				}
			}
		}

		private void ConvertFrom2C(Memory<uint>[] sourceLimbs, uint[][] sourceVals, Memory<uint>[] resultLimbs, uint[][] resultVals, int[] inPlayList)
		{
			CheckReservedBitIsClear(sourceLimbs, "ConvertFrom2C");

			var indexes = inPlayList;

			var signBitFlags = new int[VecCount];
			var signBitVecs = new Vector256<int>[VecCount];
			var msls = GetLimbVectorsUW(sourceLimbs[^1]);

			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var left = Avx2.And(msls[idx].AsInt32(), TEST_BIT_30_VEC);
				var areZerosVec = Avx2.CompareEqual(left, ZERO_VEC); // dst[i+31:i] := ( a[i+31:i] == b[i+31:i] ) ? 0xFFFFFFFF : 0
				signBitVecs[idx] = areZerosVec;
				signBitFlags[idx] = Avx2.MoveMask(areZerosVec.AsByte());
			}

			var carryVectors = _ones;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = GetLimbVectorsUW(sourceLimbs[limbPtr]);
				var resultLimbVecs = GetLimbVectorsUW(resultLimbs[limbPtr]);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					if (signBitFlags[idx] == -1)
					{
						// All positive values
						resultLimbVecs[idx] = limbVecs[idx];
					}
					else
					{
						// All negative values or a mix

						var left = limbVecs[idx];

						var notVector = Avx2.Xor(left, ALL_BITS_SET_VEC);
						var newValuesVector = Avx2.Add(notVector, carryVectors[idx]);
						MathOpCounts.NumberOfAdditions += 2;

						var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
						var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
						resultLimbVecs[idx] = limbValues;
						MathOpCounts.NumberOfSplits++;

						if (USE_DET_DEBUG)
							ReportForNegation(limbPtr, left, carryVectors[idx], newValuesVector, limbValues, newCarries);

						carryVectors[idx] = newCarries;

						if (signBitFlags[idx] != 0)
						{
							// We have a mix of positive and negative values.
							// For each postive value, set each vector element back to the original value.

							var signBitVec = signBitVecs[idx];
							var resultPtr = idx * _lanes;

							for (var i = 0; i < _lanes; i++)
							{
								var signBit = signBitVec.GetElement(i);
								if (signBit == -1)
								{
									var valPtr = resultPtr + i;

									resultVals[limbPtr][valPtr] = sourceVals[limbPtr][valPtr];
								}
							}
						}

					}

				}
			}
		}

		private void CheckReservedBitIsClear(Memory<uint>[] resultLimbs, string description)
		{
			var sb = new StringBuilder();

			var indexes = InPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbs = GetLimbVectorsUW(resultLimbs[limbPtr]);

				var oneFound = false;

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					var justReservedBit = Avx2.And(limbs[idx], RESERVED_BIT_MASK_VEC);

					var cFlags = Avx2.CompareEqual(justReservedBit, Vector256<uint>.Zero);
					var cComposite = Avx2.MoveMask(cFlags.AsByte());
					if (cComposite != -1)
					{
						sb.AppendLine($"Top bit was set at.\t{idxPtr}\t{limbPtr}\t{cComposite}.");
					}
				}

				if (oneFound)
				{
					sb.AppendLine();
				}
			}

			var errors = sb.ToString();

			if (errors.Length > 1)
			{
				throw new InvalidOperationException($"Found a set ReservedBit while {description}.Results:\nIdx\tlimb\tVal\n {errors}");
			}
		}

		// By Vector
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

		// By Vector
		private void ReportForNegation(int step, Vector256<uint> left, Vector256<uint> carry, Vector256<uint> nv, Vector256<uint> lo, Vector256<uint> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = FP31ValHelper.ConvertFrom2C(leftVal0);
			var cd = FP31ValHelper.ConvertFrom2C(carryVal0);
			var nvd = FP31ValHelper.ConvertFrom2C(nvVal0);
			var hid = FP31ValHelper.ConvertFrom2C(newCarryVal0);
			var lod = FP31ValHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0;
			var unSNv = leftVal0 + carryVal0;


			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {leftVal0:X4}, wc:{carryVal0:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		// By Limb, By Vector
		private void NegateExp(Memory<uint>[] sourceLimbs, Memory<uint>[] resultLimbs, int[] inPlayList)
		{
			CheckReservedBitIsClear(sourceLimbs, "Negate");

			ClearManatissMems(resultLimbs, onlyInPlayItems: true);

			var foundASetBit = Enumerable.Repeat(Vector256<int>.Zero, VecCount).ToArray();
			var tzCnts = Enumerable.Repeat(Vector256<uint>.Zero, VecCount).ToArray();

			var indexes = InPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = GetLimbVectorsUW(sourceLimbs[limbPtr]);
				var resultLimbVecs = GetLimbVectorsUW(resultLimbs[limbPtr]);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					if (Avx.TestZ(limbVecs[idx], ALL_BITS_SET_VEC))
					{
						resultLimbVecs[idx] = UINT_ZERO_VEC;
					}
					else
					{
						tzCnts[idx] = GetTzCnts(limbVecs[idx]);
					}
				}
			}

			//var result = Clone();

			//var indexes = inPlayList;
			//for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			//{
			//	var idx = indexes[idxPtr];
			//	var resultPtr = idx * Lanes;

			//	for (var i = 0; i < Lanes; i++)
			//	{
			//		var valPtr = resultPtr + i;
			//		var limbs = result.GetMantissa(valPtr);
			//		var non2CPWLimbs = FP31ValHelper.FlipBitsAndAdd1(limbs);
			//		result.SetMantissa(valPtr, non2CPWLimbs);
			//	}
			//}

			//return result;
		}

		// Credit: YumiYumiYumi
		// https://old.reddit.com/r/simd/comments/b3k1oa/looking_for_sseavx_bitscan_discussions/
		private Vector256<uint> GetTzCnts(Vector256<uint> limbVecs)
		{

			//const __m256i lut_lo = _mm256_set_epi8(
			//	0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0, 32,
			//	0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0, 32
			//);
			//const __m256i lut_hi = _mm256_set_epi8(
			//	4, 5, 4, 6, 4, 5, 4, 7, 4, 5, 4, 6, 4, 5, 4, 32,
			//	4, 5, 4, 6, 4, 5, 4, 7, 4, 5, 4, 6, 4, 5, 4, 32
			//);
			//const __m256i nibble_mask = _mm256_set1_epi8(0x0F);
			//const __m256i byte_offset = _mm256_set1_epi32(0x18100800);
			//__m256i t;

			///* find tzcnt for each byte */
			//t = _mm256_and_si256(nibble_mask, v);
			var t = Avx2.And(_nibbleMask, limbVecs);

			//v = _mm256_and_si256(_mm256_srli_epi16(v, 4), nibble_mask);
			var v = Avx2.And(Avx2.ShiftRightLogical(limbVecs, 4), _nibbleMask);

			//t = _mm256_shuffle_epi8(lut_lo, t);
			t = Avx2.Shuffle(_lutLow, t.AsByte()).AsUInt32();

			//v = _mm256_shuffle_epi8(lut_hi, v);
			v = Avx2.Shuffle(_lutHigh, v.AsByte()).AsUInt32();

			//v = _mm256_min_epu8(v, t);
			v = Avx2.Min(v, t);


			///* find tzcnt for each dword */
			//v = _mm256_or_si256(v, byte_offset);
			v = Avx2.Or(v, _byteOffset);

			//v = _mm256_min_epu8(v, _mm256_srli_epi16(v, 8));
			v = Avx2.Or(v, Avx2.ShiftRightLogical(v, 8));

			//v = _mm256_min_epu8(v, _mm256_srli_epi32(v, 16));
			v = Avx2.Min(v, Avx2.ShiftRightLogical(v, 16));

			return v;
		}

		#endregion

		#region Retrieve FP31Val From FP31Deck

		public FP31Val GetFP31ValAtIndex(FP31Deck fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var mantissa = fPValues.GetMantissa(index);
			var result = new FP31Val(mantissa, TargetExponent, BitsBeforeBP, precision);

			return result;
		}

		#endregion

		#region Comparison

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
				MathOpCounts.NumberOfGrtrThanOps++;
			}
		}

		#endregion
	}
}
