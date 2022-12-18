﻿using MSS.Common;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSetGenP
{
	public class FPVecMathHelper
	{
		#region Private Properties

		private const int BITS_PER_LIMB = 32;
		//private const int BITS_BEFORE_BP = 8;
		private static readonly ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);

		private const ulong LOW_BITS_SET = 0x00000000FFFFFFFF;  // bits 0 - 31 are set.
		private const ulong HIGH_MASK = LOW_BITS_SET;			// bits 0 - 31 are set.
		private static readonly Vector256<ulong> HIGH_MASK_VEC = Vector256.Create(HIGH_MASK);

		private const ulong HIGH_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.
		//private const ulong HIGH_FILL = HIGH_BITS_SET;
		//private static readonly Vector256<ulong> HIGH_FILL_VEC = Vector256.Create(HIGH_FILL);

		private const ulong SIGN_BIT_MASK = 0x7FFFFFFFFFFFFFFF;
		private static readonly Vector256<ulong> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const ulong TOP_BITS_MASK = 0xFF00000000000000;
		private static readonly Vector256<ulong> TOP_BITS_MASK_VEC = Vector256.Create(TOP_BITS_MASK);

		//private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		private static readonly int _lanes = Vector256<ulong>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private ulong[][] _squareResult3Ba;

		private Vector256<long> _thresholdVector;
		private Vector256<ulong> _zeroVector;
		private Vector256<long> _maxDigitValueVector;

		//private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public FPVecMathHelper(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			ApFixedPointFormat = SmxHelper.GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			ValueCount = valueCount;
			VecCount = Math.DivRem(ValueCount, _lanes, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException("The valueCount must be an even multiple of Vector<ulong>.Count.");
			}

			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, VecCount).ToArray();

			// Initially, all values are 'In Play.'
			DoneFlags = new bool[ValueCount];

			LimbCount = SmxHelper.GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = (uint)Math.Pow(2, BitsBeforeBP) - 1;

			Threshold = threshold; 
			
			var mslPower = ((LimbCount - 1) * BITS_PER_LIMB) - FractionalBits;
			MslWeight = Math.Pow(2, mslPower);
			MslWeightVector = Vector256.Create(MslWeight);

			_thresholdVector = BuildTheThresholdVector(threshold);
			_zeroVector = Vector256<ulong>.Zero;
			_maxDigitValueVector = Vector256.Create((long)MAX_DIGIT_VALUE - 1);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);

			_squareResult3Ba = BuildMantissaBackingArray(LimbCount * 2, ValueCount);
		}

		private Vector256<long> BuildTheThresholdVector(uint threshold)
		{
			if (threshold > MaxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat: {ApFixedPointFormat}.");
			}

			var thresholdMsl = SmxHelper.GetThresholdMsl(threshold, TargetExponent, LimbCount, BitsBeforeBP);
			var thresholdMslIntegerVector = Vector256.Create(thresholdMsl);
			var thresholdVector = thresholdMslIntegerVector.AsInt64();

			return thresholdVector;
		}

		#endregion

		#region Mantissa Support

		private Memory<ulong>[] BuildMantissaMemoryArray(int limbCount, int valueCount)
		{
			var ba = BuildMantissaBackingArray(limbCount, valueCount);
			var result = BuildMantissaMemoryArray(ba);

			return result;
		}

		private ulong[][] BuildMantissaBackingArray(int limbCount, int valueCount)
		{
			var result = new ulong[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new ulong[valueCount];
			}

			return result;
		}

		private Memory<ulong>[] BuildMantissaMemoryArray(ulong[][] backingArray)
		{
			var result = new Memory<ulong>[backingArray.Length];

			for (var i = 0; i < backingArray.Length; i++)
			{
				result[i] = new Memory<ulong>(backingArray[i]);
			}

			return result;
		}

		private void ClearManatissMems(Memory<ulong>[] mantissaMems, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var indexes = InPlayList;

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

					for (var i = 0; i < VecCount; i++)
					{
						vectors[i] = Vector256<ulong>.Zero;
					}
				}
			}
		}

		private void ClearBackingArray(ulong[][] backingArray, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var template = new ulong[_lanes];

				var indexes = InPlayList;

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

		private Span<Vector256<ulong>> GetLimbVectorsUL(Memory<ulong> memory)
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }

		public int LimbCount { get; init; }
		public int TargetExponent { get; init; }

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public int[] InPlayList { get; set; }	// Vector-Level 
		public bool[] DoneFlags { get; set; }	// Value-Level

		public uint MaxIntegerValue { get; init; }

		public uint Threshold { get; init; }
		public double MslWeight { get; init; }
		public Vector256<double> MslWeightVector { get; init; }

		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }


		#endregion

		#region Multiply and Square

		public void Square(FPValues a, FPValues result)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			//_ = CheckPWValues(result.MantissaMemories, out var errors);

			//if (errors.Length > 1)
			//{
			//	Debug.WriteLine($"PW Errors found at Square, Errors:\n{errors}.");
			//}


			//ClearManatissMems(_squareResult1Mems, onlyInPlayItems: false);
			//ClearManatissMems(_squareResult2Mems, onlyInPlayItems: false);
			//ClearManatissMems(result.MantissaMemories, onlyInPlayItems: false);

			SquareInternal(a, _squareResult1Mems);
			PropagateCarries(_squareResult1Mems, _squareResult2Mems);
			ShiftAndTrim(_squareResult2Mems, result.MantissaMemories);

			//_ = CheckPWValues(result.MantissaMemories, out errors);

			//if(errors.Length > 1)
			//{
			//	Debug.WriteLine($"PW Errors found at Square, Errors:\n{errors}.");
			//}

			//if (flags.Any(x => x != 0))
			//{
			//	Debug.WriteLine($"Found a prb.");
			//}
		}

		private void SquareInternal(FPValues a, Memory<ulong>[] resultLimbs)
		{
			ClearManatissMems(resultLimbs, onlyInPlayItems: true);

			var indexes = InPlayList;
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
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

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var lows = Avx2.And(productVector, HIGH_MASK_VEC);    // Create new ulong from bits 0 - 31.
						var highs = Avx2.ShiftRightLogical(productVector, 32);   // Create new ulong from bits 32 - 63.

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
					}
				}
			}
		}

		#endregion

		#region Multiply Post Processing

		private void PropagateCarries(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var intermediateLimbCount = mantissaMems.Length;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var limbVecs = GetLimbVectorsUL(mantissaMems[0]);
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[0]);

				var carries = Avx2.ShiftRightLogical(limbVecs[idx], 32); // The high 32 bits of sum becomes the new carry.
				resultLimbVecs[idx] = Avx2.And(limbVecs[idx], HIGH_MASK_VEC);    // The low 32 bits of the sum is the result.

				for (int i = 1; i < intermediateLimbCount; i++)
				{
					limbVecs = GetLimbVectorsUL(mantissaMems[i]);
					resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);

					var withCarries = Avx2.Add(limbVecs[idx], carries);

					NumberOfSplits++;
					carries = Avx2.ShiftRightLogical(withCarries, 32);          // The high 32 bits of sum becomes the new carry.
					resultLimbVecs[idx] = Avx2.And(withCarries, HIGH_MASK_VEC); // The low 32 bits of the sum is the result.
				}

				//var isZeroFlags = Avx2.CompareEqual(carries, _zeroVector);
				//var isZeroComposite = (uint)Avx2.MoveMask(isZeroFlags.AsByte());

				//if (isZeroComposite != 0xffffffff)
				//{
				//	// At least one carry is not zero.
				//	throw new OverflowException("Overflow on PropagateCarries.");

				//	//ulong maxValueBeforeShift = 16711679;
				//	//var valuePtr = idx * _lanes;
				//	//for (var i = 0; i < _lanes; i++)
				//	//{
				//	//	if (carries.GetElement(i) > 0)
				//	//	{
				//	//		//throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
				//	//		SetMantissa(resultBa, valuePtr + i, new ulong[] { 0, maxValueBeforeShift });
				//	//		NumberOfMCarries++;
				//	//	}
				//	//}
				//}

			}
		}
		
		private void ShiftAndTrim(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int i = 0; i < resultLimbs.Length; i++)
			{
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);
				var limbVecs = GetLimbVectorsUL(mantissaMems[i + sourceIndex]);

				if (sourceIndex > 0)
				{
					var prevLimbVecs = GetLimbVectorsUL(mantissaMems[i + sourceIndex - 1]);
					ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);
				}
				else
				{
					ShiftAndCopyBits(limbVecs, resultLimbVecs);
				}
			}
		}

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> prevSource, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);

				// Take the top shiftAmount of bits from the previous limb
				var previousLimbVector = Avx2.And(prevSource[idx], HIGH_MASK_VEC);
				result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(previousLimbVector, (byte)(32 - shiftAmount)));
			}
		}

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);
			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(FPValues a, FPValues b, FPValues c)
		{
			Add(a, b.Negate(), c);
		}

		public void Add(FPValues a, FPValues b, FPValues c)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var limbVecsA = a.GetLimbVectorsUL(0);
				var limbVecsB = b.GetLimbVectorsUL(0);
				var resultLimbVecs = GetLimbVectorsUL(c.MantissaMemories[0]);

				var carryVector = Vector256<ulong>.Zero;

				var sumVector = Avx2.Add(limbVecsA[0], limbVecsB[0]);
				var withCarriesVector = Avx2.Add(sumVector, carryVector);

				var (lo, newCarries) = GetResultWithCarry(withCarriesVector);
				resultLimbVecs[0] = lo;

				carryVector = newCarries;

				for (int i = 1; i < LimbCount; i++)
				{
					limbVecsA = a.GetLimbVectorsUL(i);
					limbVecsB = b.GetLimbVectorsUL(i);
					resultLimbVecs = GetLimbVectorsUL(c.MantissaMemories[i]);

					sumVector = Avx2.Add(limbVecsA[idx], limbVecsB[idx]);
					withCarriesVector = Avx2.Add(sumVector, carryVector);

					(lo, newCarries) = GetResultWithCarry(withCarriesVector);
					resultLimbVecs[idx] = lo;

					carryVector = newCarries;
				}

				// TODO: If the final carry > 0
				// Set the value on c to be a new MaxInteger Vector

			}
		}

		private (Vector256<ulong> limbs, Vector256<ulong> carry) GetResultWithCarry(Vector256<ulong> nvs)
		{
			NumberOfGetCarries++;

			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.


			//var limbValue = x & HIGH_MASK;

			var limbs = Avx2.And(nvs, HIGH_MASK_VEC); // The low 32 bits of the sum is the result.

			//bool carryFlag;



			//var resultIsNegative = BitOperations.LeadingZeroCount(limbValue) == 32;
			//var nextBitIsNegative = BitOperations.TrailingZeroCount(x >> 32) == 0;

			//if (resultIsNegative)
			//{
			//	limbValue |= HIGH_FILL; // sign extend the result
			//	carryFlag = !nextBitIsNegative; // true if next higher bit is zero
			//}
			//else
			//{
			//	carryFlag = nextBitIsNegative; // true if next higher bit is one
			//}

			//var result = (limbValue, carryFlag ? 1uL : 0uL);
			//return result;


			for (var i = 0; i < _lanes; i++)
			{
			}

			var carryVector = TOP_BITS_MASK_VEC; //  Vector256<ulong>.Zero;

			return (limbs, carryVector);
		}

		#endregion

		#region Create Smx Support

		public Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(fPValues.GetSign(index), fPValues.GetMantissa(index), TargetExponent, BitsBeforeBP, precision);
			return result;
		}

		#endregion

		#region Comparison

		public void IsGreaterOrEqThanThreshold(FPValues a, bool[] results)
		{
			var left = a.GetLimbVectorsUL(LimbCount - 1);
			var right = _thresholdVector;

			IsGreaterOrEqThan(left, right, results);
		}

		private void IsGreaterOrEqThan(Span<Vector256<ulong>> left, Vector256<long> right, bool[] results)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				//var ta = new ulong[_ulongSlots];
				//left[idx].AsVector().CopyTo(ta);

				//var bi = SmxMathHelper.FromPwULongs(ta);
				//var rv = new RValue(bi, -24);

				//var rvS = RValueHelper.ConvertToString(rv);

				// TODO: Add a check confirm the value we receiving is positive.

				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				var resultVector = Avx2.CompareGreaterThan(sansSign.AsInt64(), right);

				var vectorPtr = idx * _lanes;

				for(var i = 0; i < _lanes; i++)
				{
					results[vectorPtr + i] = resultVector.GetElement(i) == -1;
				}
			}
		}

		private uint[][] CheckPWValues(Memory<ulong>[] resultLimbs, out string errors)
		{
			var result = new uint[resultLimbs.Length][];
			var sb = new StringBuilder();

			var indexes = InPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				result[limbPtr] = new uint[ValueCount];

				var limbs = GetLimbVectorsUL(resultLimbs[limbPtr]);

				var oneFound = false;

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];
					var vector = limbs[idx];

					var sansSign = Avx2.And(vector, SIGN_BIT_MASK_VEC);
					var cFlags = Avx2.CompareEqual(vector, sansSign);
					var cComposite = Avx2.MoveMask(cFlags.AsByte());
					if (cComposite != -1)
					{
						sb.AppendLine("Top bit was set.");
					}

					var areGtFlags = (Avx2.CompareGreaterThan(sansSign.AsInt64(), _maxDigitValueVector)).AsUInt64();
					var compositeFlags = (uint)Avx2.MoveMask(areGtFlags.AsByte());

					result[limbPtr][idx] = compositeFlags;

					if (compositeFlags != 0)
					{
						if (!oneFound)
						{
							oneFound = true;
							sb.Append($"Limb: {limbPtr}::");
						}
						sb.AppendLine($"{idx} ");
					}
				}

				if (oneFound)
				{
					sb.AppendLine();
				}
			}

			errors = sb.ToString();

			return result;
		}

		#endregion

		#region TEMPLATES

		//private void MultiplyVecs(Span<Vector256<uint>> left, Span<Vector256<uint>> right, Span<Vector256<ulong>> result)
		//{
		//	foreach (var idx in InPlayList)
		//	{
		//		result[idx] = Avx2.Multiply(left[idx], right[idx]);
		//	}
		//}

		//private void Split(Span<Vector256<ulong>> x, Span<Vector256<ulong>> highs, Span<Vector256<ulong>> lows)
		//{
		//	foreach (var idx in InPlayList)
		//	{
		//		highs[idx] = Avx2.And(x[idx], HIGH_MASK_VEC);   // Create new ulong from bits 32 - 63.
		//		lows[idx] = Avx2.And(x[idx], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.
		//	}
		//}

		//private void AddVecs(Span<Vector256<ulong>> left, Span<Vector256<ulong>> right, Span<Vector256<ulong>> result)
		//{
		//	for (var i = 0; i < left.Length; i++)
		//	{
		//		result[i] = Avx2.Add(left[i], right[i]);
		//	}
		//}

		#endregion

	}
}
