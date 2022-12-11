﻿using MSS.Common;
using MSS.Types;
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

		private static readonly int _lanes = Vector256<ulong>.Count;

		private Memory<ulong>[] _squareResult1Mems;

		private ulong[][] _squareResult2Ba;
		private Memory<ulong>[] _squareResult2Mems;

		private Vector256<ulong>[] _productVectors;

		private Vector256<long> _thresholdVector;
		private Vector256<ulong> _zeroVector;

		private Memory<ulong>[] _addResult1Mem;

		#endregion

		#region Constructor

		public SmxVecMathHelper(ApFixedPointFormat apFixedPointFormat, uint threshold, bool[] doneFlags)
		{
			ValueCount = doneFlags.Length;
			VecCount = Math.DivRem(ValueCount, _lanes, out var remainder);

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

			MslWeight = Math.Pow(2, TargetExponent + (LimbCount - 1) * BITS_PER_LIMB);
			MslWeightVector = Vector256.Create(MslWeight);

			_thresholdVector = BuildTheThresholdVector(threshold);
			_zeroVector = Vector256<ulong>.Zero;

			InPlayList = BuildTheInplayList(doneFlags, VecCount);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);

			_squareResult2Ba = BuildMantissaBackingArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(_squareResult2Ba);

			_productVectors = new Vector256<ulong>[VecCount];

			_addResult1Mem = BuildMantissaMemoryArray(LimbCount, ValueCount);
		}

		private List<int> BuildTheInplayList(bool[] doneFlags, int vecCount)
		{
			var result = Enumerable.Range(0, vecCount).ToList();

			for (int j = 0; j < vecCount; j++)
			{
				var arrayPtr = j * _lanes;

				for(var lanePtr = 0; lanePtr < _lanes; lanePtr++)
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

		private Memory<ulong>[] BuildMantissaMemoryArray(int limbCount, int valueCount)
		{
			var ba = BuildMantissaBackingArray(limbCount, valueCount);
			var result = BuildMantissaMemoryArray(ba);

			return result;
		}


		private ulong[][] BuildMantissaBackingArray(int limbCount, int valueCount)
		{
			var result = new ulong[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new ulong[valueCount];
			}

			return result;
		}

		private Memory<ulong>[] BuildMantissaMemoryArray(ulong[][] backingArray)
		{
			var result = new Memory<ulong>[backingArray.Length];

			for (var i = 0; i < backingArray.Length; i++)
			{
				result[i] = new Memory<ulong>(backingArray[i]);
			}

			return result;
		}

		private Vector256<long> BuildTheThresholdVector(uint threshold)
		{
			if (threshold > MaxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat: {ApFixedPointFormat}.");
			}

			var smxMathHelper = new SmxMathHelper(ApFixedPointFormat, threshold);
			var thresholdMslIntegerVector = Vector256.Create(smxMathHelper.ThresholdMsl);
			var thresholdVector = thresholdMslIntegerVector.AsInt64();

			return thresholdVector;
		}

		private Span<Vector256<ulong>> GetLimbVectorsUL(Memory<ulong> memory)
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}

		private Span<Vector256<uint>> GetLimbVectorsUW(Memory<ulong> memory)
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
		public double MslWeight { get; init; }

		public Vector256<double> MslWeightVector { get; init; }

		public int BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }

		#endregion

		#region Multiply and Square

		public void Square(FPValues a, FPValues result)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			SquareInternal(a, _squareResult1Mems);
			PropagateCarries(_squareResult1Mems, _squareResult2Mems);
			ShiftAndTrim(_squareResult2Mems, result.MantissaMemories);
		}

		private void SquareInternal(FPValues a, Memory<ulong>[] resultLimbs)
		{
			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < a.LimbCount; j++)
			{
				for (int i = j; i < a.LimbCount; i++)
				{
					var left = a.GetLimbVectorsUW(j);
					var right = a.GetLimbVectorsUW(i);

					foreach (var idx in InPlayList)
					{
						_productVectors[idx] = Avx2.Multiply(left[idx], right[idx]);
					}

					if (i > j)
					{
						//product *= 2;

						foreach (var idx in InPlayList)
						{
							_productVectors[idx] = Avx2.ShiftLeftLogical(_productVectors[idx], 1);
						}
					}

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectorsUL(resultLimbs[resultPtr]);
					var resultHighs = GetLimbVectorsUL(resultLimbs[resultPtr + 1]);

					//Split(_productVectors, _productHighVectors, _productLowVectors);
					foreach (var idx in InPlayList)
					{
						var highs = Avx2.ShiftRightLogical(_productVectors[idx], 32);   // Create new ulong from bits 32 - 63.
						var lows = Avx2.And(_productVectors[idx], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);
					}

					//foreach (var idx in InPlayList)
					//{
					//	resultLows[idx] = Avx2.Add(resultLows[idx], _productLowVectors[idx]);
					//	resultHighs[idx] = Avx2.Add(resultHighs[idx], _productHighVectors[idx]);
					//}
				}
			}
		}

		private void PropagateCarries(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var intermediateLimbCount = mantissaMems.Length;

			foreach (var idx in InPlayList)
			{
				var limbVecs = GetLimbVectorsUL(mantissaMems[0]);
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[0]);

				var carries = Avx2.ShiftRightLogical(limbVecs[idx], 32); // The high 32 bits of sum becomes the new carry.
				resultLimbVecs[idx] = Avx2.And(limbVecs[idx], LOW_MASK_VEC);    // The low 32 bits of the sum is the result.

				for (int i = 1; i < intermediateLimbCount; i++)
				{
					limbVecs = GetLimbVectorsUL(mantissaMems[i]);
					resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);

					var withCarries = Avx2.Add(limbVecs[idx], carries);

					carries = Avx2.ShiftRightLogical(withCarries, 32);			// The high 32 bits of sum becomes the new carry.
					resultLimbVecs[idx] = Avx2.And(withCarries, LOW_MASK_VEC);	// The low 32 bits of the sum is the result.
				}

				var isZeroFlags = Avx2.CompareEqual(carries, _zeroVector);
				var isZeroComposite = (uint)Avx2.MoveMask(isZeroFlags.AsByte());

				if (isZeroComposite != 0xffffffff)
				{
					// At least one carry is not zero.

					var valuePtr = idx * _lanes;
					for (var i = 0; i < _lanes; i++)
					{
						if (carries.GetElement(i) > 0)
						{
							//throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
							SetMantissa(_squareResult2Ba, valuePtr + i, new ulong[] { 0, _maxValueBeforeShift });
							NumberOfMCarries++;
						}
					}
				}

			}
		}

		private const ulong _maxValueBeforeShift = 16711679;

		public void SetMantissa(ulong[][] mantissas, int index, ulong[] values)
		{
			for (var i = 0; i < values.Length; i++)
			{
				mantissas[i][index] = values[i];
			}
		}

		#endregion

		#region Add and Subtract

		public void Sub(FPValues a, FPValues b, FPValues c)
		{
			Add(a, b.Negate(), c);
		}

		public void Add(FPValues a, FPValues b, FPValues c)
		{
			var signsA = a.GetSignVectorsUL();
			var signsB = b.GetSignVectorsUL();

			foreach (var idx in InPlayList)
			{
				Vector256<ulong> areEqualFlags = Avx2.CompareEqual(signsA[idx], signsB[idx]);
				var areEqualComposite = (uint) Avx2.MoveMask(areEqualFlags.AsByte());

				if (areEqualComposite == 0xffffffff)
				{
					var resultPtr = idx * _lanes;

					for (var i = 0; i < _lanes; i++)
					{
						c.SetSign(resultPtr + i, a.GetSign(resultPtr + i));
					}

					AddInternal(idx, a, b, c);
				}
				else
				{
					var comps = Compare(idx, a.MantissaMemories, b.MantissaMemories);
					//var dComps = string.Join(", ", comps);
					//Debug.WriteLine($"{dComps}");

					SubInternal(idx, comps, a, b, c);
				}
			}
		}

		private void AddInternal(int idx, FPValues a, FPValues b, FPValues c)
		{
			var resultPtr = idx * _lanes;

			for (var i = 0; i < _lanes; i++)
			{
				var result = Add(a.GetMantissa(resultPtr + i), b.GetMantissa(resultPtr + i), out var carry);

				if (carry > 0)
				{
					result = CreateNewMaxIntegerSmx().Mantissa;
					NumberOfACarries++;
				}

				c.SetMantissa(resultPtr + i, result);
			}
		}

		private void SubInternal(int idx, int[] comps, FPValues a, FPValues b, FPValues c)
		{
			var resultPtr = idx * _lanes;

			for (var i = 0; i < comps.Length; i++)
			{
				var comp = comps[i];
				if (comp == 0)
				{
					c.SetMantissa(resultPtr + i, a.GetMantissa(resultPtr + i));
					c.SetSign(resultPtr + i, a.GetSign(resultPtr + i));
				}
				else if (comp > 0)
				{
					var result = Sub(a.GetMantissa(resultPtr + i), b.GetMantissa(resultPtr + i));
					c.SetMantissa(resultPtr + i, result);
					c.SetSign(resultPtr + i, a.GetSign(resultPtr + i));
				}
				else
				{
					var result = Sub(b.GetMantissa(resultPtr + i), a.GetMantissa(resultPtr + i));
					c.SetMantissa(resultPtr + i, result);
					c.SetSign(resultPtr + i, b.GetSign(resultPtr + i));
				}
			}

			//return signs;
		}


		private void AddInternalVec(int idx, Memory<ulong>[] leftMantissaMems, Memory<ulong>[] rightMantissaMems, Memory<ulong>[] resultLimbs, Vector256<ulong> carries)
		{

		}

		private void SubInternalVec(int idx, int[] comps, Memory<ulong>[] leftMantissaMems, Memory<ulong>[] rightMantissaMems, Memory<ulong>[] resultLimbs)
		{
			foreach(var comp in comps)
			{
				if (comp == 0)
				{
				}
				else if (comp > 0)
				{

				}
				else
				{

				}
			}
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

		//		Split(x: withCarries, highs: carries, lows: resultLimbVecs);
		//	}

		//	return result;
		//}

		//private bool AllAreEqual(Vector256<ulong> r)
		//{
		//	var f = r.GetElement(0);

		//	for (var i = 1; i < _lanes; i++)
		//	{
		//		if (r.GetElement(i) != f)
		//		{
		//			return false;
		//		}
		//	}

		//	return true;
		//}

		#endregion

		#region Add Subtract Scalar

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
				var nv = left[i] + right[i] + carry;
				var lo = Split(nv, out carry);
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

			}

			if (left[^1] < (right[^1] + borrow))
			{
				// TOOD: Since we always call sub with the left argument > the right argument, then this should never occur.
				//throw new OverflowException("MSB too small.");
			}

			result[^1] = left[^1] - right[^1] - borrow;

			return result;
		}

		private ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & LOW_MASK; // Create new ulong from bits 0 - 31.
		}

		#endregion

		#region Create Smx Support

		public Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(fPValues.GetSign(index), fPValues.GetMantissa(index), TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(true, new ulong[LimbCount], TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var mantissa = new ulong[LimbCount];
			mantissa[^1] = (ulong)(MaxIntegerValue * Math.Pow(2, BITS_PER_LIMB - BitsBeforeBP));

			var result = new Smx(true, mantissa, TargetExponent, precision, BitsBeforeBP);

			return result;
		}

		#endregion

		#region Normalization Support

		public void ShiftAndTrim(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int i = 0; i < resultLimbs.Length; i++)
			{
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);
				var limbVecs = GetLimbVectorsUL(mantissaMems[i + sourceIndex]);

				if (sourceIndex > 0)
				{
					var prevLimbVecs = GetLimbVectorsUL(mantissaMems[i + sourceIndex - 1]);
					ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);
				}
				else
				{
					ShiftAndCopyBits(limbVecs, resultLimbVecs);
				}
			}
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

		#endregion

		#region Comparison

		//private int[] Compare(int idx, Memory<ulong>[] mantissaMemsA, Memory<ulong>[] mantissaMemsB)
		//{
		//	return new int[0];
		//}


		private int[] Compare(int idx, Memory<ulong>[] mantissaMemsA, Memory<ulong>[] mantissaMemsB)
		{
			var limbPtr = LimbCount - 1;

			var limb0A = GetLimbVectorsUL(mantissaMemsA[limbPtr]);
			var limb0B = GetLimbVectorsUL(mantissaMemsB[limbPtr]);

			//var a = Vector256.AsInt64(limb0A[idx]);
			//var b = Vector256.AsInt64(limb0B[idx]);

			var areEqualFlags = Avx2.CompareEqual(limb0A[idx], limb0B[idx]);
			var compositeFlags = (uint)Avx2.MoveMask(areEqualFlags.AsByte());

			while(--limbPtr >= 0 && compositeFlags == 0xffffffff)
			{
				limb0A = GetLimbVectorsUL(mantissaMemsA[limbPtr]);
				limb0B = GetLimbVectorsUL(mantissaMemsB[limbPtr]);

				areEqualFlags = Avx2.CompareEqual(limb0A[idx], limb0B[idx]);
				compositeFlags = (uint) Avx2.MoveMask(areEqualFlags.AsByte());
			}

			if (compositeFlags == 0xffffffff)
			{
				// All comparisons return equal
				return new int[_lanes];
			}

			var areGtFlags = (Avx2.CompareGreaterThan(limb0A[idx].AsInt64(), limb0B[idx].AsInt64())).AsUInt64();
			compositeFlags = (uint)Avx2.MoveMask(areGtFlags.AsByte());

			if (compositeFlags == 0xffffffff)
			{
				// All comparisons return greater than
				return Enumerable.Repeat(1, _lanes).ToArray();
			}

			if (compositeFlags == 0x0)
			{
				/// All comparisons return less than
				return Enumerable.Repeat(-1, _lanes).ToArray();
			}

			// Compare each pair, individually
			var result = new int[_lanes];

			var eqFlags = new ulong[_lanes];
			areEqualFlags.AsVector().CopyTo(eqFlags);

			var gtFlags = new ulong[_lanes];
			areGtFlags.AsVector().CopyTo(gtFlags);

			for(var i = 0; i < _lanes; i++)
			{
				if (eqFlags[i] != 0)
				{
					result[i] = (gtFlags[i] == 0xffffffff ? 1 : -1);
				}
				else
				{
					var j = limbPtr - 1;
					for (; j >= 0; j--)
					{
						limb0A = GetLimbVectorsUL(mantissaMemsA[limbPtr]);
						limb0B = GetLimbVectorsUL(mantissaMemsB[limbPtr]);

						var a = limb0A[idx].GetElement(i);
						var b = limb0B[idx].GetElement(i);

						if (a != b)
						{
							result[i] = a > b ? 1 : -1;
							break;
						}
					}
				}
			}

			return result;
		}

		public void IsGreaterOrEqThanThreshold(FPValues a, Span<Vector256<long>> escapedFlagVectors)
		{
			var left = a.GetLimbVectorsUL(LimbCount - 1);
			var right = _thresholdVector;

			IsGreaterOrEqThan(left, right, escapedFlagVectors);
		}

		private void IsGreaterOrEqThan(Span<Vector256<ulong>> left, Vector256<long> right, Span<Vector256<long>> result)
		{
			foreach (var idx in InPlayList)
			{
				//var ta = new ulong[_ulongSlots];
				//left[idx].AsVector().CopyTo(ta);

				//var bi = SmxMathHelper.FromPwULongs(ta);
				//var rv = new RValue(bi, -24);

				//var rvS = RValueHelper.ConvertToString(rv);

				var x = Vector256.AsInt64(left[idx]);
				var y = Avx2.CompareGreaterThan(x, right);

				result[idx] = y;
			}
		}

		#endregion

		#region TEMPLATES

		private void MultiplyVecs(Span<Vector256<uint>> left, Span<Vector256<uint>> right, Span<Vector256<ulong>> result)
		{
			foreach (var idx in InPlayList)
			{
				result[idx] = Avx2.Multiply(left[idx], right[idx]);
			}
		}

		private void Split(Span<Vector256<ulong>> x, Span<Vector256<ulong>> highs, Span<Vector256<ulong>> lows)
		{
			foreach (var idx in InPlayList)
			{
				highs[idx] = Avx2.And(x[idx], HIGH_MASK_VEC);   // Create new ulong from bits 32 - 63.
				lows[idx] = Avx2.And(x[idx], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.
			}
		}

		private void AddVecs(Span<Vector256<ulong>> left, Span<Vector256<ulong>> right, Span<Vector256<ulong>> result)
		{
			for (var i = 0; i < left.Length; i++)
			{
				result[i] = Avx2.Add(left[i], right[i]);
			}
		}

		#endregion

	}
}
