using MSS.Types.APValues;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSS.Common
{
	public static class FP31VecMathHelper
	{
		#region Private Fields

		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		private const uint RESERVED_BIT_MASK = 0x80000000;
		private static readonly Vector256<uint> RESERVED_BIT_MASK_VEC = Vector256.Create(RESERVED_BIT_MASK);

		private const uint LOW31_BITS_SET = 0x7FFFFFFF; // bits 0 - 30 are set.
		private static readonly Vector256<uint> HIGH33_MASK_VEC = Vector256.Create(LOW31_BITS_SET);

		private const uint TEST_BIT_30 = 0x40000000; // bit 30 is set.
		private static readonly Vector256<uint> TEST_BIT_30_VEC = Vector256.Create(TEST_BIT_30);

		private const uint TEST_BIT_31 = 0x80000000; // bit 30 is set.
		private static readonly Vector256<uint> TEST_BIT_31_VEC = Vector256.Create(TEST_BIT_31);

		private static readonly Vector256<int> THREE = Vector256.Create(3);

		#endregion

		#region Limb Set Support

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector256<uint>[] CreateNewLimbSet(int limbCount)
		{
			return new Vector256<uint>[limbCount];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector256<ulong>[] CreateNewLimbSetWide(int limbCount)
		{
			return new Vector256<ulong>[limbCount * 2];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearLimbSet(Vector256<uint>[] limbSet)
		{
			for (var i = 0; i < limbSet.Length; i++)
			{
				limbSet[i] = Vector256<uint>.Zero; // Avx2.Xor(limbs[i], limbs[i]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ClearLimbSetXor(Vector256<uint>[] limbSet)
		{
			// Clear instead of copying form source
			for (var i = 0; i < limbSet.Length; i++)
			{
				limbSet[i] = Avx2.Xor(limbSet[i], limbSet[i]);
			}
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WarnIfAnyCarry(Vector256<uint> source, Vector256<int> mask, string description)
		{
			var carry = Avx2.ShiftRightLogical(source, EFFECTIVE_BITS_PER_LIMB).AsInt32();
			var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (isZeroComp != -1)
			{
				Debug.WriteLine($"WARNING: Found one element with a carry while {description}.");
			}
		}

		[Conditional("DEBUG2")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WarnIfAnyNotZero(Vector256<uint> carry, Vector256<int> mask, string description)
		{
			var cIsZeroFlags = Avx2.CompareEqual(carry.AsInt32(), Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (isZeroComp != -1)
			{
				Debug.WriteLine($"WARNING: Found one element with a carry while {description}.");
			}
		}

		[Conditional("DEBUG2")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WarnIfAnyNotZero(Vector256<ulong> carry, Vector256<int> mask, string description)
		{
			var cIsZeroFlags = Avx2.CompareEqual(carry.AsInt32(), Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (isZeroComp != -1)
			{
				Debug.WriteLine($"WARNING: Found one element with a carry while {description}.");
			}
		}

		// FOR ADD OR SUB
		[Conditional("DEBUG2")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WarnIfAnyCarryForAddSub(Vector256<uint> source, Vector256<int> mask, string description)
		{
			var carryA = Avx2.ShiftRightLogical(source, EFFECTIVE_BITS_PER_LIMB - 1).AsInt32();
			var cIsZeroFlagsA = Avx2.CompareEqual(carryA, Vector256<int>.Zero);
			var cIsThreeFlags = Avx2.CompareEqual(carryA, THREE);

			var cIsZeroOrThreeFlags = Avx2.Or(cIsZeroFlagsA, cIsThreeFlags);

			cIsZeroOrThreeFlags = Avx2.BlendVariable(cIsZeroOrThreeFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroCompA = Avx2.MoveMask(cIsZeroOrThreeFlags.AsByte());

			//if (isZeroCompA != -1)
			//{
			//	Debug.WriteLine($"WARNING: Found one element with a carry while {description}.");
			//}

			// Now that we have the Negation logic fixed, we can go back to simple overflow detection.
			var carry = Avx2.ShiftRightLogical(source, EFFECTIVE_BITS_PER_LIMB);
			var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<uint>.Zero);
			var cIsZeroFlags2 = Avx2.BlendVariable(cIsZeroFlags, Vector256<uint>.AllBitsSet, mask.AsUInt32());
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags2.AsByte());

			if (isZeroComp != -1)
			{
				Debug.WriteLine($"WARNING: Found one element with a carry while {description}.");
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AnyCarryFound(Vector256<uint> source, Vector256<int> mask)
		{
			var carry = Avx2.ShiftRightLogical(source, EFFECTIVE_BITS_PER_LIMB).AsInt32();
			var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			return isZeroComp != -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfAnyCarry(Vector256<uint> source, Vector256<int> mask, string description)
		{
			var carry = Avx2.ShiftRightLogical(source, EFFECTIVE_BITS_PER_LIMB).AsInt32();
			var cIsZeroFlags = Avx2.CompareEqual(carry, Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);

			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (isZeroComp != -1)
			{
				throw new OverflowException($"Found one element with a carry while {description}.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfAnyNotZero(Vector256<uint> carry, Vector256<int> mask, string description)
		{
			var cIsZeroFlags = Avx2.CompareEqual(carry.AsInt32(), Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);

			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			if (isZeroComp != -1)
			{
				throw new OverflowException($"Found one element with a carry while {description}.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AnyNotZero(Vector256<uint> carry, Vector256<int> mask)
		{
			var cIsZeroFlags = Avx2.CompareEqual(carry.AsInt32(), Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			return isZeroComp != -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AnyNotZero(Vector256<ulong> carry, Vector256<int> mask)
		{
			var cIsZeroFlags = Avx2.CompareEqual(carry.AsInt32(), Vector256<int>.Zero);
			cIsZeroFlags = Avx2.BlendVariable(cIsZeroFlags, Vector256<int>.AllBitsSet, mask);
			var isZeroComp = Avx2.MoveMask(cIsZeroFlags.AsByte());

			return isZeroComp != -1;
		}

		[Conditional("DEBUG2")]
		public static void CheckReservedBitIsClear(Vector256<uint>[] source, string description)
		{
			for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
			{
				var justReservedBit = Avx2.And(source[limbPtr], RESERVED_BIT_MASK_VEC);
				var cFlags = Avx2.CompareEqual(justReservedBit, Vector256<uint>.Zero);
				var cComposite = Avx2.MoveMask(cFlags.AsByte());
				if (cComposite != -1)
				{
					throw new InvalidOperationException($"While {description}, found a limb with bit-31 set.");
				}
			}
		}

		//public static Vector256<uint> ExtendSignBit(Vector256<uint> source)
		//{
		//	var signBitIsResetVecs = Avx2.CompareEqual(Avx2.And(source, TEST_BIT_30_VEC), Vector256<uint>.Zero);
		//	var zeroOutReserve = Avx2.And(source, HIGH33_MASK_VEC);
		//	var setReserveBit = Avx2.Or(source, TEST_BIT_31_VEC);

		//	var result = Avx2.BlendVariable(setReserveBit, zeroOutReserve, signBitIsResetVecs); // if signBitIsReset, set the result to zeroOutTheReserveBit, otherwise turn on the reserveBit
			
		//	return result;
		//}

		//[Conditional("DEBUG")]
		//public static void CheckReservedBitIsClear(Vector256<uint>[] source, string description, int[] inPlayList)
		//{
		//	var sb = new StringBuilder();

		//	var indexes = inPlayList;

		//	for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
		//	{
		//		var oneFound = false;

		//		for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
		//		{
		//			var idx = indexes[idxPtr];

		//			var justReservedBit = Avx2.And(source[idx], RESERVED_BIT_MASK_VEC);

		//			var cFlags = Avx2.CompareEqual(justReservedBit, Vector256<uint>.Zero);
		//			var cComposite = Avx2.MoveMask(cFlags.AsByte());
		//			if (cComposite != -1)
		//			{
		//				sb.AppendLine($"Top bit was set at.\t{idxPtr}\t{limbPtr}\t{cComposite}.");
		//			}
		//		}

		//		if (oneFound)
		//		{
		//			sb.AppendLine();
		//		}
		//	}

		//	var errors = sb.ToString();

		//	if (errors.Length > 1)
		//	{
		//		throw new InvalidOperationException($"Found a set ReservedBit while {description}.Results:\nIdx\tlimb\tVal\n {errors}");
		//	}
		//}

		#endregion

		#region Reporting

		[Conditional("DEBUG2")]
		public static void ReportForAddition(int step, Vector256<uint> left, Vector256<uint> right, Vector256<uint> carry, Vector256<uint> nv, Vector256<uint> lo, Vector256<uint> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var rightVal0 = right.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = FP31ValHelper.ConvertFrom2C(leftVal0);
			var rd = FP31ValHelper.ConvertFrom2C(rightVal0);
			var cd = FP31ValHelper.ConvertFrom2C(carryVal0);
			var nvd = FP31ValHelper.ConvertFrom2C(nvVal0);
			var hid = FP31ValHelper.ConvertFrom2C(newCarryVal0);
			var lod = FP31ValHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0;
			var unSNv = leftVal0 + rightVal0 + carryVal0;


			Debug.WriteLine($"Step:{step}: Adding {leftVal0:X4}, {rightVal0:X4} wc:{carryVal0:X4} ");
			Debug.WriteLine($"Step:{step}: Adding {ld}, {rd} wc:{cd} ");
			Debug.WriteLine($"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLine($"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		[Conditional("DEBUG2")]
		public static void ReportForNegation(int step, Vector256<uint> left, Vector256<uint> carry, Vector256<uint> nv, Vector256<uint> lo, Vector256<uint> newCarry)
		{
			var leftVal0 = left.GetElement(0);
			var carryVal0 = carry.GetElement(0);
			var nvVal0 = nv.GetElement(0);
			var newCarryVal0 = newCarry.GetElement(0);
			var loVal0 = lo.GetElement(0);

			var ld = FP31ValHelper.ConvertFrom2C(leftVal0);
			var cd = FP31ValHelper.ConvertFrom2C(carryVal0);
			var nvd = FP31ValHelper.ConvertFrom2C(nvVal0);
			var hid = FP31ValHelper.ConvertFrom2C(newCarryVal0);
			var lod = FP31ValHelper.ConvertFrom2C(loVal0);

			var nvHiPart = nvVal0;
			var unSNv = leftVal0 + carryVal0;


			Debug.WriteLine($"Step:{step}: Adding {leftVal0:X4}, wc:{carryVal0:X4} ");
			Debug.WriteLine($"Step:{step}: Adding {ld}, wc:{cd} ");
			Debug.WriteLine($"\t-> {nvVal0:X4}: hi:{newCarryVal0:X4}, lo:{loVal0:X4}");
			Debug.WriteLine($"\t-> {nvd}: hi:{hid}, lo:{lod}. hpOfNv: {nvHiPart}. unSNv: {unSNv}\n");
		}

		#endregion
	}
}
