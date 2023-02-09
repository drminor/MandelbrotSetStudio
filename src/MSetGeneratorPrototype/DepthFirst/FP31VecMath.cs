using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSetGeneratorPrototype
{
	public class FP31VecMath
	{
		#region Private Properties

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

		private Vector256<uint>[] _squareResult0Lo;
		private Vector256<uint>[] _squareResult0Hi;

		private Vector256<ulong>[] _squareResult1Lo;
		private Vector256<ulong>[] _squareResult1Hi;

		private Vector256<ulong>[] _squareResult2Lo;
		private Vector256<ulong>[] _squareResult2Hi;

		private Vector256<uint>[] _negationResult;
		private Vector256<uint>[] _additionResult;

		private Vector256<uint> _ones;

		private Vector256<uint> _carryVectors;
		private Vector256<ulong> _carryVectorsLong;

		private Vector256<int> _signBitVecs;

		byte _shiftAmount;
		byte _inverseShiftAmount;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VecMath(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			LimbCount = apFixedPointFormat.LimbCount;

			_squareResult0Lo = new Vector256<uint>[LimbCount * 2];
			_squareResult0Hi = new Vector256<uint>[LimbCount * 2];

			_squareResult1Lo = new Vector256<ulong>[LimbCount * 2];
			_squareResult1Hi = new Vector256<ulong>[LimbCount * 2];

			_squareResult2Lo = new Vector256<ulong>[LimbCount * 2];
			_squareResult2Hi = new Vector256<ulong>[LimbCount * 2];


			_negationResult = new Vector256<uint>[LimbCount];
			_additionResult = new Vector256<uint>[LimbCount];

			_ones = Vector256.Create(1u);

			_carryVectors = Vector256<uint>.Zero;
			_carryVectorsLong = Vector256<ulong>.Zero;

			_signBitVecs = Vector256<int>.Zero;

			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			MathOpCounts = new MathOpCounts();
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; private set; }

		public MathOpCounts MathOpCounts { get; init; }

		#endregion

		#region Multiply and Square

		public void Square(Vector256<uint>[] a, Vector256<uint>[] result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			//CheckReservedBitIsClear(a, "Squaring");

			ConvertFrom2C(a, _squareResult0Lo, _squareResult0Hi);
			//MathOpCounts.NumberOfConversions++;

			SquareInternal(_squareResult0Lo, _squareResult1Lo);
			SquareInternal(_squareResult0Hi, _squareResult1Hi);

			SumThePartials(_squareResult1Lo, _squareResult2Lo);
			SumThePartials(_squareResult1Hi, _squareResult2Hi);

			ShiftAndTrim(_squareResult2Lo, _squareResult2Hi, result);
		}

		private void SquareInternal(Vector256<uint>[] source, Vector256<ulong>[] result)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.ClearManatissMems();

			for (int j = 0; j < LimbCount; j++)
			{
				for (int i = j; i < LimbCount; i++)
				{
					var resultPtr = j + i;  // 0+0, 0+1; 1+1, 0, 1, 2

					var productVector = Avx2.Multiply(source[j], source[i]);
					IncrementNoMultiplications();

					if (i > j)
					{
						//product *= 2;
						productVector = Avx2.ShiftLeftLogical(productVector, 1);
					}

					// 0/1; 1/2; 2/3
					result[resultPtr] = Avx2.Add(result[resultPtr], Avx2.And(productVector, HIGH33_MASK_VEC_L));
					result[resultPtr + 1] = Avx2.Add(result[resultPtr + 1], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
					//MathOpCounts.NumberOfSplits++;
					//MathOpCounts.NumberOfAdditions += 2;
				}
			}
		}

		[Conditional("PERF")]
		private void IncrementNoMultiplications()
		{
			MathOpCounts.NumberOfMultiplications ++;
		}

		#endregion

		#region Multiply Post Processing

		private void SumThePartials(Vector256<ulong>[] source, Vector256<ulong>[] result)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			_carryVectorsLong = Vector256<ulong>.Zero;

			var limbCnt = source.Length;

			for (int limbPtr = 0; limbPtr < limbCnt; limbPtr++)
			{
				var withCarries = Avx2.Add(source[limbPtr], _carryVectorsLong);

				result[limbPtr] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);                     // The low 31 bits of the sum is the result.
				_carryVectorsLong = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

				// Clear the source so that square internal will not have to make a separate call.
				source[limbPtr] = Avx2.Xor(source[limbPtr], source[limbPtr]);
			}
		}

		private void ShiftAndTrim(Vector256<ulong>[] sourceLimbsLo, Vector256<ulong>[] sourceLimbsHi, Vector256<uint>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var sourceIndex = Math.Max(sourceLimbsLo.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
			{

				if (sourceIndex > 0)
				{
					// Calcualte the lo end

					// Take the bits from the source limb, discarding the top shiftAmount of bits.
					var source = sourceLimbsLo[limbPtr + sourceIndex];
					var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

					// Take the top shiftAmount of bits from the previous limb
					var prevSource = sourceLimbsLo[limbPtr + sourceIndex - 1];
					wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

					// Calculate the hi end

					// Take the bits from the source limb, discarding the top shiftAmount of bits.
					source = sourceLimbsHi[limbPtr + sourceIndex];
					var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

					// Take the top shiftAmount of bits from the previous limb
					prevSource = sourceLimbsHi[limbPtr + sourceIndex - 1];
					wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSource, HIGH33_MASK_VEC_L), _inverseShiftAmount));

					var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
					var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

					resultLimbs[limbPtr] = Avx2.Or(low128, high128);

					//MathOpCounts.NumberOfSplits += 4;
				}
				else
				{
					// Calcualte the lo end

					// Take the bits from the source limb, discarding the top shiftAmount of bits.
					var source = sourceLimbsLo[limbPtr + sourceIndex];
					var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

					// Calculate the hi end

					// Take the bits from the source limb, discarding the top shiftAmount of bits.
					source = sourceLimbsHi[limbPtr + sourceIndex];
					var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source, _shiftAmount), HIGH33_MASK_VEC_L);

					var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
					var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

					resultLimbs[limbPtr] = Avx2.Or(low128, high128);

					//MathOpCounts.NumberOfSplits += 2;
				}
			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(Vector256<uint>[] a, Vector256<uint>[] b, Vector256<uint>[] c)
		{
			//CheckReservedBitIsClear(b, "Negating B");

			Negate(b, _negationResult);
			//MathOpCounts.NumberOfConversions++;

			Add(a, _negationResult, c);
		}

		public void Add(Vector256<uint>[] a, Vector256<uint>[] b, Vector256<uint>[] c)
		{
			_carryVectors = Vector256<uint>.Zero;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var sumVector = Avx2.Add(a[limbPtr], b[limbPtr]);
				var newValuesVector = Avx2.Add(sumVector, _carryVectors);
				//MathOpCounts.NumberOfAdditions += 2;

				c[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
				_carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
																								   //MathOpCounts.NumberOfSplits++;
			}
		}

		public void AddThenSquare(Vector256<uint>[] a, Vector256<uint>[] b, Vector256<uint>[] c)
		{
			Add(a, b, _additionResult);
			Square(_additionResult, c);
		}

		#endregion

		#region Two Compliment Support

		private void Negate(Vector256<uint>[] source, Vector256<uint>[] result)
		{
			_carryVectors = _ones;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var notVector = Avx2.Xor(source[limbPtr], ALL_BITS_SET_VEC);
				var newValuesVector = Avx2.Add(notVector, _carryVectors);
				//MathOpCounts.NumberOfAdditions += 2;

				result[limbPtr] = Avx2.And(newValuesVector, HIGH33_MASK_VEC); ;
				_carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
				//MathOpCounts.NumberOfSplits++;
			}
		}

		private void ConvertFrom2C(Vector256<uint>[] source, Vector256<uint>[] resultLo, Vector256<uint>[] resultHi)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");

			var signBitFlags = GetSignBits(source, out _signBitVecs);

			if (signBitFlags == -1)
			{
				// All positive values
				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					// Take the lower 4 values and set the low halves of each result
					resultLo[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

					// Take the higher 4 values and set the low halves of each result
					resultHi[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(source[limbPtr], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
				}
			}
			else
			{
				// Mixed Positive and Negative values
				_carryVectors = _ones;

				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					var notVector = Avx2.Xor(source[limbPtr], ALL_BITS_SET_VEC);
					var newValuesVector = Avx2.Add(notVector, _carryVectors);
					//MathOpCounts.NumberOfAdditions += 2;

					var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					_carryVectors = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

					//MathOpCounts.NumberOfSplits++;

					var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), source[limbPtr].AsByte(), _signBitVecs.AsByte())).AsUInt32();

					// Take the lower 4 values and set the low halves of each result
					resultLo[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

					// Take the lower 4 values and set the low halves of each result
					resultHi[limbPtr] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

				}
			}
		}

		private int GetSignBits(Vector256<uint>[] source, out Vector256<int> signBitVecs)
		{
			var msl = source[LimbCount - 1];

			var left = Avx2.And(msl.AsInt32(), TEST_BIT_30_VEC);
			signBitVecs = Avx2.CompareEqual(left, ZERO_VEC); // dst[i+31:i] := ( a[i+31:i] == b[i+31:i] ) ? 0xFFFFFFFF : 0
			var result = Avx2.MoveMask(signBitVecs.AsByte());

			return result;
		}

		private void CheckReservedBitIsClear(FP31Vectors sourceLimbs, string description, int[] inPlayList)
		{
			var sb = new StringBuilder();

			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbs = sourceLimbs.GetLimbVectorsUW(limbPtr);

				var oneFound = false;

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
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

		private void ExpandTo(FP31Vectors source, FP31VectorsPW result, int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var limbPtr = 0; limbPtr < source.LimbCount; limbPtr++)
			{
				var left = source.GetLimbVectorsUW(limbPtr);                // Walk through the source at 1x rate.
				var resultVectorsNarrow = result.GetLimbVectorsUW(limbPtr); // Walk through the result at 2x rate.

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
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

		private void PackTo(FP31VectorsPW source, FP31Vectors result, int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var limbPtr = 0; limbPtr < source.LimbCount; limbPtr++)
			{
				var leftNarrow = source.GetLimbVectorsUW(limbPtr);      // Walk through the source at 2x rate
				var resultVectors = result.GetLimbVectorsUW(limbPtr);   // Walk through the result at 1x rate

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
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

		public Vector256<int> CreateVectorForComparison(uint value)
		{
			var fp31Val = FP31ValHelper.CreateFP31Val(new RValue(value, 0), ApFixedPointFormat);
			var msl = (int)fp31Val.Mantissa[^1] - 1;
			var result = Vector256.Create(msl);

			return result;
		}

		public Vector256<int> IsGreaterOrEqThan(Vector256<uint> left, Vector256<int> right)
		{
			var sansSign = Avx2.And(left, SIGN_BIT_MASK_VEC);
			var result = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);
			//MathOpCounts.NumberOfGrtrThanOps++;

			return result;
		}

		#endregion

		#region Value Support

		public Vector256<uint>[] GetNewLimbSet()
		{
			return new Vector256<uint>[LimbCount];
		}

		#endregion
	}
}
