using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSetGenP.Types
{
    internal class ScalarMathFloatingHelper
	{
		#region Private Members

		private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)(Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to split full-word values into partial-word values.
		private static readonly ulong UL_HALF_WORD_FACTOR = (ulong)Math.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const ulong TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.
		//private const ulong TEST_BIT_30 = 0x0000000040000000; // bit 30 is set.

		private const ulong LOW32_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH32_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.
		private const ulong HIGH32_FILL = HIGH32_BITS_SET;
		//private const ulong HIGH32_CLEAR = LOW32_BITS_SET;


		//private const ulong HIGH33_BITS_SET = 0xFFFFFFFF80000000; // bits 63 - 31 are set.
		//private const ulong LOW31_BITS_SET = 0x000000007FFFFFFF;    // bits 0 - 30 are set.

		//private const ulong HIGH32_MASK = LOW31_BITS_SET;
		//private const ulong LOW31_MASK = HIGH33_BITS_SET;

		//private const ulong HIGH33_FILL = HIGH33_BITS_SET;
		//private const ulong HIGH33_CLEAR = LOW31_BITS_SET;

		//private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region 2C Support - 32

		public static ulong[] FlipBitsAndAdd1Old(ulong[] partialWordLimbs)
		{
			//	Start at the least significant bit (LSB), copy all the zeros, until the first 1 is reached;
			//	then copy that 1, and flip all the remaining bits.

			var resultLength = partialWordLimbs.Length;

			var result = new ulong[resultLength];

			var foundASetBit = false;
			var limbPtr = 0;

			while (limbPtr < resultLength && !foundASetBit)
			{
				var ourVal = partialWordLimbs[limbPtr] & HIGH_MASK_OLD;         // Set all high bits to zero
				if (ourVal == 0)
				{
					result[limbPtr] = ourVal;
				}
				else
				{
					var someFlipped = FlipLowBitsAfterFirst1Old(ourVal);
					result[limbPtr] = someFlipped;

					foundASetBit = true;
				}

				limbPtr++;
			}

			if (foundASetBit)
			{
				// For all remaining limbs...
				// flip the low bits and clear the high bits

				for (; limbPtr < resultLength; limbPtr++)
				{
					var ourVal = partialWordLimbs[limbPtr] & HIGH_MASK_OLD;         // Set all high bits to zero	
					var lowBitsFlipped = FlipAllLowBitsOld(ourVal);
					result[limbPtr] = lowBitsFlipped;
				}

				// Sign extend the msl
				result[^1] = ExtendSignBitOld(result[^1]);
			}
			else
			{
				// All bits are zero
			}


			if (CheckPW2CValuesOld(result))
			{
				if (!foundASetBit) Debug.WriteLine("All bits are zero, on call to FlipBitsAndAdd1");
				throw new ArgumentException($"FlipBitsAndAdd1 is returning an incorrect value. {ScalarMathHelper.GetDiagDisplayHex("Before", partialWordLimbs)}, {ScalarMathHelper.GetDiagDisplayHex("After", result)}");
			}

			return result;
		}

		private static ulong FlipLowBitsAfterFirst1Old(ulong limb)
		{
			limb &= HIGH_MASK_OLD;

			var tzc = BitOperations.TrailingZeroCount(limb);

			Debug.Assert(tzc < 32, "Expecting Trailing Zero Count to be between 0 and 31, inclusive.");

			var numToKeep = tzc + 1;
			var numToFlip = 64 - numToKeep;

			var flipMask = limb ^ LOW32_BITS_SET;                 // flips lower half, sets upper half to all ones
			flipMask = (flipMask >> numToKeep) << numToKeep;    // set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			var target = (limb << numToFlip) >> numToFlip;      // set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			var newVal = target | flipMask;

			return newVal;
		}

		private static ulong FlipAllLowBitsOld(ulong limb)
		{
			limb &= HIGH_MASK_OLD;
			var newVal = limb ^ LOW32_BITS_SET;

			return newVal;
		}

		//// Used for diagnostics
		//public static double ConvertFrom2COld(ulong partialWordLimb)
		//{
		//	var signBitIsSet = (partialWordLimb & TEST_BIT_31) > 0;
		//	var isNegative = signBitIsSet;

		//	double result;

		//	if (isNegative)
		//	{
		//		var resultLimbs = FlipBitsAndAdd1(new ulong[] { partialWordLimb });
		//		result = resultLimbs[0];
		//	}
		//	else
		//	{
		//		result = partialWordLimb;
		//	}

		//	return isNegative ? result * -1 : result;
		//}

		#endregion

		#region Split and Pack -- 32

		private const ulong HIGH_MASK_OLD = LOW32_BITS_SET;
		private const ulong LOW_MASK_OLD = HIGH32_BITS_SET;


		public static (ulong hi, ulong lo) Split32(ulong x)
		{
			var hi = x >> BITS_PER_LIMB; // Create new ulong from bits 33 - 63.
			var lo = x & HIGH_MASK_OLD; // Create new ulong from bits 0 - 30.

			return (hi, lo);
		}

		private static ulong[] SetHighHalvesToZeroOld(ulong[] partialWordLimbs, bool? sign = null)
		{
			var result = new ulong[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = partialWordLimbs[i] & HIGH_MASK_OLD;
			}

			return result;
		}

		public static ulong[] ExtendSignBitOld(ulong[] partialWordLimbs)
		{
			var result = new ulong[partialWordLimbs.Length];
			Array.Copy(result, partialWordLimbs, result.Length);
			result[^1] = ExtendSignBitOld(result[^1]);

			return result;
		}

		public static ulong ExtendSignBitOld(ulong limb)
		{
			var result = (limb & TEST_BIT_31) > 0
						? limb | HIGH32_FILL
						: limb & HIGH_MASK_OLD;

			return result;
		}

		public static ulong GetLowHalfOld(ulong partialWordLimb, out bool resultIsNegative, out bool extendedCarryOutIsNegative)
		{
			var result = partialWordLimb & HIGH_MASK_OLD;

			var lzc = BitOperations.LeadingZeroCount(result);

			resultIsNegative = lzc == 32; // The first bit of the limb is set.

			extendedCarryOutIsNegative = BitOperations.TrailingZeroCount(partialWordLimb >> 32) == 0; // The first bit of the top half is set.

			return result;
		}

		public static bool GetSignOld(ulong[] partialWordLimbs)
		{
			var signBitIsSet = (partialWordLimbs[^1] & TEST_BIT_31) > 0;
			var result = !signBitIsSet; // Bit 31 is set for negative values.

			return result;
		}

		// Checks for proper sign extension
		// Returns true if not correct!!
		public static bool CheckPW2CValuesOld(ulong[] partialWordLimbs)
		{
			var msl = partialWordLimbs[^1];

			if (CheckPw2cMslOld(msl))
			{
				return true;
			}

			for (var i = 0; i < partialWordLimbs.Length - 1; i++)
			{
				var hiBits = partialWordLimbs[i] & LOW_MASK_OLD;

				if (hiBits != 0)
				{
					return true;
				}
			}

			return false;
		}

		// Returns true if not correct!!
		public static bool CheckPw2cMslOld(ulong limb)
		{
			var isNegative = (limb & TEST_BIT_31) > 0;
			var hi = limb & LOW_MASK_OLD;
			var result = isNegative ? hi == 0 : hi != 0;

			return result;
		}

		//public static bool CheckPW2CValues(ulong[] partialWordLimbs)
		//{
		//	for (var i = 0; i < partialWordLimbs.Length; i++)
		//	{
		//		var limb = partialWordLimbs[i] >> 32;
		//		if (!(limb == 0 || limb == LOW_BITS_SET))
		//		{
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		//[Conditional("DEBUG")]
		//private static void CheckPW2CValuesBeforeClearingHighs(ulong[] partialWordLimbs, bool? sign = null)
		//{
		//	bool checkFails;
		//	if (sign.HasValue)
		//	{
		//		checkFails = CheckPW2CValues(partialWordLimbs, sign.Value);
		//	}
		//	else
		//	{
		//		checkFails = CheckPW2CValues(partialWordLimbs);
		//	}

		//	if (checkFails)
		//	{
		//		throw new InvalidOperationException("One or more partial-word limbs has a non-zero value in the top half.");
		//	}
		//}

		#endregion

		#region Unused 

		private static ulong[] PackPartialWordLimbs(ulong[] partialWordLimbs)
		{
			Debug.Assert(partialWordLimbs.Length % 2 == 0, "The array being split has a length that is not an even multiple of two.");

			var result = new ulong[partialWordLimbs.Length / 2];

			for (int i = 0; i < partialWordLimbs.Length; i += 2)
			{
				result[i / 2] = partialWordLimbs[i] + UL_HALF_WORD_FACTOR * partialWordLimbs[i + 1];
			}

			return result;
		}

		//// Values are ordered from least significant to most significant.
		//private static ulong[] SplitFullWordLimbs(ulong[] packedValues)
		//{
		//	var result = new ulong[packedValues.Length * 2];

		//	for (int i = 0; i < packedValues.Length; i++)
		//	{
		//		var (hi, lo) = Split32(packedValues[i]);

		//		result[2 * i] = lo;
		//		result[2 * i + 1] = hi;
		//	}

		//	return result;
		//}

		private static bool IsValueTooLargeOld(ulong[] partialWordLimbs, int currentExponent, byte bitsBeforeBP, bool isSigned, out byte maxMagnitude, out int indexOfMsb)
		{
			var magnitude = GetMagnitudeOfIntegerPart_NotUsed(partialWordLimbs, currentExponent, out indexOfMsb);
			maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);

			var result = magnitude > maxMagnitude;

			return result;
		}

		private static bool IsValueTooLarge(RValue rValue, byte bitsBeforeBP, bool isSigned, out string bitExpInfo)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);

			var numberOfBitsInVal = rValue.Value.GetBitLength();

			var magnitude = rValue.Value.GetBitLength() + rValue.Exponent;

			bitExpInfo = $"NumBits: {numberOfBitsInVal}, Exponent: {rValue.Exponent}, Max Magnitude: {maxMagnitude}.";
			var result = magnitude > maxMagnitude + 10;

			return result;
		}

		public static uint GetMaxIntegerValue(byte bitsBeforeBP, bool isSigned)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);

			var result = (uint)Math.Pow(2, maxMagnitude) - 1; // 2^8 - 1 = 255
			return result;
		}

		public static byte GetMaxMagnitude(byte bitsBeforeBP, bool isSigned)
		{
			// If using signed values the range of positive (and negative) values is halved. (For example  0 to 127 instead of 0 to 255. (or -128 to 0)
			var maxMagnitude = (byte)(bitsBeforeBP - (isSigned ? 1 : 0));

			return maxMagnitude;
		}



		//Calculate the number of bits from the most significant limb, this value will require.
		private static byte GetMagnitudeOfIntegerPart_NotUsed(ulong[] partialWordLimbs, int exponent, out int indexOfMsb)
		{
			// TODO: Is the GetMagnitudeOfIntegerPart method "to picky?"
			// TODO: What happens if the limbs provided has a leading zero
			// TODO: Can the code be improved to find the '1's place in the incoming value -- more clearly?

			var lzc = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);

			Debug.Assert(lzc >= 32 && lzc <= 64, "The MSL has a value larger than the max digit.");

			// Adjust leading zero count to reference the start of the bottom (least significant word)
			lzc -= 32;

			// This is the bit position of the top-most bit of the incoming value, relative to the lsb end of the most significant limb.
			indexOfMsb = 32 - lzc;  // 0, if lzc = 32, 0 if lzc = 32

			var bitsBeforeBP = 8; // GetNumberOfBitsBeforeBP(partialWordLimbs.Length, exponent);

			if (bitsBeforeBP <= 0)
			{
				// There are no bits present in the partialWordLimbs that are beyond the binary point.
				return 0;
			}

			if (bitsBeforeBP > BITS_PER_LIMB)
			{
				// There are 32 or more bits present in the partialWordLims that are beyond the binary point. 
				// We only support 32 or less.
				throw new OverflowException("The integer portion is larger than uint max.");
			}

			var indexOfBP = 32 - bitsBeforeBP; // This is the bit position of the bit having exponent 0, i.e., its binary point, relative to the lsb end of the most significant limb 

			// This is the number of bits found in the msl above the value's binary point.
			var sizeInBitsOfIntegerVal = Math.Max(indexOfMsb - indexOfBP, 0);

			//var diagVal = GetTopBits(partialWordLimbs[^1], bitsBeforeBP);
			//Debug.Assert(diagVal <= Math.Pow(2, sizeInBitsOfIntegerVal));

			return (byte)sizeInBitsOfIntegerVal;
		}

		private static ApFixedPointFormat GetCurrentFpFormat(int limbCount, int exponent)
		{
			var totalBits = BITS_PER_LIMB * limbCount;

			(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

			var fractionalBits = limbIndex * BITS_PER_LIMB + bitOffset;
			var bitsBeforeBP = (byte)(totalBits - fractionalBits);

			var result = new ApFixedPointFormat(bitsBeforeBP, fractionalBits);
			return result;
		}

		private static uint GetIntegerPart(ulong[] partialWordLimbs, ApFixedPointFormat fpFormat)
		{
			if (fpFormat.BitsBeforeBinaryPoint <= 0)
			{
				return 0;
			}

			if (fpFormat.BitsBeforeBinaryPoint > 32)
			{
				throw new OverflowException("The integer portion is larger than uint max.");
			}

			var topBits = GetTopBits(partialWordLimbs[^1], fpFormat.BitsBeforeBinaryPoint);

			return topBits;

			//var msl = partialWordLimbs[^1];
			//var digitFactor = exponent + BITS_PER_LIMB * (partialWordLimbs.Length - 1);

			//Debug.Assert(digitFactor <= 0, "digitFactor is > 0.");
			//Debug.Assert(digitFactor >= -32, "digitFactor < -32.");

			//var digitWeight = Math.Pow(2, digitFactor);
			//var result = (uint)Math.Round(msl * digitWeight);

			//return result;
		}

		private static uint GetTopBits(ulong partialWordValue, int numberOfBits)
		{
			if (numberOfBits < 0 || numberOfBits > 32)
			{
				throw new ArgumentException($"The number of bits must be between 0 and 32.");
			}

			var v1 = (partialWordValue & LOW32_BITS_SET) >> (32 - numberOfBits);
			return (uint)v1;
		}

		private static int GetNumberOfBitsBeforeBP(int limbCount, int exponent)
		{
			var totalBits = EFFECTIVE_BITS_PER_LIMB * limbCount;

			(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

			var fractionalBits = limbIndex * EFFECTIVE_BITS_PER_LIMB + bitOffset;
			var bitsBeforeBP = totalBits - fractionalBits;

			return bitsBeforeBP;
		}

		private static (int index, int offset) GetIndexOfBitBeforeBP(int exponent)
		{
			// What is the index of the limb that is the 1's place 
			int limbIndex;
			int bitOffset;

			if (exponent == 0)
			{
				limbIndex = 0;
				bitOffset = 0;
			}
			else if (exponent > 0)
			{
				(limbIndex, bitOffset) = Math.DivRem(exponent, EFFECTIVE_BITS_PER_LIMB);
				if (bitOffset > 0)
				{
					limbIndex--;
					bitOffset = EFFECTIVE_BITS_PER_LIMB - bitOffset;
				}
			}
			else
			{
				(limbIndex, bitOffset) = Math.DivRem(-1 * exponent, EFFECTIVE_BITS_PER_LIMB);
			}

			return (limbIndex, bitOffset);
		}

		private static int GetNumberOfNonZeroBitsAfterBP_NotUsed(ulong[] mantissa, byte bitsBeforeBP)
		{
			var indexOfLastNonZeroLimb = GetIndexOfLastNonZeroLimb(mantissa);

			if (indexOfLastNonZeroLimb == -1)
			{
				return 0;
			}

			if (indexOfLastNonZeroLimb == mantissa.Length - 1)
			{
				var m = (mantissa[^1] << 32 + bitsBeforeBP) >> 32;
				var lzc = BitOperations.LeadingZeroCount(m) - BITS_PER_LIMB;

				var bitsAfterBp = 32 - lzc;

				return bitsAfterBp;
			}

			var result = ((indexOfLastNonZeroLimb + 1) * BITS_PER_LIMB) - bitsBeforeBP;
			return result;
		}

		private static int GetNumberOfBitsAfterBP(Smx a)
		{
			var result = a.LimbCount * BITS_PER_LIMB - a.BitsBeforeBP;
			return result;
		}

		private static int GetNumberOfLeadingZeroLimbs(Smx a)
		{
			var result = a.Mantissa.Length - GetLogicalLength(a);
			return result;
		}

		private static int GetLogicalLength(Smx a)
		{
			var result = 1 + GetIndexOfLastNonZeroLimb(a.Mantissa);
			return result;
		}

		private static int GetLogicalLength(ulong[] mantissa)
		{
			var result = 1 + GetIndexOfLastNonZeroLimb(mantissa);
			return result;
		}

		private static int GetIndexOfLastNonZeroLimb(ulong[] mantissa)
		{
			for (var i = mantissa.Length - 1; i >= 0; i--)
			{
				if (mantissa[i] != 0)
				{
					break;
				}
			}

			return -1;
		}

		private static uint GetBottomBits(ulong partialWordValue, int numberOfBits)
		{
			if (numberOfBits < 0 || numberOfBits > 32)
			{
				throw new ArgumentException($"The number of bits must be between 0 and 32.");
			}

			var v1 = partialWordValue << (64 - numberOfBits);
			var v2 = v1 >> (64 - numberOfBits);
			return (uint)v2;
		}

		private static ulong ShiftTopBitsDown(ulong partialWordValue, int shiftAmount)
		{
			if (shiftAmount < 0 || shiftAmount > 32)
			{
				throw new ArgumentException($"The number of bits must be between 0 and 32.");
			}

			var v1 = partialWordValue >> shiftAmount;
			return (uint)v1;
		}

		private static ulong ShiftBottomBitsUp(ulong partialWordValue, int shiftAmount)
		{
			if (shiftAmount < 0 || shiftAmount > 32)
			{
				throw new ArgumentException($"The number of bits must be between 0 and 32.");
			}

			var v1 = partialWordValue << (32 + shiftAmount);
			var v2 = v1 >> 32;
			return (uint)v2;
		}

		private static ulong[] CopyFirstXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, newLength);

			return result;
		}

		private static Smx TrimLeadingZeros(Smx a)
		{
			var mantissa = TrimLeadingZeros(a.Mantissa);
			var result = new Smx(a.Sign, mantissa, a.Exponent, a.BitsBeforeBP, a.Precision);
			return result;
		}

		// Remove zero-valued limbs from the Most Significant end.
		// Leaving all least significant limbs intact.
		private static ulong[] TrimLeadingZeros(ulong[] mantissa)
		{
			if (mantissa.Length == 1 && mantissa[0] == 0)
			{
				return mantissa;
			}

			var i = mantissa.Length;
			for (; i > 0; i--)
			{
				if (mantissa[i - 1] != 0)
				{
					break;
				}
			}

			if (i == mantissa.Length)
			{
				return mantissa;
			}

			if (i == 0)
			{
				// All digits are zero
				return new ulong[] { 0 };
			}

			var result = new ulong[i];
			Array.Copy(mantissa, 0, result, 0, i);
			return result;
		}

		// Remove zero-valued limbs from the Least Significant end.
		// Leaving all most significant limbs intact.
		// The out parameter is set to the new exponent
		private static ulong[] TrimTrailingZeros(ulong[] mantissa, int exponent, out int newExponent)
		{
			newExponent = exponent;

			if (mantissa.Length == 0 || mantissa[0] != 0)
			{
				return mantissa;
			}

			var i = 1;
			for (; i < mantissa.Length; i++)
			{
				if (mantissa[i] != 0)
				{
					break;
				}
			}

			if (i == mantissa.Length)
			{
				// All digits are zero
				newExponent = 1;
				return new ulong[] { 0 };
			}

			var result = new ulong[mantissa.Length - i];
			Array.Copy(mantissa, i, result, 0, result.Length);

			newExponent += i * 32;

			return result;
		}


		//private void CopyPWBits(ulong[] source, int sourceIndex, int sourceOffset, ulong[] destination, int destinationIndex, int destinationOffset)
		//{
		//	destination = new ulong[LimbCount];

		//	var sourceLimbPtr = sourceIndex;

		//	var resultLimbPtr = 0;

		//	var shiftAmount = 1;

		//	while (resultLimbPtr < destination.Length - 1 && sourceLimbPtr < source.Length - 1)
		//	{
		//		// discard (remainder count) source bits off the lsb end, leaving (32 - remainder) source bits on the low end, and zeros for the first remainder count msbs.
		//		var hx = source[sourceLimbPtr] >> shiftAmount;
		//		destination[resultLimbPtr] |= hx;
		//		sourceLimbPtr++;
		//		var lx = (source[sourceLimbPtr] << 64 - shiftAmount) >> 32;
		//		destination[resultLimbPtr] |= lx;

		//		resultLimbPtr++;
		//	}

		//	var hx2 = source[sourceLimbPtr] >> shiftAmount;
		//	destination[resultLimbPtr] |= hx2;
		//	sourceLimbPtr++;

		//	if (sourceLimbPtr < source.Length)
		//	{
		//		var lx2 = (source[sourceLimbPtr] << 64 - shiftAmount) >> 32;
		//		destination[resultLimbPtr] |= lx2;
		//	}
		//}

		#endregion

		public static bool CheckPWValues(ShiftedArray<ulong> values)
		{
			var result = values.Array.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
		}

		public static RValue CreateRValue(SmxFloating smx)
		{
			var biValue = ScalarMathHelper.FromPwULongs(smx.Mantissa, smx.Sign);
			//biValue = smx.Sign ? biValue : -1 * biValue;
			var exponent = smx.Exponent;
			var precision = smx.Precision;

			var result = new RValue(biValue, exponent, precision);

			return result;
		}

		public static RValue CreateRValue(SmxSa smx)
		{
			var biValue = ScalarMathHelper.FromPwULongs(smx.MantissaSa.MaterializeAll(), smx.Sign);
			//biValue = smx.Sign ? biValue : -1 * biValue;
			var exponent = smx.Exponent;
			var precision = smx.Precision;

			var result = new RValue(biValue, exponent, precision);

			return result;
		}


	}
}
