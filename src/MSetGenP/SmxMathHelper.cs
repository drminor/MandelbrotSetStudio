using Grpc.Core;
using MSS.Common;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public class SmxMathHelper
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

		public SmxMathHelper(int precision)
		{
			Precision = precision;
		}

		#endregion

		#region Public Properties

		private int _precision;
		public int Precision
		{
			get => _precision;
			set
			{
				_precision = value;
				LimbCount = GetLimbCount(_precision);
			}
		}

		public int LimbCount { get; private set; }

		#endregion

		#region Multiply and Square

		public Smx Multiply(Smx a, Smx b)
		{
			if (a.IsZero || b.IsZero)
			{
				return new Smx(0, 1, Math.Min(a.Precision, b.Precision));
			}

			var sign = a.Sign == b.Sign;
			var exponent = a.Exponent + b.Exponent;
			var precision = Math.Min(a.Precision, b.Precision);

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa, out var indexOfLastNonZeroLimb);
			var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

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

			var sign = true;
			var exponent = a.Exponent * 2;
			var precision = a.Precision;

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa, out var indexOfLastNonZeroLimb);
			var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		private ulong[] Square(ulong[] ax)
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

					var resultPtr = j + i;  // 0, 1, 1, 2
					var lo = Split(product, out var hi);
					mantissa[resultPtr] += lo;
					mantissa[resultPtr + 1] += hi;
				}
			}

			return mantissa;
		}

		public Smx Multiply(Smx a, int b)
		{
			if (a.IsZero || b == 0 )
			{
				return new Smx(0, 1, a.Precision);
			}

			var signOfB = b >= 0;
			var sign = a.Sign == signOfB;
			var exponent = a.Exponent;
			var precision = a.Precision;

			var rawMantissa = Multiply(a.Mantissa, (uint)Math.Abs(b));
			var mantissa = PropagateCarries(rawMantissa, out var indexOfLastNonZeroLimb);
			var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		public ulong[] Multiply(ulong[] ax, uint b)
		{
			//Debug.WriteLine(GetDiagDisplay("ax", ax));
			//Debug.WriteLine($"b = {b}");

			//var seive = new ulong[ax.Length];

			var mantissa = new ulong[ax.Length + 1];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				var product = ax[j] * b;
				//seive[j] = product;

				var lo = Split(product, out var hi);	//		2 x 1			3 x 1			4 x 1
				mantissa[j] += lo;			            //			0, 1			0, 1, 2			0, 1, 2, 3
				mantissa[j + 1] += hi;			        //			1, 2			1, 2, 3			1, 2, 3, 4
			}

			//var splitSieve = Split(seive);
			//Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, 2));
			//Debug.WriteLine(GetDiagDisplay("result", mantissa));

			return mantissa;
		}

		public ulong[] PropagateCarries(ulong[] mantissa, out int indexOfLastNonZeroLimb)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// Sometimes we need the LS-Limb. We are including
			// #Don't include the least significant limb, as this value will always be discarded as the result is rounded.

			// Remove all zero-valued leading limbs 
			// If the MSL produces a carry, throw an exception.

			var result = new ulong[mantissa.Length];

			indexOfLastNonZeroLimb = -1;
			var carry = 0ul;

			for (int i = 0; i < mantissa.Length; i++)
			{
				var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter
				result[i] = lo;

				if (lo > 0)
				{
					indexOfLastNonZeroLimb = i;
				}

				carry = hi;
			}

			if (carry != 0)
			{
				throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			}

			//if (indexOfLastNonZeroLimb < mantissa.Length - 1)
			//{
			//	// TODO: Update the ShiftedArrayClass to 'virtually' trim the leading zeros
			//	// Trim Leading Zeros
			//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//	return newResult;
			//}
			//else
			//{
			//	return result;
			//}

			return result;
		}

		/* What partial product gets added to which bin

			//  2 x 2						3 x 3										4 x 4
			// 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6 
			// 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7

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

		public SmxSa Sub(SmxSa a, SmxSa b)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new SmxSa(!b.Sign, b.Mantissa, b.Exponent, b.Precision);
			var result = Add(a, bNegated);

			return result;
		}

		public SmxSa Add(SmxSa a, SmxSa b)
		{
			if (b.IsZero)
			{
				//var trimmedA = TrimLeadingZeros(a);
				//return trimmedA;
				return a;
			}

			if (a.IsZero)
			{
				//var trimmedB = TrimLeadingZeros(b);
				//return trimmedB;
				return b;
			}

			bool sign;
			ulong[] mantissa;
			int indexOfLastNonZeroLimb;
			var precision = Math.Min(a.Precision, b.Precision);

			(var left, var right) = AlignExponents(a, b, out var exponent);

			if (right.IsZero)
			{
				return a;
			}

			if (left.IsZero)
			{
				return b;
			}

			ExtendLimbs(left.Mantissa, right.Mantissa);

			if (a.Sign == b.Sign)
			{
				sign = a.Sign;
				mantissa = Add(left.Mantissa, right.Mantissa, out indexOfLastNonZeroLimb);
			}
			else
			{
				var cmp = Compare(left.Mantissa, right.Mantissa);

				if (cmp >= 0)
				{
					sign = a.Sign;
					mantissa = Sub(left.Mantissa, right.Mantissa, out indexOfLastNonZeroLimb);
				}
				else
				{
					sign = b.Sign;
					mantissa = Sub(right.Mantissa, left.Mantissa, out indexOfLastNonZeroLimb);
				}

				//var result = new Smx(sign, mantissa, exponent, precision);
				//return result;
			}

			//if (mantissa.Length > LimbCount)
			//{
			//	var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, exponent, precision, out var nrmExponent);
			//	indexOfLastNonZeroLimb = GetNumberOfSignificantB32Digits(nrmMantissa) - 1;

			//	var result = new SmxSa(sign, nrmMantissa, indexOfLastNonZeroLimb, nrmExponent, precision);
			//	return result;
			//}
			//else
			//{
			//	var result = new SmxSa(sign, mantissa, indexOfLastNonZeroLimb, exponent, precision);
			//	return result;
			//}

			var result = new SmxSa(sign, mantissa, indexOfLastNonZeroLimb, exponent, precision);
			return result;
		}

		private ulong[] Add(ShiftedArray<ulong> left, ShiftedArray<ulong> right, out int indexOfLastNonZeroLimb)
		{
			//Debug.Assert(left.Length == right.Length);
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var resultLength = left.Length;
			var result = new ulong[resultLength + 1];

			indexOfLastNonZeroLimb = -1;
			var carry = 0ul;

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

			//// TODO: return carry and let the caller use this value when calling NormalizeFPV.
			//if (carry != 0)
			//{
			//	// Add a Limb
			//	var newResult = Extend(result, resultLength + 1);
			//	newResult[^1] = carry;
			//	return newResult;
			//}
			//else if (indexOfLastNonZeroLimb < resultLength - 1)
			//{
			//	// Trim leading zeros
			//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//	return newResult;
			//}
			//else
			//{
			//	return result;
			//}

			if (carry > 0)
			{
				result[resultLength] = carry;
				indexOfLastNonZeroLimb = resultLength;
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
				throw new OverflowException("MSB too small.");
			}

			result[^1] = left[^1] - right[^1] - borrow;

			//if (result[^1] == 0 && indexOfLastNonZeroLimb < resultLength - 1)
			//{
			//	// TODO: Update the ShiftedArrayClass to 'virtually' trim the leading zeros
			//	// Trim Leading Zeros
			//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//	return newResult;
			//}
			//else
			//{
			//	return result;
			//}

			if (result[^1] > 0)
			{
				indexOfLastNonZeroLimb = resultLength - 1;
			}

			return result;
		}

		#endregion

		#region Split and Pack 

		private ulong[] Pack(ulong[] splitValues)
		{
			Debug.Assert(splitValues.Length % 2 == 0, "The array being split has a length that is not an even multiple of two.");	

			var result = new ulong[splitValues.Length / 2];

			for (int i = 0; i < splitValues.Length; i += 2)
			{
				result[i / 2] = splitValues[i] + UL_UINT_FACTOR * splitValues[i + 1];
			}

			return result;
		}

		// Values are ordered from least significant to most significant.
		private ulong[] Split(ulong[] packedValues)
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

		public ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		private ulong[] ReSplit(ulong[] mantissa/*, out ulong carry, out int indexOfLastNonZeroLimb*/) 
		{
			// To be used after a addition or subtraction operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// Include the least significant limb.

			// Remove all zero-valued leading limbs 
			// If the MSL produces a carry, append an additional limb.

			var result = new ulong[mantissa.Length];

			//indexOfLastNonZeroLimb = -1;
			var carry = 0ul;

			for (int i = 0; i < mantissa.Length; i++)
			{
				var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter
				result[i] = lo;

				//if (lo > 0)
				//{
				//	indexOfLastNonZeroLimb = i;
				//}

				carry = hi;
			}

			//if (carry != 0)
			//{
			//	// Add a Limb
			//	var newResult = Extend(result, mantissa.Length + 1);
			//	newResult[^1] = carry;
			//	return newResult;
			//}
			//else if (indexOfLastNonZeroLimb < mantissa.Length - 1)
			//{
			//	// Trim Leading Zeros
			//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//	return newResult;
			//}
			//else
			//{
			//	return result;
			//}

			return result;
		}

		public static bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
		}

		public static bool CheckPWValues(ShiftedArray<ulong> shiftedArray)
		{
			var result = CheckPWValues(shiftedArray.Array) || shiftedArray.Carry >= MAX_DIGIT_VALUE;
			return result;
		}

		#endregion

		#region To String Support

		private string GetDiagDisplay(string name, ulong[] values, int stride)
		{
			var rowCnt = values.Length / stride;

			var sb = new StringBuilder();
			sb.AppendLine($"{name}:");

			for(int i = 0; i < rowCnt; i++)
			{
				var rowValues = new ulong[stride];

				Array.Copy(values, i * stride, rowValues, 0, stride);
				sb.AppendLine(GetDiagDisplay($"Row {i}", rowValues));
			}

			return sb.ToString();
		}

		private string GetDiagDisplay(string name, ulong[] values)
		{
			var strAry = GetStrArray(values);

			return $"{name}: {string.Join("; ", strAry)}";
		}

		private string GetHiLoDiagDisplay(string name, ulong[] values)
		{
			Debug.Assert(values.Length % 2 == 0, "GetHiLoDiagDisplay is being called with an array that has a length that is not an even multiple of two.");

			var strAry = GetStrArray(values);
			var pairs = new string[values.Length / 2];

			for (int i = 0; i < values.Length; i += 2)
			{
				pairs[i / 2] = $"{strAry[i]}, {strAry[i+1]}";
			}

			return $"{name}: {string.Join("; ", pairs)}";
		}

		private string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
		}

		#endregion

		#region To ULong Support

		public ulong[] ToULongs(BigInteger bi)
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

		public BigInteger FromULongs(ulong[] values)
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

			for (int i = values.Length - 1; i >= 0; i--)
			{
				result *= BI_UINT_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region Normalization Support

		public Smx Convert(SmxSa smxSa)
		{
			var mantissa = smxSa.Materialize();
			var indexOfLastNonZeroLimb = GetNumberOfSignificantB32Digits(mantissa) - 1;

			var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, smxSa.Exponent, smxSa.Precision, out var nrmExponent);
			return new Smx(smxSa.Sign, nrmMantissa, nrmExponent, smxSa.Precision);
		}

		public SmxSa Convert(Smx smx)
		{
			//var indexOfLastNonZeroLimb = GetNumberOfSignificantB32Digits(smx.Mantissa) - 1;
			var trimmedMantissa = TrimLeadingZeros(smx.Mantissa);
			var indexOfLastNonZeroLimb = trimmedMantissa.Length - 1;
			return new SmxSa(smx.Sign, smx.Mantissa, indexOfLastNonZeroLimb, smx.Exponent, smx.Precision);
		}

		[Conditional("DEBUG")]
		private void DblChkIndexOfLastNonZeroLimb(int valueToCheck, ulong[] mantissa)
		{
			var realVal = -1 + GetNumberOfSignificantB32Digits(mantissa);

			if (valueToCheck != realVal)
			{
				throw new InvalidOperationException($"DblChkIndexOfLastNonZeroLimb failed.");
			}
		}

		public ulong[] NormalizeFPV(ulong[] mantissa, int indexOfLastNonZeroLimb, int exponent, int precision, out int nrmExponent)
		{
			ValidateIsSplit(mantissa);

			Debug.Assert(indexOfLastNonZeroLimb >= -1, "indexOfLastNonZeroLimb should not be negative.");

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

			ulong[] result;
			int newExponent;

			if (additionalSignificantDigitsDesired <= 0)
			{
				result = Round(mantissa, indexOfLastNonZeroLimb, exponent, LimbCount, out newExponent);
			}
			else
			{
				var potentialShiftAmount = BitOperations.LeadingZeroCount(mantissa[indexOfLastNonZeroLimb]) - 32;
				var shiftAmount = Math.Min(additionalSignificantDigitsDesired, potentialShiftAmount);

				// Compensate for shifting towards the MSB (i.e., multiplying)
				newExponent = exponent - shiftAmount;

				//var startIndex = mantissa.Length - LimbCount;
				var startIndex = indexOfLastNonZeroLimb + 1 - LimbCount;

				result = new ulong[LimbCount];
				Array.Copy(mantissa, startIndex, result, 0, LimbCount);

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
					// Compensate for discarding startIndex number of LSB
					newExponent += 32 * startIndex;

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
			}

			if (CheckPWValues(result))
			{
				throw new InvalidOperationException("NormalizeFPV is returning a value that is not normalized.");
			}

			nrmExponent = newExponent;
			return result;
		}

		[Conditional("DEBUG")]
		private void ValidateIsSplit(ulong[] mantissa)
		{
			if (CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		public static int GetLimbCount(int precision)
		{
			var dResult = precision / 32d;
			var result = (int)Math.Ceiling(dResult);

			return result;
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
			result = ReSplit(result/*, out var carry, out var indexOfLastNonZeroLimb*/);

			//if (extendOnCarry && carry != 0)
			//{
			//	// Add a Limb
			//	var newResult = Extend(result, mantissa.Length + 1);
			//	newResult[^1] = carry;
			//	return newResult;
			//}
			//else
			//{
			//	if (indexOfLastNonZeroLimb < result.Length - 1)
			//	{
			//		// TODO: Update the ShiftedArrayClass to 'virtually' trim the leading zeros
			//		// Trim Leading Zeros
			//		var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//		return newResult;
			//	}
			//	else
			//	{
			//		return result;
			//	}
			//}

			return result;
		}

		// TODO: Make AlignExponents Reduce as well as Scale. 
		private (SmxSa left, SmxSa right) AlignExponents(SmxSa a, SmxSa b, out int exponent)
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

				var newAMantissa = ScaleAndSplit(a.Mantissa, diff);
				var normalizedA = new SmxSa(a.Sign, newAMantissa, b.Exponent, a.Precision);
				//CheckScaleResult("a", a, normalizedA);

				// The first limbOffset digits are only present on b (result.right), they are missing from a (result.left.)
				exponent = b.Exponent;
				return (normalizedA, b);
			}
			else
			{
				// Multiply b's Mantissa by 2^Diff, and reduce b's exponent by Diff so that b's exponent is equal to a's exponent.

				var newBMantissa = ScaleAndSplit(b.Mantissa, diff * -1);
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

		//var limbCountIncrease = newBMantissa.Length - b.Mantissa.Length;
		//if (limbCountIncrease > 0)
		//{
		//	Debug.WriteLine($"Adjusting exp for C by {diff}. Limb cnt increased by: {limbCountIncrease}.");
		//}
		//else
		//{
		//	Debug.WriteLine($"Adjusting exp for C by {diff}. Limb cnt stays at: {newBMantissa.Length}.");
		//}

		private ShiftedArray<ulong> ScaleAndSplit(ShiftedArray<ulong> values, int power)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			var indexOfLastNonZeroLimb = -1;
			if (!values.Array.Any(x => x > 0))
			{
				//limbOffset = 0;
				//return new ulong[] { 0 };

				//return values;
				return new ShiftedArray<ulong>();
			}

			(var limbOffset, var remainder) = Math.DivRem(power, BITS_PER_LIMB);

			if (limbOffset > LimbCount + 1)
			{
				return new ShiftedArray<ulong>();
			}

			var factor = (ulong)Math.Pow(2, remainder);

			var resultArray = new ulong[values.Length];

			var carry = 0ul;

			for (var i = 0; i < values.Length; i++)
			{
				var newLimbVal = values[i] * factor + carry;
				var lo = Split(newLimbVal, out carry); // :Spliter
				resultArray[i] = lo;

				if (lo > 0)
				{
					indexOfLastNonZeroLimb = i;
				}
			}

			//// TODO: have the result always allocate room for the carry
			//// And update the ShiftedArray to 'virtually' trim the leading zeros -- including the carry limb
			//if (carry != 0)
			//{
			//	// Add a Limb
			//	var newResult = Extend(resultArray, resultArray.Length + 1);
			//	newResult[^1] = carry;

			//	//return newResult;

			//	return new ShiftedArray<ulong>(newResult, qr.Quotient);
			//}
			//else if (indexOfLastNonZeroLimb < values.Length - 1)
			//{
			//	// Remove leading zeros
			//	var newResult = CopyFirstXElements(resultArray, indexOfLastNonZeroLimb + 1);
			//	//return newResult;
			//	return new ShiftedArray<ulong>(newResult, qr.Quotient);

			//}
			//else
			//{
			//	//return result;
			//	return new ShiftedArray<ulong>(resultArray, qr.Quotient);
			//}

			var result = new ShiftedArray<ulong>(resultArray, limbOffset, indexOfLastNonZeroLimb);

			if (carry > 0)
			{
				result.SetCarry(carry);
			}

			return result;
		}

		//private ulong[] ExtendLimbs(ulong[] ax, ulong[] bx, out ulong[] extendedBx)
		//{
		//	if (ax.Length == bx.Length)
		//	{
		//		extendedBx = bx;
		//		return ax;
		//	}
		//	else if (ax.Length < bx.Length)
		//	{
		//		extendedBx = bx;
		//		return Extend(ax, bx.Length);
		//	}
		//	else
		//	{
		//		extendedBx = Extend(bx, ax.Length);
		//		return ax;
		//	}
		//}

		private void ExtendLimbs(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		{
			if (Math.Abs(left.Length - right.Length) > 3)
			{
				Debug.WriteLine($"Left and Right have significant difference in lengths.");
			}

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

			if (left.Length > 10 || right.Length > 10)
			{
				Debug.WriteLine($"The left length is {left.Length} and the right length is {right.Length}.");
			}

		}

		//private ulong[] Extend(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];
		//	Array.Copy(values, 0, result, 0, values.Length);

		//	return result;
		//}

		private ulong[] CopyFirstXElements(ulong[] values, int newLength)
		{
			var result = new ulong[newLength];
			Array.Copy(values, 0, result, 0, newLength);

			return result;
		}

		//private Smx TrimLeadingZeros(Smx a)
		//{
		//	var mantissa = TrimLeadingZeros(a.Mantissa);
		//	var result = new Smx(a.Sign, mantissa, a.Exponent, a.Precision);
		//	return result;
		//}

		// Remove zero-valued limbs from the Most Significant end.
		// Leaving all least significant limbs intact.
		public ulong[] TrimLeadingZeros(ulong[] mantissa)
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

		//private ulong[] TrimTrailingZeros(ulong[] mantissa, int exponent, out int newExponent)
		//{
		//	newExponent = exponent;

		//	var i = 0;
		//	for (; i < mantissa.Length; i++)
		//	{
		//		if (mantissa[i] != 0)
		//		{
		//			break;
		//		}
		//	}

		//	if (i == 0)
		//	{
		//		return mantissa;
		//	}

		//	if (i == mantissa.Length)
		//	{
		//		// All digits are zero
		//		return new ulong[] { 0 };
		//	}

		//	var result = new ulong[mantissa.Length - i];
		//	Array.Copy(mantissa, i, result, 0, result.Length);

		//	newExponent += i * 32;

		//	return result;
		//}

		#endregion

		#region Reduce - Not Used

		private ulong[] Reduce(ulong[] mantissa, out int shiftAmount)
		{
			shiftAmount = 0;

			if (AreComponentsEven(mantissa))
			{
				shiftAmount++;
				var w = new ulong[mantissa.Length];

				for (int i = 0; i < w.Length; i++)
				{
					w[i] = mantissa[i] / 2;
				}

				while (AreComponentsEven(w))
				{
					shiftAmount++;

					for (int i = 0; i < w.Length; i++)
					{
						w[i] = w[i] / 2;
					}
				}

				return w;
			}
			else
			{
				return mantissa;
			}
		}

		private bool AreComponentsEven(ulong[] mantissa)
		{
			foreach (var value in mantissa)
			{
				if (value % 2 != 0)
				{
					return false;
				}
			}

			return true;
		}

		private int GetWholeExp(ulong[] mantissa)
		{
			var result = 0;

			for(var i = 0; i < mantissa.Length; i++)
			{
				if (mantissa[i] == 0)
				{
					result += 32;
				}
				else
				{
					var we = GetWholeExp(mantissa[i]);

					if (we > 0)
					{
						result += we;
						return result;
					}
					else
					{
						result -= we;
					}
				}
			}

			result *= -1;
			return result;
		}

		private int GetWholeExp(ulong limb)
		{
			var result = 0;

			while(limb > 0 && limb % 2 == 0)
			{
				limb <<= 1;
				result++;
			}

			if (limb == 0)
			{
				result *= -1;
			}

			return result;
		}

		#endregion

		#region Comparison

		private int Compare(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		{
			//var sdA = GetNumberOfSignificantB32Digits(left);
			//var sdB = GetNumberOfSignificantB32Digits(right);

			//var numberOfSignificantDigitsA = 1 + left.IndexOfLastNonZeroLimb;
			//var numberOfSignificantDigitsB = 1 + right.IndexOfLastNonZeroLimb;

			//if (numberOfSignificantDigitsA != numberOfSignificantDigitsB)
			//{
			//	return numberOfSignificantDigitsA > numberOfSignificantDigitsB ? 1 : -1;
			//}

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

		private int GetNumberOfSignificantB32Digits(ulong[] mantissa)
		{
			var i = mantissa.Length;
			for (; i > 0; i--)
			{
				if (mantissa[i - 1] != 0)
				{
					break;
				}
			}

			return i;
		}

		public bool IsGreaterOrEqThan(Smx a, uint b)
		{
			var aAsDouble = 0d;

			for(var i = a.Mantissa.Length - 1; i >= 0; i--)
			{
				aAsDouble += a.Mantissa[i] * Math.Pow(2, a.Exponent + (i * 32));

				if (aAsDouble >= b)
				{
					return true;
				}
			}

			return false;
		}

		public bool AreClose(SmxSa left, SmxSa right)
		{
			var leftRValue = left.GetRValue();
			var rightRValue = right.GetRValue();
			var result = RValueHelper.AreClose(leftRValue, rightRValue, failOnTooFewDigits: false);

			return result;
		}

		//var areClose = RValueHelper.AreClose(bSmxRValue, bRValue);


		#endregion

		#region Map Generartion Support

		public Smx[] BuildSamplePoints(Smx startValue, Smx[] samplePointOffsets)
		{
			var result = new Smx[samplePointOffsets.Length];
			var startValueSa = Convert(startValue);

			for (var i = 0; i < samplePointOffsets.Length; i++)
			{
				var samplePointOffsetSa = Convert(samplePointOffsets[i]);
				var samplePointSa = Add(startValueSa, samplePointOffsetSa);

				result[i] = Convert(samplePointSa);
			}

			return result;
		}

		public Smx[] BuildSamplePointOffsets(Smx delta, int sampleCount)
		{
			var result = new Smx[sampleCount];

			for (var i = 0; i < sampleCount; i++)
			{
				result[i] = Multiply(delta, i);
			}

			return result;
		}

		public Smx CreateSmxFromDto(long[] values, int exponent, int precision)
		{
			var sign = !values.Any(x => x < 0);
			var mantissa = ConvertDtoLongsToSmxULongs(values, out var indexOfLastNonZeroLimb);
			DblChkIndexOfLastNonZeroLimb(indexOfLastNonZeroLimb, mantissa);
			var nrmMantissa = NormalizeFPV(mantissa, indexOfLastNonZeroLimb, exponent, precision, out var nrmExponent);
			var result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		public ulong[] ConvertDtoLongsToSmxULongs(long[] values, out int indexOfLastNonZeroLimb)
		{
			// DtoLongs are in Big-Endian order, convert to Little-Endian order.
			//var leValues = values.Reverse().ToArray();

			// Currently the Dto classes produce an array of longs with length of either 1 or 2.

			var leValues = TrimLeadingZerosBigEndian(values);

			if (leValues.Length > 1)
			{
				throw new NotSupportedException("ConvertDtoLongsToSmxULongs only supports values with a single 'digit.'");
			}

			indexOfLastNonZeroLimb = -1;

			var result = new ulong[leValues.Length * 2];

			for (int i = 0; i < leValues.Length; i++)
			{
				var value = (ulong)Math.Abs(leValues[i]);
				var lo = Split(value, out var hi);
				result[2 * i] = lo;
				result[2 * i + 1] = hi;

				if (hi > 0)
				{
					indexOfLastNonZeroLimb = i + 1;
				}
				else
				{
					if (lo > 0)
					{
						indexOfLastNonZeroLimb = i;
					}
				}

			}

			//var trResult = TrimLeadingZeros(result);
			//return trResult;

			return result;
		}

		// Trim Leading Zeros for a Big-Endian formatted array of longs.
		private long[] TrimLeadingZerosBigEndian(long[] mantissa)
		{
			var i = 0;
			for (; i < mantissa.Length; i++)
			{
				if (mantissa[i] != 0)
				{
					break;
				}
			}

			if (i == 0)
			{
				return mantissa;
			}

			if (i == mantissa.Length)
			{
				// All digits are zero
				return new long[] { 0 };
			}

			var result = new long[mantissa.Length - i];
			Array.Copy(mantissa, i, result, 0, result.Length);
			return result;
		}

		#endregion
	}
}
