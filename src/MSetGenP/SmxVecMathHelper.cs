using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGenP
{
	public class SmxVecMathHelper
	{
		#region Private Properties

		private const int BITS_PER_LIMB = 32;
		private const int BITS_BEFORE_BP = 8;

		private const ulong LOW_MASK = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private const ulong HIGH_MASK = 0xFFFFFFFF00000000; // bits 32 - 63 are set.

		private static readonly Vector256<ulong> LOW_MASK_VEC = Vector256.Create(LOW_MASK);
		private static readonly Vector256<ulong> HIGH_MASK_VEC = Vector256.Create(HIGH_MASK);

		private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.

		private static readonly int _ulongSlots = Vector<ulong>.Count;

		//private readonly SmxMathHelper _smxMathHelper;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;
		private Memory<ulong>[] _squareResult3Mems;

		private Memory<ulong> _productsMem;
		private Memory<ulong> _productLowsMem;
		private Memory<ulong> _productHighsMem;

		private Memory<ulong> _carriesMem;
		private Memory<ulong> _withCarriesMem;

		private Memory<ulong>[] _addResult1Mem;

		#endregion

		#region Constructor

		public SmxVecMathHelper(bool[] doneFlags, ApFixedPointFormat apFixedPointFormat)
		{
			ValueCount = doneFlags.Length;
			VecCount = Math.DivRem(ValueCount, _ulongSlots, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException("The valueCount must be an even multiple of Vector<ulong>.Count.");
			}

			ApFixedPointFormat = SmxMathHelper.GetAdjustedFixedPointFormat(apFixedPointFormat);

			//if (FractionalBits != apFixedPointFormat.NumberOfFractionalBits)
			//{
			//	Debug.WriteLine($"WARNING: Increasing the number of fractional bits to {FractionalBits} from {apFixedPointFormat.NumberOfFractionalBits}.");
			//}

			LimbCount = SmxMathHelper.GetLimbCount(ApFixedPointFormat.TotalBits);
			TargetExponent = -1 * FractionalBits;
			MaxIntegerValue = (uint)Math.Pow(2, BitsBeforeBP) - 1;

			//_smxMathHelper = new SmxMathHelper(ApFixedPointFormat);

			InPlayList = BuildTheInplayList(doneFlags, VecCount);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult3Mems = BuildMantissaMemoryArray(LimbCount, ValueCount);

			_productsMem = new Memory<ulong>(new ulong[ValueCount]);
			_productLowsMem = new Memory<ulong>(new ulong[ValueCount]);
			_productHighsMem = new Memory<ulong>(new ulong[ValueCount]);

			_carriesMem = new Memory<ulong>(new ulong[ValueCount]);
			_withCarriesMem = new Memory<ulong>(new ulong[ValueCount]);
			_addResult1Mem = BuildMantissaMemoryArray(LimbCount, ValueCount);
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

		private Span<Vector256<ulong>> GetLimbVectors2L(Memory<ulong> memory)
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}

		private Span<Vector256<uint>> GetLimbVectors2S(Memory<ulong> memory)
		{
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(memory.Span);
			return result;
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }

		public int LimbCount { get; init; }
		public int TargetExponent { get; init; }

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public List<int> InPlayList { get; }

		public uint MaxIntegerValue { get; init; }


		public int BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;


		#endregion

		#region Multiply and Square

		public void Square(FPValues a, FPValues result)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			var valueCount = a.Length;
			var signs = Enumerable.Repeat(true, valueCount).ToArray();

			var rawMantissas = SquareInternal(a);
			var mantissas = PropagateCarries(rawMantissas);
			var nrmMantissa = ShiftAndTrim(mantissas);			// TODO: Give the ShiftAndTrim method a reference to the new FPValues's MantissaMemories to avoid copying

			var targetLimbCount = a.LimbCount;
			//var result = new FPValues(signs, a.Exponents, targetLimbCount);

			for (var i = 0; i < nrmMantissa.Length; i++)
			{
				if (!nrmMantissa[i].TryCopyTo(result.MantissaMemories[i]))
				{
					throw new InvalidOperationException($"Cannot copy limb {i} to the result.");
				}
			}

			//return result;
		}

		private Memory<ulong>[] SquareInternal(FPValues a)
		{
			var result = _squareResult1Mems;
			var products = GetLimbVectors2L(_productsMem);

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < a.LimbCount; j++)
			{
				for (int i = j; i < a.LimbCount; i++)
				{
					var left = a.GetLimbVectors2S(j);
					var right = a.GetLimbVectors2S(i);

					MultiplyVecs(left, right, products);

					if (i > j)
					{
						//product *= 2;
						for (var q = 0; q < products.Length; q++)
						{
							products[q] = Avx2.ShiftLeftLogical(products[q],1);
						}
					}

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectors2L(result[resultPtr]);
					var resultHighs = GetLimbVectors2L(result[resultPtr + 1]);

					var productHighs = GetLimbVectors2L(_productHighsMem);
					var productLows = GetLimbVectors2L(_productLowsMem);

					Split(products, productHighs, productLows);

					for (var p = 0; p < products.Length; p++)
					{
						resultLows[p] = Avx2.Add(resultLows[p], productLows[p]);
						resultHighs[p] = Avx2.Add(resultHighs[p], productHighs[p]);
					}
				}
			}

			return result;
		}

		private void MultiplyVecs(Span<Vector256<uint>> left, Span<Vector256<uint>> right, Span<Vector256<ulong>> result)
		{
			foreach (var idx in InPlayList)
			{
				result[idx] = Avx2.Multiply(left[idx], right[idx]);
			}
		}

		private void Split(Span<Vector256<ulong>> x, Span<Vector256<ulong>> highs, Span<Vector256<ulong>> lows)
		{
			for (var i = 0; i < x.Length; i++)
			{
				highs[i] = Avx2.And(x[i], HIGH_MASK_VEC);	// Create new ulong from bits 32 - 63.
				lows[i] = Avx2.And(x[i], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.
			}
		}

		private Memory<ulong>[] PropagateCarries(Memory<ulong>[] mantissaMems)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			if (mantissaMems.Length == 0)
			{
				return mantissaMems;
			}


			var result = _squareResult2Mems;

			var carries = GetLimbVectors2L(_carriesMem);
			var withCarries = GetLimbVectors2L(_withCarriesMem);

			var limbVecs = GetLimbVectors2L(mantissaMems[0]);
			var resultLimbVecs = GetLimbVectors2L(result[0]);
			Split(limbVecs, carries, resultLimbVecs);

			for (int i = 0; i < mantissaMems.Length; i++)
			{
				limbVecs = GetLimbVectors2L(mantissaMems[i]);
				resultLimbVecs = GetLimbVectors2L(result[i]);

				AddVecs(limbVecs, carries, withCarries);
				Split(withCarries, carries, resultLimbVecs);
			}

			// TODO: Check to see if any of the carries have any zero value.
			//if (carry != 0)
			//{
			//	throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			//}

			return result;
		}

		#endregion

		#region Add and Subtract

		public void Sub(FPValues a, FPValues b, FPValues c)
		{
		}

		public static Smx Sub(Smx a, Smx b)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = new Smx(!b.Sign, b.Mantissa, b.Exponent, b.Precision, a.BitsBeforeBP);
			var result = Add(a, bNegated);

			return result;
		}

		public void Add(FPValues a, FPValues b, FPValues c)
		{
			
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
		
		//private Memory<ulong>[] AddF(FPValues a, FPValues b)
		//{
		//	Debug.Assert(a.Length == b.Length);

		//	var result = _addResult1Mem;

		//	Span<Vector<ulong>> carries = GetLimbVectors(_carriesMem);
		//	Span<Vector<ulong>> withCarries = GetLimbVectors(_withCarriesMem);

		//	for (var i = 0; i < a.Length; i++)
		//	{
		//		var limbVecsA = GetLimbVectors(a.MantissaMemories[i]);
		//		var limbVecsB = GetLimbVectors(b.MantissaMemories[i]);
		//		var resultLimbVecs = GetLimbVectors(result[i]);

		//		AddVecs(limbVecsA, limbVecsB, withCarries);

		//		if (i > 0)
		//		{
		//			// add the caries produced from splitting the previous limb's
		//			AddVecs(withCarries, carries, withCarries);
		//		}

		//		Split(x:withCarries, highs:carries, lows:resultLimbVecs);
		//	}

		//	//if (carry != 0)
		//	//{
		//	//	// Add a Limb
		//	//	var newResult = Extend(result, resultLength + 1);
		//	//	newResult[^1] = carry;
		//	//	return newResult;
		//	//}
		//	//else if (indexOfLastNonZeroLimb < resultLength - 1)
		//	//{
		//	//	// Trim leading zeros
		//	//	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

		//	//	return newResult;
		//	//}
		//	//else
		//	//{
		//	//	return result;
		//	//}


		//	return result;
		//}

		private ulong[] Add(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		{
			Debug.Assert(left.Length == right.Length);

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			var indexOfLastNonZeroLimb = 0;
			var carry = 0ul;

			for (var i = 0; i < resultLength; i++)
			{
				var nv = left[i] + right[i] + carry;
				result[i] = SmxMathHelper.Split(nv, out carry);
				indexOfLastNonZeroLimb = result[i] == 0 ? indexOfLastNonZeroLimb : i;
			}

			//if (carry != 0)
			//{
			//	// Add a Limb
			//	var newResult = _smxMathHelper.Extend(result, resultLength + 1);
			//	newResult[^1] = carry;
			//	return newResult;
			//}
			////else if (indexOfLastNonZeroLimb < resultLength - 1)
			////{
			////	// Trim leading zeros
			////	var newResult = CopyFirstXElements(result, indexOfLastNonZeroLimb + 1);

			////	return newResult;
			////}
			//else
			//{
			//	return result;
			//}

			return result;
		}

		private ulong[] Sub(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		{
			Debug.Assert(left.Length == right.Length);

			var resultLength = left.Length;
			var result = new ulong[resultLength];

			var indexOfLastNonZeroLimb = 0;
			var borrow = 0ul;

			for (var i = 0; i < resultLength - 1; i++)
			{
				// Set the lsb of the high part of a.
				var sax = left[i] | TEST_BIT_32;

				result[i] = sax - right[i] - borrow;

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

			if (left[^1] < (right[^1] + borrow))
			{
				throw new OverflowException("MSB too small.");
			}

			result[^1] = left[^1] - right[^1] - borrow;

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

			return result;
		}

		private void AddVecs(Span<Vector256<ulong>> left, Span<Vector256<ulong>> right, Span<Vector256<ulong>> result)
		{
			for (var i = 0; i < left.Length; i++)
			{
				result[i] = Avx2.Add(left[i], right[i]);
			}
		}

		#endregion

		#region Normalization Support

		public Memory<ulong>[] ShiftAndTrim(Memory<ulong>[] mantissaMems)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			var result = _squareResult3Mems;

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int i = 0; i < result.Length; i++)
			{
				var resultLimbVecs = GetLimbVectors2L(result[i]);
				var limbVecs = GetLimbVectors2L(mantissaMems[i + sourceIndex]);

				if (sourceIndex > 0)
				{
					var prevLimbVecs = GetLimbVectors2L(mantissaMems[i + sourceIndex - 1]);
					ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);
				}
				else
				{
					ShiftAndCopyBits(limbVecs, resultLimbVecs);
				}
			}

			return result;
		}

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> prevSource, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			foreach (var idx in InPlayList)
			{
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);

				// Take the top shiftAmount of bits from the previous limb
				result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(prevSource[idx], (byte)(32 - shiftAmount)));
			}
		}

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			foreach (var idx in InPlayList)
			{
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);
			}
		}

		//private Vector<ulong>[][] CopyFirstXElements(Vector<ulong>[][] values, int newLimbCount)
		//{
		//	var result = new Vector<ulong>[newLimbCount][];

		//	for (var i = 0; i < newLimbCount; i++)
		//	{
		//		result[i] = values[i];	
		//	}

		//	return result;
		//}

		#endregion

		#region Comparison

		//private int Compare(ShiftedArray<ulong> left, ShiftedArray<ulong> right)
		//{
		//	if (left.Length != right.Length)
		//	{
		//		throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
		//	}

		//	var i = -1 + Math.Min(left.Length, right.Length);

		//	for (; i >= 0; i--)
		//	{
		//		if (left[i] != right[i])
		//		{
		//			return left[i] > right[i] ? 1 : -1;
		//		}
		//	}

		//	return 0;
		//}

		//private int GetNumberOfSignificantB32Digits(ShiftedArray<ulong> mantissa)
		//{
		//	var i = mantissa.Length;
		//	for (; i > 0; i--)
		//	{
		//		if (mantissa[i - 1] != 0)
		//		{
		//			break;
		//		}
		//	}

		//	return i;
		//}

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

		private bool IsGreaterOrEqThan(ulong[][] mantissas, int valueIndex, int exponent, uint b)
		{
			var mslWeight = Math.Pow(2, exponent + (mantissas.Length - 1) * 32); // TODO: Make this a member field.
			var val = mantissas[^1][valueIndex] * mslWeight;

			var result = val >= b;

			return result;

			//var aAsDouble = 0d;

			//for (var i = mantissas.Length - 1; i >= 0; i--)
			//{
			//	aAsDouble += mantissas[i][valueIndex] * Math.Pow(2, exponent + (i * 32));

			//	if (aAsDouble >= b)
			//	{
			//		return true;
			//	}
			//}

			//return false;
		}

		#endregion
	}
}
