using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public class ScalarMath : IScalerMath
	{
		#region Constants

		private static readonly ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);
		private static readonly ulong HALF_DIGIT_VALUE = (ulong)Math.Pow(2, 16);

		private static readonly ulong HIGH_MASK = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		#endregion

		#region Constructor

		public ScalarMath(ApFixedPointFormat apFixedPointFormat, uint thresold)
		{
			ApFixedPointFormat = ScalarMathHelper.GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			Threshold = thresold;
			LimbCount = ScalarMathHelper.GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = (uint)Math.Pow(2, BitsBeforeBP) - 1;


			ThresholdMsl = ScalarMathHelper.GetThresholdMsl(thresold, TargetExponent, LimbCount, BitsBeforeBP);
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

		public Smx Multiply(Smx a, Smx b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroSmx(Math.Min(a.Precision, b.Precision));
			}

			CheckLimbs(a, b, "Multiply");

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = ShiftAndTrim(mantissa);

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

			CheckLimb(a, "Square");

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
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
					//if (i < j) continue;

					var product = ax[j] * ax[i];

					if (i > j)
					{
						product *= 2;
					}
					// j = 
					var resultPtr = j + i;                  // 0, 1		1, 2		0, 1, 2		1, 2, 3, 
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

				var lo = Split(product, out var hi);    //		2 x 1			3 x 1			4 x 1
				mantissa[j] += lo;                      //			0, 1			0, 1, 2			0, 1, 2, 3
				mantissa[j + 1] += hi;                  //			1, 2			1, 2, 3			1, 2, 3, 4
			}

			var product2 = ax[^1] * b;
			var lo2 = Split(product2, out var hi2);

			mantissa[^1] = lo2;

			if (hi2 != 0)
			{
				throw new OverflowException($"Multiply {ScalarMathHelper.GetDiagDisplay("ax", ax)} x {b} resulted in a overflow. The hi value is {hi2}.");
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
			int indexOfLastNonZeroLimb;
			var precision = Math.Min(a.Precision, b.Precision);

			var carry = 0ul;

			if (a.Sign == b.Sign)
			{
				//NumberOfMCarries++;
				sign = a.Sign;
				mantissa = Add(a.Mantissa, b.Mantissa, out indexOfLastNonZeroLimb, out carry);
			}
			else
			{
				//NumberOfACarries++;
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
				NumberOfACarries++;
			}
			else
			{
				result = new Smx(sign, mantissa, a.Exponent, a.BitsBeforeBP, precision);
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
				result[i] = (mantissa[sourceIndex] << 32 + shiftAmount) >> 32;  // Discard the top shiftAmount of bits.
				if (sourceIndex > 0)
				{
					result[i] |= (mantissa[sourceIndex - 1] >> 32 - shiftAmount); // Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
				}
				sourceIndex++;
			}

			//var sResult = ScaleAndSplit(mantissa, BitsBeforeBP, "Force Exp"); // Shift Bits towards the most significant end. After a multiply, the portion of the mantissa before the BP is doubled, this discards the high bits.
			//var tResult = TakeMostSignificantLimbs(sResult, LimbCount);
			//Debug.WriteLine($"ShiftAndTrim is returing {GetDiagDisplay("result", result)}, prev: {GetDiagDisplay("tResult", tResult)}");

			//return tResult;
			return result;
		}

		public Smx CreateSmx(RValue rValue)
		{
			var result = ScalarMathHelper.CreateSmx(rValue, TargetExponent, LimbCount, BitsBeforeBP);
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
			var result = ScalarMathHelper.CreateSmx(new RValue(MaxIntegerValue, 0, precision), TargetExponent, LimbCount, BitsBeforeBP);
			return result;
		}

		private ulong Split(ulong x, out ulong hi)
		{
			NumberOfSplits++;
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & HIGH_MASK; // Create new ulong from bits 0 - 31.
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

			ValidateIsSplit2C(a.Mantissa, a.Sign);
			ValidateIsSplit2C(b.Mantissa, b.Sign);
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

			ValidateIsSplit2C(a.Mantissa, a.Sign);
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

		private bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
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

		#region Map Generartion Support

		public FPValues BuildMapPoints(Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, out FPValues cIValues)
		{
			var stride = (byte)blockSize.Width;
			var samplePointOffsets = BuildSamplePointOffsets(delta, stride);
			var samplePointsX = BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = BuildSamplePoints(startingCy, samplePointOffsets);

			var resultLength = blockSize.NumberOfCells;

			var crSmxes = new Smx[resultLength];
			var ciSmxes = new Smx[resultLength];

			var resultPtr = 0;
			for (int j = 0; j < samplePointsY.Length; j++)
			{
				for (int i = 0; i < samplePointsX.Length; i++)
				{
					ciSmxes[resultPtr] = samplePointsY[j];
					crSmxes[resultPtr++] = samplePointsX[i];
				}
			}

			var result = new FPValues(crSmxes);
			cIValues = new FPValues(ciSmxes);

			return result;
		}

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
