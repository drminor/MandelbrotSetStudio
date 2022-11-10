using Grpc.Core;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public class SmxMathHelper
	{
		#region Constants

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
				Limbs = SmxMathHelper.GetLimbsCount(_precision);
			}
		}

		public int Limbs { get; private set; }

		#endregion

		#region Multiply and Square

		public static Smx Multiply(Smx a, Smx b)
		{
			if (a.IsZero || b.IsZero)
			{
				return new Smx(0, 1, Math.Min(a.Precision, b.Precision));
			}

			var sign = a.Sign == b.Sign;
			var exponent = a.Exponent + b.Exponent;
			var precision = Math.Min(a.Precision, b.Precision);

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		public static ulong[] Multiply(ulong[] ax, ulong[] bx)
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

		public static Smx Square(Smx a)
		{
			if (a.IsZero)
			{
				return a;
			}

			var sign = true;
			var exponent = a.Exponent * 2;
			var precision = a.Precision;

			var rawMantissa = Square(a.Mantissa);
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		public static ulong[] Square(ulong[] ax)
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

		public static Smx Multiply(Smx a, int b)
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
			var mantissa = PropagateCarries(rawMantissa);
			var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		public static ulong[] Multiply(ulong[] ax, uint b)
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

		public static ulong[] PropagateCarries(ulong[] mantissa)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// Sometimes we need the LS-Limb. We are including
			// #Don't include the least significant limb, as this value will always be discarded as the result is rounded.

			// Remove all zero-valued leading limbs 
			// If the MSL produces a carry, throw an exception.

			var result = new ulong[mantissa.Length];

			var indexOfLastNonZeroLimb = 0;
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

			if (indexOfLastNonZeroLimb < mantissa.Length - 1)
			{
				// Trim Leading Zeros
				var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

				return newResult;
			}
			else
			{
				return result;
			}
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

		public static Smx Sub(Smx a, Smx b)
		{
			var result = Sub(a, b, out _);
			return result;
		}

		public static Smx Sub(Smx a, Smx b, out Smx normalizedB)
		{
			if (b.IsZero)
			{
				normalizedB = b;
				return a;
			}

			var bNegated = new Smx(!b.Sign, b.Mantissa, b.Exponent, b.Precision);
			var result = Add(a, bNegated, out normalizedB);

			return result;
		}

		public static Smx Add(Smx a, Smx b)
		{
			var result = Add(a, b, out _);
			return result;
		}

		public static Smx Add(Smx a, Smx b, out Smx normalizedB)
		{
			if (b.IsZero)
			{
				normalizedB = b;
				var trimmedA = TrimLeadingZeros(a);
				return trimmedA;
			}

			if (a.IsZero)
			{
				var trimmedB = TrimLeadingZeros(b);
				normalizedB = trimmedB;
				return trimmedB;
			}

			var normalizedA = AlignExponents(a, b, out normalizedB);

			bool sign;
			ulong[] mantissa;
			var exponent = normalizedA.Exponent;
			var precision = Math.Min(a.Precision, b.Precision);

			if (a.Sign == b.Sign)
			{
				sign = a.Sign;
				var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
				mantissa = Add(aMantissa, bMantissa);

				if (mantissa.Length > aMantissa.Length)
				{
					var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
					var result = new Smx(sign, nrmMantissa, nrmExponent, precision);
					return result;
				}
				else
				{
					var result = new Smx(sign, mantissa, exponent, precision);
					return result;
				}
			}
			else
			{
				var cmp = Compare(normalizedA.Mantissa, normalizedB.Mantissa);

				if (cmp >= 0)
				{
					sign = a.Sign;
					var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
					mantissa = Sub(aMantissa, bMantissa);
				}
				else
				{
					sign = b.Sign;
					var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
					mantissa = Sub(bMantissa, aMantissa);
				}

				var result = new Smx(sign, mantissa, exponent, precision);
				return result;
			}
		}

		public static ulong[] Add(ulong[] ax, ulong[] bx)
		{
			Debug.Assert(ax.Length == bx.Length);

			var resultLength = ax.Length;
			var result = new ulong[resultLength];

			var indexOfLastNonZeroLimb = 0;
			var carry = 0ul;

			for (var i = 0; i < resultLength; i++)
			{
				var nv = ax[i] + bx[i] + carry;
				result[i] = Split(nv, out carry);
				indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
			}

			if (carry != 0)
			{
				// Add a Limb
				var newResult = Extend(result, resultLength + 1);
				newResult[^1] = carry;
				return newResult;
			}
			else if (indexOfLastNonZeroLimb < resultLength - 1)
			{
				// Trim leading zeros
				var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

				return newResult;
			}
			else
			{
				return result;
			}
		}

		//public static ulong[] AddOld(ulong[] ax, ulong[] bx)
		//{
		//	var resultLength = ax.Length > bx.Length ? ax.Length : bx.Length;
		//	var result = new ulong[resultLength];

		//	var indexOfLastNonZeroLimb = 0;
		//	var carry = 0ul;

		//	var i = 0;
		//	for (; i < ax.Length && i < bx.Length; i++)
		//	{
		//		var nv = ax[i] + bx[i] + carry;
		//		result[i] = Split(nv, out carry);
		//		indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
		//	}

		//	if (ax.Length > bx.Length)
		//	{
		//		for (; i < ax.Length; i++)
		//		{
		//			var nv = ax[i] + carry;
		//			result[i] = Split(nv, out carry);
		//			indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
		//		}
		//	}
		//	else if (bx.Length > ax.Length)
		//	{
		//		for (; i < bx.Length; i++)
		//		{
		//			var nv = bx[i] + carry;
		//			result[i] = Split(nv, out carry);
		//			indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
		//		}
		//	}

		//	if (carry != 0)
		//	{
		//		var newResult = new ulong[resultLength + 1];
		//		Array.Copy(result, 0, newResult, 0, resultLength);
		//		newResult[^1] = carry;
		//		return newResult;
		//	}
		//	else if (indexOfLastNonZeroLimb < resultLength - 1)
		//	{
		//		// Remove leading zeros
		//		var newResult = new ulong[indexOfLastNonZeroLimb + 1];
		//		Array.Copy(result, 0, newResult, 0, indexOfLastNonZeroLimb + 1);
		//		return newResult;
		//	}
		//	else
		//	{
		//		return result;
		//	}
		//}

		//public static ulong[] AddOldOld(ulong[] ax, ulong[] bx)
		//{
		//	ulong[] result;

		//	if (ax.Length < bx.Length)
		//	{
		//		result = AddInternal(bx, ax);
		//	}
		//	else
		//	{
		//		result = AddInternal(ax, bx);
		//	}

		//	return result;
		//}

		//private static ulong[] AddInternal(ulong[] ax, ulong[] bx)
		//{
		//	if (ax.Length < bx.Length)
		//	{
		//		throw new Exception("AddInternal expects ax to have as many (or more) digits as bx.");
		//	}

		//	var result = new ulong[ax.Length];

		//	var i = 0;

		//	for(; i < bx.Length; i++)
		//	{
		//		result[i] = ax[i] + bx[i];	
		//	}

		//	for(; i < ax.Length; i++)
		//	{
		//		result[i] = ax[i];
		//	}

		//	return result;
		//}

		public static ulong[] Sub(ulong[] ax, ulong[] bx)
		{
			Debug.Assert(ax.Length == bx.Length);

			var resultLength = ax.Length;
			var result = new ulong[resultLength];

			var indexOfLastNonZeroLimb = 0;
			var borrow = 0ul;

			for (var i = 0; i < resultLength - 1; i++)
			{
				// Set the lsb of the high part of a.
				var sax = ax[i] | TEST_BIT_32;

				result[i] = sax - bx[i] - borrow;

				if ((result[i] & TEST_BIT_32) > 0)
				{
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

			if (ax[^1] < (bx[^1] + borrow))
			{
				throw new OverflowException("MSB too small.");
			}

			result[^1] = ax[^1] - bx[^1] - borrow;

			if (result[^1] == 0 && indexOfLastNonZeroLimb < resultLength - 1)
			{
				// Remove leading zeros
				var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

				return newResult;
			}
			else
			{
				return result;
			}
		}

		//public static ulong[] SubOld(ulong[] ax, ulong[] bx)
		//{
		//	if (bx.Length > ax.Length)
		//	{
		//		throw new Exception($"While subtracting {GetDiagDisplay("b", bx)} from {GetDiagDisplay("a", ax)}, b was found to have more digits than a. A should be larger and therefor should have the same or more digits than b.");
		//	}

		//	var result = new ulong[ax.Length];

		//	var i = 0;
		//	for (; i < bx.Length; i++)
		//	{
		//		var diff = (long)ax[i] - (long)bx[i];

		//		if (diff < 0)
		//		{
		//			//var t = (ulong)Math.Abs(diff);
		//			//Debug.Assert(t < MAX_DIGIT_VALUE, $"When subtracting {(long)bx[i]} from {(long)ax[i]}, the result is larger than MAX_DIGIT_VALUE.");

		//			try
		//			{
		//				Borrow(ax, i + 1);
		//			}
		//			catch (OverflowException oe)
		//			{
		//				throw new Exception($"While subtracting {GetDiagDisplay("b", bx)} {GetDiagDisplay("a", ax)}, Borrow threw an execption.", oe);
		//			}

		//			diff = (long)ax[i] - (long)bx[i];

		//			if (diff < 0)
		//			{
		//				throw new Exception($"While subtracting {GetDiagDisplay("b", bx)} from {GetDiagDisplay("a", ax)}, after borrow, the difference is still negative.");
		//			}
		//		}

		//		result[i] = (ulong)diff;
		//	}

		//	for (; i < ax.Length; i++)
		//	{
		//		result[i] = ax[i];
		//	}

		//	return result;
		//}

		//private static void Borrow(ulong[] x, int index)
		//{
		//	if (index >= x.Length)
		//	{
		//		throw new OverflowException("UnderFlow while subtracting. Attempting to borrow from a digit past the msb.");
		//	}

		//	if (x[index] == 0)
		//	{
		//		Borrow(x, index + 1);
		//	}

		//	x[index] -= 1;
		//	x[index - 1] += MAX_DIGIT_VALUE;
		//}

		#endregion

		#region Split and Pack 

		private static ulong[] Pack(ulong[] splitValues)
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
		private static ulong[] Split(ulong[] packedValues)
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
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		private static ulong[] ReSplit(ulong[] mantissa, out ulong carry) 
		{
			// To be used after a addition or subtraction operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// Include the least significant limb.

			// Remove all zero-valued leading limbs 
			// If the MSL produces a carry, append an additional limb.

			var result = new ulong[mantissa.Length];

			var indexOfLastNonZeroLimb = 0;
			carry = 0ul;

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
				// Add a Limb
				var newResult = Extend(result, mantissa.Length + 1);
				newResult[^1] = carry;
				return newResult;
			}
			else if (indexOfLastNonZeroLimb < mantissa.Length - 1)
			{
				// Trim Leading Zeros
				var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

				return newResult;
			}
			else
			{
				return result;
			}
		}

		public static bool CheckPWValues(ulong[] values)
		{
			var result = values.Any(x => x >= MAX_DIGIT_VALUE);
			return result;
		}

		#endregion

		#region To String Support

		private static string GetDiagDisplay(string name, ulong[] values, int stride)
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

		private static string GetDiagDisplay(string name, ulong[] values)
		{
			var strAry = GetStrArray(values);

			return $"{name}: {string.Join("; ", strAry)}";
		}

		private static string GetHiLoDiagDisplay(string name, ulong[] values)
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

		private static string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
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

		// TODO: Update NormalizeFPV to remove trailing zeros (and update the exponent accordingly.)
		// TODO: Update NormalizeFPV to restrict the number of limbs
		public static ulong[] NormalizeFPV(ulong[] mantissa, int exponent, int precision, out int nrmExponent)
		{
			ValidateIsSplit(mantissa);
			if (mantissa.Length == 1 && mantissa[0] == 0)
			{
				nrmExponent = exponent;
				return mantissa;
			}

			var limbsCount = GetLimbsCount(precision);

			if (mantissa.Length <= limbsCount)
			{
				nrmExponent = exponent;
				return mantissa;
			}

			var numSignificantDigits = GetNumberOfSignificantDigits(mantissa, limbsCount);
			var additionalSignificantDigitsDesired = precision - numSignificantDigits;

			ulong[] result;
			int newExponent;

			if (additionalSignificantDigitsDesired <= 0)
			{
				result = Round(mantissa, exponent, limbsCount, out newExponent);
			}
			else
			{
				var potentialShiftAmount = BitOperations.LeadingZeroCount(mantissa[^1]) - 32;
				var shiftAmount = Math.Min(additionalSignificantDigitsDesired, potentialShiftAmount);

				// Compensate for shifting towards the MSB (i.e., multiplying)
				newExponent = exponent - shiftAmount;

				var startIndex = mantissa.Length - limbsCount;

				result = new ulong[limbsCount];
				Array.Copy(mantissa, startIndex, result, 0, limbsCount);

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

			//var ltResult = TrimTrailingZeros(result, newExponent, out nrmExponent);

			//if (ltResult.Any(x => x >= MAX_DIGIT_VALUE))
			//{
			//	throw new InvalidOperationException("NormalizeFPV is returning a value that is not normalized.");
			//}

			//return ltResult;

			if (result.Any(x => x >= MAX_DIGIT_VALUE))
			{
				throw new InvalidOperationException("NormalizeFPV is returning a value that is not normalized.");
			}

			nrmExponent = newExponent;
			return result;
		}

		[Conditional("DEBUG")]
		private static void ValidateIsSplit(ulong[] mantissa)
		{
			if (CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			}
		}

		public static int GetLimbsCount(int precision)
		{
			var dResult = precision / 32d;
			var result = (int)Math.Ceiling(dResult);

			return result;
		}

		public static int GetNumberOfSignificantDigits(ulong[] mantissa, int numberOfPlacesToInspect = int.MaxValue)
		{
			numberOfPlacesToInspect = Math.Min(mantissa.Length, numberOfPlacesToInspect);
			var endIndex = mantissa.Length - numberOfPlacesToInspect;

			var result = 0;

			for (int i = mantissa.Length - 1; i >= endIndex; i--)
			{
				var lz = BitOperations.LeadingZeroCount(mantissa[i]);
				Debug.Assert(lz >= 32);

				result += 64 - lz;
			}

			return result;
		}

		private static ulong[] Round(ulong[] mantissa, int exponent, int limbs, out int newExponent)
		{
			var result = new ulong[limbs];
			var startIndex = mantissa.Length - limbs;

			if (startIndex == 0)
			{
				newExponent = exponent;
				return mantissa;
			}

			if (startIndex < 0)
			{
				throw new ArgumentException("limbs must be greater or equal to the current number of limbs.");
			}

			Array.Copy(mantissa, startIndex, result, 0, limbs);

			if (mantissa[startIndex - 1] >= HALF_DIGIT_VALUE)
			{
				var rndResult = (ulong[])result.Clone();
				result = Add1AndReSplit(rndResult, extendOnCarry: false);
			}

			//var rndExponent = exponent + 32 * startIndex;
			//var ltResult = TrimTrailingZeros(result, rndExponent, out newExponent);

			//return ltResult;

			newExponent = exponent + 32 * startIndex;
			return result;
		}

		private static ulong[] Add1AndReSplit(ulong[] mantissa, bool extendOnCarry)
		{
			var result = (ulong[])mantissa.Clone();
			result[0] += 1;
			result = ReSplit(result, out var carry);

			if (extendOnCarry)
			{
				return result;
			}
			else
			{
				if (carry == 1)
				{
					return mantissa;
				}
				else if (carry == 0)
				{
					return result;
				}
				else
				{
					throw new InvalidOperationException("Carry was neither 0 or 1 upon return from call to Resplit.");
				}
			}
		}

		// TODO: Make AlignExponents Reduce as well as Scale. 
		private static Smx AlignExponents(Smx a, Smx b, out Smx normalizedB)
		{
			if (a.Exponent == b.Exponent)
			{
				normalizedB = b;
				return a;
			}

			var diff = a.Exponent - b.Exponent;

			if (diff > 0)
			{
				normalizedB = b;
				var newAMantissa = ScaleAndSplit(a.Mantissa, diff);
				var result = new Smx(a.Sign, newAMantissa, b.Exponent, a.Precision);
				return result;
			}
			else
			{
				var newBMantissa = ScaleAndSplit(b.Mantissa, diff * -1);

				//var limbCountIncrease = newBMantissa.Length - b.Mantissa.Length;
				//if (limbCountIncrease > 0)
				//{
				//	Debug.WriteLine($"Adjusting exp for C by {diff}. Limb cnt increased by: {limbCountIncrease}.");
				//}
				//else
				//{
				//	Debug.WriteLine($"Adjusting exp for C by {diff}. Limb cnt stays at: {newBMantissa.Length}.");
				//}

				normalizedB = new Smx(b.Sign, newBMantissa, a.Exponent, b.Precision);
				return a;
			}
		}

		private static ulong[] ScaleAndSplit(ulong[] values, int power)
		{
			if (power <= 0)
			{
				throw new ArgumentException("The value of power must be 1 or greater.");
			}

			if (!values.Any(x => x > 0))
			{
				return new ulong[] { 0 };
			}

			var qr = Math.DivRem(power, 32);
			var newLimbs = qr.Quotient;
			var factor = (ulong)Math.Pow(2, qr.Remainder);

			var result = new ulong[values.Length + newLimbs];

			var indexOfLastNonZeroLimb = 0;
			var carry = 0ul;

			for (var i = 0; i < values.Length; i++)
			{
				var newLimbVal = values[i] * factor + carry;
				var lo = Split(newLimbVal, out carry); // :Spliter
				result[i + newLimbs] = lo;

				if (lo > 0)
				{
					indexOfLastNonZeroLimb = i;
				}
			}

			if (carry != 0)
			{
				// Add a Limb
				var newResult = Extend(result, result.Length + 1);
				newResult[^1] = carry;
				return newResult;
			}
			else if (indexOfLastNonZeroLimb < values.Length - 1)
			{
				// Remove leading zeros
				var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);
				return newResult;
			}
			else
			{
				return result;
			}
		}

		private static ulong[] ExtendLimbs(ulong[] ax, ulong[] bx, out ulong[] extendedBx)
		{
			if (ax.Length == bx.Length)
			{
				extendedBx = bx;
				return ax;
			}
			else if (ax.Length < bx.Length)
			{
				extendedBx = bx;
				return Extend(ax, bx.Length);
			}
			else
			{
				extendedBx = Extend(bx, ax.Length);
				return ax;
			}
		}

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

		private static Smx TrimLeadingZeros(Smx a)
		{
			var mantissa = TrimLeadingZeros(a.Mantissa);
			var result = new Smx(a.Sign, mantissa, a.Exponent, a.Precision);
			return result;
		}

		public static ulong[] TrimLeadingZeros(ulong[] mantissa)
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

		private static ulong[] TrimTrailingZeros(ulong[] mantissa, int exponent, out int newExponent)
		{
			newExponent = exponent;

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
				return new ulong[] { 0 };
			}

			var result = new ulong[mantissa.Length - i];
			Array.Copy(mantissa, i, result, 0, result.Length);

			newExponent += i * 32;

			return result;
		}

		#endregion

		#region Reduce - Not Used

		public static ulong[] Reduce(ulong[] mantissa, out int shiftAmount)
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

		public static bool AreComponentsEven(ulong[] mantissa)
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

		private static int GetWholeExp(ulong[] mantissa)
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

		private static int GetWholeExp(ulong limb)
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

		private static int Compare(ulong[] ax, ulong[] bx)
		{
			var sdA = GetNumberOfSignificantB32Digits(ax);
			var sdB = GetNumberOfSignificantB32Digits(bx);

			if (sdA != sdB)
			{
				return sdA > sdB ? 1 : -1;
			}

			var i = -1 + Math.Min(ax.Length, bx.Length);

			for (; i >= 0; i--)
			{
				if (ax[i] != bx[i])
				{
					return ax[i] > bx[i] ? 1 : -1;
				}
			}

			return 0;
		}

		public static int GetNumberOfSignificantB32Digits(ulong[] mantissa)
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

		public static bool IsGreaterOrEqThan(Smx a, uint b)
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

		//public static int SumAndCompare(Smx a, Smx b, uint c)
		//{
		//	var aMsb = GetMsb(a.Mantissa, out var indexA);
		//	var aValue = aMsb * Math.Pow(2, a.Exponent + (indexA * 32));

		//	var bMsb = GetMsb(b.Mantissa, out var indexB);
		//	var bValue = bMsb * Math.Pow(2, b.Exponent + (indexB * 32));

		//	var sum = aValue + bValue;
		//	var result = sum < c ? -1 : sum > c ? 1 : 0;

		//	return result;
		//}

		//private static ulong GetMsb(ulong[] x, out int index)
		//{
		//	for(int i = x.Length - 1; i >= 0; i--)
		//	{
		//		if (x[i] != 0)
		//		{
		//			index = i;
		//			return x[i];
		//		}
		//	}

		//	index = 0;
		//	return 0;
		//}

		#endregion
	}
}
