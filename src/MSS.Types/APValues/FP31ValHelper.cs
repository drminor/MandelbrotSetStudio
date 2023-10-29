using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;


namespace MSS.Types.APValues
{
	public class FP31ValHelper
	{
		#region Private Members

		//private const int BITS_PER_LIMB = 32;
		public const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly uint MAX_DIGIT_VALUE = (uint)(Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_HALF_WORD_FACTOR = BigInteger.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const uint LOW31_BITS_SET = 0x7FFFFFFF;		            // bits 0 - 30 are set.

		//private const uint HIGH33_MASK = LOW31_BITS_SET;
		//private const uint CLEAR_RESERVED_BIT = LOW31_BITS_SET;

		//private const ulong HIGH_33_BITS_SET_L =	0xFFFFFFFF80000000; // bits 0 - 30 are set.
		private const ulong LOW31_BITS_SET_L =		0x000000007FFFFFFF; // bits 0 - 30 are set.

		//private const ulong HIGH33_FILL_L = HIGH_33_BITS_SET_L;			// bits 63 - 31 are set.
		private const ulong HIGH33_CLEAR_L = LOW31_BITS_SET_L;			// bits 63 - 31 are reset.

		private const uint TEST_BIT_31 = 0x80000000;					// bit 31 is set.
		private const uint TEST_BIT_30 = 0x40000000;                    // bit 30 is set.
		
		public const uint RESERVED_BIT_MASK = 0x80000000;

		private const uint MOST_NEG_VAL = 0x40000000;					// This negative number causes an overflow when negated.
		//private const ulong MOST_NEG_VAL_REPLACMENT = 0x40000001;		// Most negative value + 1.

		//private static readonly bool USE_DET_DEBUG = true;

		//private const ulong LOW32_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		//private const ulong HIGH32_CLEAR = LOW32_BITS_SET;

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
			var biValue = FromFwUInts(partialWordLimbs, sign);
			var result = new RValue(biValue, exponent, precision);
			return result;
		}

		//public static RValue CreateRValue(bool sign, uint[] partialWordLimbs, ApFixedPointFormat apFixedPointFormat)
		//{
		//	var biValue = FromFwUInts(partialWordLimbs, sign);
		//	var result = new RValue(biValue, apFixedPointFormat.TargetExponent, apFixedPointFormat.TotalBits);
		//	return result;
		//}

		public static FP31Val CreateNewZeroFP31Val(ApFixedPointFormat apFixedPointFormat, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new FP31Val(new uint[apFixedPointFormat.LimbCount], apFixedPointFormat.TargetExponent, apFixedPointFormat.BitsBeforeBinaryPoint, precision);
			return result;
		}

		public static FP31Val CreateFP31Val(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		{
			var result = CreateFP31Val(rValue, apFixedPointFormat.TargetExponent, apFixedPointFormat.LimbCount, apFixedPointFormat.BitsBeforeBinaryPoint);
			return result;
		}

		// Adjust the RValue then convert to FW uint Limbs.
		public static FP31Val CreateFP31Val(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			if (IsValueTooLarge(rValue, bitsBeforeBP, out var bitExpInfo))
			{
				var maxMagnitude = GetMaxMagnitude(bitsBeforeBP);
				throw new ArgumentException($"An RValue with magnitude > {maxMagnitude} cannot be used to create a FP31Val. BitExpInfo: {bitExpInfo}.");
			}

			var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);

			BigInteger adjustedValue;

			if (shiftAmount < 0)
			{
				throw new NotSupportedException("Converting an RValue to a FP31Val that has an exponent smaller than the target is not supported.");
			}
			else if (shiftAmount > 0)
			{
				adjustedValue = rValue.Value * BigInteger.Pow(2, shiftAmount);
			}
			else
			{
				adjustedValue = rValue.Value;
			}

			var limbs = ToFwUInts(adjustedValue, out var sign);
			if (limbs.Length < limbCount)
			{
				limbs = Extend(limbs, limbCount);
			}

			var result = CreateFP31Val(sign, limbs, targetExponent, bitsBeforeBP, rValue.Precision);

			//CheckFP31ValFromRValueResult(rValue, targetExponent, limbCount, bitsBeforeBP, adjustedValue, result, bitExpInfo);

			return result;
		}

