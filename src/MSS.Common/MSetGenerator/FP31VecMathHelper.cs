using MSS.Types.APValues;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace MSS.Common.MSetGenerator
{
	public static class FP31VecMathHelper
	{
		private const uint RESERVED_BIT_MASK = 0x80000000;
		private static readonly Vector256<uint> RESERVED_BIT_MASK_VEC = Vector256.Create(RESERVED_BIT_MASK);

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


		#region Diagnostics 

		public static void CheckReservedBitIsClear(Vector256<uint>[] source, string description, int[] inPlayList)
		{
			var sb = new StringBuilder();

			var indexes = inPlayList;

			for (int limbPtr = 0; limbPtr < source.Length; limbPtr++)
			{
				var oneFound = false;

				for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++)
				{
					var idx = indexes[idxPtr];

					var justReservedBit = Avx2.And(source[idx], RESERVED_BIT_MASK_VEC);

					var cFlags = Avx2.CompareEqual(justReservedBit, Vector256<uint>.Zero);
					var cComposite = Avx2.MoveMask(cFlags.AsByte());
					if (cComposite != -1)
					{
						sb.AppendLine($"Top bit was set at.\t{idxPtr}\t{limbPtr}\t{cComposite}.");
					}
				}

				if (oneFound)
				{
					sb.AppendLine();
				}
			}

			var errors = sb.ToString();

			if (errors.Length > 1)
			{
				throw new InvalidOperationException($"Found a set ReservedBit while {description}.Results:\nIdx\tlimb\tVal\n {errors}");
			}
		}

		#endregion

		#region Reporting

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
