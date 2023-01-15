using MongoDB.Driver;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MSS.Common.SmxVals
{
    public static class ScalarMathHelper
	{
		#region Private Members

		//private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong) (Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_HALF_WORD_FACTOR = BigInteger.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const ulong LOW32_BITS_SET =	0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH32_BITS_SET =	0xFFFFFFFF00000000; // bits 63 - 32 are set.

		private const ulong HIGH33_BITS_SET =	0xFFFFFFFF80000000; // bits 63 - 31 are set.
		private const ulong LOW31_BITS_SET =	0x000000007FFFFFFF;    // bits 0 - 30 are set.

		private const ulong TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.
		private const ulong TEST_BIT_30 = 0x0000000040000000; // bit 30 is set.

		private const ulong MOST_NEG_VAL = 0x0000000040000000;
		private const ulong MOST_NEG_VAL_REPLACMENT = 0x0000000040000001;       // Most negative value + 1.

		//limbs:0x0000; 0x0000; 0x40000000

		private const ulong HIGH32_FILL = HIGH32_BITS_SET;
		private const ulong HIGH32_CLEAR = LOW32_BITS_SET;

		private const ulong HIGH33_MASK = LOW31_BITS_SET;
		private const ulong LOW31_MASK = HIGH33_BITS_SET;

		private const ulong HIGH33_FILL = HIGH33_BITS_SET;
		private const ulong HIGH33_CLEAR = LOW31_BITS_SET;

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Construction Support

		public static ulong GetThresholdMsl(uint threshold, ApFixedPointFormat apFixedPointFormat, bool isSigned)
		{
			var maxIntegerValue = GetMaxIntegerValue(apFixedPointFormat.BitsBeforeBinaryPoint, isSigned);
			if (threshold > maxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat.");
			}

			var thresholdSmx = CreateSmx(new RValue(threshold, 0), apFixedPointFormat);
			var result = thresholdSmx.Mantissa[^1] - 1;

			return result;
		}

		#endregion

		#region Smx and RValue Support

		public static RValue CreateRValue(Smx smx)
		{
			var result = CreateRValue(smx.Sign, smx.Mantissa, smx.Exponent, smx.Precision);
			return result;
		}

		public static RValue CreateRValue(Smx2C smx2C)
		{
			var sign = GetSign(smx2C.Mantissa);

			var negatedPartialWordLimbs = ConvertFrom2C(smx2C.Mantissa);

			//var partialWordLimbsTopCleared = ClearHighHalves(negatedPartialWordLimbs);

			var result = CreateRValue(sign, negatedPartialWordLimbs, smx2C.Exponent, smx2C.Precision);

			return result;
		}

		public static RValue CreateRValue(bool sign, ulong[] partialWordLimbs, int exponent, int precision)
		{
			//if (CheckPWValues(partialWordLimbs))
			//{
			//	throw new ArgumentException($"Cannot create an RValue from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			//}

			var biValue = FromPwULongs(partialWordLimbs, sign);
			//biValue = sign ? biValue : -1 * biValue;
			var result = new RValue(biValue, exponent, precision);
			return result;
		}

		public static Smx CreateSmx(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		{
			var fpFormat = apFixedPointFormat;
			var result = CreateSmx(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
			return result;
		}

		public static Smx CreateSmx(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			var partialWordLimbs = ToPwULongs(rValue.Value, out var sign);

			if (IsValueTooLarge(rValue, bitsBeforeBP, isSigned: false, out var bitExpInfo))
			{
				var maxIntegerValue = GetMaxIntegerValue(bitsBeforeBP, isSigned: false);
				throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx. IndexOfMsb: {GetDiagDisplayHex("limbs", partialWordLimbs)}. Info: {bitExpInfo}.");
			}

			var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);

			//var sign = rValue.Value >= 0;


			var newPartialWordLimbs = ShiftBits(partialWordLimbs, shiftAmount, limbCount, "CreateSmx");

			var partialWordLimbsTopCleared = ClearHighHalves(newPartialWordLimbs, null);
			var result = new Smx(sign, partialWordLimbsTopCleared, targetExponent, bitsBeforeBP, rValue.Precision);

			//var result2 = new Smx(sign, newPartialWordLimbs, targetExponent, bitsBeforeBP, rValue.Precision);

			//if (result != result2)
			//{
			//	Debug.WriteLine("H");
			//}

			return result;
		}

		public static Smx2C CreateSmx2C(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		{
			var fpFormat = apFixedPointFormat;
			var result = CreateSmx2C(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
			return result;
		}

		public static Smx2C CreateSmx2C(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			var smx = CreateSmx(rValue, targetExponent, limbCount, bitsBeforeBP);

			var twoCMantissa = ConvertTo2C(smx.Mantissa, smx.Sign);
			var result = new Smx2C(smx.Sign, twoCMantissa, smx.Exponent, bitsBeforeBP, smx.Precision);

			return result;

			//var partialWordLimbs = ToPwULongs(rValue.Value);
			//if (IsValueTooLarge(rValue, bitsBeforeBP, isSigned: true, out var bitExpInfo))
			//{
			//	var maxIntegerValue = GetMaxIntegerValue(bitsBeforeBP, isSigned: true);
			//	throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx. {GetDiagDisplayHex("limbs", partialWordLimbs)}. Info: {bitExpInfo}.");
			//}

			//var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
			//var newPartialWordLimbs = ShiftBits(partialWordLimbs, shiftAmount, limbCount);

			//var sign = rValue.Value >= 0;
			//var partialWordLimbs2C = ConvertTo2C(newPartialWordLimbs, sign);

			//var cSign = GetSign(partialWordLimbs2C);

			//Debug.Assert(cSign == sign, $"Signs don't match on CreateSmx2C from RValue. RValue has sign: {sign}, new Smx2C has sign: {cSign}.");

			//var result = new Smx2C(cSign, partialWordLimbs2C, targetExponent, bitsBeforeBP, rValue.Precision);

			//return result;
		}

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

		#endregion

		#region 2C Support

		// Convert from two's compliment, use the sign bit of the mantissa
		public static ulong[] ConvertFrom2C(ulong[] partialWordLimbs)
		{
			//if (!CheckReserveBit(partialWordLimbs))
			//{
			//	throw new InvalidOperationException($"Cannot ConvertFrom2C, unless the reserve bit agrees with the sign bit. {GetDiagDisplayHex("input", partialWordLimbs)}.");
			//}

			ulong[] result;

			var sign = GetSign(partialWordLimbs);

			if (sign)
			{
				result = ClearHighHalves(partialWordLimbs);
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
		public static ulong[] ConvertTo2C(ulong[] partialWordLimbs, bool sign)
		{
			ulong[] result;

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

				var rawResult = FlipBitsAndAdd1(partialWordLimbs);

				var signBitIsClear = GetSign(rawResult);
				if (signBitIsClear)
				{
					//throw new OverflowException($"Cannot ConvertAbsValTo2C, after the conversion the msb is NOT set to 1. {GetDiagDisplayHex("OrigVal", partialWordLimbs)}. {GetDiagDisplayHex("Result", result)}.");
					Debug.WriteLine($"Cannot ConvertAbsValTo2C, after the conversion the msb is NOT set to 1. {GetDiagDisplayHex("OrigVal", partialWordLimbs)}. {GetDiagDisplayHex("Result", rawResult)}.");
				}

				result = ExtendSignBit(rawResult);
			}

			return result;
		}

		// Flip all bits and add 1, update the sign to be !sign
		public static Smx2C Negate(Smx2C smx2C)
		{
			//if (!CheckReserveBit(smx2C.Mantissa))
			//{
			//	throw new InvalidOperationException($"Cannot Negate a Smx2C value, unless the reserve bit agrees with the sign bit. {GetDiagDisplayHex("input", smx2C.Mantissa)}.");
			//}

			var currentSign = GetSign(smx2C.Mantissa);
			var negatedPartialWordLimbs = FlipBitsAndAdd1(smx2C.Mantissa);
			var sign = GetSign(negatedPartialWordLimbs);

			if (sign != currentSign)
			{
				Debug.WriteLineIf(USE_DET_DEBUG, $"Negate an Smx2C var did not change the sign. Prev: {GetDiagDisplay("Prev", smx2C.Mantissa)}, {GetDiagDisplay("New", negatedPartialWordLimbs)}");
			}

			var withReserveBitsUpdated = ExtendSignBit(negatedPartialWordLimbs);

			var result = new Smx2C(sign, withReserveBitsUpdated, smx2C.Exponent, smx2C.BitsBeforeBP, smx2C.Precision);

			//Debug.Assert(GetSign(smx2C.Mantissa) == !sign, "Negate an Smx2C var did not change the sign.");

			return result;
		}

		public static ulong[] Toggle2C(ulong[] partialWordLimbs, bool includeTopHalves)
		{
			//if (!CheckReserveBit(partialWordLimbs))
			//{
			//	throw new InvalidOperationException($"Cannot Toggle2C unless the reserve bit agrees with the sign bit. {GetDiagDisplayHex("input", partialWordLimbs)}.");
			//}

			var rawResult = FlipBitsAndAdd1(partialWordLimbs);

			//if (!CheckReserveBit(result))
			//{
			//	throw new InvalidOperationException($"FlipBitsAndAdd1 did not extend the sign into the reserve. {GetDiagDisplayHex("input", partialWordLimbs)}, {GetDiagDisplayHex("result", result)}");
			//}

			var result = ExtendSignBit(rawResult, includeTopHalves);

			return result;
		}

		private static ulong[] FlipBitsAndAdd1(ulong[] partialWordLimbs)
		{
			//	Start at the least significant bit (LSB), copy all the zeros, until the first 1 is reached;
			//	then copy that 1, and flip all the remaining bits.

			var resultLength = partialWordLimbs.Length;

			var result = new ulong[resultLength];

			var foundASetBit = false;
			var limbPtr = 0;

			while (limbPtr < resultLength && !foundASetBit)
			{
				var ourVal = partialWordLimbs[limbPtr] & HIGH33_CLEAR;			// Set all high bits to zero
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
					result[limbPtr] = someFlipped & HIGH33_CLEAR;

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
					//var ourVal = partialWordLimbs[limbPtr] & HIGH33_CLEAR;         // Set all high bits to zero	
					//var lowBitsFlipped = FlipAllLowBits(ourVal);
					//result[limbPtr] = lowBitsFlipped;

					result[limbPtr] = ~partialWordLimbs[limbPtr] & HIGH33_CLEAR;
				}

				//result = ExtendSignBit(result, onlyUseLows:false);
			}

			//if (CheckPW2CValues(result))
			//{
			//	if (!foundASetBit) Debug.WriteLine("All bits are zero, on call to FlipBitsAndAdd1");
			//	throw new ArgumentException($"FlipBitsAndAdd1 is returning an incorrect value. {GetDiagDisplayHex("Before", partialWordLimbs)}, {GetDiagDisplayHex("After", result)}");
			//}

			return result;
		}

		private static ulong FlipLowBitsAfterFirst1(ulong limb)
		{
			var tzc = BitOperations.TrailingZeroCount(limb);

			Debug.Assert(tzc < 31, "Expecting Trailing Zero Count to be between 0 and 31, inclusive.");

			var numToKeep = tzc + 1;
			var numToFlip = 64 - numToKeep;

			var flipMask = limb ^ LOW31_BITS_SET;				// flips lower half, sets upper half to all ones
			flipMask = (flipMask >> numToKeep) << numToKeep;	// set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			var target = (limb << numToFlip) >> numToFlip;		// set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			var newVal = target | flipMask;

			return newVal;
		}

		//private static ulong FlipAllLowBits(ulong limb)
		//{
		//	var newVal = limb ^ LOW31_BITS_SET;

		//	return newVal;
		//}

		// Returns false, if not correct!!
		public static bool CheckReserveBit(ulong[] partialWordLimbs)
		{
			var reserveBitIsSet = (partialWordLimbs[^1] & TEST_BIT_31) > 0;
			var signBitIsSet = (partialWordLimbs[^1] & TEST_BIT_30) > 0;

			var result = reserveBitIsSet == signBitIsSet;

			return result;
		}

		// Returns true if the ReserveBit was updated.
		public static bool UpdateTheReserveBit(ulong[] partialWordLimbs, bool newValue)
		{
			var msl = partialWordLimbs[^1];
			var reserveBitWasSet = (msl & TEST_BIT_31) > 0;

			var valueWasUpdated = reserveBitWasSet != newValue;

			if (newValue)
			{
				partialWordLimbs[^1] = msl | TEST_BIT_31;
			}
			else
			{
				partialWordLimbs[^1] = msl & ~TEST_BIT_31;
			}

			return valueWasUpdated;
		}


		public static ulong[] ExtendSignBit(ulong[] partialWordLimbs, bool includeTopHalves = false)
		{
			var result = new ulong[partialWordLimbs.Length];

			//Array.Copy(partialWordLimbs, result, result.Length);

			//if (includeHighHalf)
			//{
			//	result[^1] = (result[^1] & TEST_BIT_30) > 0
			//				? result[^1] | HIGH33_FILL
			//				: result[^1] & HIGH33_CLEAR;
			//}
			//else
			//{
			//	result[^1] = (result[^1] & TEST_BIT_30) > 0
			//				? result[^1] | TEST_BIT_31
			//				: result[^1] & ~TEST_BIT_31;

			//}

			if (includeTopHalves)
			{
				for (var i = 0; i < partialWordLimbs.Length; i++)
				{
					result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
								? partialWordLimbs[i] | HIGH33_FILL
								: partialWordLimbs[i] & HIGH33_CLEAR;
				}
			}
			else
			{
				for (var i = 0; i < partialWordLimbs.Length; i++)
				{
					result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
								? partialWordLimbs[i] | TEST_BIT_31
								: partialWordLimbs[i] & ~TEST_BIT_31;

					result[i] &= HIGH32_CLEAR;
				}
			}

			return result;
		}

		//private static ulong ExtendSignBitInternal(ulong limb, bool includeHighHalf = false)
		//{
		//	ulong result;
		//	if (includeHighHalf)
		//	{
		//		result = (limb & TEST_BIT_30) > 0
		//					? limb | HIGH33_FILL
		//					: limb & HIGH33_CLEAR;
		//	}
		//	else
		//	{
		//		result = (limb & TEST_BIT_30) > 0
		//					? limb | TEST_BIT_31
		//					: limb & ~TEST_BIT_31;
		//	}

		//	return result;
		//}

		public static bool GetSign(ulong[] partialWordLimbs)
		{
			var signBitIsSet = (partialWordLimbs[^1] & TEST_BIT_30) > 0;
			var result = !signBitIsSet; // Bit 30 is set for negative values.

			return result;
		}

		public static (ulong hi, ulong lo) Split(ulong x)
		{
			// bit 31 (and 63) is being reserved to detect carries when adding / subtracting.
			// this bit should be zero at this point.

			// The low value is in the low 31 bits, indexes 0 - 30.
			// The high value is in bits 32-62

			var hi = x >> EFFECTIVE_BITS_PER_LIMB; // Create new ulong from bits 32 - 62.
			var lo = x & HIGH33_MASK; // Create new ulong from bits 0 - 31.

			return (hi, lo);
		}

		// Clears Bits 33 bits (From 31 to 63)
		// Used when converting back to standard binary representation, i.e., when creating an Smx var.
		public static ulong[] ClearHighHalves(ulong[] partialWordLimbs, bool? sign = null)
		{
			var result = new ulong[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = partialWordLimbs[i] & HIGH33_CLEAR;
			}

			return result;
		}
		
		// Simply update the sign to !sign
		public static Smx Negate(Smx smx)
		{
			var result = new Smx(!smx.Sign, smx.Mantissa, smx.Exponent, smx.BitsBeforeBP, smx.Precision);

			return result;
		}

		// Used for diagnostics
		public static double ConvertFrom2C(ulong partialWordLimb)
		{
			var isNegative = (partialWordLimb & TEST_BIT_30) > 0 | BitOperations.LeadingZeroCount(partialWordLimb) == 0;

			double result;

			if (isNegative)
			{
				var resultLimbs = FlipBitsAndAdd1(new ulong[] { partialWordLimb });
				result = resultLimbs[0];
			}
			else
			{
				result = partialWordLimb;
			}

			if (!CheckReserveBit(new ulong[] { partialWordLimb }))
			{
				var strAdjective = isNegative ? "negative" : "positive";
				Debug.WriteLineIf(USE_DET_DEBUG, $"WARNING: The reserve bit did not agree with the sign bit on ConvertFrom2C a {strAdjective} value to a Double.");
			}

			return isNegative ? result * -1 : result;
		}

		#endregion

		#region GetResultWithCarry

		public static (ulong limbValue, ulong carry) GetResultWithCarrySigned(ulong partialWordLimb, bool isMsl)
		{
			// A carry is generated any time the bit just above the result limb is different than the
			// most significant bit of the limb.msb of the limb
			// i.e. when this next bit is not an extension of the sign.

			var limbValue = GetLowHalfSigned(partialWordLimb, isMsl, out var topBitIsSet, out bool reserveBitIsSet);

			//var carryFlag = topBitIsSet != reserveBitIsSet;

			var carryFlag = reserveBitIsSet; // != topBitIsSet;
			var nvHiPart = partialWordLimb >> 32;


			if (carryFlag)
			{
				if (isMsl)
				{
					//Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned-MSL: {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");
					//carryFlag = reserveBitIsSet != topBitIsSet;
					//carryFlag = reserveBitIsSet;
					Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned-MSL: {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");
				}
				else
				{
					//Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned: {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");
					//carryFlag = reserveBitIsSet;
					//carryFlag = reserveBitIsSet != topBitIsSet;
					Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned: {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");

				}
			}
			else 
			{
				if (nvHiPart > 0)
				{
					if (isMsl)
					{
						Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned-MSL: NO CARRY {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");
					}
					else
					{
						Debug.WriteLineIf(USE_DET_DEBUG, $"GetResultWithCarrySigned: NO CARRY {limbValue} {limbValue:X4}. TopBit:{topBitIsSet}, ReserveBit:{reserveBitIsSet}. Hi: {nvHiPart}.");
					}
				}
			}

			//if (carryFlag)
			//{
			//	//var updatedLimb =  FlipBitsAndAdd1(new ulong[] {limbValue});

			//	//limbValue = updatedLimb[0];

			//	//limbValue = UpdateTheReservedBit(limbValue, topBitIsSet);

			//	//_ = GetLowHalfSigned(limbValue, isMsl, out var topBitIsSetNew, out var reserveBitIsSetNew);
			//	//Debug.WriteLine($"GetResultWithCarrySigned: new value: {limbValue} (with t/r: {topBitIsSetNew}/{reserveBitIsSetNew}). The input is {partialWordLimb} (with t/r: {topBitIsSet}/{reserveBitIsSet}");
			//}

			var result = (limbValue, carryFlag ? 1uL : 0uL);
			return result;
		}

		public static ulong GetLowHalfSigned(ulong partialWordLimb, bool isMsl, out bool topBitIsSet, out bool reserveBitIsSet)
		{
			reserveBitIsSet = (partialWordLimb & TEST_BIT_31) > 0;
			topBitIsSet = (partialWordLimb & TEST_BIT_30) > 0;

			var result = partialWordLimb & HIGH33_CLEAR;
			//// For diagnositics only

			//var hi = partialWordLimb >> 32;

			//if (topBitIsSet && isMsl)
			//{
			//	if (hi == 0)
			//	{
			//		throw new InvalidOperationException("Limb was not sign extended. Expecting this negative limb to have bits 32 - 63 be set to 1.");
			//	}
			//}
			//else
			//{
			//	if (hi > 0)
			//	{
			//		throw new InvalidOperationException("Limb was not sign extended. Expecting this postive limb to have bits 32 - 63 be set to 0");
			//	}
			//}

			return result;
		}

		public static (ulong limbValue, ulong carry) GetResultWithCarry(ulong partialWordLimb)
		{
			//var (hi, lo) = Split(partialWordLimb);
			//return (lo, hi);

			// bit 31 (and 63) is being reserved to detect carries when adding / subtracting.
			// this bit should be zero at this point.

			// The low value is in the low 31 bits, indexes 0 - 30.
			// The high value is in bits 32-62

			var hi = partialWordLimb >> EFFECTIVE_BITS_PER_LIMB; // Create new ulong from bits 32 - 62.
			var lo = partialWordLimb & HIGH33_CLEAR; // Create new ulong from bits 0 - 31.

			return (lo, hi);
		}

		private static ulong UpdateTheReservedBit(ulong limb, bool newValue)
		{
			var result = newValue ? limb | TEST_BIT_31 : limb & ~TEST_BIT_31;
			return result;
		}

		//private static void UpdateTheReservedBitToAgree(ulong limb)
		//{
		//	if ((limb & TEST_BIT_30) > 0)
		//	{
		//		limb |= TEST_BIT_31;
		//	}
		//	else
		//	{
		//		limb &= ~TEST_BIT_31;
		//	}
		//}

		#endregion

		#region	Shift and Trim

		public static ulong[] ShiftAndTrim(ulong[] mantissa, ApFixedPointFormat apFixedPointFormat, bool isSigned, bool includeDebugStatements = false)
		{
			if (includeDebugStatements)
			{
				CheckLimbsBeforeShiftAndTrim(mantissa, apFixedPointFormat.BitsBeforeBinaryPoint);
			}

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Clear the bits from the uppper half + the reserved bit, these will either be all 0's or all 1'. Our values are confirmed to be split at this point.

			var shiftAmount = apFixedPointFormat.BitsBeforeBinaryPoint;

			var result = new ulong[apFixedPointFormat.LimbCount];

			//var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);
			var sourceIndex = mantissa.Length - 1;
			var i = result.Length - 1;

			for (; i >= 0; i--)
			{
				if (sourceIndex > 0)
				{
					// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
					var topHalf = mantissa[sourceIndex] << shiftAmount;
					topHalf &= HIGH33_MASK;										// This will clear the top 32 bits as well as the reserved bit.

					// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
					var bottomHalf = mantissa[sourceIndex - 1] & HIGH33_MASK;
					bottomHalf >>= 31 - shiftAmount;							// Don't include the reserved bit.

					result[i] = topHalf | bottomHalf;

					if (includeDebugStatements)
					{
						var strResult = string.Format("0x{0:X4}", result[i]);
						var strTopHalf = string.Format("0x{0:X4}", topHalf);
						var strBottomHalf = string.Format("0x{0:X4}", bottomHalf);
						Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopHalf} and {strBottomHalf}.");
					}
				}
				else
				{
					// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
					var topHalf = mantissa[sourceIndex] << shiftAmount;
					topHalf &= HIGH33_MASK;

					result[i] = topHalf;

					if (includeDebugStatements)
					{
						var strResult = string.Format("0x{0:X4}", result[i]);
						var strTopH = string.Format("0x{0:X4}", topHalf);
						Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopH}.");
					}
				}

				sourceIndex--;
			}

			if (isSigned)
			{
				// SignExtend the MSL
				result = ExtendSignBit(result);
			}

			if (includeDebugStatements)
			{
				CheckLimbsAfterShiftAndTrim(result);
			}

			return result;
		}

		private static void CheckLimbsBeforeShiftAndTrim(ulong[] mantissa, byte bitsBeforeBP)
		{
			//ValidateIsSplit(mantissa); // Conditional Method

			var lZCounts = GetLZCounts(mantissa);

			Debug.WriteLine($"Before Shift and Trim, LZCounts:");
			for (var lzcPtr = 0; lzcPtr < lZCounts.Length; lzcPtr++)
			{
				Debug.WriteLine($"{lzcPtr}: {lZCounts[lzcPtr]} {mantissa[lzcPtr]}");
			}

			Debug.Assert(lZCounts[^1] >= 32 + 1 + bitsBeforeBP, "The multiplication result is > Max Integer.");
		}

		private static void CheckLimbsAfterShiftAndTrim(ulong[] result)
		{
			var lZCounts = GetLZCounts(result);
			Debug.WriteLine($"S&T LZCounts2:");
			for (var lzcPtr = 0; lzcPtr < lZCounts.Length; lzcPtr++)
			{
				Debug.WriteLine($"{lzcPtr}: {lZCounts[lzcPtr]} {result[lzcPtr]}");
			}
		}

		#endregion

		#region Shift Bits / Scale and Split EXPERIMENTAL

		//private static ulong[] ShiftBits(ulong[] partialWordLimbs, int shiftAmount, int limbCount, string desc)
		//{
		//	ulong[] result;

		//	if (shiftAmount == 0)
		//	{
		//		result = TakeMostSignificantLimbs(partialWordLimbs, limbCount);
		//	}
		//	else if (shiftAmount < 0)
		//	{
		//		throw new NotImplementedException();
		//	}
		//	else
		//	{
		//		result = ScaleAndSplit(partialWordLimbs, shiftAmount, limbCount, desc);
		//	}

		//	// ExtendSignBits, into the Reserved bit, clear the top half!
		//	result = ExtendSignBit(result, includeTopHalves: false);

		//	return result;
		//}

		//private static ulong[] ScaleAndSplit(ulong[] mantissa, int power, int limbCount, string desc)
		//{
		//	if (power <= 0)
		//	{
		//		throw new ArgumentException("The value of power must be 1 or greater.");
		//	}

		//	(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

		//	if (limbOffset > limbCount + 3)
		//	{
		//		return Enumerable.Repeat(0ul, limbCount).ToArray();
		//	}

		//	var factor = (ulong)Math.Pow(2, remainder);

		//	var resultArray = new ulong[mantissa.Length];

		//	var carry = 0ul;

		//	for (var i = 0; i < mantissa.Length; i++)
		//	{
		//		var newLimbVal = mantissa[i] * factor + carry;

		//		var (hi, lo) = Split(newLimbVal); // :Spliter
		//		resultArray[i] = lo;

		//		carry = hi;
		//	}

		//	if (carry > 0)
		//	{
		//		//Debug.WriteLine($"While {desc}, setting carry: {carry}, ll: {result.IndexOfLastNonZeroLimb}, len: {result.Length}, power: {power}, factor: {factor}.");
		//	}

		//	var result = AssembleScaledValue(resultArray, limbOffset, carry, limbCount);

		//	return result;
		//}

		//private static ulong[] AssembleScaledValue(ulong[] resultArray, int offset, ulong carry, int limbCount)
		//{
		//	ulong[] wArray;

		//	if (carry > 0)
		//	{
		//		if (offset > 0) offset -= 1;

		//		var len = resultArray.Length + 1 + offset;
		//		wArray = new ulong[len];

		//		Array.Copy(resultArray, 0, wArray, offset, resultArray.Length);
		//		wArray[^1] = carry;
		//	}
		//	else
		//	{
		//		var len = resultArray.Length + offset;
		//		wArray = new ulong[len];

		//		Array.Copy(resultArray, 0, wArray, offset, resultArray.Length);
		//	}

		//	var result = TakeMostSignificantLimbs(wArray, limbCount);

		//	return result;
		//}

		//private static ulong[] TakeMostSignificantLimbs(ulong[] partialWordLimbs, int length)
		//{
		//	ulong[] result;

		//	var diff = length - partialWordLimbs.Length;

		//	if (diff > 0)
		//	{
		//		result = PadLeft(partialWordLimbs, diff);
		//	}
		//	else if (diff < 0)
		//	{
		//		result = TrimLeft(partialWordLimbs, -1 * diff);
		//	}
		//	else
		//	{
		//		result = partialWordLimbs;
		//	}

		//	return result;
		//}

		//// Pad with leading zeros.
		//private static ulong[] PadLeft(ulong[] values, int amount)
		//{
		//	var newLength = values.Length + amount;
		//	var result = new ulong[newLength];
		//	Array.Copy(values, 0, result, amount, values.Length);

		//	return result;
		//}

		//private static ulong[] TrimLeft(ulong[] values, int amount)
		//{
		//	var newLength = values.Length - amount;
		//	var result = new ulong[newLength];
		//	Array.Copy(values, amount, result, 0, newLength);

		//	return result;
		//}

		////private static uint[] CopyLastXElements(uint[] values, int newLength)
		////{
		////	var result = new uint[newLength];

		////	var sourceStartIndex = Math.Max(values.Length - newLength, 0);

		////	var cLen = values.Length - sourceStartIndex;

		////	var destinationStartIndex = newLength - cLen;

		////	Array.Copy(values, sourceStartIndex, result, destinationStartIndex, cLen);

		////	return result;
		////}

		//public static int GetShiftAmount(int currentExponent, int targetExponent)
		//{
		//	var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
		//	return shiftAmount;
		//}

		////public static (uint hi, uint lo) Split(ulong x)
		////{
		////	// bit 31 is being reserved to detect carries when adding / subtracting.
		////	// this bit should be zero at this point.

		////	// The low value is in the low 31 bits, indexes 0 - 30.
		////	// The high value is in bits 32-63

		////	var hi = (uint)(x >> 32); // Create new ulong from bits 32 - 63.
		////	var lo = (uint)x & CLEAR_RESERVED_BIT;  // Create new ulong from bits 0 - 31.

		////	return (hi, lo);
		////}

		#endregion

		#region Shift Bits / Scale and Split

		private static ulong[] ShiftBits(ulong[] partialWordLimbs, int shiftAmount, int limbCount, string desc)
		{
			ulong[] result;

			if (shiftAmount == 0)
			{
				result = TakeMostSignificantLimbs(partialWordLimbs, limbCount);
			}
			else if (shiftAmount < 0)
			{
				throw new NotImplementedException();
			}
			else
			{
				var sResult = ScaleAndSplit(partialWordLimbs, shiftAmount, limbCount, "Create Smx");

				result = TakeMostSignificantLimbs(sResult, limbCount);
			}

			// ExtendSignBits, into the Reserved bit, clear the top half
			result = ExtendSignBit(result);

			return result;
		}

		private static ulong[] ScaleAndSplit(ulong[] mantissa, int power, int limbCount, string desc)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

			if (limbOffset > limbCount + 3)
			{
				return new ulong[] { 0 };
			}

			var factor = (ulong)Math.Pow(2, remainder);

			var resultArray = new ulong[mantissa.Length];

			var carry = 0ul;

			var indexOfLastNonZeroLimb = -1;
			for (var i = 0; i < mantissa.Length; i++)
			{
				var newLimbVal = mantissa[i] * factor + carry;

				var (hi, lo) = Split(newLimbVal); // :Spliter
				resultArray[i] = lo;

				carry = hi;

				if (lo > 0)
				{
					indexOfLastNonZeroLimb = i;
				}
			}

			if (indexOfLastNonZeroLimb > -1)
			{
				indexOfLastNonZeroLimb += limbOffset;
			}

			var resultSa = new ShiftedArray<ulong>(resultArray, limbOffset, indexOfLastNonZeroLimb);

			if (carry > 0)
			{
				//Debug.WriteLine($"While {desc}, setting carry: {carry}, ll: {result.IndexOfLastNonZeroLimb}, len: {result.Length}, power: {power}, factor: {factor}.");
				resultSa.SetCarry(carry);
			}

			var result = resultSa.MaterializeAll();

			return result;
		}

		private static ulong[] TakeMostSignificantLimbs(ulong[] partialWordLimbs, int length)
		{
			ulong[] result;

			if (partialWordLimbs.Length == length)
			{
				result = partialWordLimbs;
			}
			else if (partialWordLimbs.Length > length)
			{
				result = CopyLastXElements(partialWordLimbs, length);
			}
			else
			{
				result = Extend(partialWordLimbs, length);
			}

			return result;
		}

		private static ulong[] CopyLastXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];

			var startIndex = Math.Max(values.Length - newLength, 0);

			var cLen = values.Length - startIndex;

			Array.Copy(values, startIndex, result, 0, cLen);

			return result;
		}

		// Pad with leading zeros.
		private static ulong[] Extend(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, values.Length);

			return result;
		}

		public static int GetShiftAmount(int currentExponent, int targetExponent)
		{
			var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
			return shiftAmount;
		}

		#endregion

		#region Check and Diagnostic Methods

		private static void ValidateConversion2C(ulong[] partialWordLimbs, string valueName)
		{
			if (CheckPW2CValues(partialWordLimbs))
			{
				throw new ArgumentException($"The {valueName} partialWordLimbs have some high bits set.");
			}
		}

		// Returns true if not correct!!
		public static bool CheckPWValues(ulong[] partialWordLimbs)
		{
			var result = partialWordLimbs.Any(x => x > MAX_DIGIT_VALUE);
			return result;
		}

		// Checks for proper sign extension
		// Returns true if not correct!!
		public static bool CheckPW2CValues(ulong[] partialWordLimbs)
		{
			var msl = partialWordLimbs[^1];

			if (CheckPw2cMsl(msl))
			{
				return true;
			}

			for (var i = 0; i < partialWordLimbs.Length - 1; i++)
			{
				var hiBits = partialWordLimbs[i] & LOW31_MASK;

				if (hiBits != 0)
				{
					return true;
				}
			}

			return false;
		}

		// Returns true if not correct!!
		public static bool CheckPw2cMsl(ulong limb)
		{
			var signBitIsSet = (limb & TEST_BIT_30) > 0;

			var hi = limb & LOW31_MASK;
			var isGood = signBitIsSet ? hi != 0 : hi == 0;

			return !isGood;
		}

		// Just for diagnostics
		public static byte[] GetLZCounts(ulong[] values)
		{
			var result = new byte[values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				result[i] = (byte)BitOperations.LeadingZeroCount(values[i]);
			}

			return result;
		}

		#endregion

		#region Convert to Partial-Word Limbs

		// TOOD: Consider first using ToLongs and then calling Split(

		public static ulong[] ToPwULongs(BigInteger bi, out bool sign)
		{
			var tResult = new List<ulong>();

			//var hi = BigInteger.Abs(bi);

			sign = bi.Sign >= 0;
			var hi = sign ? bi : BigInteger.Negate(bi);

			while (hi > MAX_DIGIT_VALUE)
			{
				hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
				tResult.Add((ulong)lo);
			}

			tResult.Add((ulong)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromPwULongs(ulong[] partialWordLimbs, bool sign)
		{
			var result = BigInteger.Zero;

			for (var i = partialWordLimbs.Length - 1; i >= 0; i--)
			{
				result *= BI_HALF_WORD_FACTOR;
				result += partialWordLimbs[i];
			}

			result = sign ? result : BigInteger.Negate(result);

			return result;
		}

		#endregion

		#region To String Support

		public static string GetDiagDisplay(string name, ulong[] values, int stride)
		{
			var rowCnt = values.Length / stride;

			var sb = new StringBuilder();
			sb.AppendLine($"{name}:");

			for (int i = 0; i < rowCnt; i++)
			{
				var rowValues = new ulong[stride];

				Array.Copy(values, i * stride, rowValues, 0, stride);
				sb.AppendLine(GetDiagDisplay($"Row {i}", rowValues));
			}

			return sb.ToString();
		}

		public static string GetDiagDisplay(string name, ulong[] values)
		{
			var strAry = GetStrArray(values);

			return $"{name}:{string.Join("; ", strAry)}";
		}

		public static string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
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

		#region To ULong Support -- Not Used

		// Integer used to convert BigIntegers to/from array of ulongs containing full-word values
		private static readonly BigInteger BI_FULL_WORD_FACTOR = BigInteger.Pow(2, 2 * EFFECTIVE_BITS_PER_LIMB);

		public static ulong[] ToULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > ulong.MaxValue)
			{
				hi = BigInteger.DivRem(hi, BI_FULL_WORD_FACTOR, out var lo);
				tResult.Add((ulong)lo);
			}

			tResult.Add((ulong)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromULongs(ulong[] values)
		{
			var result = BigInteger.Zero;

			for (int i = values.Length - 1; i >= 0; i--)
			{
				result *= BI_FULL_WORD_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region Reduce 

		//public static ulong[] Reduce(ulong[] mantissa, int exponent, out int newExponent)
		//{
		//	var w = TrimTrailingZeros(mantissa, exponent, out newExponent);

		//	if (w.Length == 0)
		//	{
		//		return w;
		//	}

		//	if (AreComponentsEven(mantissa))
		//	{
		//		newExponent++;
		//		w = new ulong[mantissa.Length];

		//		for (int i = 0; i < w.Length; i++)
		//		{
		//			w[i] = mantissa[i] / 2;
		//		}

		//		while (AreComponentsEven(w))
		//		{
		//			newExponent++;

		//			for (int i = 0; i < w.Length; i++)
		//			{
		//				w[i] = w[i] / 2;
		//			}
		//		}

		//		return w;
		//	}
		//	else
		//	{
		//		return mantissa;
		//	}
		//}

		//private static bool AreComponentsEven(ulong[] mantissa)
		//{
		//	foreach (var value in mantissa)
		//	{
		//		if (value % 2 != 0)
		//		{
		//			return false;
		//		}
		//	}

		//	return mantissa.Any(x => x != 0);
		//}

		#endregion
	}
}
