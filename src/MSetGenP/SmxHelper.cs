using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	public static class SmxHelper
	{
		private const int BITS_PER_LIMB = 32;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);
		//private static readonly ulong HALF_DIGIT_VALUE = (ulong)Math.Pow(2, 16);

		// Integer used to convert BigIntegers to/from array of ulongs.
		private static readonly BigInteger BI_ULONG_FACTOR = BigInteger.Pow(2, 64);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-words
		private static readonly BigInteger BI_UINT_FACTOR = BigInteger.Pow(2, 32);

		// Integer used to pack a pair of ulong values into a single ulong.
		//private static readonly ulong UL_UINT_FACTOR = (ulong)Math.Pow(2, 32);

		private static readonly ulong LOW_MASK = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		//private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		#region Construction Support

		public static ApFixedPointFormat GetAdjustedFixedPointFormat(ApFixedPointFormat fpFormat)
		{
			if (fpFormat.BitsBeforeBinaryPoint > 32)
			{
				throw new NotSupportedException("An APFixedFormat with a BitsBeforeBinaryPoint of 32 is not supported.");
			}

			var range = fpFormat.TotalBits;

			// Add one additional Limb to provide room to represent smaller values with some precision.
			//range += BITS_PER_LIMB;

			var dResult = range / (double)BITS_PER_LIMB;
			var limbCount = (int)Math.Ceiling(dResult);

			// Make sure the resulting FixPointFormat uses an integral number of limbs.
			var adjustedFractionalBits = limbCount * BITS_PER_LIMB - fpFormat.BitsBeforeBinaryPoint;

			var result = new ApFixedPointFormat(fpFormat.BitsBeforeBinaryPoint, adjustedFractionalBits);

			return result;
		}

		public static int GetLimbCount(int numberOfBits)
		{
			var dResult = numberOfBits / (double)BITS_PER_LIMB;
			var result = (int)Math.Ceiling(dResult);

			return result;
		}

		public static ulong GetThreshold(uint threshold, int targetExponent, int limbCount, int bitsBeforeBP)
		{
			var maxIntegerValue = (uint)Math.Pow(2, bitsBeforeBP) - 1;


			if (threshold > maxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat.");
			}

			var thresholdSmx = CreateSmx(new RValue(threshold, 0), targetExponent, limbCount, bitsBeforeBP);
			//var ss = thresholdSmx.GetStringValue();

			var result = thresholdSmx.Mantissa[^1] - 1; // Subtract 1 * 2^-24

			return result;
		}

		public static RValue GetRValue(Smx smx)
		{
			var biValue = FromPwULongs(smx.Mantissa);
			biValue = smx.Sign ? biValue : -1 * biValue;
			var exponent = smx.Exponent;
			var precision = smx.Precision;

			var result = new RValue(biValue, exponent, precision);

			return result;
		}



		public static Smx CreateSmx(RValue rValue, int targetExponent, int limbCount, int bitsBeforeBP)
		{
			var partialWordLimbs = ToPwULongs(rValue.Value);

			var magnitude = GetMagnitudeOfIntegerPart(partialWordLimbs, rValue.Exponent);
			if (magnitude > bitsBeforeBP)
			{
				// Magnitude is the exponent of the most significant bit within the first BitsBeforeBP at the top of the most significant limb.
				var maxIntegerValue = (uint)Math.Pow(2, bitsBeforeBP) - 1;

				throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx.");
			}

			var sign = rValue.Value >= 0;

			var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
			var newPartialWordLimbs = ShiftBits(partialWordLimbs, shiftAmount, limbCount);

			var exponent = targetExponent;
			var precision = rValue.Precision;
			var result = new Smx(sign, newPartialWordLimbs, exponent, precision, bitsBeforeBP);

			return result;
		}

		private static ulong[] ShiftBits(ulong[] partialWordLimbs, int shiftAmount, int limbCount)
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

			return result;
		}

		private static ulong[] ScaleAndSplit(ulong[] mantissa, int power, int limbCount, string desc)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			(var limbOffset, var remainder) = Math.DivRem(power, BITS_PER_LIMB);

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
				var lo = Split(newLimbVal, out carry); // :Spliter
				resultArray[i] = lo;

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

		private static uint GetTopBits(ulong partialWordValue, int numberOfBits)
		{
			if (numberOfBits < 0 || numberOfBits > 32)
			{
				throw new ArgumentException($"The number of bits must be between 0 and 32.");
			}

			var v1 = partialWordValue >> (32 - numberOfBits);
			return (uint)v1;
		}

		//private uint GetBottomBits(ulong partialWordValue, int numberOfBits)
		//{
		//	if (numberOfBits < 0 || numberOfBits > 32)
		//	{
		//		throw new ArgumentException($"The number of bits must be between 0 and 32.");
		//	}

		//	var v1 = partialWordValue << (64 - numberOfBits);
		//	var v2 = v1 >> (64 - numberOfBits);
		//	return (uint) v2;
		//}


		//private ulong ShiftTopBitsDown(ulong partialWordValue, int shiftAmount)
		//{
		//	if (shiftAmount < 0 || shiftAmount > 32)
		//	{
		//		throw new ArgumentException($"The number of bits must be between 0 and 32.");
		//	}

		//	var v1 = partialWordValue >> shiftAmount;
		//	return (uint)v1;
		//}

		//private ulong ShiftBottomBitsUp(ulong partialWordValue, int shiftAmount)
		//{
		//	if (shiftAmount < 0 || shiftAmount > 32)
		//	{
		//		throw new ArgumentException($"The number of bits must be between 0 and 32.");
		//	}

		//	var v1 = partialWordValue << (32 + shiftAmount);
		//	var v2 = v1 >> 32;
		//	return (uint)v2;
		//}

		private static byte GetMagnitudeOfIntegerPart(ulong[] partialWordLimbs, int exponent)
		{
			var bitsBeforeBP = GetNumberOfBitsBeforeBP(partialWordLimbs.Length, exponent);

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

			var lzc = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);

			Debug.Assert(lzc >= 32 && lzc <= 64, "The MSL has a value larger than the max digit.");

			//var logicalLzc = lzc - 32; // (0, if lzc == 32, 32, if lzc == 64

			var indexOfMsb = 64 - lzc;  // 0, if lzc = 64, 32 if lzc = 32

			var indexOfBP = 32 - bitsBeforeBP;

			var sizeInBitsOfIntegerVal = Math.Max(indexOfMsb - indexOfBP, 0);

			//var diagVal = GetTopBits(partialWordLimbs[^1], bitsBeforeBP);

			//Debug.Assert(diagVal <= Math.Pow(2, sizeInBitsOfIntegerVal));

			return (byte)sizeInBitsOfIntegerVal;
		}

		//private uint GetIntegerPart(ulong[] partialWordLimbs, ApFixedPointFormat fpFormat)
		//{
		//	if (fpFormat.BitsBeforeBinaryPoint <= 0)
		//	{
		//		return 0;
		//	}

		//	if (fpFormat.BitsBeforeBinaryPoint > 32)
		//	{
		//		throw new OverflowException("The integer portion is larger than uint max.");
		//	}

		//	var topBits = GetTopBits(partialWordLimbs[^1], fpFormat.BitsBeforeBinaryPoint);

		//	return topBits;

		//	//var msl = partialWordLimbs[^1];
		//	//var digitFactor = exponent + BITS_PER_LIMB * (partialWordLimbs.Length - 1);

		//	//Debug.Assert(digitFactor <= 0, "digitFactor is > 0.");
		//	//Debug.Assert(digitFactor >= -32, "digitFactor < -32.");

		//	//var digitWeight = Math.Pow(2, digitFactor);
		//	//var result = (uint)Math.Round(msl * digitWeight);

		//	//return result;
		//}

		private static int GetNumberOfBitsBeforeBP(int limbCount, int exponent)
		{
			var totalBits = BITS_PER_LIMB * limbCount;

			(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

			var fractionalBits = limbIndex * BITS_PER_LIMB + bitOffset;
			var bitsBeforeBP = totalBits - fractionalBits;

			return bitsBeforeBP;
		}

		//private ApFixedPointFormat GetCurrentFpFormat(int limbCount, int exponent)
		//{
		//	var totalBits = BITS_PER_LIMB * limbCount;

		//	(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

		//	var fractionalBits = limbIndex * BITS_PER_LIMB + bitOffset;
		//	var bitsBeforeBP = totalBits - fractionalBits;

		//	var result = new ApFixedPointFormat(bitsBeforeBP, fractionalBits);
		//	return result;
		//}

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
				(limbIndex, bitOffset) = Math.DivRem(exponent, BITS_PER_LIMB);
				if (bitOffset > 0)
				{
					limbIndex--;
					bitOffset = BITS_PER_LIMB - bitOffset;
				}
			}
			else
			{
				(limbIndex, bitOffset) = Math.DivRem(-1 * exponent, BITS_PER_LIMB);
			}

			return (limbIndex, bitOffset);
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

		// Pad with leading zeros.
		private static ulong[] Extend(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, values.Length);

			return result;
		}

		//private ulong[] CopyFirstXElements(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];
		//	Array.Copy(values, 0, result, 0, newLength);

		//	return result;
		//}

		private static ulong[] CopyLastXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];

			var startIndex = Math.Max(values.Length - newLength, 0);

			var cLen = values.Length - startIndex;

			Array.Copy(values, startIndex, result, 0, cLen);

			return result;
		}

		//private Smx TrimLeadingZeros(Smx a)
		//{
		//	var mantissa = TrimLeadingZeros(a.Mantissa);
		//	var result = new Smx(a.Sign, mantissa, a.Exponent, a.Precision, a.BitsBeforeBP);
		//	return result;
		//}

		//// Remove zero-valued limbs from the Most Significant end.
		//// Leaving all least significant limbs intact.
		//public ulong[] TrimLeadingZeros(ulong[] mantissa)
		//{
		//	if (mantissa.Length == 1 && mantissa[0] == 0)
		//	{
		//		return mantissa;
		//	}

		//	var i = mantissa.Length;
		//	for (; i > 0; i--)
		//	{
		//		if (mantissa[i - 1] != 0)
		//		{
		//			break;
		//		}
		//	}

		//	if (i == mantissa.Length)
		//	{
		//		return mantissa;
		//	}

		//	if (i == 0)
		//	{
		//		// All digits are zero
		//		return new ulong[] { 0 };
		//	}

		//	var result = new ulong[i];
		//	Array.Copy(mantissa, 0, result, 0, i);
		//	return result;
		//}

		//// Remove zero-valued limbs from the Least Significant end.
		//// Leaving all most significant limbs intact.
		//// The out parameter is set to the new exponent
		//public static ulong[] TrimTrailingZeros(ulong[] mantissa, int exponent, out int newExponent)
		//{
		//	newExponent = exponent;

		//	if (mantissa.Length == 0 || mantissa[0] != 0)
		//	{
		//		return mantissa;
		//	}

		//	var i = 1;
		//	for (; i < mantissa.Length; i++)
		//	{
		//		if (mantissa[i] != 0)
		//		{
		//			break;
		//		}
		//	}

		//	if (i == mantissa.Length)
		//	{
		//		// All digits are zero
		//		newExponent = 1;
		//		return new ulong[] { 0 };
		//	}

		//	var result = new ulong[mantissa.Length - i];
		//	Array.Copy(mantissa, i, result, 0, result.Length);

		//	newExponent += i * 32;

		//	return result;
		//}

		//private int GetNumberOfNonZeroBitsAfterBP_NotUsed(ulong[] mantissa)
		//{
		//	var indexOfLastNonZeroLimb = GetIndexOfLastNonZeroLimb(mantissa);

		//	if (indexOfLastNonZeroLimb == -1)
		//	{
		//		return 0;
		//	}

		//	if (indexOfLastNonZeroLimb == mantissa.Length - 1)
		//	{
		//		var m = (mantissa[^1] << 32 + BitsBeforeBP) >> 32;
		//		var lzc = BitOperations.LeadingZeroCount(m) - BITS_PER_LIMB;

		//		var bitsAfterBp = 32 - lzc;

		//		return bitsAfterBp;
		//	}

		//	var result = ((indexOfLastNonZeroLimb + 1) * BITS_PER_LIMB) - BitsBeforeBP;
		//	return result;
		//}

		//private int GetNumberOfBitsAfterBP(Smx a)
		//{
		//	var result = a.LimbCount * BITS_PER_LIMB - a.BitsBeforeBP;
		//	return result;
		//}

		//private int GetNumberOfLeadingZeroLimbs(Smx a)
		//{
		//	var result = a.Mantissa.Length - GetLogicalLength(a);
		//	return result;
		//}

		//private int GetLogicalLength(Smx a)
		//{
		//	var result = 1 + GetIndexOfLastNonZeroLimb(a.Mantissa);
		//	return result;
		//}

		//private int GetLogicalLength(ulong[] mantissa)
		//{
		//	var result = 1 + GetIndexOfLastNonZeroLimb(mantissa);
		//	return result;
		//}

		//private int GetIndexOfLastNonZeroLimb(ulong[] mantissa)
		//{
		//	for (var i = mantissa.Length - 1; i >= 0; i--)
		//	{
		//		if (mantissa[i] != 0)
		//		{
		//			break;
		//		}
		//	}

		//	return -1;
		//}

		public static int GetShiftAmount(int currentExponent, int targetExponent)
		{
			var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
			return shiftAmount;
		}

		public static bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x >= MAX_DIGIT_VALUE);
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

		#region Split and Pack 

		//public static ulong[] Pack(ulong[] splitValues)
		//{
		//	Debug.Assert(splitValues.Length % 2 == 0, "The array being split has a length that is not an even multiple of two.");

		//	var result = new ulong[splitValues.Length / 2];

		//	for (int i = 0; i < splitValues.Length; i += 2)
		//	{
		//		result[i / 2] = splitValues[i] + UL_UINT_FACTOR * splitValues[i + 1];
		//	}

		//	return result;
		//}

		//// Values are ordered from least significant to most significant.
		//public static ulong[] Split(ulong[] packedValues)
		//{
		//	var result = new ulong[packedValues.Length * 2];

		//	for (int i = 0; i < packedValues.Length; i++)
		//	{
		//		var lo = Split(packedValues[i], out var hi);
		//		result[2 * i] = lo;
		//		result[2 * i + 1] = hi;
		//	}

		//	return result;
		//}

		public static ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		//public static ulong[] ReSplit(ulong[] splitValues, out ulong carry)
		//{
		//	var result = new ulong[splitValues.Length];
		//	carry = 0ul;

		//	for (int i = 0; i < splitValues.Length; i++)
		//	{
		//		var lo = Split(splitValues[i] + carry, out var hi);  // :Spliter
		//		result[i] = lo;
		//		carry = hi;
		//	}

		//	return result;
		//}

		#endregion

		#region To ULong Support

		//public ulong[] ToULongs(BigInteger bi)
		//{
		//	var tResult = new List<ulong>();
		//	var hi = BigInteger.Abs(bi);

		//	while (hi > ulong.MaxValue)
		//	{
		//		hi = BigInteger.DivRem(hi, BI_ULONG_FACTOR, out var lo);
		//		tResult.Add((ulong)lo);
		//	}

		//	tResult.Add((ulong)hi);

		//	return tResult.ToArray();
		//}

		//public BigInteger FromULongs(ulong[] values)
		//{
		//	var result = BigInteger.Zero;

		//	for (int i = values.Length - 1; i >= 0; i--)
		//	{
		//		result *= BI_ULONG_FACTOR;
		//		result += values[i];
		//	}

		//	return result;
		//}

		#endregion

		#region Convert to Partial-Word ULongs

		public static ulong[] ToPwULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > uint.MaxValue)
			{
				hi = BigInteger.DivRem(hi, BI_UINT_FACTOR, out var lo);
				tResult.Add((ulong)lo);
			}

			tResult.Add((ulong)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromPwULongs(ulong[] values)
		{
			var result = BigInteger.Zero;

			for (var i = values.Length - 1; i >= 0; i--)
			{
				result *= BI_UINT_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion
	}
}