		//[Conditional("DEBUG2")]
		//private static void CheckFP31ValFromRValueResult(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP, BigInteger adjustedValue, FP31Val result, string bitExpInfo) 
		//{
		//	var rValueStrVal = RValueHelper.ConvertToString(rValue);
		//	var resultStrVal = result.GetStringValue();

		//	if (rValueStrVal.Length - resultStrVal.Length > 5)
		//	{
		//		var adjustedValueStrVal = adjustedValue.ToString();
		//		Debug.WriteLine($"CreateFP31Val failed. The result is {resultStrVal}, the adjusted value is {adjustedValueStrVal}, the input is {rValueStrVal}. BitExpInfo: {bitExpInfo}.");
		//	}

		//	//var cResult = CreateFP31ValOld(rValue, targetExponent, limbCount, bitsBeforeBP);

		//	//if (cResult.Mantissa.Length != result.Mantissa.Length)
		//	//{
		//	//	Debug.WriteLine("Mantissa Lengths dont' match.");
		//	//}
		//}

		public static FP31Val CreateFP31Val(bool sign, uint[] limbs, int targetExponent, byte bitsBeforeBP, int precision)
		{
			var twoCMantissa = ConvertTo2C(limbs, sign);
			var result = new FP31Val(twoCMantissa, targetExponent, bitsBeforeBP, precision);
			return result;
		}

		private static bool IsValueTooLarge(RValue rValue, byte bitsBeforeBP, out string bitExpInfo)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP);

			var numberOfBitsInVal = rValue.Value.GetBitLength();

			var magnitude = numberOfBitsInVal + rValue.Exponent;

			bitExpInfo = $"Magnitude:{magnitude} (NumBits:{numberOfBitsInVal}, Exponent:{rValue.Exponent}). Max Magnitude:{maxMagnitude}.";
			var result = magnitude > maxMagnitude + 1;

