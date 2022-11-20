using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MSetGenP
{
	public class SmxVecMathHelper
	{
		#region Private Properties

		private const ulong LOW_MASK = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH_MASK = 0xFFFFFFFF00000000; // bits 32 - 63 are set.

		private static readonly Vector<ulong> LOW_MASK_VEC = new Vector<ulong>(LOW_MASK);
		private static readonly Vector<ulong> HIGH_MASK_VEC = new Vector<ulong>(HIGH_MASK);

		private static readonly int _ulongSlots = Vector<ulong>.Count;

		private int _valueCount;
		private int _precision;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;
		private Memory<ulong> _productsMem;
		private Memory<ulong> _productLowsMem;
		private Memory<ulong> _productHighsMem;

		private Memory<ulong> _carriesMem;
		private Memory<ulong> _withCarriesMem;

		private Memory<ulong>[] _addResult1Mem;

		#endregion

		#region Constructor

		public SmxVecMathHelper(bool[] doneFlags, int precision)
		{
			_valueCount = doneFlags.Length;
			VecCount = Math.DivRem(_valueCount, _ulongSlots, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException("The valueCount must be an even multiple of Vector<ulong>.Count.");
			}

			Precision = precision;

			InPlayList = BuildTheInplayList(doneFlags, VecCount);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, _valueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, _valueCount);

			_productsMem = new Memory<ulong>(new ulong[_valueCount]);
			_productLowsMem = new Memory<ulong>(new ulong[_valueCount]);
			_productHighsMem = new Memory<ulong>(new ulong[_valueCount]);

			_carriesMem = new Memory<ulong>(new ulong[_valueCount]);
			_withCarriesMem = new Memory<ulong>(new ulong[_valueCount]);
			_addResult1Mem = BuildMantissaMemoryArray(LimbCount + 1, _valueCount);
		}

		private List<int> BuildTheInplayList(bool[] doneFlags, int vecCount)
		{
			var result = Enumerable.Range(0, vecCount).ToList();

			for (int j = 0; j < vecCount; j++)
			{
				var arrayPtr = j * _ulongSlots;

				for(var lanePtr = 0; lanePtr < _ulongSlots; lanePtr++)
				{
					if (doneFlags[arrayPtr + lanePtr])
					{
						result.Remove(j);
						break;
					}
				}
			}

			return result;
		}

		private Memory<ulong>[] BuildMantissaMemoryArray(int limbCount, int vectorCount)
		{
			var result = new Memory<ulong>[limbCount];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new Memory<ulong>(new ulong[vectorCount]);
			}

			return result;
		}

		private Span<Vector<ulong>> GetLimbVectors(Memory<ulong> memory)
		{
			Span<Vector<ulong>> result = MemoryMarshal.Cast<ulong, Vector<ulong>>(memory.Span);
			return result;
		}

		#endregion

		#region Public Properties

		public int Precision
		{
			get => _precision;
			set
			{
				_precision = value;
				LimbCount = SmxMathHelper.GetLimbCount(_precision);
			}
		}

		public int VecCount { get; private set; }
		public int LimbCount { get; private set; }

		public List<int> InPlayList { get; }

		#endregion

		#region Multiply and Square

		public FPValues Square(FPValues a)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			var valueCount = a.Length;
			//var sign = true;
			var signs = Enumerable.Repeat(true, valueCount).ToArray();

			//var exponent = a.Exponent * 2;
			//var exponents = a.Exponents.Select(x => (short)(x * 2)).ToArray();
			//var precision = a.Precision;

			//var rawMantissa = Square(a.Mantissa);
			var rawMantissas = SquareInternal(a);
			var mantissas = PropagateCarries(rawMantissas);

			//var nrmMantissa = NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);
			//Smx result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			// Instead of Normalizing, discard all 'extra' limbs starting from the Least Significant.
			var targetLimbCount = a.LimbCount;
			var result = new FPValues(signs, a.Exponents, targetLimbCount);

			var currentLimbCount = mantissas.Length;
			var startIndex = currentLimbCount - targetLimbCount;

			for (var i = 0; i < targetLimbCount; i++)
			{
				if(!mantissas[i + startIndex].TryCopyTo(result.MantissaMemories[i]))
				{
					throw new InvalidOperationException($"Cannot copy limb {i} to the result.");
				}
			}

			return result;
		}

		private Memory<ulong>[] SquareInternal(FPValues a)
		{
			var result = _squareResult1Mems;
			var products = GetLimbVectors(_productsMem);

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < a.LimbCount; j++)
			{
				for (int i = j; i < a.LimbCount; i++)
				{
					//var products = new Vector<ulong>[vCnt];

					var left = a.GetLimbVectors(j);
					var right = a.GetLimbVectors(i);

					MultiplyVecs(left, right, products);

					if (i > j)
					{
						//product *= 2;
						for (var q = 0; q < products.Length; q++)
						{
							products[q] = products[q] * 2;
						}

					}

					var resultPtr = j + i;  // 0, 1, 1, 2

					//var lo = Split(product, out var hi);
					//mantissa[resultPtr] += lo;
					//mantissa[resultPtr + 1] += hi;

					var resultLows = GetLimbVectors(result[resultPtr]);
					var resultHighs = GetLimbVectors(result[resultPtr + 1]);

					var productHighs = GetLimbVectors(_productHighsMem);
					var productLows = GetLimbVectors(_productLowsMem);

					Split(products, productHighs, productLows);

					for(var p = 0; p < products.Length; p++)
					{
						resultLows[p] = resultLows[p] + productLows[p];
						resultHighs[p] = resultHighs[p] + productHighs[p];
						//result[resultPtr][p] = result[resultPtr][p] + lows[p];
						//result[resultPtr + 1][p] = result[resultPtr + 1][p] + highs[p];
					}
				}
			}

			return result;
		}

		public void MultiplyVecs(Span<Vector<ulong>> left, Span<Vector<ulong>> right, Span<Vector<ulong>> result)
		{
			foreach(var idx in InPlayList)
			{
				result[idx] = left[idx] * right[idx];
			}
		}

		private void Split(Span<Vector<ulong>> x, Span<Vector<ulong>> highs, Span<Vector<ulong>> lows)
		{
			for (var i = 0; i < x.Length; i++)
			{
				highs[i] = x[i] & HIGH_MASK_VEC;
				lows[i] = x[i] & LOW_MASK_VEC;
			}

			//hi = x. x >> 32; // Create new ulong from bits 32 - 63.
			//return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		private Memory<ulong>[] PropagateCarries(Memory<ulong>[] mantissaMems)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// Sometimes we need the LS-Limb. We are including
			// #Don't include the least significant limb, as this value will always be discarded as the result is rounded.

			// Remove all zero-valued leading limbs 
			// If the MSL produces a carry, throw an exception.

			if (mantissaMems.Length == 0)
			{
				return mantissaMems;
			}

			//var indexOfLastNonZeroLimb = 0;

			//var result = new Vector<ulong>[mantissas.Length][];
			var result = _squareResult2Mems;

			var carries = GetLimbVectors(_carriesMem);
			var withCarries = GetLimbVectors(_withCarriesMem);

			var limbVecs = GetLimbVectors(mantissaMems[0]);
			var resultLimbVecs = GetLimbVectors(result[0]);
			Split(limbVecs, carries, resultLimbVecs);

			for (int i = 0; i < mantissaMems.Length; i++)
			{
				limbVecs = GetLimbVectors(mantissaMems[i]);
				resultLimbVecs = GetLimbVectors(result[i]);

				AddVecs(limbVecs, carries, withCarries);
				Split(withCarries, carries, resultLimbVecs);
			}

			// TODO: Check to see if any of the carries have any zero value.
			//if (carry != 0)
			//{
			//	throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			//}

			//if (indexOfLastNonZeroLimb < mantissas.Length - 1)
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

		public FPValues Sub(FPValues a, FPValues b)
		{
			return a;
		}

		public static Smx Sub(Smx a, Smx b)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new Smx(!b.Sign, b.Mantissa, b.Exponent, b.Precision);
			var result = Add(a, bNegated);

			return result;
		}

		public FPValues Add(FPValues a, FPValues b)
		{
			return a;
		}

		public static Smx Add(Smx a, Smx b)
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

			return a;
		}
		
		private Memory<ulong>[] AddF(FPValues a, FPValues b)
		{
			Debug.Assert(a.Length == b.Length);

			var result = _addResult1Mem;

			Span<Vector<ulong>> carries = GetLimbVectors(_carriesMem);
			Span<Vector<ulong>> withCarries = GetLimbVectors(_withCarriesMem);

			for (var i = 0; i < a.Length; i++)
			{
				var limbVecsA = GetLimbVectors(a.MantissaMemories[i]);
				var limbVecsB = GetLimbVectors(b.MantissaMemories[i]);
				var resultLimbVecs = GetLimbVectors(result[i]);

				AddVecs(limbVecsA, limbVecsB, withCarries);

				if (i > 0)
				{
					// add the caries produced from splitting the previous limb's
					AddVecs(withCarries, carries, withCarries);
				}

				Split(x:withCarries, highs:carries, lows:resultLimbVecs);
			}

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


			return result;
		}

		public static ulong[] Sub(ulong[] ax, ulong[] bx)
		{
			//Debug.Assert(ax.Length == bx.Length);

			//var resultLength = ax.Length;
			//var result = new ulong[resultLength];

			//var indexOfLastNonZeroLimb = 0;
			//var borrow = 0ul;

			//for (var i = 0; i < resultLength - 1; i++)
			//{
			//	// Set the lsb of the high part of a.
			//	var sax = ax[i] | TEST_BIT_32;

			//	result[i] = sax - bx[i] - borrow;

			//	if ((result[i] & TEST_BIT_32) > 0)
			//	{
			//		result[i] &= LOW_MASK;
			//		borrow = 0;
			//	}
			//	else
			//	{
			//		borrow = 1;
			//	}

			//	if (result[i] > 0)
			//	{
			//		indexOfLastNonZeroLimb = i;
			//	}
			//}

			//if (ax[^1] < (bx[^1] + borrow))
			//{
			//	throw new OverflowException("MSB too small.");
			//}

			//result[^1] = ax[^1] - bx[^1] - borrow;

			//if (result[^1] == 0 && indexOfLastNonZeroLimb < resultLength - 1)
			//{
			//	// Remove leading zeros
			//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			//	return newResult;
			//}
			//else
			//{
			//	return result;
			//}

			return ax;
		}

		private void AddVecs(Span<Vector<ulong>> left, Span<Vector<ulong>> right, Span<Vector<ulong>> result)
		{
			for(var i = 0; i < left.Length; i++)
			{
				result[i] = left[i] + right[i];
			}
		}
		
		#endregion

		#region Normalization Support

		private Vector<ulong>[][] CopyFirstXElements(Vector<ulong>[][] values, int newLimbCount)
		{
			var result = new Vector<ulong>[newLimbCount][];

			for (var i = 0; i < newLimbCount; i++)
			{
				result[i] = values[i];	
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

		public void IsGreaterOrEqThan(FPValues a, uint b, Span<Vector<ulong>> escapedFlagVectors)
		{
			var escapedFlags = new bool[_ulongSlots];

			foreach (var idx in InPlayList)
			{
				var arrayPtr = idx * _ulongSlots;

				for (var i = 0; i < _ulongSlots ; i++)
				{
					escapedFlags[i] = IsGreaterOrEqThan(a.Mantissas, arrayPtr, a.Exponents[arrayPtr], b);
					arrayPtr++;
				}

				var escapeFlagVecValues = escapedFlags.Select(x => x ? 1ul : 0ul).ToArray();
				escapedFlagVectors[idx] = new Vector<ulong>(escapeFlagVecValues);
			}
		}

		public bool IsGreaterOrEqThan(ulong[][] mantissas, int valueIndex, int exponent, uint b)
		{
			var aAsDouble = 0d;

			for (var i = mantissas.Length - 1; i >= 0; i--)
			{
				aAsDouble += mantissas[i][valueIndex] * Math.Pow(2, exponent + (i * 32));

				if (aAsDouble >= b)
				{
					return true;
				}
			}

			return false;
		}


		#endregion
	}
}
