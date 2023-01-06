using MSS.Common.APValSupport;
using MSS.Common.APValues;
using MSS.Types;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class VecMath9
	{
		#region Private Properties

		//private const int EFFECTIVE_BITS_PER_LIMB = 31;
		//private static readonly ulong MAX_DIGIT_VALUE = (ulong)(-1 + Math.Pow(2, EFFECTIVE_BITS_PER_LIMB));

		//private const ulong HIGH32_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.

		//private const ulong LOW32_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		//private static readonly Vector256<ulong> HIGH32_MASK_VEC = Vector256.Create(LOW32_BITS_SET);

		//private const ulong HIGH33_BITS_SET = 0xFFFFFFFF80000000; // bits 63 - 31 are set.
		//private static readonly Vector256<ulong> LOW31_MASK_VEC = Vector256.Create(HIGH33_BITS_SET); // diagnostics

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC_L = Vector256.Create(LOW31_BITS_SET_L);

		private const uint SIGN_BIT_MASK = 0x3FFFFFFF;
		private static readonly Vector256<uint> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		//private static readonly int _lanes = Vector256<ulong>.Count;
		private static readonly int _lanes = Vector256<uint>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private Vector256<int> _thresholdVector;
		//private Vector256<ulong> _zeroVector;
		//private Vector256<long> _maxDigitValueVector;

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
			Threshold = threshold;
			MaxIntegerValue = ScalarMathHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint, IsSigned);
			
			ThresholdMsl = FP31ValHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);
			_thresholdVector = Vector256.Create(ThresholdMsl).AsInt32();

			//_zeroVector = Vector256<ulong>.Zero;
			//_maxDigitValueVector = Vector256.Create((long)MAX_DIGIT_VALUE);

			_squareResult1Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArrayL(LimbCount * 2, ValueCount);
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

		public uint Threshold { get; init; }
		public uint ThresholdMsl { get; init; }

		//public double MslWeight { get; init; }
		//public Vector256<double> MslWeightVector { get; init; }

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public long NumberOfConversions { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Multiply and Square

		public void Square(FP31Deck a, FP31Deck result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			FPValues non2CFPValues = a.ConvertFrom2C(InPlayList, _lanes);
			NumberOfConversions++;

			// There are 8 ints to a Vector, but only 4 longs. Adjust the InPlayList to support multiplication
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			SquareInternal(non2CFPValues, _squareResult1Mems);
			SumThePartials(_squareResult1Mems, _squareResult2Mems);

			ShiftAndTrim(_squareResult2Mems, non2CFPValues);

			result.UpdateFrom(non2CFPValues, InPlayList, _lanes);
		}

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
						NumberOfMCarries++;

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var lows = Avx2.And(productVector, HIGH33_MASK_VEC_L);    // Create new ulong from bits 0 - 31.
						var highs = Avx2.ShiftRightLogical(productVector, 31);   // Create new ulong from bits 32 - 63.
						NumberOfSplits++;

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
						NumberOfACarries += 2;
					}
				}
			}
		}

		//private void Di(int idx, bool isAfterTheDiagonal, Span<Vector256<uint>> left, Span<Vector256<uint>> right, Span<Vector256<ulong>> resultLows, Span<Vector256<ulong>> resultHighs)
		//{
		//	//var idx = indexes[idxPtr];
		//	var productVector = Avx2.Multiply(left[idx], right[idx]);

		//	if (isAfterTheDiagonal)
		//	{
		//		//product *= 2;
		//		productVector = Avx2.ShiftLeftLogical(productVector, 1);
		//	}

		//	var lows = Avx2.And(productVector, HIGH33_MASK_VEC_L);    // Create new ulong from bits 0 - 31.
		//	var highs = Avx2.ShiftRightLogical(productVector, 31);   // Create new ulong from bits 32 - 63.

		//	resultLows[idx] = Avx2.Add(resultLows[idx], lows);
		//	resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
		//}

		#endregion

		#region Multiply Post Processing

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

				var carries = Avx2.ShiftRightLogical(pProductVectors[idx], 31);	// The high 32 bits of sum becomes the new carry.
				resultVectors[idx] = Avx2.And(pProductVectors[idx], HIGH33_MASK_VEC_L);                 // The low 32 bits of the sum is the result.
				NumberOfSplits++;

				for (; limbPtr < limbCnt; limbPtr++)
				{

					pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
					resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

					var withCarries = Avx2.Add(pProductVectors[idx], carries);
					carries = Avx2.ShiftRightLogical(withCarries, 31);     // The high 32 bits of sum becomes the new carry.
					resultVectors[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC_L);               // The low 32 bits of the sum is the result.
					NumberOfSplits++;
				}
			}
		}
		
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

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
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

		public void Sub(FP31Deck a, FP31Deck b, FP31Deck c)
		{
			NumberOfConversions++;

			var notB = b.Negate(InPlayList);
			Add(a, notB, c);
		}

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

					//var va = limbVecsA[idx];
					//var vb = limbVecsB[idx];

					var va = Avx2.And(limbVecsA[idx], HIGH33_MASK_VEC);
					var vb = Avx2.And(limbVecsB[idx], HIGH33_MASK_VEC);


					var sumVector = Avx2.Add(va, vb);
					var newValuesVector = Avx2.Add(sumVector, carryVector);

					NumberOfACarries += 2;

					//NumberOfSplits++;
					//var newCarries = Avx2.ShiftRightLogical(newValuesVector, EFFECTIVE_BITS_PER_LIMB);          // The high 32 bits of sum becomes the new carry.
					//var limbValues = Avx2.And(newValuesVector, HIGH33_MASK_VEC); // The low 32 bits of the sum is the result.
					//resultLimbVecs[idx] = limbValues;

					var (limbValues, newCarries) = GetResultWithCarry(newValuesVector, isMsl: (limbPtr == LimbCount - 1), out var aCarryOccured);
					resultLimbVecs[idx] = limbValues;
					NumberOfGetCarries++;

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

		private (Vector256<uint> limbs, Vector256<uint> carry) GetResultWithCarry(Vector256<uint> nvs, bool isMsl, out bool aCarryOccured)
		{
			NumberOfGetCarries++;

			aCarryOccured = false;
			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.

			var ltemp = new uint[_lanes];
			var ctemp = new uint[_lanes];

			for (var i = 0; i < _lanes; i++)
			{
				//var limbValue = ScalarMathHelper.GetLowHalf(nvs.GetElement(i), out var resultIsNegative, out var extendedCarryOutIsNegative);
				var (limbValue, newCarry) = ScalarMathHelper.GetResultWithCarrySigned(nvs.GetElement(i), isMsl);

				ltemp[i] = (uint)limbValue;
				ctemp[i] = (uint) newCarry;

				if (newCarry > 0) aCarryOccured = true;
			}

			var limbs = Vector256.Create(ltemp[0], ltemp[1], ltemp[2], ltemp[3], ltemp[4], ltemp[5], ltemp[6], ltemp[7]);

			var carryVector = Vector256.Create(ctemp[0], ctemp[1], ctemp[2], ctemp[3], ctemp[4], ctemp[5], ctemp[6], ctemp[7]);	

			return (limbs, carryVector);
		}

		private void ReportForAddition(int step, Vector256<uint> left, Vector256<uint> right, Vector256<uint> carry, Vector256<uint> nv, Vector256<uint> lo, Vector256<uint> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var rightVal0 = right.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = ScalarMathHelper.ConvertFrom2C(leftVal0);
			var rd = ScalarMathHelper.ConvertFrom2C(rightVal0);
			var cd = ScalarMathHelper.ConvertFrom2C(carryVal0);
			var nvd = ScalarMathHelper.ConvertFrom2C(nvVal0);
			var hid = ScalarMathHelper.ConvertFrom2C(newCarryVal0);
			var lod = ScalarMathHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0;
			var unSNv = leftVal0 + rightVal0 + carryVal0;


			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {leftVal0:X4}, {rightVal0:X4} wc:{carryVal0:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		#endregion

		#region Comparison

		public void IsGreaterOrEqThanThreshold(FP31Deck a, bool[] results)
		{
			var left = a.GetLimbVectorsUW(LimbCount - 1);
			var right = _thresholdVector;

			IsGreaterOrEqThan(left, right, results);
		}

		private void IsGreaterOrEqThan(Span<Vector256<uint>> left, Vector256<int> right, bool[] results)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				var resultVector = Avx2.CompareGreaterThan(sansSign.AsInt32(), right);
				NumberOfGrtrThanOps++;

				var vectorPtr = idx * _lanes;

				for (var i = 0; i < _lanes; i++)
				{
					results[vectorPtr + i] = resultVector.GetElement(i) == -1;
				}
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
