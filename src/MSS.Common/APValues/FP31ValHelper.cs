using MSS.Common.SmxVals;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static MongoDB.Driver.WriteConcern;


namespace MSS.Common.APValues
{
	public class FP31ValHelper
	{
		#region Private Members

		//private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly uint MAX_DIGIT_VALUE = (uint)(Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_HALF_WORD_FACTOR = BigInteger.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const uint LOW31_BITS_SET = 0x7FFFFFFF;		            // bits 0 - 30 are set.

		private const uint HIGH33_MASK = LOW31_BITS_SET;
		private const uint CLEAR_RESERVED_BIT = LOW31_BITS_SET;

		private const ulong HIGH_33_BITS_SET_L =	0xFFFFFFFF80000000; // bits 0 - 30 are set.
		private const ulong LOW31_BITS_SET_L =		0x000000007FFFFFFF; // bits 0 - 30 are set.

		private const ulong HIGH33_FILL_L = HIGH_33_BITS_SET_L;			// bits 63 - 31 are set.
		private const ulong HIGH33_CLEAR_L = LOW31_BITS_SET_L;			// bits 63 - 31 are reset.

		private const uint TEST_BIT_31 = 0x80000000;					// bit 31 is set.
		private const uint TEST_BIT_30 = 0x40000000;	                // bit 30 is set.


		private const uint MOST_NEG_VAL = 0x40000000;					// This negative number causes an overflow when negated.
		private const ulong MOST_NEG_VAL_REPLACMENT = 0x40000001;		// Most negative value + 1.

		private static readonly bool USE_DET_DEBUG = false;

		private const ulong LOW32_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH32_CLEAR = LOW32_BITS_SET;

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

		public static FP31Val CreateNewZeroFP31Val(ApFixedPointFormat apFixedPointFormat, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new FP31Val(new uint[apFixedPointFormat.LimbCount], apFixedPointFormat.TargetExponent, apFixedPointFormat.BitsBeforeBinaryPoint, precision);
			return result;
		}

		//public static FP31Val CreateFP31ValOld(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		//{
		//	var fpFormat = apFixedPointFormat;
		//	var result = CreateFP31ValOld(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
		//	return result;
		//}

		//public static FP31Val CreateFP31ValOld(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		//{
		//	var smx = /*ScalarMathHelper.*/CreateSmx(rValue, targetExponent, limbCount, bitsBeforeBP);
		//	var packedMantissa = TakeLowerHalves(smx.Mantissa);
		//	var twoCMantissa = ConvertTo2C(packedMantissa, smx.Sign);

		//	var result = new FP31Val(twoCMantissa, smx.Exponent, bitsBeforeBP, smx.Precision);
		//	return result;
		//}

		//public static FP31Val CreateFP31ValFwUints(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		//{
		//	var limbs = ToFwUInts(rValue.Value, out var sign);

		//	if (IsValueTooLarge(rValue, bitsBeforeBP, out var bitExpInfo))
		//	{
		//		var maxMagnitude = GetMaxMagnitude(bitsBeforeBP);
		//		throw new ArgumentException($"An RValue with magnitude > {maxMagnitude} cannot be used to create a FP31Val. " +
		//			$"IndexOfMsb: {GetDiagDisplayHex("limbs", limbs)}. Info: {bitExpInfo}.");
		//	}

		//	var rValueStrVal = RValueHelper.ConvertToString(rValue);

		//	var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
		//	var newLimbs = ShiftBits(limbs, shiftAmount, limbCount, nameof(CreateFP31ValFwUints));

		//	var result = CreateFP31Val(sign, newLimbs, targetExponent, bitsBeforeBP, rValue.Precision);

		//	var resultStrVal = result.GetStringValue();
		//	//Debug.WriteLine($"Got FP31Val: {resultStrVal} from: {rValueStrVal}. {bitExpInfo}");

		//	if (rValueStrVal.Length - resultStrVal.Length > 5)
		//	{
		//		Debug.WriteLine("CreateFP31Val failed.");
		//	}

		//	return result;
		//}

