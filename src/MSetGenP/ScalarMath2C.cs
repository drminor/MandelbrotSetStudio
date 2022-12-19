﻿using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Numerics;
using static MongoDB.Driver.WriteConcern;

namespace MSetGenP
{
	public class ScalarMath2C : IScalerMath2C
	{
		#region Constants

		//private const ulong ALL_BITS_SET =	0xFFFFFFFFFFFFFFFF; // bits 0 - 64 are set.
		//private const ulong TEST_BIT_32 =		0x0000000100000000; // bit 32 is set.
		//private const ulong TEST_BIT_31 =		0x0000000080000000; // bit 31 is set.

		private const ulong LOW_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.

		private const ulong HIGH_MASK = LOW_BITS_SET;
		private const ulong HIGH_FILL = HIGH_BITS_SET;

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public ScalarMath2C(ApFixedPointFormat apFixedPointFormat, uint thresold)
		{
			ApFixedPointFormat = ScalarMathHelper.GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			LimbCount = ScalarMathHelper.GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = ScalarMathHelper.GetMax2CIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint);

			Threshold = thresold;
			ThresholdMsl = ScalarMathHelper.GetThresholdMsl(thresold, TargetExponent, LimbCount, ApFixedPointFormat.BitsBeforeBinaryPoint);
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; init; }
		public int TargetExponent { get; init; }

		public uint MaxIntegerValue { get; init; }
		public uint Threshold { get; init; }
		public ulong ThresholdMsl { get; init; }

		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public int NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Multiply and Square

		public Smx2C Multiply(Smx2C a, Smx2C b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroSmx2C(Math.Min(a.Precision, b.Precision));
			}

