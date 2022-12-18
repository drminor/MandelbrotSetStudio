using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public static class SmxHelper
	{
		#region Private Members

		private const int BITS_PER_LIMB = 32;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);

		// Integer used to convert BigIntegers to/from array of ulongs containing full-word values
		private static readonly BigInteger BI_ULONG_FACTOR = BigInteger.Pow(2, 64);

		// Integer used to split full-word values into partial-word values.
		private static readonly ulong UL_UINT_FACTOR = (ulong) Math.Pow(2, 64);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_UINT_FACTOR = BigInteger.Pow(2, 32);

		private const ulong LOW_BITS_SET =	0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong ALL_BITS_SET =	0xFFFFFFFFFFFFFFFF; // bits 0 - 64 are set.

		private const ulong HIGH_MASK = LOW_BITS_SET;

		#endregion

		#region Construction Support

		public static ApFixedPointFormat GetAdjustedFixedPointFormat(ApFixedPointFormat fpFormat)
		{
			if (fpFormat.BitsBeforeBinaryPoint > 32)
			{
				throw new NotSupportedException("An APFixedFormat with a BitsBeforeBinaryPoint of 32 is not supported.");
			}

			var range = fpFormat.TotalBits;
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

		public static int GetMaxSignedIntegerValue(byte bitsBeforeBP)
		{
			var result = (int)Math.Pow(2, bitsBeforeBP - 1) - 1; // 2^7 - 1 = 127
			return result;
		}

		public static uint GetMaxIntegerValue(byte bitsBeforeBP)
		{
			var result = (uint)Math.Pow(2, bitsBeforeBP) - 1; // 2^8 - 1 = 255
			return result;
		}


		public static ulong GetThresholdMsl(uint threshold, int targetExponent, int limbCount, byte bitsBeforeBP)
		{
			var maxIntegerValue = (uint)Math.Pow(2, bitsBeforeBP) - 1;
			if (threshold > maxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat.");
			}

			var thresholdSmx = CreateSmx(new RValue(threshold, 0), targetExponent, limbCount, bitsBeforeBP);
			var result = thresholdSmx.Mantissa[^1] - 1;

			return result;
		}

		#endregion

		#region Smx and RValue Support

		public static Smx CreateSmx(RValue rValue, int targetExponent, int limbCount, byte bitsBeforeBP)
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
			var result = new Smx(sign, newPartialWordLimbs, exponent, bitsBeforeBP, precision);

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

			var v1 = partialWordValue >> (32 - numberOfBits);
			return (uint)v1;
		}


		private static int GetNumberOfBitsBeforeBP(int limbCount, int exponent)
		{
			var totalBits = BITS_PER_LIMB * limbCount;

			(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

			var fractionalBits = limbIndex * BITS_PER_LIMB + bitOffset;
			var bitsBeforeBP = totalBits - fractionalBits;

			return bitsBeforeBP;
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

		private static ulong[] CopyFirstXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, newLength);

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

		public static int GetShiftAmount(int currentExponent, int targetExponent)
		{
			var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
			return shiftAmount;
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

		#endregion

		#region 2C Support

		public static ulong[] ConvertTo2C(ulong[] partialWordLimbs, bool sign)
		{
			if (sign)
			{
				// Postive values have the same representation in both two's compliment and standard form.
				return partialWordLimbs;
			}

			//	Start at the least significant bit (LSB), copy all the zeros, until the first 1 is reached;
			//	then copy that 1, and flip all the remaining bits.

			var resultLength = partialWordLimbs.Length;

			var result = new ulong[resultLength];
			var limbPtr = 0;

			while (limbPtr < resultLength && partialWordLimbs[limbPtr] == 0)
			{
				result[limbPtr] = partialWordLimbs[limbPtr];
				limbPtr++;
			}

			if (limbPtr < resultLength)
			{
				var tzc = BitOperations.TrailingZeroCount(partialWordLimbs[limbPtr]);
				var numToKeep = tzc + 1;
				var numToFlip = 64 - numToKeep;

				var flipped = partialWordLimbs[limbPtr] ^ ALL_BITS_SET;
				flipped = (flipped >> numToKeep) << numToKeep; // set the bottom bits to zero

				var target = partialWordLimbs[limbPtr];
				target = (target << numToFlip) >> numToFlip; // set the top bits to zero

				var newVal = target | flipped;
				result[limbPtr] = newVal;

				limbPtr++;
			}

			for (; limbPtr < resultLength; limbPtr++)
			{
				var flipped = partialWordLimbs[limbPtr] ^ ALL_BITS_SET;
				result[limbPtr] = flipped;
			}

			return result;
		}

		public static double ConvertFrom2C(ulong partialWordLimb)
		{
			var lzcMsl = BitOperations.LeadingZeroCount(partialWordLimb);
			var isNegative = lzcMsl == 0;

			double result;

			if (isNegative)
			{
				var resultLimbs = ConvertTo2C(new ulong[] { partialWordLimb }, false);
				result = resultLimbs[0];
			}
			else
			{
				result = partialWordLimb;
			}

			return isNegative ? result * -1 : result;
		}

		public static ulong[] ConvertFrom2C(ulong[] partialWordLimbs)
		{
			var lzcMsl = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);
			var isNegative = lzcMsl == 0;

			var result = ConvertFrom2C(partialWordLimbs, !isNegative);

			return result;
		}

		public static ulong[] ConvertFrom2C(ulong[] partialWordLimbs, bool sign)
		{
			ulong[] result;

			if (sign)
			{
				result = partialWordLimbs;
			}
			else
			{
				// Convert negative values back to 'standard' representation
				result = ConvertTo2C(partialWordLimbs, sign);
			}

			var clearedResults = ClearPW2CValues(result, null);

			return clearedResults;
		}

		public static Smx2C Negate(Smx2C smx2C)
		{
			var negatedPartialWordLimbs = ConvertTo2C(smx2C.Mantissa, sign: false);
			var result = new Smx2C(!smx2C.Sign, negatedPartialWordLimbs, smx2C.Exponent, smx2C.Precision, smx2C.BitsBeforeBP);

			return result;
		}

		//public static ulong[] Negate(ulong[] partialWordLimbs)
		//{
		//	// Force the conversion by indicating we have a negative value.
		//	var result = ConvertTo2C(partialWordLimbs, sign: false);
		//	return result;
		//}

		#endregion

		#region Split and Pack 

		public static ulong[] PackPartialWordLimbs(ulong[] partialWordLimbs)
		{
			Debug.Assert(partialWordLimbs.Length % 2 == 0, "The array being split has a length that is not an even multiple of two.");

			var result = new ulong[partialWordLimbs.Length / 2];

			for (int i = 0; i < partialWordLimbs.Length; i += 2)
			{
				result[i / 2] = partialWordLimbs[i] + UL_UINT_FACTOR * partialWordLimbs[i + 1];
			}

			return result;
		}

		// Values are ordered from least significant to most significant.
		public static ulong[] SplitFullWordLimbs(ulong[] packedValues)
		{
			var result = new ulong[packedValues.Length * 2];

			for (int i = 0; i < packedValues.Length; i++)
			{
				var lo = Split(packedValues[i], out var hi);
				result[2 * i] = lo;
				result[2 * i + 1] = hi;
			}

			return result;
		}

		public static ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & HIGH_MASK; // Create new ulong from bits 0 - 31.
		}

		public static bool CheckPWValues(ulong[] partialWordLimbs)
		{
			var result = partialWordLimbs.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
		}

		public static bool CheckPW2CValues(ulong[] partialWordLimbs)
		{
			for (var i = 0; i < partialWordLimbs.Length; i++)
			{
				var limb = partialWordLimbs[i] >> 32;
				if (!(limb == 0 || limb == LOW_BITS_SET)) return true;
			}

			return false;
		}

		public static bool CheckPW2CValues(ulong[] partialWordLimbs, bool sign)
		{
			var topHalf = partialWordLimbs[^1] >> 32;
			if (sign)
			{
				if (topHalf != 0)
				{
					return true;
				}
			}
			else
			{
				if (topHalf != LOW_BITS_SET)
				{
					return true;
				}
			}

			for(var i = 0; i < partialWordLimbs.Length - 1; i++)
			{
				topHalf = partialWordLimbs[i] >> 32;
				if (! (topHalf == 0 || topHalf == LOW_BITS_SET )) return true;
			}

			return false;
		}

		public static ulong[] ClearPW2CValues(ulong[] partialWordLimbs, bool? sign = null)
		{
			CheckPW2CValuesBeforeClear(partialWordLimbs, sign);

			var result = new ulong[partialWordLimbs.Length];

			Array.Copy(partialWordLimbs, result, partialWordLimbs.Length);

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = result[i] & HIGH_MASK;
			}

			return result;
		}

		[Conditional("DEBUG")]
		private static void CheckPW2CValuesBeforeClear(ulong[] partialWordLimbs, bool? sign = null)
		{
			bool checkFails;
			if (sign.HasValue)
			{
				checkFails = CheckPW2CValues(partialWordLimbs, sign.Value);
			}
			else
			{
				checkFails = CheckPW2CValues(partialWordLimbs);
			}

			if (checkFails)
			{
				throw new InvalidOperationException("One or more partial-word limbs has a non-zero value in the top half.");
			}
		}

		#endregion

		#region To ULong Support

			public static ulong[] ToULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > ulong.MaxValue)
			{
				hi = BigInteger.DivRem(hi, BI_ULONG_FACTOR, out var lo);
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
				result *= BI_ULONG_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region Convert to Partial-Word Limbs

		// TOOD: Consider first using ToLongs and then calling Split(

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

		public static BigInteger FromPwULongs(ulong[] partialWordLimbs)
		{
			var result = BigInteger.Zero;

			for (var i = partialWordLimbs.Length - 1; i >= 0; i--)
			{
				result *= BI_UINT_FACTOR;
				result += partialWordLimbs[i];
			}

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

		// TODO: Implement the GetgDiagDisplayHex method.
		public static string GetDiagDisplayHex(string name, ulong[] values)
		{
			var strAry = GetStrArrayHex(values);

			return $"{name}:{string.Join("; ", strAry)}";
		}


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

		public static string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
		}

		public static string[] GetStrArrayHex(ulong[] values)
		{
			var result = values.Select(x => string.Format("0x{0:X4}", x)).ToArray();
			return result;
		}

		#endregion

		#region Unused 

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
