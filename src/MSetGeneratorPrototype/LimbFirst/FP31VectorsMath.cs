using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSetGeneratorPrototype
{
	public class FP31VectorsMath
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

		private FP31VectorsPW _squareResult0;
		private FP31VectorsPW _squareResult1;
		private FP31VectorsPW _squareResult2;

		private FP31Vectors _negationResult;
		private FP31Vectors _additionResult;

		private Vector256<uint>[] _ones;

		private Vector256<uint>[] _carryVectors;
		private Vector256<ulong>[] _carryVectorsLong;

		private Vector256<int>[] _signBitVecs;
		private int[] _signBitFlags;

		byte _shiftAmount;
		byte _inverseShiftAmount;

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FP31VectorsMath(ApFixedPointFormat apFixedPointFormat, int valueCount)
		{
			ApFixedPointFormat = apFixedPointFormat;

			ValueCount = valueCount;
			VectorCount = Math.DivRem(ValueCount, _lanes, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException($"The valueCount must be an even multiple of {_lanes}.");
			}

			_squareResult0 = new FP31VectorsPW(LimbCount, ValueCount);
			_squareResult1 = new FP31VectorsPW(LimbCount * 2, ValueCount);
			_squareResult2 = new FP31VectorsPW(LimbCount * 2, ValueCount);

			_negationResult = new FP31Vectors(LimbCount, ValueCount);
			_additionResult = new FP31Vectors(LimbCount, ValueCount);

			var justOne = Vector256.Create(1u);
			_ones = Enumerable.Repeat(justOne, VectorCount).ToArray();

			_carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, VectorCount * 2).ToArray();
			_carryVectorsLong = Enumerable.Repeat(Vector256<ulong>.Zero, VectorCount * 2).ToArray();

			_signBitVecs = new Vector256<int>[VectorCount];
			_signBitFlags = new int[VectorCount];

			_shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;
			_inverseShiftAmount = (byte)(31 - _shiftAmount);

			MathOpCounts = new MathOpCounts();
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int ValueCount { get; init; }
		public int VectorCount { get; init; }

		public MathOpCounts MathOpCounts { get; init; }

		#endregion

		#region Multiply and Square

		public void Square(FP31Vectors a, FP31Vectors result, int[] inPlayList, int[] inPlayListNarrow)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			//CheckReservedBitIsClear(a, "Squaring");

			ConvertFrom2C(a, _squareResult0, inPlayList);
			//MathOpCounts.NumberOfConversions++;

			// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
			//var inPlayListNarrow = BuildNarowInPlayList(inPlayList);

			SquareInternal(_squareResult0, _squareResult1, inPlayListNarrow);
			SumThePartials(_squareResult1, _squareResult2, inPlayListNarrow);
			ShiftAndTrim(_squareResult2, result, inPlayList);
		}

		private void SquareInternal(FP31VectorsPW source, FP31VectorsPW result, int[] inPlayListNarrow)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)

			//result.ClearManatissMems();

			for (int j = 0; j < LimbCount; j++)
			{
				for (int i = j; i < LimbCount; i++)
				{
					var left = source.GetLimbVectorsUW(j);
					var right = source.GetLimbVectorsUW(i);

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = result.GetLimbVectorsUL(resultPtr);
					var resultHighs = result.GetLimbVectorsUL(resultPtr + 1);

					for(var idxPtr = 0; idxPtr < inPlayListNarrow.Length; idxPtr++)
					{
						var idx = inPlayListNarrow[idxPtr];
						var productVector = Avx2.Multiply(left[idx], right[idx]);
						//MathOpCounts.NumberOfMultiplications++;

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						//var lows = Avx2.And(productVector, HIGH33_MASK_VEC_L);                      // Create new ulong from bits 0 - 31.
						//var highs = Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB); // Create new ulong from bits 32 - 63.
						////MathOpCounts.NumberOfSplits++;

						//resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						//resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);

						resultLows[idx] = Avx2.Add(resultLows[idx], Avx2.And(productVector, HIGH33_MASK_VEC_L));
						resultHighs[idx] = Avx2.Add(resultHighs[idx], Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB));
						//MathOpCounts.NumberOfSplits++;
						//MathOpCounts.NumberOfAdditions += 2;
					}
				}
			}
		}

		#endregion

		#region Multiply Post Processing

		private void SumThePartials(FP31VectorsPW source, FP31VectorsPW result, int[] inPlayListNarrow)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			//Array.Clear(_carryVectorsLong);

			var limbCnt = source.LimbCount;

			for (int limbPtr = 0; limbPtr < limbCnt; limbPtr++)
			{
				var pProductVectors = source.GetLimbVectorsUL(limbPtr);
				var resultVectors = result.GetLimbVectorsUL(limbPtr);

				for (var idxPtr = 0; idxPtr < inPlayListNarrow.Length; idxPtr++)
				{
					var idx = inPlayListNarrow[idxPtr];

					var withCarries = Avx2.Add(pProductVectors[idx], _carryVectorsLong[idx]);

					resultVectors[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);						// The low 31 bits of the sum is the result.
					_carryVectorsLong[idx] = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);   // The high 31 bits of sum becomes the new carry.

					// Clear the source so that square internal will not have to make a separate call.
					pProductVectors[idx] = Avx2.Xor(pProductVectors[idx], pProductVectors[idx]);
				}

			}
		}

		private void ShiftAndTrim(FP31VectorsPW sourceLimbs, FP31Vectors resultLimbs, int[] inPlayList)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			//var shiftAmount = BitsBeforeBP;
			//byte inverseShiftAmount = (byte)(31 - shiftAmount);

			var sourceIndex = Math.Max(sourceLimbs.LimbCount - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.LimbCount; limbPtr++)
			{
				var resultVectors = resultLimbs.GetLimbVectorsUW(limbPtr);

				if (sourceIndex > 0)
				{
					var source = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex);
					var prevSource = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex - 1);

					for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
					{
						var idx = inPlayList[idxPtr];
						var sourceIdx = idx * 2;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], _shiftAmount), HIGH33_MASK_VEC_L);

						// Take the top shiftAmount of bits from the previous limb
						wideResultLow = Avx2.Or(wideResultLow, Avx2.ShiftRightLogical(Avx2.And(prevSource[sourceIdx], HIGH33_MASK_VEC_L), _inverseShiftAmount));

						sourceIdx++;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], _shiftAmount), HIGH33_MASK_VEC_L);

						// Take the top shiftAmount of bits from the previous limb
						wideResultHigh = Avx2.Or(wideResultHigh, Avx2.ShiftRightLogical(Avx2.And(prevSource[sourceIdx], HIGH33_MASK_VEC_L), _inverseShiftAmount));

						var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
						var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

						resultVectors[idx] = Avx2.Or(low128, high128);

						//MathOpCounts.NumberOfSplits += 4;

						// Clear the carry vectors used by SumThePartials
						_carryVectors[idx] = Avx2.Xor(_carryVectors[idx], _carryVectors[idx]);
						_carryVectorsLong[idx] = Avx2.Xor(_carryVectorsLong[idx], _carryVectorsLong[idx]);
					}
				}
				else
				{
					var source = sourceLimbs.GetLimbVectorsUL(limbPtr + sourceIndex);

					for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
					{
						var idx = inPlayList[idxPtr];
						var sourceIdx = idx * 2;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultLow = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], _shiftAmount), HIGH33_MASK_VEC_L);

						sourceIdx++;

						// Take the bits from the source limb, discarding the top shiftAmount of bits.
						var wideResultHigh = Avx2.And(Avx2.ShiftLeftLogical(source[sourceIdx], _shiftAmount), HIGH33_MASK_VEC_L);

						var low128 = Avx2.PermuteVar8x32(wideResultLow.AsUInt32(), SHUFFLE_PACK_LOW_VEC).WithUpper(Vector128<uint>.Zero);
						var high128 = Avx2.PermuteVar8x32(wideResultHigh.AsUInt32(), SHUFFLE_PACK_HIGH_VEC).WithLower(Vector128<uint>.Zero);

						resultVectors[idx] = Avx2.Or(low128, high128);

						//MathOpCounts.NumberOfSplits += 2;

						// Clear the carry vectors used by SumThePartials
						_carryVectors[idx] = Avx2.Xor(_carryVectors[idx], _carryVectors[idx]);
						_carryVectorsLong[idx] = Avx2.Xor(_carryVectorsLong[idx], _carryVectorsLong[idx]);
					}
				}

			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(FP31Vectors a, FP31Vectors b, FP31Vectors c, int[] inPlayList)
		{
			//CheckReservedBitIsClear(b, "Negating B");

			Negate(b, _negationResult, inPlayList);
			//MathOpCounts.NumberOfConversions++;

			Add(a, _negationResult, c, inPlayList);
		}

		public void Add(FP31Vectors a, FP31Vectors b, FP31Vectors c, int[] inPlayList)
		{
			//var carryVectors = Enumerable.Repeat(Vector256<uint>.Zero, VectorCount).ToArray();
			//Array.Clear(_carryVectors);

			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecsA = a.GetLimbVectorsUW(limbPtr);
				var limbVecsB = b.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = c.GetLimbVectorsUW(limbPtr);

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					var left = limbVecsA[idx];
					var right = limbVecsB[idx];

					var sumVector = Avx2.Add(left, right);
					var newValuesVector = Avx2.Add(sumVector, _carryVectors[idx]);
					//MathOpCounts.NumberOfAdditions += 2;

					//var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					//var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
					//resultLimbVecs[idx] = limbValues;
					////MathOpCounts.NumberOfSplits++;

					//if (USE_DET_DEBUG)
					//	ReportForAddition(limbPtr, left, right, _carryVectors[idx], newValuesVector, limbValues, newCarries);

					//_carryVectors[idx] = newCarries;

					resultLimbVecs[idx] = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					_carryVectors[idx] = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
					//MathOpCounts.NumberOfSplits++;
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

		public void AddThenSquare(FP31Vectors a, FP31Vectors b, FP31Vectors c, int[] inPlayList, int[] inPlayListNarrow)
		{
			Add(a, b, _additionResult, inPlayList);
			Square(_additionResult, c, inPlayList, inPlayListNarrow);
		}

		#endregion

		#region Two Compliment Support

		private void Negate(FP31Vectors source, FP31Vectors result, int[] inPlayList)
		{
			var carryVectors = _ones;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = source.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
				{
					var idx = inPlayList[idxPtr];

					var left = limbVecs[idx];

					var notVector = Avx2.Xor(left, ALL_BITS_SET_VEC);
					var newValuesVector = Avx2.Add(notVector, carryVectors[idx]);
					//MathOpCounts.NumberOfAdditions += 2;

					//var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
					//var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
					//resultLimbVecs[idx] = limbValues;
					////MathOpCounts.NumberOfSplits++;

					//if (USE_DET_DEBUG)
					//	ReportForNegation(limbPtr, left, carryVectors[idx], newValuesVector, limbValues, newCarries);

					//carryVectors[idx] = newCarries;

					resultLimbVecs[idx] = Avx2.And(newValuesVector, HIGH33_MASK_VEC); ;
					carryVectors[idx] = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);
					//MathOpCounts.NumberOfSplits++;

				}
			}
		}

		private void ConvertFrom2C(FP31Vectors source, FP31VectorsPW result, int[] inPlayList)
		{
			//CheckReservedBitIsClear(source, "ConvertFrom2C");

			GetSignBits(source, inPlayList, _signBitVecs, _signBitFlags);
			var carryVectors = _ones;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				var limbVecs = source.GetLimbVectorsUW(limbPtr);
				var resultLimbVecs = result.GetLimbVectorsUW(limbPtr);

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
				{
					var idx = inPlayList[idxPtr];
					var resultIdx = idx * 2;

					if (_signBitFlags[idx] == -1)
					{
						// All positive values

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx] = Avx2.And(Avx2.PermuteVar8x32(limbVecs[idx], SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

						// Take the higher 4 values and set the low halves of each result
						resultLimbVecs[resultIdx + 1] = Avx2.And(Avx2.PermuteVar8x32(limbVecs[idx], SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);
					}
					else
					{
						// Mixed Positive and Negative values

						var left = limbVecs[idx];

						var notVector = Avx2.Xor(left, ALL_BITS_SET_VEC);
						var newValuesVector = Avx2.Add(notVector, carryVectors[idx]);
						//MathOpCounts.NumberOfAdditions += 2;

						var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC);                        // The low 31 bits of the sum is the result.
						//var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.
						carryVectors[idx] = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);  // The high 31 bits of sum becomes the new carry.

						//MathOpCounts.NumberOfSplits++;

						var cLimbValues = (Avx2.BlendVariable(limbValues.AsByte(), left.AsByte(), _signBitVecs[idx].AsByte())).AsUInt32();

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_LOW_VEC), HIGH33_MASK_VEC);

						// Take the lower 4 values and set the low halves of each result
						resultLimbVecs[resultIdx + 1] = Avx2.And(Avx2.PermuteVar8x32(cLimbValues, SHUFFLE_EXP_HIGH_VEC), HIGH33_MASK_VEC);

						//if (USE_DET_DEBUG)
						//	ReportForNegation(limbPtr, left, carryVectors[idx], newValuesVector, limbValues, newCarries);

						//carryVectors[idx] = newCarries;
					}

				}
			}
		}

		private void GetSignBits(FP31Vectors source, int[] inPlayList, Vector256<int>[] signBitVecs, int[] signBitFlags)
		{
			//var signBitFlags = new int[VectorCount];
			//signBitVecs = new Vector256<int>[VectorCount];
			var msls = source.GetLimbVectorsUW(LimbCount - 1);

			for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
			{
				var idx = inPlayList[idxPtr];

				var left = Avx2.And(msls[idx].AsInt32(), TEST_BIT_30_VEC);
				signBitVecs[idx] = Avx2.CompareEqual(left, ZERO_VEC); // dst[i+31:i] := ( a[i+31:i] == b[i+31:i] ) ? 0xFFFFFFFF : 0
				//signBitVecs[idx] = areZerosVec;
				signBitFlags[idx] = Avx2.MoveMask(signBitVecs[idx].AsByte());
			}
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

		public void IsGreaterOrEqThan(FP31Vectors a, Vector256<int> right, Vector256<int>[] results, int[] inPlayList)
		{
			var left = a.GetLimbVectorsUW(LimbCount - 1);

			for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
			{
				var idx = inPlayList[idxPtr];
				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				results[idx] = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);
				//MathOpCounts.NumberOfGrtrThanOps++;
			}
		}

		#endregion
	}
}
