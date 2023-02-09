using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Numerics;

namespace MSetGeneratorPrototype
{
	public class FP31ScalarMath
	{
		#region Constants

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		//private const uint HIGH33_MASK = LOW31_BITS_SET;
		private const uint CLEAR_RESERVED_BIT = LOW31_BITS_SET;

		private const ulong LOW31_BITS_SET_L = 0x000000007FFFFFFF; // bits 0 - 30 are set.
		private const ulong HIGH33_MASK_L = LOW31_BITS_SET_L;

		//private const ulong HIGH_33_BITS_SET_L = 0xFFFFFFFF80000000; // bits 0 - 30 are set.

		//private const ulong HIGH33_FILL = HIGH_33_BITS_SET_L;           // bits 63 - 31 are set.
		//private const ulong HIGH33_CLEAR_L = LOW31_BITS_SET_L;          // bits 63 - 31 are reset.


		private static readonly bool USE_DET_DEBUG = false;

		//private int _thresholdMsl;

		#endregion

		#region Constructor

		public FP31ScalarMath(ApFixedPointFormat apFixedPointFormat)
		{
			ApFixedPointFormat = apFixedPointFormat;
			MaxIntegerValue = FP31ValHelper.GetMaxIntegerValue(ApFixedPointFormat.BitsBeforeBinaryPoint);
			_mathOpCounts = new MathOpCounts();			
		}

		#endregion

		#region Public Properties

		public ApFixedPointFormat ApFixedPointFormat { get; init; }
		public byte BitsBeforeBP => ApFixedPointFormat.BitsBeforeBinaryPoint;
		public int FractionalBits => ApFixedPointFormat.NumberOfFractionalBits;
		public int LimbCount => ApFixedPointFormat.LimbCount;
		public int TargetExponent => ApFixedPointFormat.TargetExponent;

		private MathOpCounts _mathOpCounts;
		public MathOpCounts MathOpCounts => _mathOpCounts;

		public uint MaxIntegerValue { get; init; }

		#endregion

		#region Multiply and Square

		public FP31Val Multiply(FP31Val a, FP31Val b)
		{
			if (a.IsZero || b.IsZero)
			{
				return CreateNewZeroFP31Val(Math.Min(a.Precision, b.Precision));
			}

			//CheckLimbs2C(a, b, "Multiply");

			var rawMantissa = Multiply(a.Mantissa, b.Mantissa);
			var mantissa = SumThePartials(rawMantissa, out var carry);
			var precision = a.Precision;

			FP31Val result;

			if (carry > 0)
			{
				result = CreateNewMaxIntegerFP31Val(precision);
			}
			else
			{
				var nrmMantissa = ShiftAndTrim(mantissa);
				result = CreateFP31Val(nrmMantissa, precision);
			}

			return result;
		}

		public ulong[] Multiply(uint[] ax, uint[] bx)
		{
			var mantissa = new ulong[ax.Length + bx.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = 0; i < bx.Length; i++)
				{
					var resultPtr = j + i;  // 0, 1, 1, 2

					var product = (ulong)Math.BigMul((int)ax[j], (int) bx[i]);
					_mathOpCounts.NumberOfMultiplications++;

					mantissa[resultPtr] += product & HIGH33_MASK_L;
					mantissa[resultPtr + 1] += product >> EFFECTIVE_BITS_PER_LIMB;  // The high 31 bits of sum becomes the new carry.
 					_mathOpCounts.NumberOfSplits++;
				}
			}

			return mantissa;
		}

		public FP31Val Square(FP31Val a)
		{
			if (a.IsZero)
			{
				return a;
			}

			//CheckLimb2C(a, "Square");

			var non2CMantissa = FP31ValHelper.ConvertFrom2C(a.Mantissa, out var sign);

			// TODO: Consider creating a method to set the high bits to all zeros or all ones -- explicitly for multiplication -- in prep of only having longs for multiplication.

			var rawMantissa = Square(non2CMantissa);
			var mantissa = SumThePartials(rawMantissa, out var carry);
			var precision = a.Precision;

			FP31Val result;

			if (carry > 0)
			{
				result = CreateNewMaxIntegerFP31Val(precision);
			}
			else
			{
				var nrmMantissa = ShiftAndTrim(mantissa);

				// Do not need to convert positive values to 2C format.
				//var mantissa2C = ScalarMathHelper.ConvertTo2C(nrmMantissa, true); // TODO: Converting to / from 2C needs to be done for the other Multiply methods.

				result = CreateFP31Val(nrmMantissa, precision);
			}

			return result;
		}

