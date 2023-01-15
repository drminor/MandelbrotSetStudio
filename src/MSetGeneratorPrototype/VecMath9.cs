using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
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

		private const int TEST_BIT_30 = 0x40000000; // bit 30 is set.
		private static readonly Vector256<int> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

		private static readonly Vector256<int> ZERO_VEC = Vector256<int>.Zero;
		private static readonly Vector256<uint> ALL_BITS_SET_VEC = Vector256<uint>.AllBitsSet;

		private static readonly Vector256<uint> SHUFFLE_EXP_LOW_VEC = Vector256.Create(0u, 0u, 1u, 1u, 2u, 2u, 3u, 3u);
		private static readonly Vector256<uint> SHUFFLE_EXP_HIGH_VEC = Vector256.Create(4u, 4u, 5u, 5u, 6u, 6u, 7u, 7u);

		private static readonly Vector256<uint> SHUFFLE_PACK_LOW_VEC = Vector256.Create(0u, 2u, 4u, 6u, 0u, 0u, 0u, 0u);
		private static readonly Vector256<uint> SHUFFLE_PACK_HIGH_VEC = Vector256.Create(0u, 0u, 0u, 0u, 0u, 2u, 4u, 6u);

		private FP31DeckPW _squareResult0;
		private FP31DeckPW _squareResult1;
		private FP31DeckPW _squareResult2;

		private FP31Deck _negationResult;
		private FP31Deck _additionResult;

		private Vector256<uint>[] _ones;
		private Vector256<int> _thresholdVector;

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public VecMath9(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			ApFixedPointFormat = apFixedPointFormat;

			var thresholdMsl = GetThresholdMsl(threshold, ApFixedPointFormat);
			_thresholdVector = Vector256.Create(thresholdMsl);

			ValueCount = valueCount;
			VectorCount = Math.DivRem(ValueCount, _lanes, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException($"The valueCount must be an even multiple of {_lanes}.");
			}

			MathOpCounts = new MathOpCounts();

			_squareResult0 = new FP31DeckPW(LimbCount, ValueCount);
			_squareResult1 = new FP31DeckPW(LimbCount * 2, ValueCount);
			_squareResult2 = new FP31DeckPW(LimbCount * 2, ValueCount);

			_negationResult = new FP31Deck(LimbCount, ValueCount);
			_additionResult = new FP31Deck(LimbCount, ValueCount);

			var justOne = Vector256.Create(1u);
			_ones = Enumerable.Repeat(justOne, VectorCount).ToArray();
		}

		private int GetThresholdMsl(uint threshold, ApFixedPointFormat apFixedPointFormat)
		{
			var maxIntegerValue = FP31ValHelper.GetMaxIntegerValue(apFixedPointFormat.BitsBeforeBinaryPoint);
			if (threshold > maxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat.");
			}

			var thresholdFP31Val = FP31ValHelper.CreateFP31Val(new RValue(threshold, 0), apFixedPointFormat);
			var result = (int)thresholdFP31Val.Mantissa[^1] - 1;

			return result;
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public int ValueCount { get; init; }
		public int VectorCount { get; init; }

		public MathOpCounts MathOpCounts { get; init; }

		#endregion

		#region Multiply and Square

		public void Square(FP31Deck a, FP31Deck result, int[] inPlayList)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			//CheckReservedBitIsClear(a, "Squaring");

			ConvertFrom2C(a, _squareResult0, inPlayList);
			MathOpCounts.NumberOfConversions++;

			// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
			var inPlayListNarrow = BuildNarowInPlayList(inPlayList);

			SquareInternal(_squareResult0, _squareResult1, inPlayListNarrow);
			SumThePartials(_squareResult1, _squareResult2, inPlayListNarrow);
			ShiftAndTrim(_squareResult2, result, inPlayList);
		}

		private void SquareInternal(FP31DeckPW source, FP31DeckPW result, int[] inPlayListNarrow)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			result.ClearManatissMems();

			var indexes = inPlayListNarrow;
			for (int j = 0; j < LimbCount; j++)
			{
				for (int i = j; i < LimbCount; i++)
				{
					var left = source.GetLimbVectorsUW(j);
					var right = source.GetLimbVectorsUW(i);

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = result.GetLimbVectorsUL(resultPtr);
					var resultHighs = result.GetLimbVectorsUL(resultPtr + 1);

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

		#region Multiply Post Processing

		private void SumThePartials(FP31DeckPW source, FP31DeckPW result, int[] inPlayListNarrow)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var carryVectors = Enumerable.Repeat(Vector256<ulong>.Zero, VectorCount * 2).ToArray();
			var indexes = inPlayListNarrow;

			var limbCnt = source.LimbCount;

			for (int limbPtr = 0; limbPtr < limbCnt; limbPtr++)
			{
				var pProductVectors = source.GetLimbVectorsUL(limbPtr);
				var resultVectors = result.GetLimbVectorsUL(limbPtr);

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

		private void ShiftAndTrim(FP31DeckPW sourceLimbs, FP31Deck resultLimbs, int[] inPlayList)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var shiftAmount = BitsBeforeBP;
			byte inverseShiftAmount = (byte)(31 - shiftAmount);
			var indexes = inPlayList;

			var sourceIndex = Math.Max(sourceLimbs.LimbCount - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.LimbCount; limbPtr++)
			{
				var resultVectors = resultLimbs.GetLimbVectorsUW(limbPtr);

				if (sourceIndex > 0)
				{
					var source = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex);
					var prevSource = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex - 1);

					for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var sourceIdx = idx * 2;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], shiftAmount), HIGH33_MASK_VEC_L);

						// Take the top shiftAmount of bits from the previous limb
						wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSource[sourceIdx], HIGH33_MASK_VEC_L), inverseShiftAmount));

						sourceIdx++;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], shiftAmount), HIGH33_MASK_VEC_L);

						// Take the top shiftAmount of bits from the previous limb
						wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSource[sourceIdx], HIGH33_MASK_VEC_L), inverseShiftAmount));

						var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
						var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

						resultVectors[idx] = Avx2.Or(low128, high128);

						MathOpCounts.NumberOfSplits += 4;
					}
				}
				else
				{
					var source = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex);

					for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var sourceIdx = idx * 2;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], shiftAmount), HIGH33_MASK_VEC_L);

						sourceIdx++;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], shiftAmount), HIGH33_MASK_VEC_L);

						var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
						var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

						resultVectors[idx] = Avx2.Or(low128, high128);

						MathOpCounts.NumberOfSplits += 2;
					}
				}

			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(FP31Deck a, FP31Deck b, FP31Deck c, int[] inPlayList)
		{
			//CheckReservedBitIsClear(b, "Negating B");

			Negate(b, _negationResult, inPlayList);
			MathOpCounts.NumberOfConversions++;

			Add(a, _negationResult, c, inPlayList);
		}

		public void Add(FP31Deck a, FP31Deck b, FP31Deck c, int[] inPlayList)
		{
			var carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, VectorCount).ToArray();
			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecsA = a.GetLimbVectorsUW(limbPtr);
				var limbVecsB = b.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = c.GetLimbVectorsUW(limbPtr);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

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

		public void AddThenSquare(FP31Deck a, FP31Deck b, FP31Deck c, int[] inPlayList)
		{
			Add(a, b, _additionResult, inPlayList);
			Square(_additionResult, c, inPlayList);
		}

		#endregion

		#region Two Compliment Support

		private void Negate(FP31Deck source, FP31Deck result, int[] inPlayList)
		{
			var carryVectors = _ones;
			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = source.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

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

		private void ConvertFrom2C(FP31Deck source, FP31DeckPW result, int[] inPlayList)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");

			var indexes = inPlayList;

			var signBitFlags = new int[VectorCount];
			var signBitVecs = new Vector256<int>[VectorCount];
			var msls = source.GetLimbVectorsUW(LimbCount - 1);

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
				var limbVecs = source.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];
					var resultIdx = idx * 2;

					if (signBitFlags[idx] == -1)
					{
						// All positive values
						
						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx] = Avx2.And(Avx2.PermuteVar8x32(limbVecs[idx], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx + 1] = Avx2.And(Avx2.PermuteVar8x32(limbVecs[idx], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
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
						MathOpCounts.NumberOfSplits++;

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx] = Avx2.And(Avx2.PermuteVar8x32(limbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx + 1] = Avx2.And(Avx2.PermuteVar8x32(limbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

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

									result.Mantissas[limbPtr][valPtr] = source.Mantissas[limbPtr][valPtr];
								}
							}
						}

					}

				}
			}
		}

		private void CheckReservedBitIsClear(FP31Deck sourceLimbs, string description, int[] inPlayList)
		{
			var sb = new StringBuilder();

			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbs = sourceLimbs.GetLimbVectorsUW(limbPtr);

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

		private void ExpandTo(FP31Deck source, FP31DeckPW result, int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var limbPtr = 0; limbPtr < source.LimbCount; limbPtr++)
			{
				var left = source.GetLimbVectorsUW(limbPtr);                // Walk through the source at 1x rate.
				var resultVectorsNarrow = result.GetLimbVectorsUW(limbPtr); // Walk through the result at 2x rate.

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];
					var rIdx = idx * 2;

					// Take the lower 4 values and set the low halves of each result
					resultVectorsNarrow[rIdx] = Avx2.And(Avx2.PermuteVar8x32(left[idx], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

					// Take the upper 4 values and set the low halves of each result
					resultVectorsNarrow[rIdx + 1] = Avx2.And(Avx2.PermuteVar8x32(left[idx], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
				}
			}
		}

		private void PackTo(FP31DeckPW source, FP31Deck result, int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var limbPtr = 0; limbPtr < source.LimbCount; limbPtr++)
			{
				var leftNarrow = source.GetLimbVectorsUW(limbPtr);      // Walk through the source at 2x rate
				var resultVectors = result.GetLimbVectorsUW(limbPtr);   // Walk through the result at 1x rate

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];
					var sIdx = idx * 2;

					var low128 = Avx2.PermuteVar8x32(leftNarrow[sIdx], SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
					var high128 = Avx2.PermuteVar8x32(leftNarrow[sIdx + 1], SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

					resultVectors[idx] = Avx2.Or(low128, high128);
				}
			}
		}

		#endregion

		#region Comparison

		public void IsGreaterOrEqThanThreshold(FP31Deck a, Memory<int> results, int[] inPlayList)
		{
			var left = a.GetLimbVectorsUW(LimbCount - 1);
			var right = _thresholdVector;

			Span<Vector256<int>> resultVectors = MemoryMarshal.Cast<int, Vector256<int>>(results.Span);
			IsGreaterOrEqThan(left, right, resultVectors, inPlayList);
		}

		private void IsGreaterOrEqThan(Span<Vector256<uint>> left, Vector256<int> right, Span<Vector256<int>> results, int[] inPlayList)
		{
			var indexes = inPlayList;
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
