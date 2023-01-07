using MSS.Common.APValues;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MSS.Common.APValSupport
{
	public class FP31ValHelper
	{
		#region Private Members

		//private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly uint MAX_DIGIT_VALUE = (uint)(Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_HALF_WORD_FACTOR = BigInteger.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const uint LOW31_BITS_SET = 0x7FFFFFFF;			    // bits 0 - 30 are set.

		private const uint TEST_BIT_31 = 0x80000000;				// bit 31 is set.
		private const uint TEST_BIT_30 = 0x40000000;				// bit 30 is set.

		private const uint MOST_NEG_VAL = 0x40000000;				// Most negative value
		private const uint MOST_NEG_VAL_REPLACMENT = 0x40000001;    // Most negative value + 1.

		private const ulong HIGH33_FILL = 0xFFFFFFFF80000000;       // bits 63 - 31 are set.
		private const ulong HIGH33_CLEAR = 0x000000007FFFFFFF;      // bits 63 - 31 are reset.

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Construction Support

		public static uint GetThresholdMsl(uint threshold, ApFixedPointFormat apFixedPointFormat, bool isSigned)
		{
			var maxIntegerValue = GetMaxIntegerValue(apFixedPointFormat.BitsBeforeBinaryPoint, isSigned);
			if (threshold > maxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat.");
			}

			var thresholdFP31Val = CreateFP31Val(new RValue(threshold, 0), apFixedPointFormat);
			var result = thresholdFP31Val.Mantissa[^1] - 1;

			return result;
		}

		#endregion

		#region RValue Support

		public static RValue CreateRValue(FP31Val fP31Val)
		{
			var negatedPartialWordLimbs = ConvertFrom2C(fP31Val.Mantissa, out var sign);
			var result = CreateRValue(sign, negatedPartialWordLimbs, fP31Val.Exponent, fP31Val.Precision);
			return result;
		}

		public static RValue CreateRValue(bool sign, uint[] partialWordLimbs, int exponent, int precision)
		{
			var biValue = FromFwUInts(partialWordLimbs);
			biValue = sign ? biValue : -1 * biValue;
			var result = new RValue(biValue, exponent, precision);
			return result;
		}

		public static FP31Val CreateFP31Val(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		{
			var fpFormat = apFixedPointFormat;
			var result = CreateFP31Val(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
			return result;
		}

		public static FP31Val CreateFP31Val(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			var smx = ScalarMathHelper.CreateSmx(rValue, targetExponent, limbCount, bitsBeforeBP);
			var packedMantissa = TakeLowerHalves(smx.Mantissa);
			var twoCMantissa = ConvertTo2C(packedMantissa, smx.Sign);



			var result = new FP31Val(smx.Sign, twoCMantissa, smx.Exponent, bitsBeforeBP, smx.Precision);
			return result;
		}

		public static uint[] TakeLowerHalves(ulong[] partialWordLimbs)
		{
			var result = new uint[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = (uint) partialWordLimbs[i];
			}

			return result;
		}

		public static ulong[] ExtendSignBit(uint[] partialWordLimbs)
		{

			var result = new ulong[partialWordLimbs.Length];

			for (var i = 0; i < partialWordLimbs.Length; i++)
			{
				result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
							? partialWordLimbs[i] | HIGH33_FILL
							: partialWordLimbs[i] & HIGH33_CLEAR;
			}

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

		public static bool GetSign(uint[] limbs)
		{
			var result = (limbs[^1] & TEST_BIT_30) == 0;
			return result;
		}

		#endregion

		#region Two's Compliment Support

		// Convert from two's compliment, use the sign bit of the mantissa
		public static uint[] ConvertFrom2C(uint[] partialWordLimbs, out bool sign)
		{
			uint[] result;

			sign = GetSign(partialWordLimbs);

			if (sign)
			{
				result = partialWordLimbs;
			}
			else
			{
				// Convert negative values back to 'standard' representation
				result = FlipBitsAndAdd1(partialWordLimbs);
			}

			return result;
		}

		/// <summary>
		/// Creates the two's compliment representation of a mantissa using the 
		/// partialWordLimbs that represent the absolute value in standard binary.
		/// </summary>
		/// <param name="partialWordLimbs"></param>
		/// <param name="sign"></param>
		/// <returns></returns>
		/// <exception cref="OverflowException"></exception>
		public static uint[] ConvertTo2C(uint[] partialWordLimbs, bool sign)
		{
			uint[] result;

			var signBitIsSet = !GetSign(partialWordLimbs);

			if (sign)
			{
				if (signBitIsSet)
				{
					throw new OverflowException($"Cannot Convert to 2C format, the msb is already set. {GetDiagDisplayHex("limbs", partialWordLimbs)}");
				}

				var reserveBitIsSet = (partialWordLimbs[^1] & TEST_BIT_31) > 0;

				if (reserveBitIsSet)
				{
					throw new ArgumentException($"The reserve bit does not match the sign bit while calling to ConvertTo2C for a positive value. {GetDiagDisplayHex("limbs", partialWordLimbs)}");
				}

				//Debug.WriteLine($"Converting a value to 2C format, {GetDiagDisplayHex("limbs", partialWordLimbs)}.");

				// Postive values have the same representation in both two's compliment and standard form.
				result = partialWordLimbs;
			}
			else
			{
				//Debug.WriteLine($"Converting and negating a value to 2C format, {GetDiagDisplayHex("limbs", partialWordLimbs)}.");

				if (signBitIsSet)
				{
					throw new ArgumentException($"Cannot Convert to 2C format, the value is negative and is already in two's compliment form. {GetDiagDisplayHex("limbs", partialWordLimbs)}");
				}

				//// Reset the Reserve bit so that it will be updated as the Sign bit is updated.
				//if (UpdateTheReserveBit(partialWordLimbs, false))
				//{
				//	Debug.WriteLineIf(USE_DET_DEBUG, "WARNING: The ReserveBit did not match the sign bit on call to ConvertTo2C a positive value.");
				//}

				result = FlipBitsAndAdd1(partialWordLimbs);

				var signBitIsClear = GetSign(result);
				if (signBitIsClear)
				{
					//throw new OverflowException($"Cannot ConvertAbsValTo2C, after the conversion the msb is NOT set to 1. {GetDiagDisplayHex("OrigVal", partialWordLimbs)}. {GetDiagDisplayHex("Result", result)}.");
					Debug.WriteLine($"Cannot ConvertAbsValTo2C, after the conversion the msb is NOT set to 1. {GetDiagDisplayHex("OrigVal", partialWordLimbs)}. {GetDiagDisplayHex("Result", result)}.");
				}
			}

			return result;
		}

		public static uint[] FlipBitsAndAdd1(uint[] partialWordLimbs)
		{
			//	Start at the least significant bit (LSB), copy all the zeros, until the first 1 is reached;
			//	then copy that 1, and flip all the remaining bits.

			var resultLength = partialWordLimbs.Length;

			var result = new uint[resultLength];

			var foundASetBit = false;
			var limbPtr = 0;

			while (limbPtr < resultLength && !foundASetBit)
			{
				var ourVal = partialWordLimbs[limbPtr] & LOW31_BITS_SET;
				if (ourVal == 0)
				{
					result[limbPtr] = ourVal;
				}
				else if (ourVal == MOST_NEG_VAL && limbPtr == resultLength - 1)
				{
					result[limbPtr] = MOST_NEG_VAL_REPLACMENT;
					Debug.WriteLineIf(USE_DET_DEBUG, "WARNING: Encountered the most negative number.");
				}
				else
				{
					var someFlipped = FlipLowBitsAfterFirst1(ourVal);
					result[limbPtr] = someFlipped & LOW31_BITS_SET;

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
					result[limbPtr] = ~partialWordLimbs[limbPtr] & LOW31_BITS_SET;
				}
			}

			return result;
		}

		private static uint FlipLowBitsAfterFirst1(uint limb)
		{
			var tzc = BitOperations.TrailingZeroCount(limb);

			Debug.Assert(tzc < 31, "Expecting Trailing Zero Count to be between 0 and 31, inclusive.");

			var numToKeep = tzc + 1;
			var numToFlip = 32 - numToKeep;

			var flipMask = ~limb;								// flips all bits
			flipMask = (flipMask >> numToKeep) << numToKeep;    // set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			var target = (limb << numToFlip) >> numToFlip;      // set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			var newVal = target | flipMask;

			return newVal;
		}

		// Used for diagnostics
		public static double ConvertFrom2C(uint partialWordLimb)
		{
			var isNegative = (partialWordLimb & TEST_BIT_30) > 0;

			double result;

			if (isNegative)
			{
				var resultLimbs = FlipBitsAndAdd1(new uint[] { partialWordLimb });
				result = resultLimbs[0];
			}
			else
			{
				result = partialWordLimb;
			}

			return isNegative ? result * -1 : result;
		}

		#endregion

		#region Convert to Full-31Bit-Word Limbs

		public static uint[] ToFwUInts(BigInteger bi)
		{
			var tResult = new List<uint>();
			var hi = BigInteger.Abs(bi);

			while (hi > MAX_DIGIT_VALUE)
			{
				hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
				tResult.Add((uint)lo);
			}

			tResult.Add((uint)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromFwUInts(uint[] partialWordLimbs)
		{
			var result = BigInteger.Zero;

			for (var i = partialWordLimbs.Length - 1; i >= 0; i--)
			{
				result *= BI_HALF_WORD_FACTOR;
				result += partialWordLimbs[i];
			}

			return result;
		}

		#endregion

		#region To String Support

		public static string GetDiagDisplay(string name, uint[] values, int stride)
		{
			var rowCnt = values.Length / stride;

			var sb = new StringBuilder();
			sb.AppendLine($"{name}:");

			for (int i = 0; i < rowCnt; i++)
			{
				var rowValues = new uint[stride];

				Array.Copy(values, i * stride, rowValues, 0, stride);
				sb.AppendLine(GetDiagDisplay($"Row {i}", rowValues));
			}

			return sb.ToString();
		}

		public static string GetDiagDisplay(string name, uint[] values)
		{
			var strAry = GetStrArray(values);

			return $"{name}:{string.Join("; ", strAry)}";
		}

		public static string[] GetStrArray(uint[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
		}

		public static string GetDiagDisplayHex(string name, uint[] values)
		{
			var strAry = GetStrArrayHex(values);

			return $"{name}:{string.Join("; ", strAry)}";
		}

		public static string[] GetStrArrayHex(uint[] values)
		{
			var result = values.Select(x => string.Format("0x{0:X4}", x)).ToArray();
			return result;
		}

		//public static string GetDiagDisplayHexBlocked(string name, ulong[] values)
		//{
		//	var strAry = GetStrArrayHexBlocked(values);

		//	return $"{name}:{string.Join("; ", strAry)}";
		//}

		//public static string[] GetStrArrayHexBlocked(ulong[] values)
		//{
		//	var strAry = GetStrArrayHex(values);

		//	var result = new string[values.Length];
		//	for (var i = 0; i < values.Length; i++)
		//	{
		//		var hexFormattedULong = strAry[i];
		//		result[i] = hexFormattedULong[0..3] + hexFormattedULong[4..7] + hexFormattedULong[8..11] + hexFormattedULong[12..15] + hexFormattedULong[16..19] + hexFormattedULong[20..23] + hexFormattedULong[24..27] + hexFormattedULong[28..31];
		//	}

		//	return result;
		//}

		//private string GetHiLoDiagDisplay(string name, ulong[] values)
		//{
		//	Debug.Assert(values.Length % 2 == 0, "GetHiLoDiagDisplay is being called with an array that has a length that is not an even multiple of two.");

		//	var strAry = GetStrArray(values);
		//	var pairs = new string[values.Length / 2];

		//	for (int i = 0; i < values.Length; i += 2)
		//	{
		//		pairs[i / 2] = $"{strAry[i]}, {strAry[i + 1]}";
		//	}

		//	return $"{name}: {string.Join("; ", pairs)}";
		//}


		#endregion

	}
}