		public static FP31Val CreateFP31Val(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		{
			var fpFormat = apFixedPointFormat;
			var result = CreateFP31Val(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
			return result;
		}
		// Adjust the RValue then convert to FW uint Limbs.
		public static FP31Val CreateFP31Val(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			if (IsValueTooLarge(rValue, bitsBeforeBP, out var bitExpInfo))
			{
				var maxMagnitude = GetMaxMagnitude(bitsBeforeBP);
				throw new ArgumentException($"An RValue with magnitude > {maxMagnitude} cannot be used to create a FP31Val. ");
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
			var result = CreateFP31Val(sign, limbs, targetExponent, bitsBeforeBP, rValue.Precision);

			CheckFP31ValFromRValueResult(rValue, targetExponent, limbCount, bitsBeforeBP, adjustedValue, result, bitExpInfo);

			return result;
		}

		[Conditional("DEBUG")]
		private static void CheckFP31ValFromRValueResult(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP, BigInteger adjustedValue, FP31Val result, string bitExpInfo) 
		{
			var rValueStrVal = RValueHelper.ConvertToString(rValue);
			var resultStrVal = result.GetStringValue();

			if (rValueStrVal.Length - resultStrVal.Length > 5)
			{
				var adjustedValueStrVal = adjustedValue.ToString();
				Debug.WriteLine($"CreateFP31Val failed. The result is {resultStrVal}, the adjusted value is {adjustedValueStrVal}, the input is {rValueStrVal}. BitExpInfo: {bitExpInfo}.");
			}

			//var cResult = CreateFP31ValOld(rValue, targetExponent, limbCount, bitsBeforeBP);

			//if (cResult.Mantissa.Length != result.Mantissa.Length)
			//{
			//	Debug.WriteLine("Mantissa Lengths dont' match.");
			//}
		}

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

			var magnitude = rValue.Value.GetBitLength() + rValue.Exponent;

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

		#endregion


		#region Imported From ScalarMathHelper

		//private static Smx CreateSmx(RValue rValue, ApFixedPointFormat apFixedPointFormat)
		//{
		//	var fpFormat = apFixedPointFormat;
		//	var result = CreateSmx(rValue, fpFormat.TargetExponent, fpFormat.LimbCount, fpFormat.BitsBeforeBinaryPoint);
		//	return result;
		//}

		//private static Smx CreateSmx(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
		//{
		//	var partialWordLimbs = ToPwULongs(rValue.Value, out var sign);

		//	if (IsValueTooLarge(rValue, bitsBeforeBP/*, isSigned: false*/, out var bitExpInfo))
		//	{
		//		var maxIntegerValue = GetMaxIntegerValue(bitsBeforeBP/*, isSigned: false*/);
		//		throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx. IndexOfMsb: {GetDiagDisplayHex("limbs", partialWordLimbs)}. Info: {bitExpInfo}.");
		//	}

		//	//var rValueStrVal = RValueHelper.ConvertToString(rValue);

		//	var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
		//	var newPartialWordLimbs = ShiftBitsOld(partialWordLimbs, shiftAmount, limbCount, "CreateSmx");

		//	var partialWordLimbsTopCleared = ClearHighHalves(newPartialWordLimbs, null);
		//	var result = new Smx(sign, partialWordLimbsTopCleared, targetExponent, bitsBeforeBP, rValue.Precision);

		//	//var resultStrVal = result.GetStringValue();
		//	//Debug.WriteLine($"Got Smx: {resultStrVal} from: {rValueStrVal}. {bitExpInfo}");

		//	//if (rValueStrVal.Length - resultStrVal.Length > 5)
		//	//{
		//	//	//Debug.WriteLine("CreateFP31Val failed.");
		//	//	throw new InvalidOperationException("CreateFP31Val failed.");
		//	//}

		//	return result;
		//}


		//#endregion

		//#region Shift Bits / Scale and Split

		//private static ulong[] ShiftBitsOld(ulong[] partialWordLimbs, int shiftAmount, int limbCount, string desc)
		//{
		//	ulong[] result;

		//	if (shiftAmount == 0)
		//	{
		//		result = TakeMostSignificantLimbsOld(partialWordLimbs, limbCount);
		//	}
		//	else if (shiftAmount < 0)
		//	{
		//		throw new NotImplementedException();
		//	}
		//	else
		//	{
		//		var sResult = ScaleAndSplitOld(partialWordLimbs, shiftAmount, limbCount, "Create Smx");

		//		result = TakeMostSignificantLimbsOld(sResult, limbCount);
		//	}

		//	// ExtendSignBits, into the Reserved bit, clear the top half
		//	result = ExtendSignBitOld(result);

		//	return result;
		//}

		//private static ulong[] ScaleAndSplitOld(ulong[] mantissa, int power, int limbCount, string desc)
		//{
		//	if (power <= 0)
		//	{
		//		throw new ArgumentException("The value of power must be 1 or greater.");
		//	}

		//	(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

		//	if (limbOffset > limbCount + 3)
		//	{
		//		return new ulong[] { 0 };
		//	}

		//	var factor = (ulong)Math.Pow(2, remainder);

		//	var resultArray = new ulong[mantissa.Length];

		//	var carry = 0ul;

		//	var indexOfLastNonZeroLimb = -1;
		//	for (var i = 0; i < mantissa.Length; i++)
		//	{
		//		var newLimbVal = mantissa[i] * factor + carry;

		//		var (hi, lo) = SplitOld(newLimbVal); // :Spliter
		//		resultArray[i] = lo;

		//		carry = hi;

		//		if (lo > 0)
		//		{
		//			indexOfLastNonZeroLimb = i;
		//		}
		//	}

		//	if (indexOfLastNonZeroLimb > -1)
		//	{
		//		indexOfLastNonZeroLimb += limbOffset;
		//	}

		//	var resultSa = new ShiftedArray<ulong>(resultArray, limbOffset, indexOfLastNonZeroLimb);

		//	if (carry > 0)
		//	{
		//		//Debug.WriteLine($"While {desc}, setting carry: {carry}, ll: {result.IndexOfLastNonZeroLimb}, len: {result.Length}, power: {power}, factor: {factor}.");
		//		resultSa.SetCarry(carry);
		//	}

		//	var result = resultSa.MaterializeAll();

		//	return result;
		//}

		//private static ulong[] TakeMostSignificantLimbsOld(ulong[] partialWordLimbs, int length)
		//{
		//	ulong[] result;

		//	if (partialWordLimbs.Length == length)
		//	{
		//		result = partialWordLimbs;
		//	}
		//	else if (partialWordLimbs.Length > length)
		//	{
		//		result = CopyLastXElementsOld(partialWordLimbs, length);
		//	}
		//	else
		//	{
		//		result = ExtendOld(partialWordLimbs, length);
		//	}

		//	return result;
		//}

		//private static ulong[] CopyLastXElementsOld(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];

		//	var startIndex = Math.Max(values.Length - newLength, 0);

		//	var cLen = values.Length - startIndex;

		//	Array.Copy(values, startIndex, result, 0, cLen);

		//	return result;
		//}

		//// Pad with leading zeros.
		//private static ulong[] ExtendOld(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];
		//	Array.Copy(values, 0, result, 0, values.Length);

		//	return result;
		//}

		////private static int GetShiftAmount(int currentExponent, int targetExponent)
		////{
		////	var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
		////	return shiftAmount;
		////}

		//private static (ulong hi, ulong lo) SplitOld(ulong x)
		//{
		//	// bit 31 (and 63) is being reserved to detect carries when adding / subtracting.
		//	// this bit should be zero at this point.

		//	// The low value is in the low 31 bits, indexes 0 - 30.
		//	// The high value is in bits 32-62

		//	var hi = x >> EFFECTIVE_BITS_PER_LIMB; // Create new ulong from bits 32 - 62.
		//	var lo = x & HIGH33_MASK; // Create new ulong from bits 0 - 31.

		//	return (hi, lo);
		//}

		//// Clears Bits 33 bits (From 31 to 63)
		//// Used when converting back to standard binary representation, i.e., when creating an Smx var.
		//public static ulong[] ClearHighHalves(ulong[] partialWordLimbs, bool? sign = null)
		//{
		//	var result = new ulong[partialWordLimbs.Length];

		//	for (var i = 0; i < result.Length; i++)
		//	{
		//		result[i] = partialWordLimbs[i] & HIGH33_CLEAR_L;
		//	}

		//	return result;
		//}

		#endregion


		#region Convert to Partial-Word Limbs -- COPIED FROM ScalarMathHelper

		// TOOD: Consider first using ToLongs and then calling Split(

		//private static ulong[] ToPwULongs(BigInteger bi, out bool sign)
		//{
		//	var tResult = new List<ulong>();

		//	//var hi = BigInteger.Abs(bi);

		//	sign = bi.Sign >= 0;
		//	var hi = sign ? bi : BigInteger.Negate(bi);

		//	while (hi > MAX_DIGIT_VALUE)
		//	{
		//		hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
		//		tResult.Add((ulong)lo);
		//	}

		//	tResult.Add((ulong)hi);

		//	return tResult.ToArray();
		//}

		//private static BigInteger FromPwULongs(ulong[] partialWordLimbs, bool sign)
		//{
		//	var result = BigInteger.Zero;

		//	for (var i = partialWordLimbs.Length - 1; i >= 0; i--)
		//	{
		//		result *= BI_HALF_WORD_FACTOR;
		//		result += partialWordLimbs[i];
		//	}

		//	result = sign ? result : BigInteger.Negate(result);

		//	return result;
		//}

		#endregion


		#region Shift Bits / Scale and Split

		//private static uint[] ShiftBits(uint[] partialWordLimbs, int shiftAmount, int limbCount, string desc)
		//{
		//	uint[] result;

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

		//	return result;
		//}

		//private static uint[] ScaleAndSplit(uint[] mantissa, int power, int limbCount, string desc)
		//{
		//	if (power <= 0)
		//	{
		//		throw new ArgumentException("The value of power must be 1 or greater.");
		//	}

		//	(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

		//	if (limbOffset > limbCount + 3)
		//	{
		//		return Enumerable.Repeat(0u, limbCount).ToArray();
		//	}

		//	var factor = (ulong)Math.Pow(2, remainder);

		//	var resultArray = new uint[mantissa.Length];

		//	var carry = 0u;

		//	for (var i = 0; i < mantissa.Length; i++)
		//	{
		//		var newLimbVal = (mantissa[i] * factor) + carry;

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

		//private static uint[] AssembleScaledValue(uint[] resultArray, int offset, uint carry, int limbCount)
		//{
		//	uint[] wArray;

		//	if (carry > 0)
		//	{
		//		if (offset > 0) offset -= 1;

		//		var len = resultArray.Length + 1 + offset;
		//		wArray = new uint[len];

		//		Array.Copy(resultArray, 0, wArray, offset, resultArray.Length);
		//		wArray[^1] = carry;
		//	}
		//	else
		//	{
		//		var len = resultArray.Length + offset;
		//		wArray = new uint[len];

		//		//Array.Copy(resultArray, 0, wArray, offset, resultArray.Length);
		//		Array.Copy(resultArray, 0, wArray, 0, resultArray.Length);
		//	}

		//	var result = TakeMostSignificantLimbs(wArray, limbCount);

		//	return result;
		//}

		//private static uint[] TakeMostSignificantLimbs(uint[] partialWordLimbs, int length)
		//{
		//	uint[] result;

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
		//private static uint[] PadLeft(uint[] values, int amount)
		//{
		//	var newLength = values.Length + amount;
		//	var result = new uint[newLength];
		//	Array.Copy(values, 0, result, amount, values.Length);

		//	return result;
		//}

		//private static uint[] TrimLeft(uint[] values, int amount)
		//{
		//	var newLength = values.Length - amount;
		//	var result = new uint[newLength];
		//	Array.Copy(values, amount, result, 0, newLength);

		//	return result;
		//}

		//private static uint[] CopyLastXElements(uint[] values, int newLength)
		//{
		//	var result = new uint[newLength];

		//	var sourceStartIndex = Math.Max(values.Length - newLength, 0);

		//	var cLen = values.Length - sourceStartIndex;

		//	var destinationStartIndex = newLength - cLen;

		//	Array.Copy(values, sourceStartIndex, result, destinationStartIndex, cLen);

		//	return result;
		//}

		private static int GetShiftAmount(int currentExponent, int targetExponent)
		{
			//var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
			var shiftAmount = -1 * (targetExponent - currentExponent);

			return shiftAmount;
		}

		//public static (uint hi, uint lo) Split(ulong x)
		//{
		//	// bit 31 is being reserved to detect carries when adding / subtracting.
		//	// this bit should be zero at this point.

		//	// The low value is in the low 31 bits, indexes 0 - 30.
		//	// The high value is in bits 32-63

		//	var hi = (uint)(x >> 32); // Create new ulong from bits 32 - 63.
		//	var lo = (uint)x & CLEAR_RESERVED_BIT;  // Create new ulong from bits 0 - 31.

		//	return (hi, lo);
		//}

		#endregion

		#region FP31Deck Support

		public static bool GetSign(uint[] limbs)
		{
			var result = (limbs[^1] & TEST_BIT_30) == 0;
			return result;
		}

		//public static void ExpandTo(FP31Deck source, FP31DeckPW result)
		//{
		//	for (var i = 0; i < source.Mantissas.Length; i++)
		//	{
		//		var signExtendedLimbs = ExtendSignBit(source.Mantissas[i]);

		//		Array.Copy(signExtendedLimbs, result.Mantissas[i], signExtendedLimbs.Length);
		//	}
		//}

		//public static void PackTo(FP31DeckPW source, FP31Deck result)
		//{
		//	for (var i = 0; i < source.Mantissas.Length; i++)
		//	{
		//		var lows = TakeLowerHalves(source.Mantissas[i]);

		//		Array.Copy(lows, result.Mantissas[i], lows.Length);
		//	}
		//}

		public static uint[] TakeLowerHalves(ulong[] partialWordLimbs)
		{
			var result = new uint[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = (uint)partialWordLimbs[i];
			}

			return result;
		}

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

		//public static ulong[] ExtendSignBitOld(ulong[] partialWordLimbs, bool includeTopHalves = false)
		//{
		//	var result = new ulong[partialWordLimbs.Length];

		//	//Array.Copy(partialWordLimbs, result, result.Length);

		//	//if (includeHighHalf)
		//	//{
		//	//	result[^1] = (result[^1] & TEST_BIT_30) > 0
		//	//				? result[^1] | HIGH33_FILL
		//	//				: result[^1] & HIGH33_CLEAR;
		//	//}
		//	//else
		//	//{
		//	//	result[^1] = (result[^1] & TEST_BIT_30) > 0
		//	//				? result[^1] | TEST_BIT_31
		//	//				: result[^1] & ~TEST_BIT_31;

		//	//}

		//	if (includeTopHalves)
		//	{
		//		for (var i = 0; i < partialWordLimbs.Length; i++)
		//		{
		//			result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
		//						? partialWordLimbs[i] | HIGH33_FILL_L
		//						: partialWordLimbs[i] & HIGH33_CLEAR_L;
		//		}
		//	}
		//	else
		//	{
		//		for (var i = 0; i < partialWordLimbs.Length; i++)
		//		{
		//			result[i] = (partialWordLimbs[i] & TEST_BIT_30) > 0
		//						? partialWordLimbs[i] | TEST_BIT_31
		//						: partialWordLimbs[i] & ~TEST_BIT_31;

		//			result[i] &= HIGH32_CLEAR;
		//		}
		//	}

		//	return result;
		//}

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

		// Flip all bits and add 1, update the sign to be !sign
		public static FP31Val Negate(FP31Val fp31Val)
		{
			//if (!CheckReserveBit(smx2C.Mantissa))
			//{
			//	throw new InvalidOperationException($"Cannot Negate a Smx2C value, unless the reserve bit agrees with the sign bit. {GetDiagDisplayHex("input", smx2C.Mantissa)}.");
			//}

			var currentSign = GetSign(fp31Val.Mantissa);
			var negatedPartialWordLimbs = FlipBitsAndAdd1(fp31Val.Mantissa);
			var sign = GetSign(negatedPartialWordLimbs);

			if (sign != currentSign)
			{
				Debug.WriteLineIf(USE_DET_DEBUG, $"Negate an FP31Val var did not change the sign. Prev: {GetDiagDisplay("Prev", fp31Val.Mantissa)}, {GetDiagDisplay("New", negatedPartialWordLimbs)}");
			}

			//var withReserveBitsUpdated = ExtendSignBit(negatedPartialWordLimbs);

			var result = new FP31Val(negatedPartialWordLimbs, fp31Val.Exponent, fp31Val.BitsBeforeBP, fp31Val.Precision);

			//Debug.Assert(GetSign(smx2C.Mantissa) == !sign, "Negate an Smx2C var did not change the sign.");

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
					//var someFlipped = FlipLowBitsAfterFirst1(ourVal);
					//result[limbPtr] = someFlipped & LOW31_BITS_SET;

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
			//var numToFlip = 32 - numToKeep;

			//var flipMask = ~limb;								// flips all bits
			//flipMask = (flipMask >> numToKeep) << numToKeep;    // set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			//var target = (limb << numToFlip) >> numToFlip;      // set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			//var newVal = target | flipMask;


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

		private static ulong UpdateTheReservedBit(ulong limb, bool newValue)
		{
			var result = newValue ? limb | TEST_BIT_31 : limb & ~TEST_BIT_31;
			return result;
		}

		#endregion

		#region Convert to Full-31Bit-Word Limbs

		public static uint[] ToFwUInts(BigInteger bi, out bool sign)
		{
			var tResult = new List<uint>();

			sign = bi.Sign >= 0;
			var hi = sign? bi : BigInteger.Negate(bi);

			while (hi > MAX_DIGIT_VALUE)
			{
				hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
				tResult.Add((uint)lo);
			}

			tResult.Add((uint)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromFwUInts(uint[] fullWordLimbs, bool sign)
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