			return result;
		}

		public static uint GetMaxIntegerValue(byte bitsBeforeBP)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP);

			var result = (uint)Math.Pow(2, maxMagnitude) - 1; // 2^8 - 1 = 255
			return result;
		}

		public static byte GetMaxMagnitude(byte bitsBeforeBP)
		{
			// If using signed values the range of positive (and negative) values is halved. (For example  0 to 127 instead of 0 to 255. (or -128 to 0)
			var maxMagnitude = (byte)(bitsBeforeBP - 1);

			return maxMagnitude;
		}

		private static int GetShiftAmount(int currentExponent, int targetExponent)
		{
			//var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
			var shiftAmount = -1 * (targetExponent - currentExponent);

			return shiftAmount;
		}

		// Pad with leading zeros.
		private static uint[] Extend(uint[] values, int newLength)
		{
			var result = new uint[newLength];
			Array.Copy(values, 0, result, 0, values.Length);

			return result;
		}

		#endregion

		#region Two's Compliment Support

		// Convert from two's compliment, use the sign bit of the mantissa
		public static uint[] ConvertFrom2C(uint[] partialWordLimbs, out bool sign)
		{
			sign = GetSign(partialWordLimbs);
			var result = sign ? partialWordLimbs : FlipBitsAndAdd1(partialWordLimbs);
			return result;
		}

		// Used for diagnostics
		public static double ConvertFrom2C(uint partialWordLimb)
		{
			var sign = (partialWordLimb & TEST_BIT_30) == 0;
			var result = sign ? partialWordLimb : FlipBitsAndAdd1(partialWordLimb) * -1;
			return result;
		}

		public static double ConvertFrom2C(ulong partialWordLimb, out double hi)
		{
			hi = partialWordLimb >> EFFECTIVE_BITS_PER_LIMB;
			var lo = (uint) (partialWordLimb & HIGH33_CLEAR_L);

			var result = ConvertFrom2C(lo);
			return result;
		}

		// Convert to two's compliment, if negative
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
					if (partialWordLimbs[^1] == MOST_NEG_VAL)
					{
						Debug.WriteLine("WARNING: About to take the two's compliment of the most negative number.");
					}
					else
					{
						throw new ArgumentException($"Cannot Convert to 2C format, the value is negative and is already in two's compliment form. {GetDiagDisplayHex("limbs", partialWordLimbs)}");
					}
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

		// Flip all bits and add 1
		public static FP31Val Negate(FP31Val fp31Val)
		{
			var negatedPartialWordLimbs = FlipBitsAndAdd1(fp31Val.Mantissa);
			CheckNegation(negatedPartialWordLimbs, fp31Val.Mantissa);

			var result = new FP31Val(negatedPartialWordLimbs, fp31Val.Exponent, fp31Val.BitsBeforeBP, fp31Val.Precision);

			return result;
		}

		[Conditional("DEBUG2")]
		private static void CheckNegation(uint[] originalPartialWordLimbs, uint[] negatedPartialWordLimbs)
		{
			var currentSign = GetSign(originalPartialWordLimbs);
			var sign = GetSign(negatedPartialWordLimbs);

			if (sign == currentSign)
			{
				Debug.WriteLine($"Negate an FP31Val var did not change the sign. Prev: {GetDiagDisplay("Prev", originalPartialWordLimbs)}, {GetDiagDisplay("New", negatedPartialWordLimbs)}");
			}
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
				var ourVal = partialWordLimbs[limbPtr]; // & LOW31_BITS_SET;

				if (ourVal == 0)
				{
					result[limbPtr] = ourVal;
				}
				//else if (ourVal == MOST_NEG_VAL && limbPtr == resultLength - 1)
				//{
				//	result[limbPtr] = MOST_NEG_VAL_REPLACMENT;
				//	Debug.WriteLineIf(USE_DET_DEBUG, "WARNING: Encountered the most negative number.");
				//}
				else
				{
					result[limbPtr] = FlipLowBitsAfterFirst1(ourVal);
					foundASetBit = true;
				}

				limbPtr++;
			}

			if (foundASetBit)
			{
				// For all remaining limbs...
				// flip the low bits and clear the reserved bit.

				for (; limbPtr < resultLength; limbPtr++)
				{
					result[limbPtr] = partialWordLimbs[limbPtr] ^ LOW31_BITS_SET;
				}
			}

			return result;
		}

		private static uint FlipBitsAndAdd1(uint partialWordLimb)
		{
			uint result;
			var ourVal = partialWordLimb; // & LOW31_BITS_SET;

			if (ourVal == 0)
			{
				result = ourVal;
			}
			//else if (ourVal == MOST_NEG_VAL)
			//{
			//	result = MOST_NEG_VAL_REPLACMENT;
			//	Debug.WriteLineIf(USE_DET_DEBUG, "WARNING: FlipBitsAndAdd1-Single: Encountered the most negative number.");
			//}
			else
			{
				//var someFlipped = FlipLowBitsAfterFirst1(ourVal);
				//result = someFlipped & LOW31_BITS_SET;
				result = FlipLowBitsAfterFirst1(ourVal);
			}

			return result;
		}

		private static uint FlipLowBitsAfterFirst1(uint limb)
		{
			var tzc = BitOperations.TrailingZeroCount(limb);

			Debug.Assert(tzc < 31, "Expecting Trailing Zero Count to be between 0 and 31, inclusive.");

			var numToKeep = tzc + 1;
			var fm = (LOW31_BITS_SET >> numToKeep) << numToKeep;
			var newVal = limb ^ fm;

			return newVal;
		}

		// Returns false, if not correct!!
		public static bool CheckReserveBit(ulong[] partialWordLimbs)
		{
			var reserveBitIsSet = (partialWordLimbs[^1] & TEST_BIT_31) > 0;
			var signBitIsSet = (partialWordLimbs[^1] & TEST_BIT_30) > 0;

			var result = reserveBitIsSet == signBitIsSet;

			return result;
		}

		//// Returns true if the ReserveBit was updated.
		//public static bool UpdateTheReserveBit(ulong[] partialWordLimbs, bool newValue)
		//{
		//	var msl = partialWordLimbs[^1];
		//	var reserveBitWasSet = (msl & TEST_BIT_31) > 0;

		//	var valueWasUpdated = reserveBitWasSet != newValue;

		//	if (newValue)
		//	{
		//		partialWordLimbs[^1] = msl | TEST_BIT_31;
		//	}
		//	else
		//	{
		//		partialWordLimbs[^1] = msl & ~TEST_BIT_31;
		//	}

		//	return valueWasUpdated;
		//}

		//private static ulong[] ExtendSignBit(uint[] partialWordLimbs)
		//{
		//	var result = new ulong[partialWordLimbs.Length];

		//	for (var i = 0; i < partialWordLimbs.Length; i++)
		//	{
		//		result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
		//					? partialWordLimbs[i] | HIGH33_FILL_L
		//					: partialWordLimbs[i] & HIGH33_CLEAR_L;
		//	}

		//	return result;
		//}

		public static bool GetSign(uint[] limbs)
		{
			var result = (limbs[^1] & TEST_BIT_30) == 0;
			return result;
		}

		public static bool CheckSignForMantissaWithLeadingZero(uint[] limbs)
		{
			if (limbs.Length > 0 && limbs[^1] == 0)
			{
				if (limbs.Any(x => x != 0))
				{
					Debug.WriteLine("Getting Sign of a value with a leading zero.");
					return true;
				}
			}

			return false;
		}

		public static uint[] TakeLowerHalves(ulong[] partialWordLimbs)
		{
			var result = new uint[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = (uint)partialWordLimbs[i];

				CheckPWLimb(partialWordLimbs[i], result[i]);
			}

			return result;
		}

		[Conditional("DEBUG2")]
		private static void CheckPWLimb(ulong source, uint result)
		{
			var diff = source - result;

			if (diff != 0)
			{
				throw new InvalidOperationException("TakeLowerHalfs found data in the top half.");
			}
		}

		#endregion

		#region Full 31Bit Limb Support

		public static int GetLimbCount(double precision)
		{
			var result = GetLimbCount(RMapConstants.BITS_BEFORE_BP + (int)precision);
			return result;
		}

		public static int GetLimbCount(int totalNumberOfBits)
		{
			var dResult = totalNumberOfBits / (double)EFFECTIVE_BITS_PER_LIMB;
			var limbCount = (int)Math.Ceiling(dResult);

			return limbCount;
		}

		private static uint[] ToFwUInts(BigInteger bi, out bool sign)
		{
			var tResult = new List<uint>();

			sign = bi.Sign >= 0;
			var hi = sign ? bi : BigInteger.Negate(bi);

			while (hi > MAX_DIGIT_VALUE)
			{
				hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
				tResult.Add((uint)lo);
			}

			tResult.Add((uint)hi);

			return tResult.ToArray();
		}

		private static BigInteger FromFwUInts(uint[] fullWordLimbs, bool sign)
		{
			var result = BigInteger.Zero;

			for (var i = fullWordLimbs.Length - 1; i >= 0; i--)
			{
				result *= BI_HALF_WORD_FACTOR;
				result += fullWordLimbs[i];
			}

			result = sign ? result : BigInteger.Negate(result);

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

		public static string GetDiagDisplayHex(string name, ulong[] values)
		{
			var strAry = GetStrArrayHex(values);

			return $"{name}:{string.Join("; ", strAry)}";
		}

		public static string[] GetStrArrayHex(ulong[] values)
		{
			var result = values.Select(x => string.Format("0x{0:X4}", x)).ToArray();
			return result;
		}

		#endregion
	}
}
