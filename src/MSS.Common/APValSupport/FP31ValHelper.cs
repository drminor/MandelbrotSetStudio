using MSS.Common.APValues;
using MSS.Types;
using System;
using System.Collections.Generic;
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

		private const uint LOW32_BITS_SET = 0xFFFFFFFF; // bits 0 - 31 are set.
		//private const uint HIGH32_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.

		private const uint HIGH33_BITS_SET = 0x80000000; // bits 63 - 31 are set.
		private const uint LOW31_BITS_SET = 0x7FFFFFFF;    // bits 0 - 30 are set.

		private const uint TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.
		private const uint TEST_BIT_30 = 0x0000000040000000; // bit 30 is set.

		private const uint MOST_NEG_VAL = 0x40000000;
		private const uint MOST_NEG_VAL_REPLACMENT = 0x40000001;       // Most negative value + 1.

		//private const ulong HIGH32_FILL = HIGH32_BITS_SET;
		//private const ulong HIGH32_CLEAR = LOW32_BITS_SET;

		private const ulong HIGH33_MASK = LOW31_BITS_SET;
		private const ulong LOW31_MASK = HIGH33_BITS_SET;

		private const ulong HIGH33_FILL = HIGH33_BITS_SET;
		private const ulong HIGH33_CLEAR = LOW31_BITS_SET;

		//private static readonly bool USE_DET_DEBUG = false;

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

		#region Smx and RValue Support

		public static RValue CreateRValue(FP31Val fP31Val)
		{
			var sign = GetSign(fP31Val.Mantissa);

			var extendedLimbs = ExtendToPartialWords(fP31Val.Mantissa);

			var negatedPartialWordLimbs = ScalarMathHelper.ConvertFrom2C(extendedLimbs);

			var result = ScalarMathHelper.CreateRValue(sign, negatedPartialWordLimbs, fP31Val.Exponent, fP31Val.Precision);

			return result;
		}

		public static RValue CreateRValue(bool sign, uint[] partialWordLimbs, int exponent, int precision)
		{
			//if (CheckPWValues(partialWordLimbs))
			//{
			//	throw new ArgumentException($"Cannot create an RValue from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			//}

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

			var twoCMantissa = ScalarMathHelper.ConvertTo2C(smx.Mantissa, smx.Sign);

			var packedTwoCMantissa = TakeLowerHalves(twoCMantissa);

			var result = new FP31Val(smx.Sign, packedTwoCMantissa, smx.Exponent, bitsBeforeBP, smx.Precision);

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

		public static ulong[] ExtendToPartialWords(uint[] limbs)
		{
			var result = new ulong[limbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = limbs[i];
			}

			return result;
		}

		//public static uint[] Expand(uint[] limbs)
		//{
		//	var result = new uint[limbs.Length * 2];

		//	for (var i = 0; i < limbs.Length; i++)
		//	{
		//		result[i * 2] = limbs[i];
		//	}

		//	return result;
		//}

		private static bool IsValueTooLarge(RValue rValue, byte bitsBeforeBP, bool isSigned, out string bitExpInfo)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);

			var numberOfBitsInVal = rValue.Value.GetBitLength();

			var magnitude = rValue.Value.GetBitLength() + rValue.Exponent;

			bitExpInfo = $"NumBits: {numberOfBitsInVal}, Exponent: {rValue.Exponent}, Max Magnitude: {maxMagnitude}.";
			var result = magnitude > maxMagnitude + 1; // TODO: Why are we fudging this??

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
			var signBitIsSet = (limbs[^1] & TEST_BIT_30) > 0;
			var result = !signBitIsSet; // Bit 30 is set for negative values.

			return result;
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
