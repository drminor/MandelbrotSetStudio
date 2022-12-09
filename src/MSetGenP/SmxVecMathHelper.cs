using MSS.Common;
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

		private static readonly int _ulongSlots = Vector<ulong>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private Vector256<ulong>[] _productVectors;
		private Vector256<ulong>[] _productHighVectors;
		private Vector256<ulong>[] _productLowVectors;

		private Vector256<ulong>[] _carryVectors;
		private Vector256<ulong>[] _withCarryVectors;

		private Vector256<long> _thresholdVector;
		private Vector256<ulong> _zeroVector;

		private Memory<ulong>[] _addResult1Mem;

		#endregion

		#region Constructor

		public SmxVecMathHelper(ApFixedPointFormat apFixedPointFormat, uint threshold, bool[] doneFlags)
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

			MslWeight = Math.Pow(2, TargetExponent + (LimbCount - 1) * BITS_PER_LIMB);
			MslWeightVector = Vector256.Create(MslWeight);

			_thresholdVector = BuildTheThresholdVector(threshold);
			_zeroVector = Vector256<ulong>.Zero;

			InPlayList = BuildTheInplayList(doneFlags, VecCount);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);

			_productVectors = new Vector256<ulong>[VecCount];
			_productHighVectors = new Vector256<ulong>[VecCount];
			_productLowVectors = new Vector256<ulong>[VecCount];

			_carryVectors = new Vector256<ulong>[VecCount];
			_withCarryVectors = new Vector256<ulong>[VecCount];


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

		private Memory<ulong>[] BuildMantissaMemoryArray(int limbCount, int valueCount)
		{
			var result = new Memory<ulong>[limbCount];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new Memory<ulong>(new ulong[valueCount]);
			}

			return result;
		}

		private Vector256<long> BuildTheThresholdVector(uint threshold)
		{
			if (threshold > MaxIntegerValue)
			{
				throw new ArgumentException($"The threshold must be less than or equal to the maximum integer value supported by the ApFixedPointformat: {ApFixedPointFormat}.");
			}

			var smxMathHelper = new SmxMathHelper(ApFixedPointFormat);

			var thresholdSmx = smxMathHelper.CreateSmx(new RValue(threshold, 0)); // TODO subtract 

			var thresholdMsl = thresholdSmx.Mantissa[^1] - 1; // Subtract 1 * 2^-24
			var thresholdMslIntegerVector = Vector256.Create((ulong)thresholdMsl);
			var thresholdVector = Vector256.AsInt64(thresholdMslIntegerVector);

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

					MultiplyVecs(left, right, _productVectors);

					if (i > j)
					{
						//product *= 2;
						for (var q = 0; q < _productVectors.Length; q++)
						{
							_productVectors[q] = Avx2.ShiftLeftLogical(_productVectors[q],1);
						}
					}

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectorsUL(resultLimbs[resultPtr]);
					var resultHighs = GetLimbVectorsUL(resultLimbs[resultPtr + 1]);

					Split(_productVectors, _productHighVectors, _productLowVectors);

					for (var p = 0; p < _productVectors.Length; p++)
					{
						resultLows[p] = Avx2.Add(resultLows[p], _productLowVectors[p]);
						resultHighs[p] = Avx2.Add(resultHighs[p], _productHighVectors[p]);
					}
				}
			}
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
			foreach (var idx in InPlayList)
			{
				highs[idx] = Avx2.And(x[idx], HIGH_MASK_VEC);	// Create new ulong from bits 32 - 63.
				lows[idx] = Avx2.And(x[idx], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.
			}
		}

		private void PropagateCarries(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			if (mantissaMems.Length == 0)
			{
				return;
			}

			var limbVecs = GetLimbVectorsUL(mantissaMems[0]);
			var resultLimbVecs = GetLimbVectorsUL(resultLimbs[0]);
			Split(limbVecs, _carryVectors, resultLimbVecs);

			for (int i = 0; i < mantissaMems.Length; i++)
			{
				limbVecs = GetLimbVectorsUL(mantissaMems[i]);
				resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);

				AddVecs(limbVecs, _carryVectors, _withCarryVectors);
				Split(_withCarryVectors, _carryVectors, resultLimbVecs);
			}

			// TODO: Check to see if any of the carries have any zero value.
			//if (carry != 0)
			//{
			//	throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
			//}
		}

		private void AddVecs(Span<Vector256<ulong>> left, Span<Vector256<ulong>> right, Span<Vector256<ulong>> result)
		{
			for (var i = 0; i < left.Length; i++)
			{
				result[i] = Avx2.Add(left[i], right[i]);
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
			var signsC = c.GetSignVectorsUL();

			foreach (var idx in InPlayList)
			{
				Vector256<ulong> se = Avx2.CompareEqual(signsA[idx], signsB[idx]);

				if (AllAreEqual(se))
				{
					signsB.CopyTo(signsC);
					AddInternal(idx, a.MantissaMemories, b.MantissaMemories, c.MantissaMemories, _carryVectors[idx]);
				}
				else
				{
					var comps = Compare(idx, a.MantissaMemories, b.MantissaMemories);
					var dComps = string.Join(", ", comps);
					Debug.WriteLine($"{dComps}");

					//var arrayPtr = idx * _ulongSlots;

				}
			}
		}

		private void AddInternal(int idx, Memory<ulong>[] leftMantissaMems, Memory<ulong>[] rightMantissaMems, Memory<ulong>[] resultLimbs, Vector256<ulong> carries)
		{

		}

		private void SubInternal(int idx, Memory<ulong>[] leftMantissaMems, Memory<ulong>[] rightMantissaMems, Memory<ulong>[] resultLimbs)
		{

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

		private bool AllAreEqual(Vector256<ulong> r)
		{
			var f = r.GetElement(0);

			for (var i = 1; i < _ulongSlots; i++)
			{
				if (r.GetElement(i) != f)
				{
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Add Subtract Scalar

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
				throw new OverflowException("MSB too small.");
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

		public Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(true, new ulong[LimbCount], TargetExponent, precision, BitsBeforeBP);
			return result;
		}

		public Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			//var result = CreateSmx(new RValue(MaxIntegerValue, 0, precision));

			var mantissa = new ulong[LimbCount];
			mantissa[^1] = (ulong)(MaxIntegerValue * Math.Pow(2, BITS_PER_LIMB - BitsBeforeBP));

			var result = new Smx(true, mantissa, TargetExponent, precision, BitsBeforeBP);

			return result;
		}

		//public Smx CreateSmx(RValue rValue)
		//{
		//	var partialWordLimbs = SmxMathHelper.ToPwULongs(rValue.Value);

		//	var magnitude = GetMagnitudeOfIntegerPart(partialWordLimbs, rValue.Exponent);
		//	if (magnitude > BitsBeforeBP)
		//	{
		//		// magnitude is the exponent of the most significant bit from set of BitsBeforeBP at the top of the most significant limb.
		//		throw new ArgumentException($"An RValue with integer portion > {MaxIntegerValue} cannot be used to create an Smx.");
		//	}

		//	//(var limbIndex, var bitOffset) = Math.DivRem(shiftAmount, BITS_PER_LIMB);

		//	//var sourceIndex = 0; // - limbIndex;
		//	//var sourceOffset = fpFormat.BitsBeforeBinaryPoint;

		//	//var newPartialWordLimbs = new ulong[LimbCount];
		//	//var destinationIndex = 0;
		//	//var destinationOffset = BitsBeforeBP;


		//	//CopyPWBits(partialWordLimbs, sourceIndex, sourceOffset, newPartialWordLimbs, destinationIndex, destinationOffset);
		//	//var result = new Smx(sign, newPartialWordLimbs, exponent, precision, BitsBeforeBP);
		//	//return result;

		//	ulong[] newPartialWordLimbs;
		//	var shiftAmount = GetShiftAmount(rValue.Exponent, TargetExponent);

		//	if (shiftAmount == 0)
		//	{
		//		newPartialWordLimbs = TakeMostSignificantLimbs(partialWordLimbs, LimbCount);
		//	}
		//	else if (shiftAmount < 0)
		//	{
		//		throw new NotImplementedException();
		//	}
		//	else
		//	{
		//		var sResult = ScaleAndSplit(partialWordLimbs, shiftAmount, "Create Smx");

		//		newPartialWordLimbs = TakeMostSignificantLimbs(sResult, LimbCount);
		//	}

		//	var sign = rValue.Value >= 0;
		//	var exponent = TargetExponent;
		//	var precision = rValue.Precision;
		//	var bitsBeforeBP = BitsBeforeBP;
		//	var result = new Smx(sign, newPartialWordLimbs, exponent, precision, bitsBeforeBP);

		//	return result;
		//}

		//private ulong[] ScaleAndSplit(ulong[] mantissa, int power, string desc)
		//{
		//	if (power <= 0)
		//	{
		//		throw new ArgumentException("The value of power must be 1 or greater.");
		//	}

		//	(var limbOffset, var remainder) = Math.DivRem(power, BITS_PER_LIMB);

		//	if (limbOffset > LimbCount + 3)
		//	{
		//		return new ulong[] { 0 };
		//	}

		//	var factor = (ulong)Math.Pow(2, remainder);

		//	var resultArray = new ulong[mantissa.Length];

		//	var carry = 0ul;

		//	var indexOfLastNonZeroLimb = -1;
		//	for (var i = 0; i < mantissa.Length; i++)
		//	{
		//		var newLimbVal = mantissa[i] * factor + carry;
		//		var lo = Split(newLimbVal, out carry); // :Spliter
		//		resultArray[i] = lo;

		//		if (lo > 0)
		//		{
		//			indexOfLastNonZeroLimb = i;
		//		}
		//	}

		//	if (indexOfLastNonZeroLimb > -1)
		//	{
		//		indexOfLastNonZeroLimb += limbOffset;
		//	}

		//	var resultSa = new ShiftedArray<ulong>(resultArray, limbOffset, indexOfLastNonZeroLimb);

		//	if (carry > 0)
		//	{
		//		//Debug.WriteLine($"While {desc}, setting carry: {carry}, ll: {result.IndexOfLastNonZeroLimb}, len: {result.Length}, power: {power}, factor: {factor}.");
		//		resultSa.SetCarry(carry);
		//	}

		//	var result = resultSa.MaterializeAll();

		//	return result;
		//}

		//private byte GetMagnitudeOfIntegerPart(ulong[] partialWordLimbs, int exponent)
		//{
		//	var bitsBeforeBP = GetNumberOfBitsBeforeBP(partialWordLimbs.Length, exponent);

		//	if (bitsBeforeBP <= 0)
		//	{
		//		// There are no bits present in the partialWordLimbs that are beyond the binary point.
		//		return 0;
		//	}

		//	if (bitsBeforeBP > BITS_PER_LIMB)
		//	{
		//		// There are 32 or more bits present in the partialWordLims that are beyond the binary point. 
		//		// We only support 32 or less.
		//		throw new OverflowException("The integer portion is larger than uint max.");
		//	}

		//	var lzc = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);

		//	Debug.Assert(lzc >= 32 && lzc <= 64, "The MSL has a value larger than the max digit.");

		//	//var logicalLzc = lzc - 32; // (0, if lzc == 32, 32, if lzc == 64

		//	var indexOfMsb = 64 - lzc;  // 0, if lzc = 64, 32 if lzc = 32

		//	var indexOfBP = 32 - bitsBeforeBP;

		//	var sizeInBitsOfIntegerVal = Math.Max(indexOfMsb - indexOfBP, 0);

		//	//var diagVal = GetTopBits(partialWordLimbs[^1], bitsBeforeBP);

		//	//Debug.Assert(diagVal <= Math.Pow(2, sizeInBitsOfIntegerVal));

		//	return (byte)sizeInBitsOfIntegerVal;
		//}

		//private int GetNumberOfBitsBeforeBP(int limbCount, int exponent)
		//{
		//	var totalBits = BITS_PER_LIMB * limbCount;

		//	(var limbIndex, var bitOffset) = GetIndexOfBitBeforeBP(exponent);

		//	var fractionalBits = limbIndex * BITS_PER_LIMB + bitOffset;
		//	var bitsBeforeBP = totalBits - fractionalBits;

		//	return bitsBeforeBP;
		//}

		//private (int index, int offset) GetIndexOfBitBeforeBP(int exponent)
		//{
		//	// What is the index of the limb that is the 1's place 
		//	int limbIndex;
		//	int bitOffset;

		//	if (exponent == 0)
		//	{
		//		limbIndex = 0;
		//		bitOffset = 0;
		//	}
		//	else if (exponent > 0)
		//	{
		//		(limbIndex, bitOffset) = Math.DivRem(exponent, BITS_PER_LIMB);
		//		if (bitOffset > 0)
		//		{
		//			limbIndex--;
		//			bitOffset = BITS_PER_LIMB - bitOffset;
		//		}
		//	}
		//	else
		//	{
		//		(limbIndex, bitOffset) = Math.DivRem(-1 * exponent, BITS_PER_LIMB);
		//	}

		//	return (limbIndex, bitOffset);
		//}

		//private ulong[] TakeMostSignificantLimbs(ulong[] partialWordLimbs, int length)
		//{
		//	ulong[] result;

		//	if (partialWordLimbs.Length == length)
		//	{
		//		result = partialWordLimbs;
		//	}
		//	else if (partialWordLimbs.Length > length)
		//	{
		//		result = CopyLastXElements(partialWordLimbs, length);
		//	}
		//	else
		//	{
		//		result = Extend(partialWordLimbs, length);
		//	}

		//	return result;
		//}

		//// Pad with leading zeros.
		//private ulong[] Extend(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];
		//	Array.Copy(values, 0, result, 0, values.Length);

		//	return result;
		//}

		////private ulong[] CopyFirstXElements(ulong[] values, int newLength)
		////{
		////	var result = new ulong[newLength];
		////	Array.Copy(values, 0, result, 0, newLength);

		////	return result;
		////}

		//private ulong[] CopyLastXElements(ulong[] values, int newLength)
		//{
		//	var result = new ulong[newLength];

		//	var startIndex = Math.Max(values.Length - newLength, 0);

		//	var cLen = values.Length - startIndex;

		//	Array.Copy(values, startIndex, result, 0, cLen);

		//	return result;
		//}

		//private int GetShiftAmount(int currentExponent, int targetExponent)
		//{
		//	var shiftAmount = Math.Abs(targetExponent) - Math.Abs(currentExponent);
		//	return shiftAmount;
		//}

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

		private int[] Compare(int idx, Memory<ulong>[] mantissaMemsA, Memory<ulong>[] mantissaMemsB)
		{
			var limbPtr = _ulongSlots - 1;

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
				return new int[_ulongSlots];
			}

			var areGtFlags = (Avx2.CompareGreaterThan(limb0A[idx].AsInt64(), limb0B[idx].AsInt64())).AsUInt64();
			compositeFlags = (uint)Avx2.MoveMask(areGtFlags.AsByte());

			if (compositeFlags == 0xffffffff)
			{
				// All comparisons return greater than
				return Enumerable.Repeat(1, _ulongSlots).ToArray();
			}

			if (compositeFlags == 0x0)
			{
				/// All comparisons return less than
				return Enumerable.Repeat(-1, _ulongSlots).ToArray();
			}

			// Compare each pair, individually
			var result = new int[_ulongSlots];

			var eqFlags = new ulong[_ulongSlots];
			areEqualFlags.AsVector().CopyTo(eqFlags);

			var gtFlags = new ulong[_ulongSlots];
			areGtFlags.AsVector().CopyTo(gtFlags);

			for(var i = 0; i < _ulongSlots; i++)
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
				var x = Vector256.AsInt64(left[idx]);
				var y = Avx2.CompareGreaterThan(x, right);

				result[idx] = y;
			}
		}

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

		//public void IsGreaterOrEqThan(FPValues a, uint b, Span<Vector<ulong>> escapedFlagVectors)
		//{
		//	var escapedFlags = new bool[_ulongSlots];

		//	foreach (var idx in InPlayList)
		//	{
		//		var arrayPtr = idx * _ulongSlots;

		//		for (var i = 0; i < _ulongSlots ; i++)
		//		{
		//			escapedFlags[i] = IsGreaterOrEqThan(a.Mantissas, arrayPtr, b);
		//			arrayPtr++;
		//		}

		//		var escapeFlagVecValues = escapedFlags.Select(x => x ? 1ul : 0ul).ToArray();
		//		escapedFlagVectors[idx] = new Vector<ulong>(escapeFlagVecValues);
		//	}
		//}

		//public void IsGreaterOrEqThan(FPValues a, uint b, Span<Vector256<ulong>> escapedFlagVectors)
		//{
		//	var left = a.GetLimbVectors2UL(LimbCount - 1);
		//	var right = Vector256.Create((double)b);

		//	IsGreaterOrEqThan(left, right, escapedFlagVectors);
		//}

		//private void IsGreaterOrEqThan(Span<Vector256<ulong>> left, Vector256<double> right, Span<Vector256<ulong>> result)
		//{
		//	foreach (var idx in InPlayList)
		//	{
		//		var x = Vector256.AsDouble(left[idx]);
		//		var x2 = Avx.Multiply(x, MslWeightVector);

		//		var y = Avx.CompareGreaterThanOrEqual(x2, right);

		//		result[idx] = Vector256.AsUInt64(y);
		//	}
		//}

		//private bool IsGreaterOrEqThan(ulong[][] mantissas, int valueIndex, uint b)
		//{
		//	var val = mantissas[^1][valueIndex] * MslWeight;

		//	var result = val >= b;

		//	return result;

		//	//var aAsDouble = 0d;

		//	//for (var i = mantissas.Length - 1; i >= 0; i--)
		//	//{
		//	//	aAsDouble += mantissas[i][valueIndex] * Math.Pow(2, exponent + (i * 32));

		//	//	if (aAsDouble >= b)
		//	//	{
		//	//		return true;
		//	//	}
		//	//}

		//	//return false;
		//}

		#endregion
	}
}