			CheckLimbs2C(a, b, "Multiply");

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa, out var carry);
			var precision = a.Precision;

			Smx2C result;

			if (carry > 0)
			{
				NumberOfMCarries++;
				result = CreateNewMaxIntegerSmx2C(precision);
			}
			else
			{
				var nrmMantissa = ShiftAndTrim(mantissa);
				result = BuildSmx2C(nrmMantissa, precision);
			}

			return result;
		}

		public ulong[] Multiply(ulong[] ax, ulong[] bx)
		{
			var mantissa = new ulong[ax.Length + bx.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = 0; i < bx.Length; i++)
				{
					var resultPtr = j + i;  // 0, 1, 1, 2

					var product = ax[j] * bx[i];

					var (hi, lo) = Split(product);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		public Smx2C Square(Smx2C a)
		{
			if (a.IsZero)
			{
				return a;
			}

			CheckLimb2C(a, "Square");

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa, out var carry);
			var precision = a.Precision;

			Smx2C result;

			if (carry > 0)
			{
				NumberOfMCarries++;
				result = CreateNewMaxIntegerSmx2C(precision);
			}
			else
			{
				var nrmMantissa = ShiftAndTrim(mantissa);
				result = BuildSmx2C(nrmMantissa, precision);
			}

			return result;
		}

		public ulong[] Square(ulong[] ax)
		{
			var mantissa = new ulong[ax.Length * 2];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = j; i < ax.Length; i++)
				{
					var resultPtr = j + i;                  // 0, 1		1, 2		0, 1, 2		1, 2, 3, 

					//// Ignore the top-halves -- these are just sign extensions.
					//var product = (ax[j] & HIGH_MASK) * (ax[i] & HIGH_MASK);

					// Turns out, the top halves are needed.
					var product = ax[j] * ax[i];

					if (i > j)
					{
						product *= 2;
					}

					var (hi, lo) = Split(product);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		public Smx2C Multiply(Smx2C a, int b)
		{
			Smx2C result;

			if (a.IsZero || b == 0)
			{
				result = CreateNewZeroSmx2C(a.Precision);
				return result;
			}

			CheckLimb2C(a, "MultiplyByInt");

			var bVal = (uint)Math.Abs(b);
			var lzc = BitOperations.LeadingZeroCount(bVal);

			if (lzc < 32 - a.BitsBeforeBP)
			{
				throw new ArgumentException("The integer multiplyer should fit into the integer portion of the Smx value.");
			}

			var rawMantissa = Multiply(a.Mantissa, bVal);
			var mantissa = PropagateCarries(rawMantissa, out var carry);
			var precision = a.Precision;

			if (carry > 0)
			{
				NumberOfMCarries++;
				result = CreateNewMaxIntegerSmx2C(precision);
			}
			else
			{
				var nrmMantissa = ShiftAndTrim(mantissa);
				result = BuildSmx2C(nrmMantissa, precision);
			}

			return result;
		}

		public ulong[] Multiply(ulong[] ax, uint b)
		{
			var mantissa = new ulong[ax.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length - 1; j++)
			{
				var product = ax[j] * b;

				var (hi, lo) = Split(product);
				mantissa[j] += lo;
				mantissa[j + 1] += hi;
			}

			var product2 = ax[^1] * b;
			var (hi2, lo2) = Split(product2);
			mantissa[^1] = lo2;

			if (hi2 != 0)
			{ 
				throw new OverflowException($"Multiply {ScalarMathHelper.GetDiagDisplayHex("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
			}

			return mantissa;
		}

		#endregion

		#region Multiply Post Processing 

		public ulong[] PropagateCarries(ulong[] mantissa, out ulong carry)
		{
			// Currently we are not producing any carries out -- the limbs are split and only a single partial product contributes to the top-half of the msl.
			// TODO: As the top half of the bin is added, we need to detect carries as we do in the Add routine.
			// TODO: If (when) this is updated to accept an incoming carry, we need to return a '1' or '0' as the Add routine does. Currently we are returning the top-half of the msl


			// To be used after a multiply operation.
			// This renormalizes the result so that each result bin with a value <= 2^32 for the final digit.

			// Starting from the LSB, each bin is split and the top-half is added to the next bin up.

			// This will be updated to take a carry coming in, as well as providing the carry out


			var result = new ulong[mantissa.Length];
			carry = 0ul;

			for (int i = 0; i < mantissa.Length - 1; i++)
			{
				var nv = mantissa[i] + carry;
				var (hi, lo) = Split(nv);
				result[i] = lo;

				//Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{mantissa.Length - 1}: Propagating {mantissa[i]:X4} wc:{carry:X4}");
				//Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv:X4}: hi:{hi:X4}, lo:{lo:X4}");

				carry = hi;
			}

			var nv2 = mantissa[^1] + carry;
			var (hi2, lo2) = Split(nv2);

			result[^1] = lo2;

			//Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{mantissa.Length - 1}: Propagating {mantissa[^1]:X4} wc:{carry:X4}");
			//Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv2:X4}: hi:{hi2:X4}, lo:{lo2:X4}");

			carry = hi2;

			if (carry > 0) throw new OverflowException("PropagateCarries found a value larger than MAX DIGIT in the top 'bin'.");

			return result;
		}

		public ulong[] ShiftAndTrim(ulong[] mantissa)
		{
			// The upper uint half of each limb should be a simple sign extension: either all 1's or all 0's
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// TODO: As the integer portion is 'trimmed', we need to check to see the result is
			// larger than the largest integer supported by the current format.

			// Discard 1 more bit?
			// Start with 1:7:56 (Sign:Integer:Fraction 
			// Intermediate has 0:16:112
			// Push 8 from behind to in front and drop the least two significant limbs for a total of 64 - 8 = 56 bits from behind
			// Push 8 off the top, for a total of 64 bits discarded.
			// The result must be positive, so if the most significant bit is a '1', we know there is an overflow.

			var shiftAmount = BitsBeforeBP;
			var result = new ulong[LimbCount];
			var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = (mantissa[sourceIndex] << 32 + shiftAmount) >> 32;  // Discard the top shiftAmount of bits.
				
				if (sourceIndex > 0)
				{
					var previousLimb = mantissa[sourceIndex - 1];
					previousLimb &= HIGH_MASK;                      // Clear the bits from the uppper half, these will either be all 0's or all 1'. Our values are confirmed to be split at this point.
					result[i] |= previousLimb >> 32 - shiftAmount;  // Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
				}

				sourceIndex++;
			}

			return result;
		}

		private (ulong hi, ulong lo) Split(ulong x)
		{
			// TODO: Upon splitting a result limb into hi and lo halves, do these values need to be signed extended?

			NumberOfSplits++;
			var hi = x >> 32;           // Create new ulong from bits 32 - 63.
			var lo = x & HIGH_MASK;     // Create new ulong from bits 0 - 31.

			return (hi, lo);
		}

		#endregion

		#region Add and Subtract

		public Smx2C Sub(Smx2C a, Smx2C b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = ScalarMathHelper.Negate(b);

			if (a.IsZero)
			{
				return bNegated;
			}

			var result = Add(a, bNegated, desc);

			return result;
		}

		public Smx2C Add(Smx2C a, Smx2C b, string desc)
		{
			CheckLimbs2C(a, b, desc);
			if (b.IsZero) return a;
			if (a.IsZero) return b;

			var precision = Math.Min(a.Precision, b.Precision);
			var mantissa = Add(a.Mantissa, b.Mantissa, out var carry);

			Smx2C result;

			if (carry > 0)
			{
				NumberOfACarries++;
				result = CreateNewMaxIntegerSmx2C(precision);
			}
			else
			{
				result = BuildSmx2C(mantissa, precision);
			}

			return result;
		}

		private ulong[] Add(ulong[] left, ulong[] right, out ulong carry)
		{
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			carry = 0uL;

			for (var i = 0; i < resultLength - 1; i++)
			{
				var nv = left[i] + right[i] + carry;
				var (lo, newCarry) = GetResultWithCarry(nv);
				result[i] = lo;

				//Report(i, left[i], right[i], carry, nv, lo, newCarry);

				carry = newCarry;
			}

			var nv2 = left[^1] + right[^1] + carry;
			var (lo2, newCarry2) = GetResultWithCarry(nv2);
			result[^1] = lo2;

			//Report(resultLength - 1, left[^1], right[^1], carry, nv2, lo2, newCarry2);
			
			carry = newCarry2;

			return result;
		}

		#endregion

		#region Add Subtract Post Processing

		private (ulong limb, ulong carry) GetResultWithCarry(ulong x)
		{
			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.

			NumberOfGetCarries++;

			var limbValue = x & HIGH_MASK;
			bool carryFlag;

			var resultIsNegative = BitOperations.LeadingZeroCount(limbValue) == 32;
			var nextBitIsNegative = BitOperations.TrailingZeroCount(x >> 32) == 0;

			if (resultIsNegative)
			{
				limbValue |= HIGH_FILL; // sign extend the result
				carryFlag = !nextBitIsNegative; // true if next higher bit is zero
			}
			else
			{
				carryFlag = nextBitIsNegative; // true if next higher bit is one
			}

			var result = (limbValue, carryFlag ? 1uL : 0uL);
			return result;
		}

		#endregion

		#region Comparison

		public bool IsGreaterOrEqThanThreshold(Smx2C a)
		{
			NumberOfGrtrThanOps++;
			var left = a.Mantissa[^1];

			var lzcHiPart = BitOperations.LeadingZeroCount(left);
			var isNegative = lzcHiPart == 0;

			Debug.Assert(!isNegative, "IsGreaterOrEqThanThreshold found a limb with a negative mantissa.");
			//Debug.Assert(a.Sign, "IsGreaterOrEqThanThreshold found a limb with a negative sign, but the mantissa is positive.");

			var right = ThresholdMsl;
			var result = left >= right;

			return result;
		}

		#endregion

		#region Smx2C Support

		public Smx Convert(Smx2C smx2C)
		{
			var un2cMantissa = ScalarMathHelper.ConvertFrom2C(smx2C.Mantissa, smx2C.Sign);

			//var result = new Smx(smx2C.Sign, un2cMantissa, smx2C.Exponent, BitsBeforeBP, smx2C.Precision);

			var rvalue = ScalarMathHelper.GetRValue(smx2C.Sign, un2cMantissa, smx2C.Exponent, smx2C.Precision);
			var result = ScalarMathHelper.CreateSmx(rvalue, TargetExponent, LimbCount, BitsBeforeBP);

			return result;
		}

		public Smx2C Convert(Smx smx, bool overrideFormatChecks = false)
		{
			if (!overrideFormatChecks) CheckLimbCountAndFPFormat(smx);

			var twoCMantissa = ScalarMathHelper.ConvertTo2C(smx.Mantissa, smx.Sign);
			var result = new Smx2C(smx.Sign, twoCMantissa, smx.Exponent, smx.Precision, BitsBeforeBP);

			return result;
		}

		public Smx2C CreateNewZeroSmx2C(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx2C(true, new ulong[LimbCount], TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx2C CreateNewMaxIntegerSmx2C(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var rValue = new RValue(MaxIntegerValue, 0, precision);
			var tResult = ScalarMathHelper.CreateSmx(rValue, TargetExponent, LimbCount, BitsBeforeBP);
			var result = Convert(tResult);

			return result;
		}

		private Smx2C BuildSmx2C(ulong[] partialWordLimbs, int precision)
		{
			var lzc = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);
			var firstBitIsAOne = lzc == 0;

			var result = new Smx2C(!firstBitIsAOne, partialWordLimbs, TargetExponent, precision, BitsBeforeBP);

			return result;
		}

		public Smx2C CreateSmx2C(RValue aRValue)
		{
			// CreateSmx produces a value that has the TargetExponent and is compatible for this LimbCount and Format.
			var smx = ScalarMathHelper.CreateSmx(aRValue, TargetExponent, LimbCount, BitsBeforeBP);

			var twoCMantissa = ScalarMathHelper.ConvertTo2C(smx.Mantissa, smx.Sign);
			var result = new Smx2C(smx.Sign, twoCMantissa, smx.Exponent, smx.Precision, BitsBeforeBP);

			return result;
		}


		private void Report(int step, ulong left, ulong right, ulong carry, ulong nv, ulong lo, ulong newCarry)
		{
			var ld = ScalarMathHelper.ConvertFrom2C(left);
			var rd = ScalarMathHelper.ConvertFrom2C(right);
			var cd = ScalarMathHelper.ConvertFrom2C(carry);
			var nvd = ScalarMathHelper.ConvertFrom2C(nv);
			var hid = ScalarMathHelper.ConvertFrom2C(newCarry);
			var lod = ScalarMathHelper.ConvertFrom2C(lo);

			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {left:X4}, {right:X4} wc:{carry:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv:X4}: hi:{newCarry:X4}, lo:{lo:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}\n");
		}

		#endregion

		#region DEBUG Checks

		[Conditional("DETAIL")]
		private void CheckLimbCountAndFPFormat(Smx smx)
		{
			if (smx.LimbCount != LimbCount)
			{
				throw new ArgumentException($"While converting an Smx2C found it to have {smx.LimbCount} limbs instead of {LimbCount}.");
			}

			if (smx.Exponent != TargetExponent)
			{
				throw new ArgumentException($"While converting an Smx2C found it to have {smx.Exponent} limbs instead of {TargetExponent}.");
			}

			if (smx.BitsBeforeBP != BitsBeforeBP)
			{
				throw new ArgumentException($"While converting an Smx2C found it to have {smx.BitsBeforeBP} limbs instead of {BitsBeforeBP}.");
			}
		}

		[Conditional("DETAIL")]
		private void CheckLimbs2C(Smx2C a, Smx2C b, string desc)
		{
			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
				throw new InvalidOperationException($"The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
			}

			if (b.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
				throw new InvalidOperationException($"The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.Exponent != b.Exponent)
			{
				Debug.WriteLine($"Warning:the exponents do not match.");
				throw new InvalidOperationException($"The exponents do not match.");
			}

			ValidateIsSplit2C(a.Mantissa, a.Sign);
			ValidateIsSplit2C(b.Mantissa, b.Sign);
		}

		[Conditional("DETAIL")]
		private void CheckLimb2C(Smx2C a, string desc)
		{
			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
				throw new InvalidOperationException($"The value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.Exponent != TargetExponent)
			{
				Debug.WriteLine($"Warning: The exponent is not the TargetExponent:{TargetExponent}.");
				throw new InvalidOperationException($"Warning: The exponent is not the TargetExponent:{TargetExponent}.");
			}

			ValidateIsSplit2C(a.Mantissa, a.Sign);
		}


		[Conditional("DETAIL")]
		private void ValidateIsSplit2C(ulong[] mantissa)
		{
			if (ScalarMathHelper.CheckPW2CValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		[Conditional("DETAIL")]
		private void ValidateIsSplit2C(ulong[] mantissa, bool sign)
		{
			if (ScalarMathHelper.CheckPW2CValues(mantissa, sign))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		#endregion
	}
}