using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public class FPMathHelper
	{
		#region Constants

		private const int BITS_PER_LIMB = 32;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);
		private static readonly ulong HALF_DIGIT_VALUE = (ulong)Math.Pow(2, 16);

		// Integer used to convert BigIntegers to/from array of ulongs.
		private static readonly BigInteger BI_ULONG_FACTOR = BigInteger.Pow(2, 64);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-words
		private static readonly BigInteger BI_UINT_FACTOR = BigInteger.Pow(2, 32);

		// Integer used to pack a pair of ulong values into a single ulong.
		private static readonly ulong UL_UINT_FACTOR = (ulong)Math.Pow(2, 32);

		private static readonly ulong LOW_MASK =    0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		#endregion

		#region Constructor

		public FPMathHelper(ApFixedPointFormat apFixedPointFormat, uint thresold)
		{
			ApFixedPointFormat = GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			LimbCount = GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = (uint) Math.Pow(2, BitsBeforeBP) - 1;

			ThresholdMsl = SmxHelper.GetThreshold(thresold, TargetExponent, LimbCount, ApFixedPointFormat.BitsBeforeBinaryPoint);
		}

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

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; init; }
		public int TargetExponent { get; init; }

		public uint MaxIntegerValue { get; init; }
		public ulong ThresholdMsl { get; init; }

		public int BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }

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

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = ShiftAndTrim(mantissa);

			var sign = a.Sign == b.Sign;
			var precision = Math.Min(a.Precision, b.Precision);
			var bitsBeforeBP = a.BitsBeforeBP;
			Smx result = new Smx(sign, nrmMantissa, TargetExponent, precision, bitsBeforeBP);

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

					var lo = Split(product, out var hi);        //  2 x 2						3 x 3										4 x 4
					mantissa[resultPtr] += lo;              // 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6 
					mantissa[resultPtr + 1] += hi;          // 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7
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

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = ShiftAndTrim(mantissa);

			var sign = true;
			var precision = a.Precision;
			var bitsBeforeBP = a.BitsBeforeBP;
			Smx result = new Smx(sign, nrmMantissa, TargetExponent, precision, bitsBeforeBP);

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
					//if (i < j) continue;

					var product = ax[j] * ax[i];

					if (i > j)
					{
						product *= 2;
					}
															// j = 
					var resultPtr = j + i;					// 0, 1		1, 2		0, 1, 2		1, 2, 3, 
					var lo = Split(product, out var hi);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		/* What partial product gets added to which bin

			//  2 limbs						3 limbs										4 limbs

			j = 0, i = 0, 1			j = 0, i = 0, 1, 2		j = 0, i = 0, 1, 2, 3
			j = 1, i = 1			j = 1, i = 1, 2			j = 1, i = 1, 2, 3
									j = 2, i = 2,			j = 2, i = 2, 3
															j = 3, i = 3

			//    d				   d  d		   d			   d  d  d		   d  d		   d
			// 0, 1		2		0, 1, 2		2, 3	4		0, 1, 2, 3		2, 3, 4		4, 5	6	-> (Index C)
			// 1, 2		3       1, 2, 3		3, 4	5       1, 2, 3, 4		3, 4, 5		5, 6	7	-> (Index C + 1)

		 */

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

			result = new Smx(sign, mantissa, a.Exponent, a.Precision, a.BitsBeforeBP);

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

				var lo = Split(product, out var hi);	//		2 x 1			3 x 1			4 x 1
				mantissa[j] += lo;			            //			0, 1			0, 1, 2			0, 1, 2, 3
				mantissa[j + 1] += hi;			        //			1, 2			1, 2, 3			1, 2, 3, 4
			}

			var product2 = ax[^1] * b;
			var lo2 = Split(product2, out var hi2);

			mantissa[^1] = lo2;

			if (hi2 != 0)
			{
				throw new OverflowException($"Multiply {SmxMathHelper.GetDiagDisplay("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
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

		/* What partial product gets added to which bin

			//  2 x 2						3 x 3										4 x 4
			// 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6	-> (Index C)
			// 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7  -> (Index C + 1)

				2 x 2
			index a index b	index c	on, or below the diagonal 
			0		0		0		D		E
			0		1		1		B		E
			1		0		1		A		S
			1		1		2		D		E

				3 x 3
			0		0		0		D		E
			0		1		1		B		E
			0		2		2		B		E

			1		0		1		A		S
			1		1		2		D		E
			1		2		3		B		E

			2		0		2		A		S
			2		1		3		A		S
			2		2		4		D		E

				4 x 4
			0		0		0		D		E
			0		1		1		B		E
			0		2		2		B		E	**
			0		3		3		B		E

			1		0		1		A		S
			1		1		2		D		E
			1		2		3		B		E
			1		3		4		B		E

			2		0		2		A		S	**
			2		1		3		A		S
			2		2		4		D		E
			2		3		5		B		E

			3		0		3		A		S
			3		1		4		A		S
			3		2		5		A		S
			3		3		6		D		E



		*/

		#endregion

		#region Add and Subtract

		public Smx Sub(Smx a, Smx b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new Smx(!b.Sign, b.Mantissa, b.Exponent, b.Precision, b.BitsBeforeBP);

			if (a.IsZero)
			{
				return bNegated;
			}

			var result = Add(a, bNegated, desc);

			return result;
		}

		public Smx Add(Smx a, Smx b, string desc)
		{
			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
				throw new InvalidOperationException($"The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.LimbCount != LimbCount)
			{
				Debug.WriteLine($"WARNING: The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
				throw new InvalidOperationException($"The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
			}

			if (a.Exponent != b.Exponent)
			{
				Debug.WriteLine($"Warning:the exponents do not match.");
				throw new InvalidOperationException($"The exponents do not match.");
			}

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

			var carry = 0ul;

			if (a.Sign == b.Sign)
			{
				NumberOfMCarries++;
				sign = a.Sign;
				mantissa = Add(a.Mantissa, b.Mantissa, out indexOfLastNonZeroLimb, out carry);
			}
			else
			{
				NumberOfACarries++;
				var cmp = Compare(a.Mantissa, b.Mantissa);

				if (cmp >= 0)
				{
					sign = a.Sign;
					mantissa = Sub(a.Mantissa, b.Mantissa, out indexOfLastNonZeroLimb);
				}
				else
				{
					sign = b.Sign;
					mantissa = Sub(b.Mantissa, a.Mantissa, out indexOfLastNonZeroLimb);
				}
			}

			Smx result;

			if (carry != 0)
			{
				result = CreateNewMaxIntegerSmx();
				//NumberOfACarries++;
			}
			else
			{
				result = new Smx(sign, mantissa, a.Exponent, precision, a.BitsBeforeBP);
			}

			return result;
		}

		private ulong[] Add(ulong[] left, ulong[] right, out int indexOfLastNonZeroLimb, out ulong carry)
		{
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

		private ulong[] Sub(ulong[] left, ulong[] right, out int indexOfLastNonZeroLimb)
		{
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
					result[i] &= LOW_MASK;
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
				result[i] = (mantissa[sourceIndex] << 32 + shiftAmount) >> 32;	// Discard the top shiftAmount of bits.
				if (sourceIndex > 0)
				{
					result[i] |= (mantissa[sourceIndex - 1] >> 32 - shiftAmount); // Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
				}
				sourceIndex++;
			}
			return result;
		}

		public Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(true, new ulong[LimbCount], TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			// TODO: Create a Static Readonly value and the use Clone to make copies

			var rValue = new RValue(MaxIntegerValue, 0, precision);

			var result = SmxHelper.CreateSmx(rValue, TargetExponent, LimbCount, BitsBeforeBP);
			return result;
		}

		#endregion



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

		public bool IsGreaterOrEqThanThreshold(Smx a)
		{
			var left = a.Mantissa[^1];
			//var right = b * Math.Pow(2, 24);
			var right = ThresholdMsl;
			var result = left >= right;

			return result;
		}

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

		#region Split and Pack 

		private ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		#endregion

	}
}
