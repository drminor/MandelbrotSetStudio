using MSS.Common;
using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using static MongoDB.Driver.WriteConcern;

namespace MSetGenP
{
	public class VecMath : IVecMath
	{
		#region Private Properties

		public bool IsSigned => false;

		private const int EFFECTIVE_BITS_PER_LIMB = 31;
		private const int BITS_BEFORE_BP = 8;

		private static readonly ulong MAX_DIGIT_VALUE = (ulong) (-1 + Math.Pow(2, EFFECTIVE_BITS_PER_LIMB));

		//private const ulong HIGH_MASK = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		//private static readonly Vector256<ulong> HIGH_MASK_VEC = Vector256.Create(HIGH_MASK);


		private const ulong HIGH33_MASK = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC = Vector256.Create(HIGH33_MASK);

		private const ulong SIGN_BIT_MASK = 0x7FFFFFFFFFFFFFFF;
		private static readonly Vector256<ulong> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const ulong TOP_BITS_MASK = 0xFF00000000000000;
		private static readonly Vector256<ulong> TOP_BITS_MASK_VEC = Vector256.Create(TOP_BITS_MASK);

		//private static readonly ulong TEST_BIT_32 =	0x0000000100000000; // bit 32 is set.
		private static readonly ulong TEST_BIT_31 =		0x0000000080000000; // bit 31 is set.

		private static readonly int _lanes = Vector256<ulong>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;

		private ulong[][] _squareResult3Ba;
		private Memory<ulong>[] _squareResult3Mems;


		private Vector256<long> _thresholdVector;
		private Vector256<ulong> _zeroVector;
		private Vector256<long> _maxDigitValueVector;

		#endregion

		#region Constructor

		public VecMath(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			ValueCount = valueCount;
			VecCount = Math.DivRem(ValueCount, _lanes, out var remainder);

			if (remainder != 0)
			{
				throw new ArgumentException("The valueCount must be an even multiple of Vector<ulong>.Count.");
			}

			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, VecCount).ToArray();

			// Initially, all values are 'In Play.'
			DoneFlags = new bool[ValueCount];

			BlockPosition = new BigVector();
			RowNumber = 0;

