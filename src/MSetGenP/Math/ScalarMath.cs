using MongoDB.Driver;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Common.SmxVals;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSetGenP
{
    public class ScalarMath : IScalerMath
	{
		#region Constants

		private const int EFFECTIVE_BITS_PER_LIMB = 31;
		private static readonly ulong MAX_DIGIT_VALUE = (ulong)(-1 + Math.Pow(2, EFFECTIVE_BITS_PER_LIMB));

		//private const ulong LOW_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		//private const ulong HIGH_MASK = LOW_BITS_SET;
		//private const ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		private const ulong LOW31_BITS_SET = 0x000000007FFFFFFF;    // bits 0 - 30 are set.
		private const ulong HIGH33_MASK = LOW31_BITS_SET;
		private const ulong TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.

		private static readonly bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public ScalarMath(ApFixedPointFormat apFixedPointFormat, uint threshold)
		{
			ApFixedPointFormat = apFixedPointFormat;	
			Threshold = threshold;
			MaxIntegerValue = ScalarMathHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint, IsSigned);
			ThresholdMsl = ScalarMathHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);
		}

		#endregion

		#region Public Properties

		public bool IsSigned => false;

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public uint MaxIntegerValue { get; init; }
		public uint Threshold { get; init; }
		public ulong ThresholdMsl { get; init; }

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public int NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Multiply and Square

		public Smx Multiply(Smx a, Smx b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroSmx(Math.Min(a.Precision, b.Precision));
			}

			CheckLimbs(a, b, "Multiply");

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = SumThePartials(rawMantissa);

			var nrmMantissa = ScalarMathHelper.ShiftAndTrim(mantissa, ApFixedPointFormat, IsSigned, USE_DET_DEBUG);

			var sign = a.Sign == b.Sign;
			var precision = Math.Min(a.Precision, b.Precision);
			var bitsBeforeBP = a.BitsBeforeBP;
			Smx result = new Smx(sign, nrmMantissa, TargetExponent, bitsBeforeBP, precision);

			return result;
		}

		public ulong[] Multiply(ulong[] ax, ulong[] bx)
		{
			//Debug.WriteLine(GetDiagDisplay("ax", ax));
			//Debug.WriteLine(GetDiagDisplay("bx", bx));

			//var seive = new ulong[ax.Length * bx.Length];

			var mantissa = new ulong[ax.Length + bx.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = 0; i < bx.Length; i++)
				{
					var product = ax[j] * bx[i];
					//seive[j * bx.Length + i] = product;

					var resultPtr = j + i;  // 0, 1, 1, 2

					//var lo = Split(product, out var hi);

					NumberOfSplits++;
					var (hi, lo) = ScalarMathHelper.Split(product);

					//var loPreviousLzc = BitOperations.LeadingZeroCount(mantissa[resultPtr]);
					//var hiPreviousLzc = BitOperations.LeadingZeroCount(mantissa[resultPtr + 1]);

					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;

					//var loLzc = BitOperations.LeadingZeroCount(mantissa[resultPtr]);
					//var hiLzc = BitOperations.LeadingZeroCount(mantissa[resultPtr + 1]);
					//var prodLzc = BitOperations.LeadingZeroCount(product);

					//Debug.WriteLine($"Leading Zero Counts: lo:{loPreviousLzc}, {loLzc} // hi:{hiPreviousLzc}, {hiLzc}. Prod:{prodLzc}.");
				}
			}

			//var splitSieve = Split(seive);
			//Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, bx.Length * 2));
			//Debug.WriteLine(GetDiagDisplay("result", fullMantissa));

			return mantissa;
		}

		public Smx Square(Smx a)
		{
			if (a.IsZero)
			{
				return a;
			}

			CheckLimb(a, "Square");

			var rawMantissa = Square(a.Mantissa);
			var mantissa = SumThePartials(rawMantissa);

			//var nrmMantissa = ShiftAndTrim(mantissa);
			var nrmMantissa = ScalarMathHelper.ShiftAndTrim(mantissa, ApFixedPointFormat, IsSigned, USE_DET_DEBUG);

			var sign = true;
			var precision = a.Precision;
			var bitsBeforeBP = a.BitsBeforeBP;
			Smx result = new Smx(sign, nrmMantissa, TargetExponent, bitsBeforeBP, precision);

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
					var product = ax[j] * ax[i];

					if (i > j)
					{
						product *= 2;
					}
					var resultPtr = j + i;

					//var lo = Split(product, out var hi);

					NumberOfSplits++;
					var (hi, lo) = ScalarMathHelper.Split(product);

					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		public Smx Multiply(Smx a, int b)
		{
			Smx result;

			if (a.IsZero || b == 0)
			{
				result = CreateNewZeroSmx(a.Precision);
				return result;
			}

			CheckLimb(a, "MultiplyByInt");

			var bVal = (uint)Math.Abs(b);
			var lzc = BitOperations.LeadingZeroCount(bVal);

			if (lzc < 32 - a.BitsBeforeBP)
			{
				throw new ArgumentException("The integer multiplyer should fit into the integer portion of the Smx value.");
			}

			var bSign = b >= 0;
			var sign = a.Sign == bSign;

			var rawMantissa = Multiply(a.Mantissa, bVal);
			var mantissa = SumThePartials(rawMantissa);

			result = new Smx(sign, mantissa, a.Exponent, a.BitsBeforeBP, a.Precision);

			return result;
		}

		public ulong[] Multiply(ulong[] ax, uint b)
		{
			//Debug.WriteLine(GetDiagDisplay("ax", ax));
			//Debug.WriteLine($"b = {b}");

			//var seive = new ulong[ax.Length];

			var mantissa = new ulong[ax.Length];
			ulong carry = 0;

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				var product = ax[j] * b;
				//seive[j] = product;

				var sum = product + carry;

				var (hi, lo) = ScalarMathHelper.Split(sum);
				NumberOfSplits++;

				mantissa[j] = lo;
				//mantissa[j + 1] += hi;
				carry = hi;
			}

			//var product2 = ax[^1] * b;

			//NumberOfSplits++;
			//var (hi2, lo2) = ScalarMathHelper.Split(product2);

			//mantissa[^1] = lo2;

			//if (hi2 != 0)
			//{
			//	throw new OverflowException($"Multiply {ScalarMathHelper.GetDiagDisplay("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
			//}

			if (carry != 0)
			{
				throw new OverflowException($"Multiply {ScalarMathHelper.GetDiagDisplayHex("ax", ax)} x {b} resulted in a overflow. The hi value is {carry}.");
			}

			//var splitSieve = Split(seive);
			//Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, 2));
			//Debug.WriteLine(GetDiagDisplay("result", mantissa));

			return mantissa;
		}

		#endregion

		#region Multiply Post Processing 

		public ulong[] SumThePartials(ulong[] mantissa)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var result = new ulong[mantissa.Length];

			//indexOfLastNonZeroLimb = -1;
			var carry = 0ul;

			for (int i = 0; i < mantissa.Length; i++)
			{
				//var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter

				NumberOfGetCarries++;
				var sum = mantissa[i] + carry;
				var (lo, newCarry) = ScalarMathHelper.GetResultWithCarry(sum);

				//ReportForAddition(i, mantissa[i], carry, sum, lo, newCarry);

				//var sumLzc = BitOperations.LeadingZeroCount(sum);
				//var loLzc = BitOperations.LeadingZeroCount(lo);
				//var hiLzc = BitOperations.LeadingZeroCount(hi);

				//Debug.WriteLine($"Leading Zero Counts: sum:{sumLzc}, lo:{loLzc}, hi:{hiLzc}.");

				result[i] = lo;

				carry = newCarry;
			}

			if (carry != 0)
			{
				throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			}

			return result;
		}

		//public ulong[] ShiftAndTrimOld(ulong[] mantissa)
		//{
		//	//ValidateIsSplit(mantissa); // Conditional Method

		//	var lZCounts = ScalarMathHelper.GetLZCounts(mantissa);

		//	Debug.WriteLine($"S&T LZCounts:");
		//	for (var lzcPtr = 0; lzcPtr < lZCounts.Length; lzcPtr++)
		//	{
		//		Debug.WriteLine($"{lzcPtr}: {lZCounts[lzcPtr]} {mantissa[lzcPtr]}");
		//	}

		//	Debug.Assert(lZCounts[^1] >= 33 + BitsBeforeBP, "The multiplication result is > Max Integer.");


		//	// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
		//	// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
		//	// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

		//	var shiftAmount = BitsBeforeBP + 1;

		//	var result = new ulong[LimbCount];
		//	var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);

		//	var i = 0;

		//	for (; i < result.Length; i++)
		//	{
		//		result[i] = (mantissa[sourceIndex] << 33 + shiftAmount) >> 33;  // Discard the top shiftAmount of bits.

		//		if (sourceIndex > 0)
		//		{
		//			result[i] |= (mantissa[sourceIndex - 1] >> 32 - shiftAmount) << 1; // Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		//		}
		//		sourceIndex++;
		//	}

		//	var lZCounts2 = ScalarMathHelper.GetLZCounts(result);
		//	Debug.WriteLine($"S&T LZCounts2:");
		//	for (var lzcPtr = 0; lzcPtr < lZCounts2.Length; lzcPtr++)
		//	{
		//		Debug.WriteLine($"{lzcPtr}: {lZCounts2[lzcPtr]} {result[lzcPtr]}");
		//	}

		//	return result;
		//}

		//public ulong[] ShiftAndTrim_Superceded(ulong[] mantissa)
		//{
		//	// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
		//	// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
		//	// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

		//	// Clear the bits from the uppper half + the reserved bit, these will either be all 0's or all 1'. Our values are confirmed to be split at this point.

		//	var shiftAmount = 8; // (LimbCount * 32) - TargetExponent;

		//	var result = new ulong[LimbCount];

		//	//var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);
		//	var sourceIndex = mantissa.Length - 1;
		//	var i = result.Length - 1;

		//	for (; i >= 0; i--)
		//	{
		//		if (sourceIndex > 0)
		//		{
		//			// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
		//			var topHalf = mantissa[sourceIndex] << shiftAmount; 
		//			topHalf &= HIGH33_MASK;

		//			// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		//			var bottomHalf = mantissa[sourceIndex - 1] & HIGH33_MASK;
		//			bottomHalf >>= 32 - shiftAmount;

		//			result[i] = topHalf | bottomHalf;

		//			var strResult = string.Format("0x{0:X4}", result[i]);
		//			var strTopHalf = string.Format("0x{0:X4}", topHalf);
		//			var strBottomHalf = string.Format("0x{0:X4}", bottomHalf);
		//			Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopHalf} and {strBottomHalf}.");
		//		}
		//		else
		//		{
		//			// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
		//			var topHalf = mantissa[sourceIndex] << shiftAmount;
		//			topHalf &= HIGH33_MASK;

		//			result[i] = topHalf;

		//			var strResult = string.Format("0x{0:X4}", result[i]);
		//			var strTopH = string.Format("0x{0:X4}", topHalf);
		//			Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopH}.");
		//		}

		//		sourceIndex--;
		//	}

		//	// SignExtend the MSL
		//	result[^1] = ScalarMathHelper.ExtendSignBit(result[^1]);	

		//	return result;
		//}

		//private ulong Split(ulong x, out ulong hi)
		//{
		//	NumberOfSplits++;
		//	hi = x >> 32; // Create new ulong from bits 32 - 63.
		//	return x & HIGH_MASK; // Create new ulong from bits 0 - 31.
		//}

		#endregion

		#region Add and Subtract

		public Smx Sub(Smx a, Smx b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new Smx(!b.Sign, b.Mantissa, b.Exponent, b.BitsBeforeBP, b.Precision);

			if (a.IsZero)
			{
				return bNegated;
			}

			var result = Add(a, bNegated, desc);

			return result;
		}

		public Smx Add(Smx a, Smx b, string desc)
		{
			CheckLimbs(a, b, "Add");
			if (b.IsZero) return a;
			if (a.IsZero) return b;

			bool sign;
			ulong[] mantissa;
			var precision = Math.Min(a.Precision, b.Precision);

			var carry = 0ul;

			if (a.Sign == b.Sign)
			{
				//NumberOfMCarries++;
				sign = a.Sign;
				mantissa = Add(a.Mantissa, b.Mantissa, out carry);
			}
			else
			{
				//NumberOfACarries++;
				var cmp = Compare(a.Mantissa, b.Mantissa);

				if (cmp >= 0)
				{
					sign = a.Sign;
					mantissa = Sub(a.Mantissa, b.Mantissa);
				}
				else
				{
					sign = b.Sign;
					mantissa = Sub(b.Mantissa, a.Mantissa);
				}
			}

			Smx result;

			if (carry != 0)
			{
				result = CreateNewMaxIntegerSmx();
				NumberOfACarries++;
			}
			else
			{
				result = new Smx(sign, mantissa, a.Exponent, a.BitsBeforeBP, precision);
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

			carry = 0ul;

			for (var i = 0; i < resultLength; i++)
			{
				//var nv = left[i] + right[i] + carry;

				// Since we are not using two's compliment, we don't need to use the Reserved Bit

				ulong nv;

				var lChopped = left[i] & HIGH33_MASK;
				var rChopped = right[i] & HIGH33_MASK;

				checked
				{
					nv = lChopped + rChopped + carry;
				}

				NumberOfSplits++;
				var (hi, lo) = ScalarMathHelper.Split(nv);
				carry = hi;

				result[i] = lo;
			}

			return result;
		}

		private ulong[] Sub(ulong[] left, ulong[] right)
		{
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			var borrow = 0ul;

			for (var i = 0; i < resultLength - 1; i++)
			{
				// Set the least significant bit of the high part of a.
				var sax = left[i] | TEST_BIT_31;

				result[i] = sax - right[i] - borrow;

				if ((result[i] & TEST_BIT_31) > 0)
				{
					// if the least significant bit of the high part of the result is still set, no borrow occured.
					result[i] &= HIGH33_MASK;
					borrow = 0;
				}
				else
				{
					borrow = 1;
				}

			}

			if (left[^1] < (right[^1] + borrow))
			{
				// TOOD: Since we always call sub with the left argument > the right argument, then this should never occur.
				throw new OverflowException("MSB too small.");
			}

			result[^1] = left[^1] - right[^1] - borrow;

			return result;
		}

		private void ReportForAddition(int step, ulong left, ulong carry, ulong nv, ulong lo, ulong newCarry)
		{
			var ld = ScalarMathHelper.ConvertFrom2C(left);
			//var rd = ScalarMathHelper.ConvertFrom2C(right);
			var cd = ScalarMathHelper.ConvertFrom2C(carry);
			var nvd = ScalarMathHelper.ConvertFrom2C(nv);
			var hid = ScalarMathHelper.ConvertFrom2C(newCarry);
			var lod = ScalarMathHelper.ConvertFrom2C(lo);

			//Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {left:X4}, {right:X4} wc:{carry:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {left:X4}, wc:{carry:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv:X4}: hi:{newCarry:X4}, lo:{lo:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}\n");
		}

		#endregion

		#region Smx2C Support 

		//public Smx Convert(Smx2C smx2C)
		//{
		//	var un2cMantissa = ScalarMathHelper.ConvertFrom2C(smx2C.Mantissa);
		//	var rvalue = ScalarMathHelper.CreateRValue(smx2C.Sign, un2cMantissa, smx2C.Exponent, smx2C.Precision);

		//	var result = ScalarMathHelper.CreateSmx(rvalue, ApFixedPointFormat);

		//	return result;
		//}

		//public Smx2C Convert(Smx smx)
		//{
		//	CheckLimbCountAndFPFormat(smx);

		//	var twoCMantissa = ScalarMathHelper.ConvertTo2C(smx.Mantissa, smx.Sign);
		//	var result = new Smx2C(smx.Sign, twoCMantissa, smx.Exponent, BitsBeforeBP, smx.Precision);

		//	return result;
		//}

		public Smx CreateSmx(RValue rValue)
		{
			var result = ScalarMathHelper.CreateSmx(rValue, ApFixedPointFormat);
			return result;
		}

		public Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(true, new ulong[LimbCount], TargetExponent, BitsBeforeBP, precision);
			return result;
		}

		public Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			// TODO: Create a Static Readonly value and the use Clone to make copies
			var result = ScalarMathHelper.CreateSmx(new RValue(MaxIntegerValue, 0, precision), ApFixedPointFormat);
			return result;
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

			// All Smx variables store the mantissa as a positive value.
			CheckPWValues(smx.Mantissa);
		}


		[Conditional("DETAIL")]
		private void CheckLimbs(Smx a, Smx b, string desc)
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

			CheckPWValues(a.Mantissa);
			CheckPWValues(b.Mantissa);
		}

		[Conditional("DETAIL")]
		private void CheckLimb(Smx a, string desc)
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

			CheckPWValues(a.Mantissa);
		}


		[Conditional("DETAIL")]
		private void ValidateIsSplit(ulong[] mantissa)
		{
			if (CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		[Conditional("DETAIL")]
		private void CheckForceExpResult(Smx smx, string desc)
		{
			if (smx.Mantissa.Length > LimbCount)
			{
				throw new InvalidOperationException($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs. Desc: {desc}.");
				//Debug.WriteLine($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs.");
			}
		}

		//[Conditional("DETAIL")]
		//private void CheckLimbs2C(Smx2C a, Smx2C b, string desc)
		//{
		//	if (a.LimbCount != LimbCount)
		//	{
		//		Debug.WriteLine($"WARNING: The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//		throw new InvalidOperationException($"The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//	}

		//	if (b.LimbCount != LimbCount)
		//	{
		//		Debug.WriteLine($"WARNING: The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
		//		throw new InvalidOperationException($"The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
		//	}

		//	if (a.Exponent != b.Exponent)
		//	{
		//		Debug.WriteLine($"Warning:the exponents do not match.");
		//		throw new InvalidOperationException($"The exponents do not match.");
		//	}

		//	ValidateIsSplit2C(a.Mantissa, a.Sign);
		//	ValidateIsSplit2C(b.Mantissa, b.Sign);
		//}

		//[Conditional("DETAIL")]
		//private void CheckLimb2C(Smx2C a, string desc)
		//{
		//	if (a.LimbCount != LimbCount)
		//	{
		//		Debug.WriteLine($"WARNING: The value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//		throw new InvalidOperationException($"The value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//	}

		//	if (a.Exponent != TargetExponent)
		//	{
		//		Debug.WriteLine($"Warning: The exponent is not the TargetExponent:{TargetExponent}.");
		//		throw new InvalidOperationException($"Warning: The exponent is not the TargetExponent:{TargetExponent}.");
		//	}

		//	ValidateIsSplit2C(a.Mantissa, a.Sign);
		//}

		//[Conditional("DETAIL")]
		//private void ValidateIsSplit2C(ulong[] mantissa)
		//{
		//	if (ScalarMathHelper.CheckPW2CValues(mantissa))
		//	{
		//		throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
		//	}
		//}

		//[Conditional("DETAIL")]
		//private void ValidateIsSplit2C(ulong[] mantissa, bool sign)
		//{
		//	//if (ScalarMathHelper.CheckPW2CValues(mantissa, sign))
		//	//{
		//	//	throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
		//	//}

		//	var signFromMantissa = ScalarMathHelper.GetSign(mantissa);

		//	if (sign != signFromMantissa)
		//	{
		//		throw new ArgumentException($"Expected the mantissa to have sign: {sign}.");
		//	}

		//	if (ScalarMathHelper.CheckPW2CValues(mantissa))
		//	{
		//		throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
		//	}
		//}

		private bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x > MAX_DIGIT_VALUE);
			return result;
		}

		#endregion

		#region Map Generartion Support

		//public FPValues BuildMapPoints(Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, out FPValues cIValues)
		//{
		//	var stride = (byte)blockSize.Width;
		//	var samplePointOffsets = BuildSamplePointOffsets(delta, stride);
		//	var samplePointsX = BuildSamplePoints(startingCx, samplePointOffsets);
		//	var samplePointsY = BuildSamplePoints(startingCy, samplePointOffsets);

		//	var resultLength = blockSize.NumberOfCells;

		//	var crSmxes = new Smx[resultLength];
		//	var ciSmxes = new Smx[resultLength];

		//	var resultPtr = 0;
		//	for (int j = 0; j < samplePointsY.Length; j++)
		//	{
		//		for (int i = 0; i < samplePointsX.Length; i++)
		//		{
		//			ciSmxes[resultPtr] = samplePointsY[j];
		//			crSmxes[resultPtr++] = samplePointsX[i];
		//		}
		//	}

		//	var result = new FPValues(crSmxes);
		//	cIValues = new FPValues(ciSmxes);

		//	return result;
		//}

		public Smx[] BuildSamplePoints(Smx startValue, Smx[] samplePointOffsets)
		{
			var result = new Smx[samplePointOffsets.Length];

			for (var i = 0; i < samplePointOffsets.Length; i++)
			{
				var samplePointSa = Add(startValue, samplePointOffsets[i], "add spd offset to start value");
				result[i] = samplePointSa;
			}

			return result;
		}

		public Smx[] BuildSamplePointOffsets(Smx delta, byte extent)
		{
			var result = new Smx[extent];

			for (var i = 0; i < extent; i++)
			{
				var samplePointOffset = Multiply(delta, (byte)i);
				CheckForceExpResult(samplePointOffset, "BuildSPOffsets");
				result[i] = samplePointOffset;
			}

			return result;
		}

		#endregion

		#region Comparison

		private int Compare(ulong[] left, ulong[] right)
		{
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var i = -1 + Math.Min(left.Length, right.Length);

			for (; i >= 0; i--)
			{
				if (left[i] != right[i])
				{
					return left[i] > right[i] ? 1 : -1;
				}
			}

			return 0;
		}

		public bool IsGreaterOrEqThanOld(Smx a, uint b)
		{
			var exponent = a.Exponent;
			var aAsDouble = 0d;

			for (var i = a.Mantissa.Length - 1; i >= 0; i--)
			{
				aAsDouble += a.Mantissa[i] * Math.Pow(2, exponent + (i * 32));

				if (aAsDouble >= b)
				{
					return true;
				}
			}

			return false;
		}

		public bool IsGreaterOrEqThanThreshold(Smx a)
		{
			NumberOfGrtrThanOps++;
			var left = a.Mantissa[^1];
			//var right = b * Math.Pow(2, 24);
			var right = ThresholdMsl;
			var result = left >= right;

			return result;
		}

		#endregion
	}
}
