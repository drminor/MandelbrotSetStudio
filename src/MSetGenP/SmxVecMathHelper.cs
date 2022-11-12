
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MSetGenP
{
	public class SmxVecMathHelper
	{
		private int _precision;

		private static readonly int _uLongSlots = Vector<ulong>.Count;

		#region Constructors

		public SmxVecMathHelper(int precision)
		{
			Precision = precision;
		}

		#endregion

		#region Public Properties

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

		public FPValues Square(FPValues a)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			//var sign = true;
			//var exponent = a.Exponent * 2;
			//var precision = a.Precision;

			//var rawMantissa = Square(a.Mantissa);
			//var mantissa = PropagateCarries(rawMantissa);
			//var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			//Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			//return result;

			return a;
		}

		//public static ulong[] Square(ulong[] ax)
		//{
		//	var mantissa = new ulong[ax.Length * 2];

		//	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
		//	for (int j = 0; j < ax.Length; j++)
		//	{
		//		for (int i = j; i < ax.Length; i++)
		//		{
		//			//if (i < j) continue;

		//			var product = ax[j] * ax[i];

		//			if (i > j)
		//			{
		//				product *= 2;
		//			}

		//			var resultPtr = j + i;  // 0, 1, 1, 2
		//			var lo = Split(product, out var hi);
		//			mantissa[resultPtr] += lo;
		//			mantissa[resultPtr + 1] += hi;
		//		}
		//	}

		//	return mantissa;
		//}

		public static Smx Multiply(Smx a, int b)
		{
			//if (a.IsZero || b == 0)
			//{
			//	return new Smx(0, 1, a.Precision);
			//}

			//var signOfB = b >= 0;
			//var sign = a.Sign == signOfB;
			//var exponent = a.Exponent;
			//var precision = a.Precision;

			//var rawMantissa = Multiply(a.Mantissa, (uint)Math.Abs(b));
			//var mantissa = PropagateCarries(rawMantissa);
			//var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			//Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			//return result;

			return a;
		}

		//public static ulong[] Multiply(ulong[] ax, uint b)
		//{
		//	//Debug.WriteLine(GetDiagDisplay("ax", ax));
		//	//Debug.WriteLine($"b = {b}");

		//	//var seive = new ulong[ax.Length];

		//	var mantissa = new ulong[ax.Length + 1];

		//	// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
		//	for (int j = 0; j < ax.Length; j++)
		//	{
		//		var product = ax[j] * b;
		//		//seive[j] = product;

		//		var lo = Split(product, out var hi);    //		2 x 1			3 x 1			4 x 1
		//		mantissa[j] += lo;                      //			0, 1			0, 1, 2			0, 1, 2, 3
		//		mantissa[j + 1] += hi;                  //			1, 2			1, 2, 3			1, 2, 3, 4
		//	}

		//	//var splitSieve = Split(seive);
		//	//Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, 2));
		//	//Debug.WriteLine(GetDiagDisplay("result", mantissa));

		//	return mantissa;
		//}

		//public static ulong[] PropagateCarries(ulong[] mantissa)
		//{
		//	// To be used after a multiply operation.
		//	// Process the carry portion of each result bin.
		//	// This will leave each result bin with a value <= 2^32 for the final digit.
		//	// Sometimes we need the LS-Limb. We are including
		//	// #Don't include the least significant limb, as this value will always be discarded as the result is rounded.

		//	// Remove all zero-valued leading limbs 
		//	// If the MSL produces a carry, throw an exception.

		//	var result = new ulong[mantissa.Length];

		//	var indexOfLastNonZeroLimb = 0;
		//	var carry = 0ul;

		//	for (int i = 0; i < mantissa.Length; i++)
		//	{
		//		var lo = Split(mantissa[i] + carry, out var hi);  // :Spliter
		//		result[i] = lo;

		//		if (lo > 0)
		//		{
		//			indexOfLastNonZeroLimb = i;
		//		}

		//		carry = hi;
		//	}

		//	if (carry != 0)
		//	{
		//		throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
		//	}

		//	if (indexOfLastNonZeroLimb < mantissa.Length - 1)
		//	{
		//		// Trim Leading Zeros
		//		var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

		//		return newResult;
		//	}
		//	else
		//	{
		//		return result;
		//	}
		//}

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
			//if (b.IsZero)
			//{
			//	normalizedB = b;
			//	var trimmedA = TrimLeadingZeros(a);
			//	return trimmedA;
			//}

			//if (a.IsZero)
			//{
			//	var trimmedB = TrimLeadingZeros(b);
			//	normalizedB = trimmedB;
			//	return trimmedB;
			//}

			//var normalizedA = AlignExponents(a, b, out normalizedB);

			//bool sign;
			//ulong[] mantissa;
			//var exponent = normalizedA.Exponent;
			//var precision = Math.Min(a.Precision, b.Precision);

			//if (a.Sign == b.Sign)
			//{
			//	sign = a.Sign;
			//	var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
			//	mantissa = Add(aMantissa, bMantissa);

			//	if (mantissa.Length > aMantissa.Length)
			//	{
			//		var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			//		var result = new Smx(sign, nrmMantissa, nrmExponent, precision);
			//		return result;
			//	}
			//	else
			//	{
			//		var result = new Smx(sign, mantissa, exponent, precision);
			//		return result;
			//	}
			//}
			//else
			//{
			//	var cmp = Compare(normalizedA.Mantissa, normalizedB.Mantissa);

			//	if (cmp >= 0)
			//	{
			//		sign = a.Sign;
			//		var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
			//		mantissa = Sub(aMantissa, bMantissa);
			//	}
			//	else
			//	{
			//		sign = b.Sign;
			//		var aMantissa = ExtendLimbs(normalizedA.Mantissa, normalizedB.Mantissa, out var bMantissa);
			//		mantissa = Sub(bMantissa, aMantissa);
			//	}

			//	var result = new Smx(sign, mantissa, exponent, precision);
			//	return result;
			//}

			normalizedB = b;
			return a;
		}

		//public static ulong[] Add(ulong[] ax, ulong[] bx)
		//{
		//	Debug.Assert(ax.Length == bx.Length);

		//	var resultLength = ax.Length;
		//	var result = new ulong[resultLength];

		//	var indexOfLastNonZeroLimb = 0;
		//	var carry = 0ul;

		//	for (var i = 0; i < resultLength; i++)
		//	{
		//		var nv = ax[i] + bx[i] + carry;
		//		result[i] = Split(nv, out carry);
		//		indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
		//	}

		//	if (carry != 0)
		//	{
		//		// Add a Limb
		//		var newResult = Extend(result, resultLength + 1);
		//		newResult[^1] = carry;
		//		return newResult;
		//	}
		//	else if (indexOfLastNonZeroLimb < resultLength - 1)
		//	{
		//		// Trim leading zeros
		//		var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

		//		return newResult;
		//	}
		//	else
		//	{
		//		return result;
		//	}
		//}

		//public static ulong[] Sub(ulong[] ax, ulong[] bx)
		//{
		//	Debug.Assert(ax.Length == bx.Length);

		//	var resultLength = ax.Length;
		//	var result = new ulong[resultLength];

		//	var indexOfLastNonZeroLimb = 0;
		//	var borrow = 0ul;

		//	for (var i = 0; i < resultLength - 1; i++)
		//	{
		//		// Set the lsb of the high part of a.
		//		var sax = ax[i] | TEST_BIT_32;

		//		result[i] = sax - bx[i] - borrow;

		//		if ((result[i] & TEST_BIT_32) > 0)
		//		{
		//			result[i] &= LOW_MASK;
		//			borrow = 0;
		//		}
		//		else
		//		{
		//			borrow = 1;
		//		}

		//		if (result[i] > 0)
		//		{
		//			indexOfLastNonZeroLimb = i;
		//		}
		//	}

		//	if (ax[^1] < (bx[^1] + borrow))
		//	{
		//		throw new OverflowException("MSB too small.");
		//	}

		//	result[^1] = ax[^1] - bx[^1] - borrow;

		//	if (result[^1] == 0 && indexOfLastNonZeroLimb < resultLength - 1)
		//	{
		//		// Remove leading zeros
		//		var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

		//		return newResult;
		//	}
		//	else
		//	{
		//		return result;
		//	}
		//}

		#endregion



		//public void Add(FPValues a, FPValues b, FPValues c, int limbIndex)
		//{

		//	int numVectors = a.Mantissas[limbIndex].Length / _uLongSlots;
		//	int ceiling = numVectors * _uLongSlots;

		//	var aMantissasVec = MemoryMarshal.Cast<ulong, Vector<ulong>>(a.MantissasMemory[limbIndex].Span);

		//	//var bMantissasVec = MemoryMarshal.Cast<ulong, Vector<ulong>>(new Span<ulong>(b.Mantissas[limbIndex], bIndex, uLongSlots));
		//	//var cMantissasVec = MemoryMarshal.Cast<ulong, Vector<ulong>>(new Span<ulong>(c.Mantissas[limbIndex], cIndex, uLongSlots));

		//	var bMantissasVec = MemoryMarshal.Cast<ulong, Vector<ulong>>(b.MantissasMemory[limbIndex].Span);
		//	var cMantissasVec = MemoryMarshal.Cast<ulong, Vector<ulong>>(c.MantissasMemory[limbIndex].Span);

		//	for (int i = 0; i < numVectors; i++)
		//	{
		//		cMantissasVec[i] = aMantissasVec[i] + bMantissasVec[i];
		//	}

		//	// Finish operation with any numbers leftover
		//	for (int i = ceiling; i < a.Mantissas[limbIndex].Length; i++)
		//	{
		//		c.Mantissas[limbIndex][i] = a.Mantissas[limbIndex][i] + b.Mantissas[limbIndex][i];
		//	}

		//	//return a;
		//}

		public void Add2(SequenceReader<Vector<ulong>> a, SequenceReader<Vector<ulong>> b, Vector<ulong>[] result)
		{
			var resultPtr = 0;
			while(a.TryRead(out var aVal))
			{
				if (b.TryRead(out var bVal))
				{
					var sum = aVal + bVal;
					result[resultPtr++] = sum;
				}
				else
				{
					throw new ArgumentException("A is longer than B.");
				}
			}

		}




	}
}