		public ulong[] Square(uint[] ax)
		{
			var mantissa = new ulong[ax.Length * 2];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = j; i < ax.Length; i++)
				{
					var resultPtr = j + i;                  // 0, 1		1, 2		0, 1, 2		1, 2, 3, 

					//// Ignore the top-halves -- these are just sign extensions.
					//var product = (ax[j] & HIGH_MASK) * (ax[i] & HIGH_MASK);

					// Turns out, the top halves are needed.
					//var product = ax[j] * ax[i];
					var product = (ulong)Math.BigMul((int)ax[j], (int)ax[i]);

					_mathOpCounts.NumberOfMultiplications++;

					if (i > j)
					{
						product *= 2;
					}

					mantissa[resultPtr] += product & HIGH33_MASK_L;
					mantissa[resultPtr + 1] += product >> EFFECTIVE_BITS_PER_LIMB;  // The high 31 bits of sum becomes the new carry.
					_mathOpCounts.NumberOfSplits++;
				}
			}

			return mantissa;
		}

		public FP31Val Multiply(FP31Val a, uint b)
		{
			FP31Val result;

			if (a.IsZero || b == 0)
			{
				result = CreateNewZeroFP31Val(a.Precision);
				return result;
			}

			//CheckLimb2C(a, "MultiplyByInt");

			var lzc = BitOperations.LeadingZeroCount(b);

			if (lzc < 32 - BitsBeforeBP)
			{
				throw new ArgumentException("The integer multiplyer should fit into the integer portion of a FP31Val value.");
			}

			var sign = FP31ValHelper.GetSign(a.Mantissa);

			if (sign)
			{
				var rawMantissa = Multiply(a.Mantissa, b);
				var mantissa = SumThePartials(rawMantissa, out var carry);

				if (carry > 0)
				{
					result = CreateNewMaxIntegerFP31Val(a.Precision);
				}
				else
				{
					var nrmMantissa = FP31ValHelper.TakeLowerHalves(mantissa);
					result = CreateFP31Val(nrmMantissa, a.Precision);
				}
			}
			else
			{
				var aNegated = FP31ValHelper.Negate(a);

				var rawMantissa = Multiply(aNegated.Mantissa, b);
				var mantissa = SumThePartials(rawMantissa, out var carry);

				if (carry > 0)
				{
					result = CreateNewMaxIntegerFP31Val(a.Precision);
				}
				else
				{
					var nrmMantissa = FP31ValHelper.TakeLowerHalves(mantissa);
					var unNegatedResult = CreateFP31Val(nrmMantissa, a.Precision);

					result = FP31ValHelper.Negate(unNegatedResult);
				}
			}

			return result;
		}

		public ulong[] Multiply(uint[] ax, uint b)
		{
			var mantissa = new ulong[ax.Length];
			ulong carry = 0; 

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				//var product = ax[j] * b;
				var left = (int) (ax[j] & CLEAR_RESERVED_BIT); 
				var product = (ulong)Math.BigMul(left, (int)b);
				_mathOpCounts.NumberOfMultiplications++;

				var sum = product + carry;
				mantissa[j] = sum & HIGH33_MASK_L;
				carry = product >> EFFECTIVE_BITS_PER_LIMB;  // The high 31 bits of sum becomes the new carry.
				_mathOpCounts.NumberOfSplits++;
			}
			
			if (carry != 0)
			{ 
				throw new OverflowException($"Multiply {FP31ValHelper.GetDiagDisplayHex("ax", ax)} x {b} resulted in a overflow. The hi value is {carry}.");
			}

			return mantissa;
		}

		#endregion

		#region Multiply Post Processing 

		public ulong[] SumThePartials(ulong[] mantissa, out ulong carry)
		{
			// Currently we are not producing any carries out -- the limbs are split and only a single partial product contributes to the top-half of the msl.
			// TODO: As the top half of the bin is added, we need to detect carries as we do in the Add routine.
			// TODO: If (when) this is updated to accept an incoming carry, we need to return a '1' or '0' as the Add routine does. Currently we are returning the top-half of the msl

			// To be used after a multiply operation.
			// This renormalizes the result so that each result bin with a value <= 2^32 for the final digit.

			// Starting from the LSB, each bin is split and the top-half is added to the next bin up.

			// This will be updated to take a carry coming in, as well as providing the carry out

			var result = new ulong[mantissa.Length];
			carry = 0uL;

			for (int i = 0; i < mantissa.Length; i++)
			{
				ulong sum;

				checked
				{
					sum = mantissa[i] + carry;
				}

				var newCarry = sum >> EFFECTIVE_BITS_PER_LIMB;  // The high 31 bits of sum becomes the new carry.
				var limbValue = sum & HIGH33_MASK_L;
				result[i] = limbValue;
				_mathOpCounts.NumberOfSplits++;

				ReportForMultiplication(i, mantissa[i], carry, sum, limbValue, newCarry);

				carry = newCarry;
			}

			if (carry > 0) throw new OverflowException("PropagateCarries found a value larger than MAX DIGIT in the top 'bin'.");

			return result;
		}

		public uint[] ShiftAndTrim(ulong[] mantissa)
		{
			if (USE_DET_DEBUG)
			{
				CheckLimbsBeforeShiftAndTrim(mantissa);
			}

			// Push x bits off the top of the mantissa to restore the Fixed Point Format, building a new mantissa having LimbCount limbs.
			// If the Fixed Point Format is, for example: 8:56, then the mantissa we are given will have the format of 16:112, and...
			// pusing 8 bits off the top and taking the two most significant limbs will return the format to 8:56.

			// Clear the bits from the uppper half + the reserved bit, these will either be all 0's or all 1'. Our values are confirmed to be split at this point.

			var shiftAmount = BitsBeforeBP;

			var result = new uint[LimbCount];

			//var sourceIndex = Math.Max(mantissa.Length - LimbCount, 0);
			var sourceIndex = mantissa.Length - 1;
			var i = result.Length - 1;

			for (; i >= 0; i--)
			{
				if (sourceIndex > 0)
				{
					// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
					var topHalf = mantissa[sourceIndex] << shiftAmount;
					topHalf &= CLEAR_RESERVED_BIT;                                     // This will clear the top 32 bits as well as the reserved bit.

					// Take the top shiftAmount of bits from the previous limb and place them in the last shiftAmount bit positions
					var bottomHalf = mantissa[sourceIndex - 1] & CLEAR_RESERVED_BIT;
					bottomHalf >>= 31 - shiftAmount;                            // Don't include the reserved bit.

					result[i] = (uint) (topHalf | bottomHalf);

					if (USE_DET_DEBUG)
					{
						var strResult = string.Format("0x{0:X4}", result[i]);
						var strTopHalf = string.Format("0x{0:X4}", topHalf);
						var strBottomHalf = string.Format("0x{0:X4}", bottomHalf);
						Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopHalf} and {strBottomHalf}.");
					}
				}
				else
				{
					// Discard the top shiftAmount of bits, moving the remainder of this limb up to fill the opening.
					var topHalf = mantissa[sourceIndex] << shiftAmount;
					topHalf &= CLEAR_RESERVED_BIT;

					result[i] = (uint) topHalf;

					if (USE_DET_DEBUG)
					{
						var strResult = string.Format("0x{0:X4}", result[i]);
						var strTopH = string.Format("0x{0:X4}", topHalf);
						Debug.WriteLine($"Result, index: {i} is {strResult} from {strTopH}.");
					}
				}

				sourceIndex--;
			}

			//if (isSigned)
			//{
			//	// SignExtend the MSL
			//	result = ExtendSignBit(result);
			//}

			if (USE_DET_DEBUG)
			{
				CheckLimbsAfterShiftAndTrim(result);
			}

			return result;
		}

		#endregion

		#region Add and Subtract

		public FP31Val Sub(FP31Val a, FP31Val b, string desc)
		{
			if (b.IsZero)
			{
				return a;
			}

			var bNegated = FP31ValHelper.Negate(b);

			if (a.IsZero)
			{
				return bNegated;
			}

			var result = Add(a, bNegated, desc);

			return result;
		}

		public FP31Val Add(FP31Val a, FP31Val b, string desc)
		{
			if (b.IsZero) return a;
			if (a.IsZero) return b;

			var precision = Math.Min(a.Precision, b.Precision);

			var mantissa = Add(a.Mantissa, b.Mantissa, out var carry);

			FP31Val result;


			if (carry > 0)
			{
				//throw new OverflowException($"scalarMath -- Overflow on Add. {desc}");
				//result = CreateNewMaxIntegerFP31Val(a.Precision);
				result = CreateFP31Val(mantissa, precision);

			}
			else
			{
				result = CreateFP31Val(mantissa, precision);
			}

			return result;
		}

		private uint[] Add(uint[] left, uint[] right, out uint carry)
		{
			if (left.Length != right.Length)
			{
				throw new ArgumentException($"The left and right arguments must have equal length. left.Length: {left.Length}, right.Length: {right.Length}.");
			}

			var limbCount = left.Length;
			var result = new uint[limbCount];

			carry = 0u;

			for (var limbPtr = 0; limbPtr < limbCount; limbPtr++)
			{
				uint newValue;

				checked
				{
					newValue = left[limbPtr] + right[limbPtr] + carry;
				}

				var newCarry = newValue >> EFFECTIVE_BITS_PER_LIMB;		// The high 31 bits of sum becomes the new carry.
				var limbValue = newValue & CLEAR_RESERVED_BIT;			// The low 31 bits of the sum is the result.

				result[limbPtr] = limbValue;

				_mathOpCounts.NumberOfSplits++;

				if (USE_DET_DEBUG)
					ReportForAddition(limbPtr, left[limbPtr], right[limbPtr], carry, newValue, limbValue, newCarry);

				carry = newCarry;
			}

			return result;
		}

		private void ReportForAddition(int step, uint left, uint right, uint carry, uint nv, uint lo, uint newCarry)
		{
			var ld = FP31ValHelper.ConvertFrom2C(left);
			var rd = FP31ValHelper.ConvertFrom2C(right);
			var cd = FP31ValHelper.ConvertFrom2C(carry);
			var nvd = FP31ValHelper.ConvertFrom2C(nv);
			var hid = FP31ValHelper.ConvertFrom2C(newCarry);
			var lod = FP31ValHelper.ConvertFrom2C(lo);

			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {left:X4}, {right:X4} wc:{carry:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv:X4}: hi:{newCarry:X4}, lo:{lo:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod}\n");
		}

		private void ReportForMultiplication(int step, ulong left, ulong carry, ulong nv, ulong lo, ulong newCarry)
		{
			var ld = FP31ValHelper.ConvertFrom2C(left, out var leftHi);
			var cd = FP31ValHelper.ConvertFrom2C(carry, out var carryHi);
			var nvd = FP31ValHelper.ConvertFrom2C(nv, out var nvHigh);
			var hid = FP31ValHelper.ConvertFrom2C(newCarry, out var newCarrryHi);
			var lod = FP31ValHelper.ConvertFrom2C(lo, out var resultHi);

			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {left:X4} wc:{carry:X4} ");
			Debug.WriteLineIf(USE_DET_DEBUG, $"Step:{step}: Adding {ld} wc:{cd} (leftHi: {leftHi}, carryHi: {carryHi}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nv:X4}: hi:{newCarry:X4}, lo:{lo:X4}");
			Debug.WriteLineIf(USE_DET_DEBUG, $"\t-> {nvd}: hi:{hid}, lo:{lod} (nvHi: {nvHigh}: hiHi: {newCarrryHi}, loHi: {resultHi} \n");
		}

		#endregion

		#region Comparison

		public bool IsGreaterOrEqThanThreshold(FP31Val a, uint threshold)
		{
			var left = a.Mantissa[^1];
			var result = left >= threshold;

			return result;
		}

		#endregion

		#region FP31Val Support

		public FP31Val CreateNewZeroFP31Val(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = FP31ValHelper.CreateNewZeroFP31Val(ApFixedPointFormat, precision);
			return result;
		}

		public FP31Val CreateNewMaxIntegerFP31Val(int precision = RMapConstants.DEFAULT_PRECISION)
		{
			//throw new InvalidOperationException("Some scalarMath Multiplication op overflowed");
			var rValue = new RValue(MaxIntegerValue, 0, precision);
			var result = FP31ValHelper.CreateFP31Val(rValue, ApFixedPointFormat);
			return result;
		}

		/// <summary>
		/// This is used to create our results. It should only be used internally.
		/// </summary>
		/// <param name="partialWordLimbs"></param>
		/// <param name="precision"></param>
		/// <returns></returns>
		private FP31Val CreateFP31Val(uint[] partialWordLimbs, int precision)
		{
			var result = new FP31Val(partialWordLimbs, TargetExponent, BitsBeforeBP, precision);
			return result;
		}

		#endregion

		#region DEBUG Checks

		private void CheckLimbsBeforeShiftAndTrim(ulong[] mantissa)
		{
			//ValidateIsSplit(mantissa); // Conditional Method

			var lZCounts = GetLZCounts(mantissa);

			Debug.WriteLine($"Before Shift and Trim, LZCounts:");
			for (var lzcPtr = 0; lzcPtr < lZCounts.Length; lzcPtr++)
			{
				Debug.WriteLine($"{lzcPtr}: {lZCounts[lzcPtr]} {mantissa[lzcPtr]}");
			}

			Debug.Assert(lZCounts[^1] >= 32 + 1 + BitsBeforeBP, "The multiplication result is > Max Integer.");
		}

		private void CheckLimbsAfterShiftAndTrim(uint[] result)
		{
			var lZCounts = GetLZCounts(result);
			Debug.WriteLine($"S&T LZCounts2:");
			for (var lzcPtr = 0; lzcPtr < lZCounts.Length; lzcPtr++)
			{
				Debug.WriteLine($"{lzcPtr}: {lZCounts[lzcPtr]} {result[lzcPtr]}");
			}
		}

		// Just for diagnostics
		public byte[] GetLZCounts(ulong[] values)
		{
			var result = new byte[values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				result[i] = (byte)BitOperations.LeadingZeroCount(values[i]);
			}

			return result;
		}

		// Just for diagnostics
		public byte[] GetLZCounts(uint[] values)
		{
			var result = new byte[values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				result[i] = (byte)BitOperations.LeadingZeroCount(values[i]);
			}

			return result;
		}

		//[Conditional("DETAIL")]
		//private void CheckLimbCountAndFPFormat(FP31Val smx)
		//{
		//	if (smx.LimbCount != LimbCount)
		//	{
		//		throw new ArgumentException($"While converting an Smx2C found it to have {smx.LimbCount} limbs instead of {LimbCount}.");
		//	}

		//	if (smx.Exponent != TargetExponent)
		//	{
		//		throw new ArgumentException($"While converting an Smx2C found it to have {smx.Exponent} limbs instead of {TargetExponent}.");
		//	}

		//	if (smx.BitsBeforeBP != BitsBeforeBP)
		//	{
		//		throw new ArgumentException($"While converting an Smx2C found it to have {smx.BitsBeforeBP} limbs instead of {BitsBeforeBP}.");
		//	}
		//}

		//[Conditional("DETAIL")]
		//private void CheckLimbs2C(FP31Val a, FP31Val b, string desc)
		//{
		//	if (a.LimbCount != LimbCount)
		//	{
		//		Debug.WriteLine($"WARNING: The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//		throw new InvalidOperationException($"The left value has a limbcount of {a.LimbCount}, expecting: {LimbCount}.");
		//	}

		//	if (b.LimbCount != LimbCount)
		//	{
		//		Debug.WriteLine($"WARNING: The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
		//		throw new InvalidOperationException($"The right value has a limbcount of {b.LimbCount}, expecting: {LimbCount}.");
		//	}

		//	if (a.Exponent != b.Exponent)
		//	{
		//		Debug.WriteLine($"Warning:the exponents do not match.");
		//		throw new InvalidOperationException($"The exponents do not match.");
		//	}

		//	//ValidateIsSplit2C(a.Mantissa, a.Sign);
		//	//ValidateIsSplit2C(b.Mantissa, b.Sign);
		//}

		[Conditional("DETAIL")]
		private void CheckLimb2C(FP31Val a, string desc)
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

			//ValidateIsSplit2C(a.Mantissa, a.Sign);
		}

		[Conditional("DETAIL")]
		private void ValidateIsSplit2C(ulong[] mantissa)
		{
			//if (ScalarMathHelper.CheckPW2CValues(mantissa))
			//{
			//	throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			//}
		}

		[Conditional("DETAIL")]
		private void ValidateIsSplit2C(uint[] mantissa, bool sign)
		{
			//if (ScalarMathHelper.CheckPW2CValues(mantissa, sign))
			//{
			//	throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			//}

			var signFromMantissa = FP31ValHelper.GetSign(mantissa);

			if (sign != signFromMantissa)
			{
				throw new ArgumentException($"Expected the mantissa to have sign: {sign}.");
			}

			//if (ScalarMathHelper.CheckPW2CValues(mantissa))
			//{
			//	throw new ArgumentException($"Expected the mantissa to be split into uint32 values.");
			//}

		}

		#endregion
	}
}
