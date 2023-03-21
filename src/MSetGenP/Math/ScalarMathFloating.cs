using MongoDB.Driver;
using MSS.Common;
using MSetGenP.Types;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
    public class ScalarMathFloating
	{
		#region Constants

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)(-1 + Math.Pow(2, EFFECTIVE_BITS_PER_LIMB));
		private static readonly ulong HALF_DIGIT_VALUE = (ulong)Math.Pow(2, 16);

		private static readonly ulong HIGH_MASK =	 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private static readonly ulong TEST_BIT_32 =	 0x0000000100000000; // bit 32 is set.

		public bool IsSigned => false;

		#endregion

		#region Constructor

		public ScalarMathFloating(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			MaxIntegerValue = ScalarMathHelper.GetMaxIntegerValue(apFixedPointFormat.BitsBeforeBinaryPoint, isSigned: false);
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public uint MaxIntegerValue { get; init; }

		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		#endregion

		#region Multiply and Square

		public Smx Multiply(Smx a, Smx b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroSmx(Math.Min(a.Precision, b.Precision));
			}

			Debug.Assert(a.LimbCount == LimbCount, $"A should have {LimbCount} limbs, instead it has {a.LimbCount}.");
			Debug.Assert(b.LimbCount == LimbCount, $"B should have {LimbCount} limbs, instead it has {b.LimbCount}.");

			Debug.Assert(a.Exponent == TargetExponent, $"A should have an exponent of {TargetExponent}, instead of {a.Exponent}.");
			Debug.Assert(b.Exponent == TargetExponent, $"B should have an exponent of {TargetExponent}, instead of {b.Exponent}.");

			//var numberOfLeadingZerosA = GetNumberOfLeadingZeroLimbs(a);
			//var numberOfLeadingZerosB = GetNumberOfLeadingZeroLimbs(b);

			//var logicalLengthA = GetLogicalLength(a);
			//var logicalLengthB = GetLogicalLength(b);

			//var logicalExpA = -1 * GetNumberOfBitsAfterBP(a);
			//var logicalExpB = -1 * GetNumberOfBitsAfterBP(b);

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);

			//var logicalLength = logicalLengthA + logicalLengthB;

			//var exponent = a.Exponent + b.Exponent;
			//var logicalExponent = logicalExpA + logicalExpB;

			//var bitsBeforeBPInResult = -1 * (TargetExponent * 2 - logicalExponent);
			//Debug.Assert(bitsBeforeBPInResult == BitsBeforeBP * 2, "BitsBeforeBPResult Miss Match. Smx x Smx.");

			//logicalExponent -= BitsBeforeBP; // Shift result down so that the Format: BitsBeforeBp:BitsAfterBP is re-established.
			//bitsBeforeBPInResult = -1 * (TargetExponent * 2 - logicalExponent);
			//Debug.Assert(bitsBeforeBPInResult == BitsBeforeBP, "BitsBeforeBPResult Miss Match -- Square Smx.");

			//logicalExponent -= BitsBeforeBP;

			var nrmMantissa = ShiftAndTrim(mantissa);

			var sign = a.Sign == b.Sign;
			var precision = Math.Min(a.Precision, b.Precision);
			var bitsBeforeBP = a.BitsBeforeBP;
			Smx result = new Smx(sign, nrmMantissa, TargetExponent, bitsBeforeBP, precision);

			return result;
		}

		//public Smx MultiplyC(Smx a, Smx b)
		//{
		//	if (a.IsZero || b.IsZero)
		//	{
		//		return new Smx(0, 1, Math.Min(a.Precision, b.Precision), a.BitsBeforeBP);
		//	}

		//	var sign = a.Sign == b.Sign;

		//	var exponent = a.Exponent + b.Exponent;

		//	var precision = Math.Min(a.Precision, b.Precision);

		//	var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
		//	var mantissa = PropagateCarries(rawMantissa, out var indexOfLastNonZeroLimb);

		//	var nrmMantissa = ConvertExp(mantissa, exponent, out var nrmExponent);

		//	Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision, a.BitsBeforeBP);

		//	return result;
		//}

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

					var lo = Split(product, out var hi);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
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

			Debug.Assert(a.LimbCount == LimbCount, $"A should have {LimbCount} limbs, instead it has {a.LimbCount}.");
			Debug.Assert(a.Exponent == TargetExponent, $"A should have an exponent of {TargetExponent}, instead of {a.Exponent}.");

			//var logicalLengthA = GetLogicalLength(a);
			//var logicalExpA = -1 * GetNumberOfBitsAfterBP(a);
			//var numberOfLeadingZeros = GetNumberOfLeadingZeroLimbs(a);

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);

			//var logicalLength = logicalLengthA * 2;

			//var exponent = a.Exponent * 2;
			//var logicalExponent = logicalExpA * 2;

			//var bitsBeforeBPInResult = -1 * (TargetExponent * 2 - logicalExponent);
			//Debug.Assert(bitsBeforeBPInResult == BitsBeforeBP * 2, "BitsBeforeBPResult Miss Match -- Square Smx.");

			//logicalExponent -= BitsBeforeBP; // Shift result down so that the Format: BitsBeforeBp:BitsAfterBP is re-established.
			//bitsBeforeBPInResult = -1 * (TargetExponent * 2 - logicalExponent);
			//Debug.Assert(bitsBeforeBPInResult == BitsBeforeBP, "BitsBeforeBPResult Miss Match -- Square Smx.");

			var nrmMantissa = ShiftAndTrim(mantissa);

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
					var lo = Split(product, out var hi);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		public Smx Multiply(Smx a, int b)
		{
			Smx result;

			if (a.IsZero || b == 0 )
			{
				result = CreateNewZeroSmx(a.Precision);
				return result;
			}

			Debug.Assert(a.LimbCount == LimbCount, $"A should have {LimbCount} limbs, instead it has {a.LimbCount}.");
			Debug.Assert(a.Exponent == TargetExponent, $"A should have an exponent of {TargetExponent}, instead of {a.Exponent}.");

			var bVal = (uint)Math.Abs(b);
			var lzc =  BitOperations.LeadingZeroCount(bVal);

			if (lzc < 32 - a.BitsBeforeBP)
			{
				throw new ArgumentException("The integer multiplyer should fit into the integer portion of the Smx value.");
			}

			var bSign = b >= 0;
			var sign = a.Sign == bSign;

			var rawMantissa = Multiply(a.Mantissa, bVal);
			var mantissa = PropagateCarries(rawMantissa);

			result = new Smx(sign, mantissa, a.Exponent, a.BitsBeforeBP, a.Precision);

			return result;
		}

		public ulong[] Multiply(ulong[] ax, uint b)
		{
			//Debug.WriteLine(GetDiagDisplay("ax", ax));
			//Debug.WriteLine($"b = {b}");

			//var seive = new ulong[ax.Length];

			var mantissa = new ulong[ax.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length - 1; j++)
			{
				var product = ax[j] * b;
				//seive[j] = product;

				var lo = Split(product, out var hi);
				mantissa[j] += lo;
				mantissa[j + 1] += hi;
			}

			var product2 = ax[^1] * b;
			var lo2 = Split(product2, out var hi2);

			mantissa[^1] = lo2;

			if (hi2 != 0)
			{
				throw new OverflowException($"Multiply {GetDiagDisplay("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
			}

			//var splitSieve = Split(seive);
			//Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, 2));
			//Debug.WriteLine(GetDiagDisplay("result", mantissa));

			return mantissa;
		}

		public ulong[] PropagateCarries(ulong[] mantissa/*, out int indexOfLastNonZeroLimb*/)
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
				var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter
				result[i] = lo;

				if (lo > 0)
				{
					//indexOfLastNonZeroLimb = i;
				}

				carry = hi;
			}

			if (carry != 0)
			{
				throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			}

			return result;
		}

		#endregion

		#region Add and Subtract

		public SmxSa Sub(SmxSa a, SmxSa b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new SmxSa(!b.Sign, b.MantissaSa, b.Exponent, b.Precision);
			var result = Add(a, bNegated, desc);

			return result;
		}

		public SmxSa Add(SmxSa a, SmxSa b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			if (a.IsZero)
			{
				return b;
			}

			bool sign;
			ulong[] mantissa;
			int indexOfLastNonZeroLimb;
			var precision = Math.Min(a.Precision, b.Precision);

			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.Exponent != b.Exponent)
			{
				Debug.WriteLine($"Warning:the exponents do not match.");
			}

			(var left, var right) = AlignExponents(a, b, desc, out var exponent);

			if (right.IsZero)
			{
				return a;
			}

			if (left.IsZero)
			{
				return b;
			}

			if (left.LimbCount != right.LimbCount)
			{
				ExtendLimbs(left.MantissaSa, right.MantissaSa);
			}

			var carry = 0ul;

			if (a.Sign == b.Sign)
			{
				sign = a.Sign;
				mantissa = Add(left.MantissaSa, right.MantissaSa, out indexOfLastNonZeroLimb, out carry);
			}
			else
			{
				var cmp = Compare(left.MantissaSa, right.MantissaSa);

				if (cmp >= 0)
				{
					sign = a.Sign;
					mantissa = Sub(left.MantissaSa, right.MantissaSa, out indexOfLastNonZeroLimb);
				}
				else
				{
					sign = b.Sign;
					mantissa = Sub(right.MantissaSa, left.MantissaSa, out indexOfLastNonZeroLimb);
				}
			}

			SmxSa result;

			if (carry != 0)
			{
				result = Convert(CreateNewMaxIntegerSmx());
			}
			else
			{
				result = new SmxSa(sign, mantissa, indexOfLastNonZeroLimb, exponent, precision);
			}

			return result;
		}

		private ulong[] Add(ShiftedArray<ulong> left, ShiftedArray<ulong> right, out int indexOfLastNonZeroLimb, out ulong carry)
		{
			//Debug.Assert(left.Length == right.Length);
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			indexOfLastNonZeroLimb = -1;
			carry = 0ul;

			for (var i = 0; i < resultLength; i++)
			{
				var nv = left[i] + right[i] + carry;
				var lo = Split(nv, out carry);
				result[i] = lo;

				if (lo > 0)
				{
					indexOfLastNonZeroLimb = i;
				}
			}

			return result;
		}

		private ulong[] Sub(ShiftedArray<ulong> left, ShiftedArray<ulong> right, out int indexOfLastNonZeroLimb)
		{
			//Debug.Assert(left.Length == right.Length);

			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			indexOfLastNonZeroLimb = -1;
			var borrow = 0ul;

			for (var i = 0; i < resultLength - 1; i++)
			{
				// Set the least significant bit of the high part of a.
				var sax = left[i] | TEST_BIT_32;

				result[i] = sax - right[i] - borrow;

				if ((result[i] & TEST_BIT_32) > 0)
				{
					// if the least significant bit of the high part of the result is still set, no borrow occured.
					result[i] &= HIGH_MASK;
					borrow = 0;
				}
				else
				{
					borrow = 1;
				}

				if (result[i] > 0)
				{
					indexOfLastNonZeroLimb = i;
				}
			}

			if (left[^1] < (right[^1] + borrow))
			{
				// TOOD: Since we always call sub with the left argument > the right argument, then this should never occur.
				throw new OverflowException("MSB too small.");
			}

			result[^1] = left[^1] - right[^1] - borrow;

			if (result[^1] > 0)
			{
				indexOfLastNonZeroLimb = resultLength - 1;
			}

			return result;
		}

		#endregion

		#region Normalization Support

		public ulong[] ShiftAndTrim(ulong[] mantissa)
		{
			ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			var shiftAmount = BitsBeforeBP;

			var result = new ulong[LimbCount];
			var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);

			for (var i = 0; i < result.Length; i++)
			{
				result[i] = (mantissa[sourceIndex] << 32 + shiftAmount) >> 32;
				if (sourceIndex > 0)
				{
					result[i] |= (mantissa[sourceIndex - 1] >> 32 - shiftAmount); // Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
				}
				sourceIndex++;
			}

			return result;
		}

		public ulong[] ConvertExp(ulong[] mantissa, int exponent, out int nrmExponent)
		{
			ValidateIsSplit(mantissa);

			ulong[] result;
			nrmExponent = TargetExponent;

			var logicalLength = GetLogicalLength(mantissa);
			var limbsToDiscard = Math.Max(logicalLength - LimbCount, 0);
			var adjExponent = exponent + limbsToDiscard * 32;

			var shiftAmount = ScalarMathHelper.GetShiftAmount(adjExponent, TargetExponent);

			if (shiftAmount > 0)
			{
				// For example target is -125, and currently we have -100
				// -125 - -100 = -25
				// Multiply coefficient by 2^25 and exponent by 1/2^25
				// (v) * (1/2^100) => (v * 2^25) * (1/2^100 * 1/2^25) => (v * 2^25) * (1/2^125)

				// Shift Left, adding zeros to the Least Significant end. If there are not enough leading zeros then the result will overflow.

				//if (shiftAmount < 31)
				//{
				//	Debug.WriteLine($"ConvertExp is having to decrease the exponent by more than 1 whole limb.");
				//}

				var sResult = ScaleAndSplit(mantissa, shiftAmount, "Convert Exp");
				result = CopyLastXElements(sResult, LimbCount);
			}
			else
			{
				// For example target is -125, and currently we have -160
				// -125 - -160 = 35
				// Multiply coefficient by 1/2^35 and exponent by 2^35
				// (v) * (1/2^160) => (v * 1/2^35) * (1/2^160 * 2^35)  => (v * 1/2^35) * (1/2^125)

				// Shift Right, adding zeros to the Most Significant end, if there are too many leading zeros then this will cause loss of precision.

				result = new ulong[LimbCount];

				(var limbOffset, var remainder) = Math.DivRem(-1 * shiftAmount, EFFECTIVE_BITS_PER_LIMB);

				//var sourceLimbPtr = mantissa.Length - LimbCount;
				var sourceLimbPtr = logicalLength - LimbCount;
				sourceLimbPtr += limbOffset;
				sourceLimbPtr = Math.Max(sourceLimbPtr, 0);

				var resultLimbPtr = 0;
				var resultAdjLen = result.Length - limbOffset;

				while (resultLimbPtr < resultAdjLen - 1 && sourceLimbPtr < mantissa.Length - 1)
				{
					// discard (remainder count) source bits off the lsb end, leaving (32 - remainder) source bits on the low end, and zeros for the first remainder count msbs.
					var hx = mantissa[sourceLimbPtr] >> remainder;
					result[resultLimbPtr] |= hx;
					sourceLimbPtr++;
					var lx = (mantissa[sourceLimbPtr] << 64 - remainder) >> 32; // Push remainder count bits back into the high part of the Partial Word limb
					result[resultLimbPtr] |= lx;

					resultLimbPtr++;
				}

				if (sourceLimbPtr < mantissa.Length)
				{
					var hx2 = mantissa[sourceLimbPtr] >> remainder;
					result[resultLimbPtr] |= hx2;
					sourceLimbPtr++;

					if (sourceLimbPtr < mantissa.Length)
					{
						var lx2 = (mantissa[sourceLimbPtr] << 64 - remainder) >> 32;
						result[resultLimbPtr] |= lx2;
					}
				}
			}

			return result;
		}

		public Smx Convert(SmxSa smxSa)
		{
			if (smxSa.LimbCount != LimbCount)
			{
				throw new ArgumentException($"While converting an SmxSa found it to have {smxSa.LimbCount} limbs instead of {LimbCount}.");
			}
			var mantissa = smxSa.Materialize();

			// TODO: Is Normalize required here?
			if (mantissa.Length < LimbCount)
			{
				mantissa = Extend(mantissa, LimbCount);
			}

			var result = new Smx(smxSa.Sign, mantissa, smxSa.Exponent, BitsBeforeBP, smxSa.Precision);

			return result;
		}

		public SmxSa Convert(Smx smx)
		{
			var nrmMantissa = ConvertExp(smx.Mantissa, smx.Exponent, out var nrmExponent);

			SmxSa result;

			if (nrmMantissa.Length > LimbCount)
			{
				result = Convert(CreateNewMaxIntegerSmx());
			}
			else
			{
				var indexOfLastNonZeroLimb = GetIndexOfLastNonZeroLimb(nrmMantissa);
				result = new SmxSa(smx.Sign, nrmMantissa, indexOfLastNonZeroLimb, nrmExponent, smx.Precision);
				CheckForceExpResult(result, "Convert to SmxSa");
			}

			return result;
		}

		private ulong[] ScaleAndSplit(ulong[] mantissa, int power, string desc)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

			if (limbOffset > LimbCount + 3)
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

		public Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(true, new ulong[LimbCount], TargetExponent, BitsBeforeBP, precision);
			return result;
		}

		public Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			// TODO: Create a Static Readonly value and the use Clone to make copies
			var rValue = new RValue(MaxIntegerValue, 0, precision);
			var result = ScalarMathHelper.CreateSmx(rValue, ApFixedPointFormat);
			return result;
		}

		public ulong[] NormalizeFPV(ulong[] mantissa, int indexOfLastNonZeroLimb, int exponent, int precision, out int nrmExponent)
		{
			ValidateIsSplit(mantissa);
			Debug.Assert(indexOfLastNonZeroLimb >= -1, "indexOfLastNonZeroLimb should >= -1.");
			DblChkIndexOfLastNonZeroLimb(indexOfLastNonZeroLimb, mantissa);

			if (indexOfLastNonZeroLimb < 1)
			{
				nrmExponent = exponent;

				if (mantissa.Length > LimbCount)
				{
					var trimmedMantissa = CopyFirstXElements(mantissa, LimbCount);
					return trimmedMantissa;
				}
				else
				{
					return mantissa;
				}
			}

			if (mantissa.Length <= LimbCount)
			{
				nrmExponent = exponent;
				return mantissa;
			}

			var numSignificantDigits = GetNumberOfSignificantDigits(mantissa, LimbCount);
			var additionalSignificantDigitsDesired = precision - numSignificantDigits;

			//var potentialShiftAmount = BitOperations.LeadingZeroCount(mantissa[indexOfLastNonZeroLimb]) - 32;
			//var exponentGap = exponent + ExponentTarget;
			//var shiftAmount = Math.Min(exponentGap, potentialShiftAmount);

			ulong[] result;

			int startIndex;
			var logicalLength = indexOfLastNonZeroLimb + 1;
			if (logicalLength > LimbCount)
			{
				//var startIndex = mantissa.Length - LimbCount;
				startIndex = logicalLength - LimbCount;

				result = new ulong[LimbCount];
				Array.Copy(mantissa, startIndex, result, 0, LimbCount);
			}
			else
			{
				//var startIndex = mantissa.Length - LimbCount;
				startIndex = 0;

				result = new ulong[logicalLength];
				Array.Copy(mantissa, startIndex, result, 0, logicalLength);
			}

			// Compensate for discarding startIndex number of LSB
			nrmExponent = exponent + 32 * startIndex;

			var potentialShiftAmount = BitOperations.LeadingZeroCount(mantissa[indexOfLastNonZeroLimb]) - 32;
			var exponentGap = exponent - TargetExponent;

			var shiftAmount = exponentGap > 0 ? Math.Min(exponentGap, potentialShiftAmount) : potentialShiftAmount;

			// Compensate for shifting towards the MSB (i.e., multiplying)
			nrmExponent -= shiftAmount;

			Debug.Assert(result[^1] != 0, "Normalize Result should not have 0 as the MSL.");

			// Fill x number of high-order bits having value zero within the low half of the MSB, creating range of zero value bits at the low-order end.
			result[^1] <<= shiftAmount;

			for (int i = result.Length - 2; i >= 0; i--)
			{
				// Fill the x amount of zero value bits at the low-order end of the previous digit
				// with the top x bits of this digit. The bits from this digit are multiplied by 2^shiftAmount and then divided by 2^32.
				result[i + 1] |= result[i] >> 32 - shiftAmount;

				// Set the top x bits of this value to zero and then multiply by 2^shiftAmount.
				result[i] = (result[i] << 32 + shiftAmount) >> 32;
			}

			// If there were more digits in the source mantissa than the number of digits being returned,
			// use the top x bits of that digit.
			if (startIndex > 0)
			{
				var preceedingDigit = mantissa[startIndex - 1];
				result[0] |= preceedingDigit >> 32 - shiftAmount;

				// Get the new value of the preceeding digit
				preceedingDigit = (preceedingDigit << 32 + shiftAmount) >> 32;

				// Round up, if the value of the next LS digit is >= 2^16.
				if (preceedingDigit >= HALF_DIGIT_VALUE)
				{
					var rndResult = (ulong[])result.Clone();
					result = Add1AndReSplit(rndResult, extendOnCarry: false);
				}
			}

			//if (CheckPWValues(result))
			//{
			//	throw new InvalidOperationException("NormalizeFPV is returning a value that is not normalized.");
			//}

			return result;
		}


		private ulong[] Round(ulong[] mantissa, int indexOfLastNonZeroLimb, int exponent, int limbCount, out int newExponent)
		{
			var result = new ulong[limbCount];

			var startIndex = indexOfLastNonZeroLimb + 1 - limbCount;

			if (startIndex < 0)
			{
				throw new ArgumentException("limbs must be greater or equal to the current number of limbs.");
			}

			Array.Copy(mantissa, startIndex, result, 0, Math.Min(limbCount, mantissa.Length));

			if (startIndex == 0)
			{
				newExponent = exponent;
				return result;
			}
			else
			{
				Debug.Assert(result[^1] != 0, "The MSL should not be zero upon round.");

				newExponent = exponent + 32 * startIndex;

				if (mantissa[startIndex - 1] >= HALF_DIGIT_VALUE)
				{
					var rndResult = Add1AndReSplit(result, extendOnCarry: false);
					return rndResult;
				}
				else
				{
					return result;
				}
			}
		}

		private ulong[] Add1AndReSplit(ulong[] mantissa, bool extendOnCarry)
		{
			if (extendOnCarry)
			{
				throw new NotSupportedException();
			}

			var result = (ulong[])mantissa.Clone();
			result[0] += 1;
			result = ReSplit(result, out var carry);

			return result;
		}

		// TODO: Make AlignExponents Reduce as well as Scale. 
		private (SmxSa left, SmxSa right) AlignExponents(SmxSa a, SmxSa b, string desc, out int exponent)
		{
			if (a.Exponent == b.Exponent)
			{
				exponent = a.Exponent;
				return (a, b);
			}

			var diff = a.Exponent - b.Exponent;

			if (diff > 0)
			{
				// Multiply a's Mantissa by 2^Diff, and reduce a's exponent by Diff so that a's exponent is equal to b's exponent.

				// The limbOffset is the number of 'digits' that a could be shifted left (zeros are added to the least significant end, existing digits move toward the most significant end.)
				// in order to have the MSB of both a and b match given that a is boosted by this offset.
				// Since a has not been shifted left, as b is added to a, this offset should be added to the index used to address b's digits

				var newAMantissa = a.IsZero ? new ShiftedArray<ulong>() : ScaleAndSplit(a.MantissaSa, diff, desc);
				var normalizedA = new SmxSa(a.Sign, newAMantissa, b.Exponent, a.Precision);
				//CheckScaleResult("a", a, normalizedA);

				// The first limbOffset digits are only present on b (result.right), they are missing from a (result.left.)
				exponent = b.Exponent;
				return (normalizedA, b);
			}
			else
			{
				// Multiply b's Mantissa by 2^Diff, and reduce b's exponent by Diff so that b's exponent is equal to a's exponent.

				var newBMantissa = b.IsZero ? new ShiftedArray<ulong>() : ScaleAndSplit(b.MantissaSa, diff * -1, desc);
				var normalizedB = new SmxSa(b.Sign, newBMantissa, a.Exponent, b.Precision);
				//CheckScaleResult("b", b, normalizedB);

				// The first limbOffset digits are only present on a (result.right), they are missing from b (result.left.)
				exponent = a.Exponent;
				return (a, normalizedB);
			}
		}

		[Conditional("DEBUG")]
		private void CheckScaleResult(string parameterName, SmxSa a, SmxSa normalizedA)
		{
			if (!AreClose(a, normalizedA))
			{
				Debug.WriteLine($"After ScalingAndSplitting paramerter {parameterName}: {a.GetStringValue()} and {normalizedA.GetStringValue()} are not close.");
				//throw new InvalidOperationException("The normalized version of A is not close.");
			}
		}

		private bool AreClose(SmxSa left, SmxSa right)
		{
			var leftRValue = left.GetRValue();
			var rightRValue = right.GetRValue();
			var result = RValueHelper.AreClose(leftRValue, rightRValue, failOnTooFewDigits: false);

			return result;
		}

		private ShiftedArray<ulong> ScaleAndSplit(ShiftedArray<ulong> mantissa, int power, string desc)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			(var limbOffset, var remainder) = Math.DivRem(power, EFFECTIVE_BITS_PER_LIMB);

			if (limbOffset > LimbCount + 3)
			{
				return new ShiftedArray<ulong>();
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

			var result = new ShiftedArray<ulong>(resultArray, limbOffset, indexOfLastNonZeroLimb);

			if (carry > 0)
			{
				//Debug.WriteLine($"While {desc}, setting carry: {carry}, ll: {result.IndexOfLastNonZeroLimb}, len: {result.Length}, power: {power}, factor: {factor}.");
				result.SetCarry(carry);
			}

			return result;
		}

		#endregion

		#region Partial Word Support

		private void ExtendLimbs(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		{
			//if (Math.Abs(left.Length - right.Length) > LimbCount * 3 + 1)
			//{
			//	Debug.WriteLine($"Left and Right have significant difference in lengths.");
			//}

			if (left.Length > right.Length)
			{
				right.Extension += left.Length - right.Length;
			}
			else
			{
				if (right.Length > left.Length)
				{
					left.Extension += right.Length - left.Length;
				}
			}

			//if (left.Length > LimbCount * 4)
			//{
			//	Debug.WriteLine($"With the exponents aligned, the value now have {left.Length} limbs.");
			//}
		}

		// Pad with leading zeros.
		private ulong[] Extend(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, values.Length);

			return result;
		}

		private ulong[] CopyFirstXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, newLength);

			return result;
		}

		private ulong[] CopyLastXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];

			var startIndex = Math.Max(values.Length - newLength, 0);

			var cLen = values.Length - startIndex;

			Array.Copy(values, startIndex, result, 0, cLen);

			return result;
		}

		private int GetLogicalLength(Smx a)
		{
			var result = 1 + GetIndexOfLastNonZeroLimb(a.Mantissa);
			return result;
		}

		private int GetLogicalLength(ulong[] mantissa)
		{
			var result = 1 + GetIndexOfLastNonZeroLimb(mantissa);
			return result;
		}

		public int GetIndexOfLastNonZeroLimb(ulong[] mantissa)
		{
			var i = mantissa.Length - 1;
			for (; i >= 0; i--)
			{
				if (mantissa[i] != 0)
				{
					break;
				}
			}

			return i;
		}

		private int GetNumberOfSignificantDigits(ulong[] mantissa, int numberOfPlacesToInspect)
		{
			Debug.Assert(numberOfPlacesToInspect > 0, "The numberOfPlacesToInspect should be > 0.");
			var result = 0;

			var ptr = mantissa.Length;

			while (--ptr >= 0 && numberOfPlacesToInspect > 0)
			{
				var limb = mantissa[ptr];
				if (limb == 0)
				{
					continue;
				}

				var lz = BitOperations.LeadingZeroCount(mantissa[ptr]);
				Debug.Assert(lz >= 32);
				result += 64 - lz;
				numberOfPlacesToInspect--;
			}

			return result;
		}

		[Conditional("DEBUG")]
		private void DblChkIndexOfLastNonZeroLimb(int valueToCheck, ulong[] mantissa)
		{
			var realVal = GetIndexOfLastNonZeroLimb(mantissa);

			if (valueToCheck != realVal)
			{
				throw new InvalidOperationException($"DblChkIndexOfLastNonZeroLimb failed.");
			}
		}

		[Conditional("DEBUG")]
		private void ValidateIsSplit(ulong[] mantissa)
		{
			if (CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckForceExpResult(Smx smx, string desc)
		{
			if (smx.Mantissa.Length > LimbCount)
			{
				throw new InvalidOperationException($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs. Desc: {desc}.");
				//Debug.WriteLine($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs.");
			}
		}

		public static bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
		}



		[Conditional("DEBUG")]
		private void CheckForceExpResult(SmxSa smx, string desc)
		{
			if (smx.MantissaSa.Length > LimbCount)
			{
				throw new InvalidOperationException($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs. Desc: {desc}.");
				//Debug.WriteLine($"The value {smx.GetStringValue()}({smx}) is too large to fit within {LimbCount} limbs.");
			}
		}

		#endregion

		#region Comparison

		private int Compare(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
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

		public bool IsGreaterOrEqThan(Smx a, uint b)
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

		#endregion

		#region Split

		private ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & HIGH_MASK; // Create new ulong from bits 0 - 31.
		}

		private ulong[] ReSplit(ulong[] splitValues, out ulong carry)
		{
			var result = new ulong[splitValues.Length];
			carry = 0ul;

			for (int i = 0; i < splitValues.Length; i++)
			{
				var lo = Split(splitValues[i] + carry, out var hi);  // :Spliter
				result[i] = lo;
				carry = hi;
			}

			return result;
		}

		#endregion

		#region To String Support

		public string GetDiagDisplay(string name, ulong[] values, int stride)
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

		private static string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
		}

		#endregion
	}
}
