using MSS.Common;
using MSS.Types;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSetGenP
{
	public class VecMathExp2C : IVecMath
	{
		#region Private Properties

		private const int EFFECTIVE_BITS_PER_LIMB = 31;
		private static readonly ulong MAX_DIGIT_VALUE = (ulong)(-1 + Math.Pow(2, EFFECTIVE_BITS_PER_LIMB));

		private const ulong HIGH32_BITS_SET = 0xFFFFFFFF00000000; // bits 63 - 32 are set.
		private const ulong LOW32_BITS_SET = 0x00000000FFFFFFFF; // bits 0 - 31 are set.
		private static readonly Vector256<ulong> HIGH32_MASK_VEC = Vector256.Create(LOW32_BITS_SET);

		private const ulong HIGH33_BITS_SET = 0xFFFFFFFF80000000; // bits 63 - 31 are set.
		private static readonly Vector256<ulong> LOW31_MASK_VEC = Vector256.Create(HIGH33_BITS_SET); // diagnostics


		private const ulong LOW31_BITS_SET = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private const ulong HIGH33_MASK = LOW31_BITS_SET; // bits 0 - 30 are set.
		private static readonly Vector256<ulong> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const ulong SIGN64_BIT_MASK = 0x7FFFFFFFFFFFFFFF;
		private static readonly Vector256<ulong> SIGN64_BIT_MASK_VEC = Vector256.Create(SIGN64_BIT_MASK);

		private const ulong SIGN_BIT_MASK = 0x000000003FFFFFFF;
		private static readonly Vector256<ulong> SIGN_BIT_MASK_VEC = Vector256.Create(SIGN_BIT_MASK);

		private const ulong TOP_BITS_MASK = 0xFF00000000000000;
		private static readonly Vector256<ulong> TOP_BITS_MASK_VEC = Vector256.Create(TOP_BITS_MASK);

		//private static readonly ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.
		//private static readonly ulong TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.
		//private static readonly ulong TEST_BIT_30 = 0x0000000040000000; // bit 30 is set.

		private static readonly int _lanes = Vector256<ulong>.Count;

		private Memory<ulong>[] _squareResult1Mems;
		private Memory<ulong>[] _squareResult2Mems;
		private ulong[][] _squareResult2BackingArray;

		private ulong[][] _squareResult3Ba;

		private Vector256<long> _thresholdVector;
		private Vector256<ulong> _zeroVector;
		private Vector256<long> _maxDigitValueVector;

		private static readonly bool USE_DET_DEBUG = false;

		private ScalarMath2C _scalarMath2C;

		#endregion

		#region Constructor

		public VecMathExp2C(ApFixedPointFormat apFixedPointFormat, int valueCount, uint threshold)
		{
			_scalarMath2C = new ScalarMath2C(apFixedPointFormat, threshold);

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
			
			ThresholdMsl = ScalarMathHelper.GetThresholdMsl(threshold, ApFixedPointFormat, IsSigned);

			var thresholdMslIntegerVector = Vector256.Create(ThresholdMsl);
			_thresholdVector = thresholdMslIntegerVector.AsInt64();

			//var mslPower = ((LimbCount - 1) * EFFECTIVE_BITS_PER_LIMB) - FractionalBits;
			//MslWeight = Math.Pow(2, mslPower);
			//MslWeightVector = Vector256.Create(MslWeight);

			_zeroVector = Vector256<ulong>.Zero;
			_maxDigitValueVector = Vector256.Create((long)MAX_DIGIT_VALUE);

			_squareResult1Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);

			//_squareResult2Mems = BuildMantissaMemoryArray(LimbCount * 2, ValueCount);
			_squareResult2BackingArray = BuildMantissaBackingArray(LimbCount * 2, ValueCount);
			_squareResult2Mems = BuildMantissaMemoryArray(_squareResult2BackingArray);

			_squareResult3Ba = BuildMantissaBackingArray(LimbCount * 2, ValueCount);
		}

		#endregion

		//public void Refresh()
		//{
		//	_scalarMath2C = new ScalarMath2C(ApFixedPointFormat, Threshold);

		//	// Initially, all vectors are 'In Play.'
		//	InPlayList = Enumerable.Range(0, VecCount).ToArray();

		//	// Initially, all values are 'In Play.'
		//	DoneFlags = new bool[ValueCount];

		//	BlockPosition = new BigVector();
		//	RowNumber = 0;
		//}

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

		private Span<Vector256<uint>> GetLimbVectorsUW(Memory<ulong> memory)
		{
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(memory.Span);

			//Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(memory.Span);
			return result;
		}
		#endregion

		#region Public Properties

		public bool IsSigned => true;

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		public int ValueCount { get; init; }
		public int VecCount { get; init; }

		public int[] InPlayList { get; set; }	// Vector-Level 
		public bool[] DoneFlags { get; set; }	// Value-Level
		public BigVector BlockPosition { get; set; }
		public int RowNumber { get; set; }


		public uint MaxIntegerValue { get; init; }

		public uint Threshold { get; init; }
		public ulong ThresholdMsl { get; init; }

		//public double MslWeight { get; init; }
		//public Vector256<double> MslWeightVector { get; init; }

		public int NumberOfMCarries { get; private set; }
		public int NumberOfACarries { get; private set; }
		public long NumberOfSplits { get; private set; }
		public long NumberOfGetCarries { get; private set; }
		public long NumberOfGrtrThanOps { get; private set; }

		#endregion

		#region Multiply and Square

		public void Square(FPValues a, FPValues result)
		{
			// Convert back to standard, i.e., non two's compliment.
			// Our multiplication routines don't support 2's compliment.
			// The result of squaring is always positive,
			// so we don't have to convert them to 2's compliment afterwards.

			var non2CFPValues = a.ConvertFrom2C(InPlayList, _lanes);

			ClearManatissMems(_squareResult1Mems, onlyInPlayItems: true);
			SquareInternal(non2CFPValues, _squareResult1Mems);

			//ClearManatissMems(_squareResult2Mems, onlyInPlayItems: false);
			SumThePartials(_squareResult1Mems, _squareResult2Mems);

			//ClearManatissMems(result.MantissaMemories, onlyInPlayItems: true);
			ShiftAndTrim(_squareResult2Mems, result);

			//ExtendSigns(result.Mantissas);
		}

		private void SquareInternal(FPValues a, Memory<ulong>[] resultLimbs)
		{

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

					for(var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
					{
						var idx = indexes[idxPtr];
						var productVector = Avx2.Multiply(left[idx], right[idx]);

						if (i > j)
						{
							//product *= 2;
							productVector = Avx2.ShiftLeftLogical(productVector, 1);
						}

						var lows = Avx2.And(productVector, HIGH33_MASK_VEC);    // Create new ulong from bits 0 - 31.
						var highs = Avx2.ShiftRightLogical(productVector, 31);   // Create new ulong from bits 32 - 63.

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

		private void SumThePartials(Memory<ulong>[] mantissaMems, Memory<ulong>[] resultLimbs)
		{
			// To be used after a multiply operation.
			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			// If the MSL produces a carry, throw an exception.

			var limbCnt = mantissaMems.Length;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];

				NumberOfSplits++;

				var limbPtr = 0;

				var pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
				var resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

				var carries = Avx2.ShiftRightLogical(pProductVectors[idx], 31);	// The high 32 bits of sum becomes the new carry.
				resultVectors[idx] = Avx2.And(pProductVectors[idx], HIGH33_MASK_VEC);					// The low 32 bits of the sum is the result.

				for (; limbPtr < limbCnt; limbPtr++)
				{
					NumberOfSplits++;

					pProductVectors = GetLimbVectorsUL(mantissaMems[limbPtr]);
					resultVectors = GetLimbVectorsUL(resultLimbs[limbPtr]);

					var withCarries = Avx2.Add(pProductVectors[idx], carries);
					carries = Avx2.ShiftRightLogical(withCarries, 31);     // The high 32 bits of sum becomes the new carry.
					resultVectors[idx] = Avx2.And(withCarries, HIGH33_MASK_VEC);				 // The low 32 bits of the sum is the result.
				}
			}
		}
		
		private void ShiftAndTrim(Memory<ulong>[] mantissaMems, FPValues result)
		{
			//ValidateIsSplit(mantissa);

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Check to see if any of these values are larger than the FP Format.
			//_ = CheckForOverflow(resultLimbs);

			Memory<ulong>[] resultLimbs = result.MantissaMemories;

			var sourceIndex = Math.Max(mantissaMems.Length - LimbCount, 0);

			for (int limbPtr = 0; limbPtr < resultLimbs.Length; limbPtr++)
			{
				var resultLimbVecs = GetLimbVectorsUL(resultLimbs[limbPtr]);

				if (sourceIndex > 0)
				{
					var limbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					var prevLimbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex - 1]);
					ShiftAndCopyBits(limbVecs, prevLimbVecs, resultLimbVecs);
				}
				else
				{
					var limbVecs = GetLimbVectorsUL(mantissaMems[limbPtr + sourceIndex]);
					ShiftAndCopyBits(limbVecs, resultLimbVecs);
				}
			}
		}

		private void ShiftAndCopyBits(Span<Vector256<ulong>> source, Span<Vector256<ulong>> prevSource, Span<Vector256<ulong>> result)
		{
			var shiftAmount = BitsBeforeBP;

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				// Take the bits from the source limb, discarding the top shiftAmount of bits.
				result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC);

				// Take the top shiftAmount of bits from the previous limb
				var previousLimbVector = Avx2.And(prevSource[idx], HIGH33_MASK_VEC); // TODO: Combine this and the next operation.
				result[idx] = Avx2.Or(result[idx], Avx2.ShiftRightLogical(previousLimbVector, (byte)(31 - shiftAmount)));
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
				result[idx] = Avx2.And(Avx2.ShiftLeftLogical(source[idx], shiftAmount), HIGH33_MASK_VEC);
			}
		}

		//private void ExtendSigns(ulong[][] backingArray)
		//{
		//	var indexes = InPlayList;
		//	for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
		//	{
		//		var idx = indexes[idxPtr];

		//		var resultPtr = idx * _lanes;

		//		for (var i = 0; i < _lanes; i++)
		//		{
		//			var valPtr = resultPtr + i;
		//			//if (DoneFlags[valPtr]) continue;

		//			var msl = backingArray[LimbCount - 1][valPtr];

		//			backingArray[LimbCount - 1][valPtr] = (msl & TEST_BIT_30) > 0
		//					? msl | TEST_BIT_31
		//					: msl & ~TEST_BIT_31;
		//		}
		//	}
		//}

		#endregion

		#region Add and Subtract

		public void Sub(FPValues a, FPValues b, FPValues c)
		{
			var notB = b.Negate2C(InPlayList);
			Add(a, notB, c);
		}

		public void Add(FPValues a, FPValues b, FPValues c)
		{
			//var mmListA = a.CheckReservedBit(InPlayList, _lanes);
			//var mmListB = b.CheckReservedBit(InPlayList, _lanes);

			//if (mmListA.Count > 0 || mmListB.Count > 0)
			//{
			//	Debug.WriteLine("hi Reserved Bit miss match as we are beginning to add/sub.");
			//}

			ClearManatissMems(c.MantissaMemories, onlyInPlayItems: false);


			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				//var resultPtr = idx * _lanes;

				var carryVector = Vector256<ulong>.Zero;

				//var aCarryOccured = false;

				for (int limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					var limbVecsA = a.GetLimbVectorsUL(limbPtr);
					var limbVecsB = b.GetLimbVectorsUL(limbPtr);
					var resultLimbVecs = c.GetLimbVectorsUL(limbPtr);

					var va = limbVecsA[idx];
					var vb = limbVecsB[idx];

					//var va = Avx2.And(limbVecsA[idx], HIGH33_MASK_VEC);
					//var vb = Avx2.And(limbVecsB[idx], HIGH33_MASK_VEC);

					//var va = Avx2.And(limbVecsA[idx], HIGH32_MASK_VEC);
					//var vb = Avx2.And(limbVecsB[idx], HIGH32_MASK_VEC);

					var sumVector = Avx2.Add(va, vb);
					var newValuesVector = Avx2.Add(sumVector, carryVector);

					//var sumVector = UnsignedAddition(va, vb);
					//var newValuesVector = UnsignedAddition(sumVector, carryVector);


					var (limbValues, newCarries) = GetResultWithCarry(newValuesVector, isMsl: (limbPtr == LimbCount - 1), out var aCarryOccured);
					resultLimbVecs[idx] = limbValues;

					if (USE_DET_DEBUG)
						ReportForAddition(limbPtr, va, vb, carryVector, newValuesVector, limbValues, newCarries);

					carryVector = newCarries; // Avx2.And(newCarries, HIGH_MASK_VEC);
				}

				//if (aCarryOccured)
				//{
				//	for (var i = 0; i < _lanes; i++)
				//	{
				//		if (!DoneFlags[resultPtr + i])
				//		{
				//			if (carryVector.GetElement(i) > 0)
				//			{
				//				DoneFlags[resultPtr + i] = true;
				//			}
				//		}
				//	}
				//}

			}
		}

		private (Vector256<ulong> limbs, Vector256<ulong> carry) GetResultWithCarry(Vector256<ulong> nvs, bool isMsl, out bool aCarryOccured)
		{
			NumberOfGetCarries++;

			aCarryOccured = false;
			// A carry is generated any time the bit just above the result limb is different than msb of the limb
			// i.e. this next higher bit is not an extension of the sign.

			var ltemp = new ulong[_lanes];
			var ctemp = new ulong[_lanes];

			for (var i = 0; i < _lanes; i++)
			{
				//var limbValue = ScalarMathHelper.GetLowHalf(nvs.GetElement(i), out var resultIsNegative, out var extendedCarryOutIsNegative);
				var (limbValue, newCarry) = ScalarMathHelper.GetResultWithCarrySigned(nvs.GetElement(i), isMsl);

				ltemp[i] = limbValue;
				ctemp[i] = newCarry;

				if (newCarry > 0) aCarryOccured = true;
			}

			var limbs = Vector256.Create(ltemp[0], ltemp[1], ltemp[2], ltemp[3]);

			var carryVector = Vector256.Create(ctemp[0], ctemp[1], ctemp[2], ctemp[3]);	

			return (limbs, carryVector);
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

		private void ReportForAddition(int step, Vector256<ulong> left, Vector256<ulong> right, Vector256<ulong> carry, Vector256<ulong> nv, Vector256<ulong> lo, Vector256<ulong> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var rightVal0 = right.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = ScalarMathHelper.ConvertFrom2C(leftVal0);
			var rd = ScalarMathHelper.ConvertFrom2C(rightVal0);
			var cd = ScalarMathHelper.ConvertFrom2C(carryVal0);
			var nvd = ScalarMathHelper.ConvertFrom2C(nvVal0);
			var hid = ScalarMathHelper.ConvertFrom2C(newCarryVal0);
			var lod = ScalarMathHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0 & HIGH32_BITS_SET;
			var unSNv = leftVal0 + rightVal0 + carryVal0;


			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {leftVal0:X4}, {rightVal0:X4} wc:{carryVal0:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		#endregion

		#region Retrieve Smx From FPValues

		public Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var mantissa = fPValues.GetMantissa(index);
			var sign = ScalarMathHelper.GetSign(mantissa);

			var nrmMantissa = ScalarMathHelper.ConvertFrom2C(mantissa);

			var result = new Smx(sign, nrmMantissa, TargetExponent, BitsBeforeBP, precision);

			return result;
		}

		public Smx2C GetSmx2CAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var mantissa = fPValues.GetMantissa(index);
			var sign = ScalarMathHelper.GetSign(mantissa);
			var result = new Smx2C(sign, mantissa, TargetExponent, BitsBeforeBP, precision);

			return result;
		}

		#endregion

		#region Comparison

		public void IsGreaterOrEqThanThreshold(FPValues a, bool[] results)
		{

			var indexes = InPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var resultPtr = idx * _lanes;

				for (var i = 0; i < _lanes; i++)
				{
					var valPtr = resultPtr + i;

					var partialWordLimbs = a.GetMantissa(valPtr);

					var res = IsGreaterOrEqThanThresholdSingle(partialWordLimbs[^1]);
					results[valPtr] = res;
				}
			}



			//var non2CFPValues = a.ConvertFrom2C(InPlayList, _lanes);

			//var left = non2CFPValues.GetLimbVectorsUL(LimbCount - 1);
			//var right = _thresholdVector;

			//IsGreaterOrEqThan(left, right, results);
		}

		private bool IsGreaterOrEqThanThresholdSingle(ulong left)
		{
			NumberOfGrtrThanOps++;
			var right = ThresholdMsl;
			var result = left >= right;

			return result;
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

				//var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);


				//var hiHalf = Avx2.And(left[idx], LOW31_MASK_VEC);
				//var compareVector = Avx2.CompareEqual(hiHalf, _zeroVector);
				
				//var cComposite = Avx2.MoveMask(compareVector.AsByte());
				//if (cComposite != -1)
				//{
				//	Debug.WriteLine($"Found a negative result from summing two squares.");
				//}


				var sansSign = Avx2.And(left[idx], SIGN_BIT_MASK_VEC);
				var resultVector = Avx2.CompareGreaterThan(sansSign.AsInt64(), right);

				var vectorPtr = idx * _lanes;

				for(var i = 0; i < _lanes; i++)
				{
					results[vectorPtr + i] = resultVector.GetElement(i) == -1;
				}
			}
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

	}
}