			ApFixedPointFormat = apFixedPointFormat;
			Threshold = threshold;
			MaxIntegerValue = ScalarMathHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint, IsSigned);
			var thresholdMsl = ScalarMathHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);

			var thresholdMslIntegerVector = Vector256.Create(thresholdMsl);
			_thresholdVector = thresholdMslIntegerVector.AsInt64();

			var mslPower = ((LimbCount - 1) * EFFECTIVE_BITS_PER_LIMB) - FractionalBits;
			MslWeight = Math.Pow(2, mslPower);
			MslWeightVector = Vector256.Create(MslWeight);

			_zeroVector = Vector256<ulong>.Zero;
			_maxDigitValueVector = Vector256.Create((long)MAX_DIGIT_VALUE);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);

			_squareResult3Ba = BuildMantissaBackingArray(LimbCount * 2, ValueCount);
			_squareResult3Mems = BuildMantissaMemoryArray(_squareResult3Ba);
		}

		#endregion

		#region Mantissa Support

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

		private void ClearManatissMems(Memory<ulong>[] mantissaMems, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var indexes = InPlayList;

				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUL(mantissaMems[j]);

					for (var i = 0; i < indexes.Length; i++)
					{
						vectors[indexes[i]] = Vector256<ulong>.Zero;
					}
				}
			}
			else
			{
				for (var j = 0; j < mantissaMems.Length; j++)
				{
					var vectors = GetLimbVectorsUL(mantissaMems[j]);

					for (var i = 0; i < VecCount; i++)
					{
						vectors[i] = Vector256<ulong>.Zero;
					}
				}
			}
		}

		private void ClearBackingArray(ulong[][] backingArray, bool onlyInPlayItems)
		{
			if (onlyInPlayItems)
			{
				var template = new ulong[_lanes];

				var indexes = InPlayList;

				for (var j = 0; j < backingArray.Length; j++)
				{
					for (var i = 0; i < indexes.Length; i++)
					{
						Array.Copy(template, 0, backingArray[j], indexes[i] * _lanes, _lanes);
					}
				}
			}
			else
			{
				var vc = backingArray[0].Length;

				for (var j = 0; j < backingArray.Length; j++)
				{
					for (var i = 0; i < vc; i++)
					{
						backingArray[j][i] = 0;
					}
				}
			}
		}

		private Span<Vector256<ulong>> GetLimbVectorsUL(Memory<ulong> memory)
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }

		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public int[] InPlayList { get; set; }   // Vector-Level 
		public bool[] DoneFlags { get; set; }   // Value-Level
		public BigVector BlockPosition { get; set; }
		public int RowNumber { get; set; }

		public uint MaxIntegerValue { get; init; }

		public uint Threshold { get; init; }
		public double MslWeight { get; init; }
		public Vector256<double> MslWeightVector { get; init; }

		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Multiply and Square

		public void Square(FPValues a, FPValues result)
		{
			SquareInternal(a, _squareResult1Mems);
			PropagateCarries(_squareResult1Mems, _squareResult2Mems);
			ShiftAndTrim(_squareResult2Mems, result.MantissaMemories);
		}

		private void SquareInternal(FPValues a, Memory<ulong>[] resultLimbs)
		{
			ClearManatissMems(resultLimbs, onlyInPlayItems: true);

			var indexes = InPlayList;

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < a.LimbCount; j++)
			{
				for (int i = j; i < a.LimbCount; i++)
				{
					var left = a.GetLimbVectorsUW(j);
					var right = a.GetLimbVectorsUW(i);

					var resultPtr = j + i;  // 0, 1, 1, 2

					var resultLows = GetLimbVectorsUL(resultLimbs[resultPtr]);
					var resultHighs = GetLimbVectorsUL(resultLimbs[resultPtr + 1]);

					for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var productVector = Avx2.Multiply(left[idx], right[idx]);

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var lows = Avx2.And(productVector, HIGH33_MASK_VEC);    // Create new ulong from bits 0 - 30.
						var highs = Avx2.ShiftRightLogical(productVector, EFFECTIVE_BITS_PER_LIMB);   // Create new ulong from bits 31 - 62.

						resultLows[idx] = Avx2.Add(resultLows[idx], lows);
						resultHighs[idx] = Avx2.Add(resultHighs[idx], highs);

						//resultLows[idx] = UnsignedAddition(resultLows[idx], lows);
						//resultHighs[idx] = UnsignedAddition(resultHighs[idx], highs);


					}
				}
			}
		}

		#endregion

		#region Multiply Post Processing

		private void PropagateCarries(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var intermediateLimbCount = mantissaMems.Length;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				var limbVecs = GetLimbVectorsUL(mantissaMems[0]);				// Array of Least Signficant input Limb values
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[0]);			// Array of least signficant result limb values

				var carries = Avx2.ShiftRightLogical(limbVecs[idx], EFFECTIVE_BITS_PER_LIMB);		// The high 32 bits of sum becomes the new carry.
				resultLimbVecs[idx] = Avx2.And(limbVecs[idx], HIGH33_MASK_VEC);   // The low 32 bits of the sum is the result.

				for (int i = 1; i < intermediateLimbCount; i++)
				{
					limbVecs = GetLimbVectorsUL(mantissaMems[i]);				// Array of next 'stack' of input limbs
					resultLimbVecs = GetLimbVectorsUL(resultLimbs[i]);			// Array of next 'stack' of result limbs

					var withCarries = Avx2.Add(limbVecs[idx], carries);         // SIGNED Addition!!
					//var withCarries = UnsignedAddition(limbVecs[idx], carries);

					NumberOfSplits++;
					carries = Avx2.ShiftRightLogical(withCarries, EFFECTIVE_BITS_PER_LIMB);          // The high 32 bits of sum becomes the new carry.
					resultLimbVecs[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC); // The low 32 bits of the sum is the result.
				}

				//var isZeroFlags = Avx2.CompareEqual(carries, _zeroVector);
				//var isZeroComposite = (uint)Avx2.MoveMask(isZeroFlags.AsByte());

				//if (isZeroComposite != 0xffffffff)
				//{
				//	// At least one carry is not zero.
				//	throw new OverflowException("Overflow on PropagateCarries.");

				//	//ulong maxValueBeforeShift = 16711679;
				//	//var valuePtr = idx * _lanes;
				//	//for (var i = 0; i < _lanes; i++)
				//	//{
				//	//	if (carries.GetElement(i) > 0)
				//	//	{
				//	//		//throw new OverflowException("While propagating carries after a multiply operation, the MSL produced a carry.");
				//	//		SetMantissa(resultBa, valuePtr + i, new ulong[] { 0, maxValueBeforeShift });
				//	//		NumberOfMCarries++;
				//	//	}
				//	//}
				//}

			}
		}

		private Vector256<ulong> UnsignedAddition(Vector256<ulong> a, Vector256<ulong> b)
		{
			var tr = new ulong[_lanes];

			for (var i = 0; i < _lanes; i++)
			{
				tr[i] = a.GetElement(i) + b.GetElement(i);
			}

			var result = Vector256.Create(tr[0], tr[1], tr[2], tr[3]);

			return result;
		}

		private void ShiftAndTrim(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

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

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);

				// Take the top shiftAmount of bits from the previous limb
				var previousLimbVector = Avx2.And(prevSource[idx], HIGH33_MASK_VEC);
				result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(previousLimbVector, (byte)(32 - shiftAmount)));
			}
		}

		// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.ShiftRightLogical(Avx2.ShiftLeftLogical(source[idx], (byte)(32 + shiftAmount)), 32);
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
			//ClearManatissMems(c.MantissaMemories, onlyInPlayItems: false);

			//CheckPWValues(a.MantissaMemories, out var errors);
			//CheckPWValues(b.MantissaMemories, out var errors2);

			//if (errors.Length > 1 || errors2.Length > 1)
			//{
			//	Debug.WriteLine("Got an error.");
			//}

			var signsA = a.GetSignVectorsUL();
			var signsB = b.GetSignVectorsUL();

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				Vector256<ulong> areEqualFlags = Avx2.CompareEqual(signsA[idx], signsB[idx]);
				var areEqualComposite = (uint)Avx2.MoveMask(areEqualFlags.AsByte());

				var resultPtr = idx * _lanes;

				if (areEqualComposite == 0xffffffff)
				{
					// All are the same.
					NumberOfMCarries += _lanes;
					AddInternal(resultPtr, a, b, c);
				}
				else if (areEqualComposite == 0)
				{
					// Each pair has one + and one -
					NumberOfACarries += _lanes;
					//var comps = Compare(idx, a.MantissaMemories, b.MantissaMemories);
					//SubInternal(idx, comps, a, b, c);
					SubInternal(resultPtr, a, b, c);
				}
				else
				{
					AddSubInternal(resultPtr, a, b, c);
				}
			}
		}

		private void AddInternal(int resultPtr, FPValues a, FPValues b, FPValues c)
		{
			for (var i = 0; i < _lanes; i++)
			{
				var valPtr = resultPtr + i;
				if (DoneFlags[valPtr]) continue;

				var aMantissa = a.GetMantissa(valPtr);
				var bMantissa = b.GetMantissa(valPtr);

				var result = Add(aMantissa, bMantissa, out var carry);

				if (carry > 0)
				{
					//Debug.WriteLine($"A-Carry: a:{SmxMathHelper.GetDiagDisplay("a", aMantissa)}, {SmxMathHelper.GetDiagDisplay("b", bMantissa)}.");
					result = new ulong[LimbCount]; // CreateNewMaxIntegerSmx().Mantissa;
					DoneFlags[valPtr] = true;
					//NumberOfACarries++;
				}

				c.SetMantissa(valPtr, result);
				c.SetSign(valPtr, a.GetSign(valPtr));
			}
		}

		private void SubInternal(int resultPtr, FPValues a, FPValues b, FPValues c)
		{
			for (var i = 0; i < _lanes; i++)
			{
				var valPtr = resultPtr + i;
				if (DoneFlags[valPtr]) continue;

				//var comp = comps[i];
				var aMantissa = a.GetMantissa(valPtr);
				var bMantissa = b.GetMantissa(valPtr);

				var comp = Compare(aMantissa, bMantissa);

				if (comp == 0)
				{
					// Result is zero
					c.SetMantissa(valPtr, new ulong[LimbCount]);
					c.SetSign(valPtr, true);
				}
				else if (comp > 0)
				{
					//var result = Sub(a.GetMantissa(valPtr), b.GetMantissa(valPtr));
					var result = Sub(aMantissa, bMantissa);
					c.SetMantissa(valPtr, result);
					c.SetSign(valPtr, a.GetSign(valPtr));
				}
				else
				{
					//var result = Sub(b.GetMantissa(valPtr), a.GetMantissa(valPtr));
					var result = Sub(bMantissa, aMantissa);
					c.SetMantissa(valPtr, result);
					c.SetSign(valPtr, b.GetSign(valPtr));
				}
			}
		}

		public void AddSubInternal(int resultPtr, FPValues a, FPValues b, FPValues c)
		{
			for (var i = 0; i < _lanes; i++)
			{
				var valPtr = resultPtr + i;
				if (DoneFlags[valPtr]) continue;

				var aSign = a.GetSign(valPtr);
				var bSign = b.GetSign(valPtr);

				var aMantissa = a.GetMantissa(valPtr);
				var bMantissa = b.GetMantissa(valPtr);

				if (aSign == bSign)
				{
					NumberOfMCarries++;
					var result = Add(aMantissa, bMantissa, out var carry);
					if (carry > 0)
					{
						//Debug.WriteLine($"A-Carry: a:{SmxMathHelper.GetDiagDisplay("a", aMantissa)}, {SmxMathHelper.GetDiagDisplay("b", bMantissa)}.");
						result = new ulong[LimbCount]; // CreateNewMaxIntegerSmx().Mantissa;
						DoneFlags[valPtr] = true;
						//NumberOfACarries++;
					}

					c.SetMantissa(valPtr, result);
					c.SetSign(valPtr, a.GetSign(valPtr));
				}
				else
				{
					NumberOfACarries++;
					var comp = Compare(aMantissa, bMantissa);

					if (comp == 0)
					{
						// Result is zero
						c.SetMantissa(valPtr, new ulong[LimbCount]);
						c.SetSign(valPtr, true);
					}
					else if (comp > 0)
					{
						//var result = Sub(a.GetMantissa(valPtr), b.GetMantissa(valPtr));
						var result = Sub(aMantissa, bMantissa);
						c.SetMantissa(valPtr, result);
						c.SetSign(valPtr, a.GetSign(valPtr));
					}
					else
					{
						//var result = Sub(b.GetMantissa(valPtr), a.GetMantissa(valPtr));
						var result = Sub(bMantissa, aMantissa);
						c.SetMantissa(valPtr, result);
						c.SetSign(valPtr, b.GetSign(valPtr));
					}
				}
			}
		}

		private bool[] CheckForOverflow(Memory<ulong>[] resultLimbs)
		{
			var limbVecs = GetLimbVectorsUL(resultLimbs[^1]);

			var result = Enumerable.Repeat(false, VecCount).ToArray();

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var onlyTopBits = Avx2.And(limbVecs[idx], TOP_BITS_MASK_VEC);
				var areEqualFlags = Avx2.CompareEqual(onlyTopBits, _zeroVector);
				var compositeFlags = (uint)Avx2.MoveMask(areEqualFlags.AsByte());

				if (compositeFlags != 0xffffffff)
				{
					// One or more limbs have some of their top bits set.
					result[idx] = true;
				}
			}

			if (result.Any())
			{
				Debug.WriteLine("Overflow occured upon multiplication, discarding upper bits.");
			}

			return result;
		}

		//public void SetMantissa(ulong[][] mantissas, int index, ulong[] values)
		//{
		//	for (var i = 0; i < values.Length; i++)
		//	{
		//		mantissas[i][index] = values[i];
		//	}
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
				ulong nv;

				//checked
				//{
				//	nv = left[i] + right[i] + carry;
				//}

				// Since we are not using two's compliment, we don't need to use the Reserved Bit

				var lChopped = left[i] & HIGH33_MASK;
				var rChopped = right[i] & HIGH33_MASK;

				checked
				{
					nv =  lChopped + rChopped + carry;
				}

				var (hi, lo) = ScalarMathHelper.Split(nv);
				carry = hi;

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
				var sax = left[i] | TEST_BIT_31;

				result[i] = sax - right[i] - borrow;

				if ((result[i] & TEST_BIT_31) > 0)
				{
					// if the least significant bit of the high part of the result is still set, no borrow occured.
					result[i] &= HIGH33_MASK;
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

		//private ulong Split(ulong x, out ulong hi)
		//{
		//	hi = x >> 32; // Create new ulong from bits 32 - 63.
		//	return x & HIGH_MASK; // Create new ulong from bits 0 - 31.
		//}

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

		#endregion

		#region Create Smx Support

		public Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(fPValues.GetSign(index), fPValues.GetMantissa(index), TargetExponent, BitsBeforeBP, precision);
			return result;
		}

		public Smx2C GetSmx2CAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			throw new NotImplementedException();
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

			while (--limbPtr >= 0 && compositeFlags == 0xffffffff)
			{
				limb0A = GetLimbVectorsUL(mantissaMemsA[limbPtr]);
				limb0B = GetLimbVectorsUL(mantissaMemsB[limbPtr]);

				areEqualFlags = Avx2.CompareEqual(limb0A[idx], limb0B[idx]);
				compositeFlags = (uint)Avx2.MoveMask(areEqualFlags.AsByte());
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

			for (var i = 0; i < _lanes; i++)
			{
				if (eqFlags[i] == 0)
				{
					// AreEqual = false, use gTFlags for the result

					result[i] = (gtFlags[i] == 0xffffffff ? 1 : -1);
				}
				else
				{
					// Compare each remaining limbs.
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

		private uint[][] CheckPWValues(Memory<ulong>[] resultLimbs, out string errors)
		{
			var result = new uint[resultLimbs.Length][];
			var sb = new StringBuilder();

			var indexes = InPlayList;

			for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
			{
				result[limbPtr] = new uint[ValueCount];

				var limbs = GetLimbVectorsUL(resultLimbs[limbPtr]);

				var oneFound = false;

				for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];
					var vector = limbs[idx];

					var sansSign = Avx2.And(vector, SIGN_BIT_MASK_VEC);
					var cFlags = Avx2.CompareEqual(vector, sansSign);
					var cComposite = Avx2.MoveMask(cFlags.AsByte());
					if (cComposite != -1)
					{
						sb.AppendLine("Top bit was set.");
					}

					var areGtFlags = (Avx2.CompareGreaterThan(sansSign.AsInt64(), _maxDigitValueVector)).AsUInt64();
					var compositeFlags = (uint)Avx2.MoveMask(areGtFlags.AsByte());

					result[limbPtr][idx] = compositeFlags;

					if (compositeFlags != 0)
					{
						if (!oneFound)
						{
							oneFound = true;
							sb.Append($"Limb: {limbPtr}::");
						}
						sb.AppendLine($"{idx} ");
					}
				}

				if (oneFound)
				{
					sb.AppendLine();
				}
			}

			errors = sb.ToString();

			return result;
		}

		//public void IsGreaterOrEqThanThreshold_Old(FPValues a, Span<Vector256<long>> escapedFlagVectors)
		//{
		//	var left = a.GetLimbVectorsL(LimbCount - 1);
		//	var right = _thresholdVector;

		//	IsGreaterOrEqThan_Old(left, right, escapedFlagVectors);
		//}

		//private void IsGreaterOrEqThan_Old(Span<Vector256<long>> left, Vector256<long> right, Span<Vector256<long>> escapedFlagVectors)
		//{
		//	var indexes = InPlayList;
		//	for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
		//	{
		//		var idx = indexes[idxPtr];
		//		//var ta = new ulong[_ulongSlots];
		//		//left[idx].AsVector().CopyTo(ta);

		//		//var bi = SmxMathHelper.FromPwULongs(ta);
		//		//var rv = new RValue(bi, -24);

		//		//var rvS = RValueHelper.ConvertToString(rv);

		//		var resultVector = Avx2.CompareGreaterThan(left[idx], right);

		//		escapedFlagVectors[idx] = resultVector;
		//	}
		//}

		public void IsGreaterOrEqThanThreshold(FPValues a, bool[] results)
		{
			var left = a.GetLimbVectorsUL(LimbCount - 1);
			var right = _thresholdVector;

			IsGreaterOrEqThan(left, right, results);
		}

		private void IsGreaterOrEqThan(Span<Vector256<ulong>> left, Vector256<long> right, bool[] results)
		{
			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				//var ta = new ulong[_ulongSlots];
				//left[idx].AsVector().CopyTo(ta);

				//var bi = SmxMathHelper.FromPwULongs(ta);
				//var rv = new RValue(bi, -24);

				//var rvS = RValueHelper.ConvertToString(rv);

				// TODO: Add a check confirm the value we receiving is positive.

				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				var resultVector = Avx2.CompareGreaterThan(sansSign.AsInt64(), right);

				var vectorPtr = idx * _lanes;

				for (var i = 0; i < _lanes; i++)
				{
					results[vectorPtr + i] = resultVector.GetElement(i) == -1;
				}
			}
		}

		#endregion

		#region TEMPLATES

		//private void MultiplyVecs(Span<Vector256<uint>> left, Span<Vector256<uint>> right, Span<Vector256<ulong>> result)
		//{
		//	foreach (var idx in InPlayList)
		//	{
		//		result[idx] = Avx2.Multiply(left[idx], right[idx]);
		//	}
		//}

		//private void Split(Span<Vector256<ulong>> x, Span<Vector256<ulong>> highs, Span<Vector256<ulong>> lows)
		//{
		//	foreach (var idx in InPlayList)
		//	{
		//		highs[idx] = Avx2.And(x[idx], HIGH_MASK_VEC);   // Create new ulong from bits 32 - 63.
		//		lows[idx] = Avx2.And(x[idx], LOW_MASK_VEC);    // Create new ulong from bits 0 - 31.
		//	}
		//}

		//private void AddVecs(Span<Vector256<ulong>> left, Span<Vector256<ulong>> right, Span<Vector256<ulong>> result)
		//{
		//	for (var i = 0; i < left.Length; i++)
		//	{
		//		result[i] = Avx2.Add(left[i], right[i]);
		//	}
		//}

		#endregion

		#region Old Versions of good Methods

		public void Square_OLD(FPValues a, FPValues result)
		{
			//if (a.IsZero)
			//{
			//	return a;
			//}

			//_ = CheckPWValues(result.MantissaMemories, out var errors);

			//if (errors.Length > 1)
			//{
			//	Debug.WriteLine($"PW Errors found at Square, Errors:\n{errors}.");
			//}


			//ClearManatissMems(_squareResult1Mems, onlyInPlayItems: false);
			//ClearManatissMems(_squareResult2Mems, onlyInPlayItems: false);
			//ClearManatissMems(result.MantissaMemories, onlyInPlayItems: false);

			SquareInternal(a, _squareResult1Mems);
			PropagateCarries(_squareResult1Mems, _squareResult2Mems);
			ShiftAndTrim(_squareResult2Mems, result.MantissaMemories);

			//_ = CheckPWValues(result.MantissaMemories, out errors);

			//if(errors.Length > 1)
			//{
			//	Debug.WriteLine($"PW Errors found at Square, Errors:\n{errors}.");
			//}

			//if (flags.Any(x => x != 0))
			//{
			//	Debug.WriteLine($"Found a prb.");
			//}
		}




		#endregion

	}
}
