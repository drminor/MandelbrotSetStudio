using MSS.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public static class ScalarMathHelper
	{
		#region Private Members

		private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong) (Math.Pow(2, EFFECTIVE_BITS_PER_LIMB) - 1);

		// Integer used to split full-word values into partial-word values.
		private static readonly ulong UL_HALF_WORD_FACTOR = (ulong) Math.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-word values
		private static readonly BigInteger BI_HALF_WORD_FACTOR = BigInteger.Pow(2, EFFECTIVE_BITS_PER_LIMB);

		private const ulong HIGH_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.
		private const ulong LOW_BITS_SET =	0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong ALL_BITS_SET =	0xFFFFFFFFFFFFFFFF; // bits 0 - 63 are set.

		private const ulong TEST_BIT_32 =	0x0000000100000000; // bit 32 is set.
		private const ulong TEST_BIT_31 =	0x0000000080000000; // bit 31 is set.
		private const ulong TEST_BIT_30 =   0x0000000040000000; // bit 30 is set.

		private const ulong HIGH_MASK_OLD = LOW_BITS_SET;
		private const ulong LOW_MASK_OLD = HIGH_BITS_SET;

		private const ulong HIGH_FILL = HIGH_BITS_SET;
		private const ulong HIGH_CLEAR = LOW_BITS_SET;

		private const ulong HIGH33_BITS_SET =	0xFFFFFFFF80000000; // bits 63 - 31 are set.
		private const ulong LOW31_BITS_SET =	0x000000007FFFFFFF;	// bits 0 - 30 are set.

		private const ulong HIGH33_MASK = LOW31_BITS_SET;
		private const ulong LOW31_MASK = HIGH33_BITS_SET;

		private const ulong HIGH33_FILL = HIGH33_BITS_SET;


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

		public static RValue GetRValue(Smx smx)
		{
			var result = GetRValue(smx.Sign, smx.Mantissa, smx.Exponent, smx.Precision);
			return result;
		}

		public static RValue GetRValue(Smx2C smx2C)
		{
			var sign = GetSign(smx2C.Mantissa);

			var negatedPartialWordLimbs = ConvertFrom2C(smx2C.Mantissa);
			var result = GetRValue(sign, negatedPartialWordLimbs, smx2C.Exponent, smx2C.Precision);

			return result;
		}

		public static RValue GetRValue(bool sign, ulong[] partialWordLimbs, int exponent, int precision)
		{
			if (CheckPWValues(partialWordLimbs))
			{
				throw new ArgumentException($"Cannot create an RValue from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			}

			var biValue = FromPwULongs(partialWordLimbs);
			biValue = sign ? biValue : -1 * biValue;
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
			var partialWordLimbs = ToPwULongs(rValue.Value);

			if (IsValueTooLarge(rValue, bitsBeforeBP, isSigned: false))
			{
				var maxIntegerValue = GetMaxIntegerValue(bitsBeforeBP, isSigned: false);
				throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx. IndexOfMsb: {GetDiagDisplay("limbs", partialWordLimbs)}.");
			}

			var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
			var newPartialWordLimbs = ShiftBits(partialWordLimbs, shiftAmount, limbCount);

			var sign = rValue.Value >= 0;
			var result = new Smx(sign, newPartialWordLimbs, targetExponent, bitsBeforeBP, rValue.Precision);

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
			var partialWordLimbs = ToPwULongs(rValue.Value);
			if (IsValueTooLarge(rValue, bitsBeforeBP, isSigned: true))
			{
				var maxIntegerValue = GetMaxIntegerValue(bitsBeforeBP, isSigned: true);
				throw new ArgumentException($"An RValue with integer portion > {maxIntegerValue} cannot be used to create an Smx. {GetDiagDisplay("limbs", partialWordLimbs)}.");
			}

			var shiftAmount = GetShiftAmount(rValue.Exponent, targetExponent);
			var newPartialWordLimbs = ShiftBits(partialWordLimbs, shiftAmount, limbCount);

			var sign = rValue.Value >= 0;
			var partialWordLimbs2C = ConvertTo2C(newPartialWordLimbs, sign);

			var cSign = GetSign(partialWordLimbs2C);

			Debug.Assert(cSign == sign, $"Signs don't match on CreateSmx2C from RValue. RValue has sign: {sign}, new Smx2C has sign: {cSign}.");

			var result = new Smx2C(cSign, partialWordLimbs2C, targetExponent, bitsBeforeBP, rValue.Precision);

			return result;
		}

		private static bool IsValueTooLarge(RValue rValue, byte bitsBeforeBP, bool isSigned)
		{
			var maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);
			var magnitude = rValue.Value.GetBitLength() + rValue.Exponent;

			var result = magnitude > maxMagnitude;

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
			// Whether we are reserving a bit from each limb for carry / borrow detection does not affect the range of values that 
			// can appear before the binary point.

			//// If the effective bits/limb is less that the actual bits/limb due to keeping a bit available to detect overflows (i.e. carry / borrow)
			////		then the range is halved. For example a range of values from 0 to 127, instead of the original 0 to 255.

			//var maxMagnitude = (byte)(bitsBeforeBP - (BITS_PER_LIMB - EFFECTIVE_BITS_PER_LIMB) - (isSigned ? 1 : 0));

			// If using signed values the range of positive (and negative) values is halved. (For example  0 to 127 instead of 0 to 255. (or -128 to 0)

			var maxMagnitude = (byte)(bitsBeforeBP - (isSigned ? 1 : 0));

			return maxMagnitude;
		}

		#endregion

		#region 2C Support - 31

		// Convert from two's compliment, use the sign bit of the mantissa
		public static ulong[] ConvertFrom2C(ulong[] partialWordLimbs)
		{
			var sign = GetSign(partialWordLimbs);
			var result = ConvertFrom2C(partialWordLimbs, sign);

			return result;
		}

		// Convert from two's compliment, using the specified sign
		private static ulong[] ConvertFrom2C(ulong[] partialWordLimbs, bool sign)
		{
			ulong[] result;

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

		// Flip all bits and add 1, update the sign to be !sign
		public static Smx2C Negate(Smx2C smx2C)
		{
			var negatedPartialWordLimbs = FlipBitsAndAdd1(smx2C.Mantissa);

			var sign = GetSign(negatedPartialWordLimbs);	
			var result = new Smx2C(sign, negatedPartialWordLimbs, smx2C.Exponent, smx2C.BitsBeforeBP, smx2C.Precision);

			Debug.Assert(GetSign(smx2C.Mantissa) == !sign, "Negate an Smx2C var did not change the sign.");

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
			var signBitIsSet = (partialWordLimb & TEST_BIT_30) > 0;
			var isNegative = signBitIsSet;

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

			return isNegative ? result * -1 : result;
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

			if (sign)
			{
				var signBitIsSet = !GetSign(partialWordLimbs);

				if (signBitIsSet)
				{
					throw new OverflowException($"Cannot Convert to 2C format, the msb is already set. {GetDiagDisplay("limbs", partialWordLimbs)}");
				}

				//Debug.WriteLine($"Converting a value to 2C format, {GetDiagDisplay("limbs", partialWordLimbs)}.");

				// Postive values have the same representation in both two's compliment and standard form.
				result = partialWordLimbs;
			}
			else
			{
				//Debug.WriteLine($"Converting and negating a value to 2C format, {GetDiagDisplay("limbs", partialWordLimbs)}.");

				result = FlipBitsAndAdd1(partialWordLimbs);

				var signBitIsSet1 = !GetSign(result);
				if (!signBitIsSet1)
				{
					var signBitWasSet = !GetSign(partialWordLimbs);
					throw new OverflowException($"Cannot ConvertAbsValTo2C, after the conversion the msb is NOT set to 1. {GetDiagDisplay("OrigVal", partialWordLimbs)}. {GetDiagDisplay("Result", result)}. SignBitBefore: {signBitWasSet}.");
				}
			}

			return result;
		}

		public static ulong[] FlipBitsAndAdd1(ulong[] partialWordLimbs)
		{
			//	Start at the least significant bit (LSB), copy all the zeros, until the first 1 is reached;
			//	then copy that 1, and flip all the remaining bits.

			var resultLength = partialWordLimbs.Length;

			var result = new ulong[resultLength];

			var foundASetBit = false;
			var limbPtr = 0;

			while (limbPtr < resultLength && !foundASetBit)
			{
				var ourVal = partialWordLimbs[limbPtr] & HIGH33_MASK;			// Set all high bits to zero
				if (ourVal == 0)
				{
					result[limbPtr] = ourVal;
				}
				else
				{
					var someFlipped = FlipLowBitsAfterFirst1(ourVal);
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
					var ourVal = partialWordLimbs[limbPtr] & HIGH33_MASK;         // Set all high bits to zero	
					var lowBitsFlipped = FlipAllLowBits(ourVal);
					result[limbPtr] = lowBitsFlipped;
				}

				// Sign extend the msl
				result[^1] = ExtendSignBit(result[^1]);
			}
			else
			{
				// All bits are zero
			}


			if (CheckPW2CValues(result))
			{
				if (!foundASetBit) Debug.WriteLine("All bits are zero, on call to FlipBitsAndAdd1");
				throw new ArgumentException($"FlipBitsAndAdd1 is returning an incorrect value. {GetDiagDisplayHex("Before", partialWordLimbs)}, {GetDiagDisplayHex("After", result)}");
			}

			return result;
		}

		private static ulong FlipLowBitsAfterFirst1(ulong limb)
		{
			limb &= HIGH33_MASK;

			var tzc = BitOperations.TrailingZeroCount(limb);

			Debug.Assert(tzc < 31, "Expecting Trailing Zero Count to be between 0 and 30, inclusive.");

			var numToKeep = tzc + 1;
			var numToFlip = 64 - numToKeep;

			var flipMask = limb ^ LOW31_BITS_SET;				// flips lower half, sets upper half to all ones
			flipMask = (flipMask >> numToKeep) << numToKeep;	// set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			var target = (limb << numToFlip) >> numToFlip;		// set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			var newVal = target | flipMask;

			return newVal;
		}

		private static ulong FlipAllLowBits(ulong limb)
		{
			limb &= HIGH33_MASK;
			var newVal = limb ^ LOW31_BITS_SET;

			return newVal;
		}

		private static void ValidateConversion2C(ulong[] partialWordLimbs, string valueName)
		{
			if (CheckPW2CValues(partialWordLimbs))
			{
				throw new ArgumentException($"The {valueName} partialWordLimbs have some high bits set.");
			}
		}

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
					var someFlipped = FlipLowBitsAfterFirst1(ourVal);
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
					var lowBitsFlipped = FlipAllLowBits(ourVal);
					result[limbPtr] = lowBitsFlipped;
				}

				// Sign extend the msl
				result[^1] = ExtendSignBit(result[^1]);
			}
			else
			{
				// All bits are zero
			}


			if (CheckPW2CValues(result))
			{
				if (!foundASetBit) Debug.WriteLine("All bits are zero, on call to FlipBitsAndAdd1");
				throw new ArgumentException($"FlipBitsAndAdd1 is returning an incorrect value. {GetDiagDisplayHex("Before", partialWordLimbs)}, {GetDiagDisplayHex("After", result)}");
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

			var flipMask = limb ^ LOW_BITS_SET;                 // flips lower half, sets upper half to all ones
			flipMask = (flipMask >> numToKeep) << numToKeep;    // set the bottom bits to zero -- by pushing them off the end, and then moving the top back to where it was
			var target = (limb << numToFlip) >> numToFlip;      // set the top bits to zero -- by pushing them off the top and then moving the bottom to where it was.
			var newVal = target | flipMask;

			return newVal;
		}

		private static ulong FlipAllLowBitsOld(ulong limb)
		{
			limb &= HIGH_MASK_OLD;
			var newVal = limb ^ LOW_BITS_SET;

			return newVal;
		}

		// Used for diagnostics
		public static double ConvertFrom2COld(ulong partialWordLimb)
		{
			var signBitIsSet = (partialWordLimb & TEST_BIT_31) > 0;
			var isNegative = signBitIsSet;

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

			return isNegative ? result * -1 : result;
		}

		#endregion

		#region GetResultWithCarry

		public static (ulong limbValue, ulong carry) GetResultWithCarrySigned(ulong partialWordLimb, bool isMsl)
		{
			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.

			bool carryFlag;

			var limbValue = GetLowHalfSigned(partialWordLimb, isMsl, out var topBitIsSet, out bool overflowBitIsSet);

			if (topBitIsSet)
			{
				//limbValue |= HIGH_FILL; // sign extend the result
				carryFlag = !overflowBitIsSet; // true if next higher bit is zero
			}
			else
			{
				carryFlag = overflowBitIsSet; // true if next higher bit is one
			}

			var result = (limbValue, carryFlag ? 1uL : 0uL);
			return result;
		}

		public static ulong GetLowHalfSigned(ulong partialWordLimb, bool isMsl, out bool topBitIsSet, out bool overflowBitIsSet)
		{
			var result = partialWordLimb & HIGH33_MASK;

			var lzc = BitOperations.LeadingZeroCount(result);

			topBitIsSet = lzc == 33; // The first bit of the limb is set.

			overflowBitIsSet = BitOperations.TrailingZeroCount(partialWordLimb >> 31) == 0; // The 'overflow' bit is set.


			// For diagnositics only

			var hi = partialWordLimb >> 32;

			if (topBitIsSet && isMsl)
			{
				if (hi == 0)
				{
					throw new InvalidOperationException("Limb was not sign extended. Expecting this negative limb to have bits 32 - 63 be set to 1.");
				}
			}
			else
			{
				if (hi > 0)
				{
					throw new InvalidOperationException("Limb was not sign extended. Expecting this postive limb to have bits 32 - 63 be set to 0");
				}
			}

			return result;
		}

		public static (ulong limbValue, ulong carry) GetResultWithCarry(ulong partialWordLimb)
		{
			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.

			var (hi, lo) = Split(partialWordLimb);

			return (lo, hi);
		}

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
				result[^1] = ExtendSignBit(result[^1]);
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
			var lZCounts2 = GetLZCounts(result);
			Debug.WriteLine($"S&T LZCounts2:");
			for (var lzcPtr = 0; lzcPtr < lZCounts2.Length; lzcPtr++)
			{
				Debug.WriteLine($"{lzcPtr}: {lZCounts2[lzcPtr]} {result[lzcPtr]}");
			}
		}

		#endregion

		#region Shift Bits / Scale and Split

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

		#region Split and Pack -- 31

		//public static ulong Split(ulong x, out ulong hi)
		//{
		//	hi = x >> BITS_PER_LIMB; // Create new ulong from bits 32 - 63.
		//	return x & HIGH_MASK_OLD; // Create new ulong from bits 0 - 31.
		//}

		public static (ulong hi, ulong lo) Split(ulong x)
		{
			// bit 31 (and 63) is being reserved to detect carries when adding / subtracting.
			// this bit should be zero at this point.

			// The low value is in the low 30 bits
			// The high value is in bits 32-62

			var hi = x >> EFFECTIVE_BITS_PER_LIMB; // Create new ulong from bits 32 - 63.
			var lo = x & HIGH33_MASK; // Create new ulong from bits 0 - 30.

			return (hi, lo);
		}

		public static ulong[] SetHighHalvesToZero(ulong[] partialWordLimbs, bool? sign = null)
		{
			var result = new ulong[partialWordLimbs.Length];

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = partialWordLimbs[i] & HIGH33_MASK;
			}

			return result;
		}

		public static ulong[] ExtendSignBit(ulong[] partialWordLimbs)
		{
			var result = new ulong[partialWordLimbs.Length];
			Array.Copy(result, partialWordLimbs, result.Length);
			result[^1] = ExtendSignBit(result[^1]);

			return result;
		}

		public static ulong ExtendSignBit(ulong limb)
		{
			var result = (limb & TEST_BIT_30) > 0
						? limb | HIGH_FILL
						: limb & HIGH_MASK_OLD;

			return result;
		}

		public static bool GetSign(ulong[] partialWordLimbs)
		{
			var signBitIsSet = (partialWordLimbs[^1] & TEST_BIT_30) > 0;
			var result = !signBitIsSet; // Bit 31 is set for negative values.

			return result;
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

		#region Split and Pack -- 32

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
						? limb | HIGH_FILL
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

			if (CheckPw2cMsl(msl))
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

		#region Convert to Partial-Word Limbs

		// TOOD: Consider first using ToLongs and then calling Split(

		public static ulong[] ToPwULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > MAX_DIGIT_VALUE)
			{
				hi = BigInteger.DivRem(hi, BI_HALF_WORD_FACTOR, out var lo);
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
				result *= BI_HALF_WORD_FACTOR;
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

		// Values are ordered from least significant to most significant.
		private static ulong[] SplitFullWordLimbs(ulong[] packedValues)
		{
			var result = new ulong[packedValues.Length * 2];

			for (int i = 0; i < packedValues.Length; i++)
			{
				var (hi, lo) = Split32(packedValues[i]);

				result[2 * i] = lo;
				result[2 * i + 1] = hi;
			}

			return result;
		}

		private static bool IsValueTooLargeOld(ulong[] partialWordLimbs, int currentExponent, byte bitsBeforeBP, bool isSigned, out byte maxMagnitude, out int indexOfMsb)
		{
			var magnitude = GetMagnitudeOfIntegerPart_NotUsed(partialWordLimbs, currentExponent, out indexOfMsb);
			maxMagnitude = GetMaxMagnitude(bitsBeforeBP, isSigned);

			var result = magnitude > maxMagnitude;

			return result;
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

			var v1 = (partialWordValue & HIGH_MASK_OLD) >> (32 - numberOfBits);
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
