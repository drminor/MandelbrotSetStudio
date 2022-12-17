using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenP
{
	public class FPMathHelper
	{
		#region Constants

		private static readonly ulong LOW_MASK =		0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private static readonly ulong LOW_MASK_SIGNED = 0x000000007FFFFFFF; // bits 0 - 30 are set.

		#endregion

		#region Constructor

		public FPMathHelper(ApFixedPointFormat apFixedPointFormat, uint thresold)
		{
			ApFixedPointFormat = SmxHelper.GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			LimbCount = SmxHelper.GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = SmxHelper.GetMaxSignedIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint);

			Threshold = thresold;
			ThresholdMsl = SmxHelper.GetThresholdMsl(thresold, TargetExponent, LimbCount, ApFixedPointFormat.BitsBeforeBinaryPoint);
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public int LimbCount { get; init; }
		public int TargetExponent { get; init; }

		public int MaxIntegerValue { get; init; }
		public uint Threshold { get; init; }
		public ulong ThresholdMsl { get; init; }

		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }

		#endregion

		#region Multiply and Square

		public Smx2C Multiply(Smx2C a, Smx2C b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroSmx2C(Math.Min(a.Precision, b.Precision));
			}

			CheckLimbs(a, b, "Multiply");

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
				var nrmMantissa = ShiftAndTrim(mantissa, out var overflowed);
				if (overflowed)
				{
					NumberOfMCarries++;
					result = CreateNewMaxIntegerSmx2C(precision);
				}
				else
				{
					result = BuildSmx2C(nrmMantissa, precision);
				}
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

					var lo = Split(product, out var hi);        //  2 x 2						3 x 3										4 x 4
					mantissa[resultPtr] += lo;              // 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6 
					mantissa[resultPtr + 1] += hi;          // 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7
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

			CheckLimb(a, "Square");

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
				var nrmMantissa = ShiftAndTrim(mantissa, out var overflowed);
				if (overflowed)
				{
					NumberOfMCarries++;
					result = CreateNewMaxIntegerSmx2C(precision);
				}
				else
				{
					result = BuildSmx2C(nrmMantissa, precision);
				}
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

					var product = ax[j] * ax[i];

					if (i > j)
					{
						product *= 2;
					}

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

		public Smx2C Multiply(Smx2C a, int b)
		{
			Smx2C result;

			if (a.IsZero || b == 0)
			{
				result = CreateNewZeroSmx2C(a.Precision);
				return result;
			}

			CheckLimb(a, "MultiplyByInt");

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
				var nrmMantissa = ShiftAndTrim(mantissa, out var overflowed);
				if (overflowed)
				{
					NumberOfMCarries++;
					result = CreateNewMaxIntegerSmx2C(precision);
				}
				else
				{
					result = BuildSmx2C(nrmMantissa, precision);
				}
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

				var lo = Split(product, out var hi);    //		2 x 1			3 x 1			4 x 1
				mantissa[j] += lo;                      //			0, 1			0, 1, 2			0, 1, 2, 3
				mantissa[j + 1] += hi;                  //			1, 2			1, 2, 3			1, 2, 3, 4
			}

			var product2 = ax[^1] * b;
			var lo2 = Split(product2, out var hi2);

			mantissa[^1] = lo2;

			if (hi2 != 0)
			{
				throw new OverflowException($"Multiply {SmxHelper.GetDiagDisplayHex("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
			}

			return mantissa;
		}

		public ulong[] PropagateCarries(ulong[] mantissa, out ulong carry)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var result = new ulong[mantissa.Length];
			carry = 0ul;

			for (int i = 0; i < mantissa.Length - 1; i++)
			{
				var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter
				result[i] = lo;
				carry = hi;
			}

			var lo2 = SplitSignedLimb(mantissa[^1] + carry, out var hi2);  // :Spliter
			result[^1] = lo2;
			carry = hi2;

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

		public Smx2C Sub(Smx2C a, Smx2C b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = SmxHelper.Negate(b);

			if (a.IsZero)
			{
				return bNegated;
			}

			var result = Add(a, bNegated, desc);

			return result;
		}

		public Smx2C Add(Smx2C a, Smx2C b, string desc)
		{
			CheckLimbs(a, b, desc);
			if (b.IsZero) return a;
			if (a.IsZero) return b;

			var precision = Math.Min(a.Precision, b.Precision);
			var mantissa = Add(a.Mantissa, b.Mantissa, out var carry);

			Smx2C result;

			if (carry != 0)
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

			//ulong nv;
			for (var i = 0; i < resultLength - 1; i++)
			{
				var nv = left[i] + right[i] + carry;
				//Debug.Write($"Adding {left[i]}, {right[i]} wc:{carry} -> {nv}, split: ");
				var lo = Split(nv, out carry);
				//Debug.WriteLine($"hi:{carry}, lo:{lo}");
				result[i] = lo;
			}

			var nv2 = left[^1] + right[^1] + carry;
			//Debug.Write($"AddingLL {left[^1]}, {right[^1]} wc:{carry} -> {nv2}, split: ");
			var lo2 = SplitSignedLimb(nv2, out carry);
			//Debug.WriteLine($"hi:{carry}, lo:{lo2}");
			result[^1] = lo2;

			return result;
		}

		private void CheckLimbs(Smx2C a, Smx2C b, string desc)
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
		}

		private void CheckLimb(Smx2C a, string desc)
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
		}

		private Smx2C BuildSmx2C(ulong[] partialWordLimbs, int precision)
		{
			var lzc = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);
			var firstBitIsAOne = lzc == 0;


			var result = new Smx2C(!firstBitIsAOne, partialWordLimbs, TargetExponent, precision, BitsBeforeBP);

			return result;
		}

		#endregion

		#region Normalization Support

		public ulong[] ShiftAndTrim(ulong[] mantissa, out bool overflowed)
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

			// Discard 1 more bit.
			// Start with 1:7:56 (Sign:Integer:Fraction 
			// Intermediate has 0:16:112
			// Push 8 from behind to in front and drop the least two significant limbs for a total of 64 - 8 = 56 bits from behind
			// Push 8 off the top, for a total of 64 bits discarded.
			// The result must be positive, so if the most significant bit is a '1', we know there is an overflow.

			var lzc = BitOperations.LeadingZeroCount(result[^1]);

			overflowed = lzc == 32;

			return result;
		}

		private ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		private ulong SplitSignedLimb(ulong x, out ulong hi)
		{
			hi = x >> 31; // Create new ulong from bits 31 - 63.
			return x & LOW_MASK_SIGNED; // Create new ulong from bits 0 - 30.
		}

		public Smx2C CreateNewZeroSmx2C(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx2C(true, new ulong[LimbCount], TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx2C CreateNewMaxIntegerSmx2C(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var rValue = new RValue(MaxIntegerValue, 0, precision);
			var tResult = SmxHelper.CreateSmx(rValue, TargetExponent, LimbCount, BitsBeforeBP);
			var result = Convert(tResult);

			return result;
		}

		[Conditional("DEBUG")]
		private void ValidateIsSplit(ulong[] mantissa)
		{
			if (SmxHelper.CheckPW2CValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		#endregion

		#region Comparison

		public bool IsGreaterOrEqThanThreshold(Smx2C a)
		{
			var left = a.Mantissa[^1];

			Debug.Assert(BitOperations.LeadingZeroCount(left) > 32, "IsGreaterOrEqThanThreshold found a limb with a negative mantissa.");
			Debug.Assert(a.Sign, "IsGreaterOrEqThanThreshold found a limb with a negative sign, but the mantissa is positive.");

			var right = ThresholdMsl;
			var result = left >= right;

			return result;
		}

		#endregion

		#region Conversion

		public Smx Convert(Smx2C smx2C)
		{
			var un2cMantissa = SmxHelper.ConvertFrom2C(smx2C.Mantissa, smx2C.Sign);
			var result = new Smx(smx2C.Sign, un2cMantissa, smx2C.Exponent, BitsBeforeBP, smx2C.Precision);
			return result;
		}

		public Smx2C Convert(Smx smx, bool overrideFormatChecks = false)
		{
			if (!overrideFormatChecks) CheckLimbCountAndFPFormat(smx);

			var twoCMantissa = SmxHelper.ConvertTo2C(smx.Mantissa, smx.Sign);
			var result = new Smx2C(smx.Sign, twoCMantissa, smx.Exponent, smx.Precision, BitsBeforeBP);

			return result;
		}

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

		#endregion
	}
}
