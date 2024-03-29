﻿using MSS.Types;
using MSS.Types.APValues;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IFP31VecMath
	{
		ApFixedPointFormat ApFixedPointFormat { get; init; }
		int LimbCount { get; }
		MathOpCounts MathOpCounts { get; init; }

		void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlags);
		void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlags);

		//bool TrySub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result, ref Vector256<int> doneFlagsVec);

		void Square(Vector256<uint>[] a, Vector256<uint>[] result, ref Vector256<int> doneFlags);

		Vector256<int> CreateVectorForComparison(uint value);
		void IsGreaterOrEqThan(Vector256<uint>[] left, Vector256<int> right, ref Vector256<int> escapedFlagsVec);

		string Implementation { get; }

		//Vector256<uint>[] CreateNewLimbSet();
		//Vector256<ulong>[] CreateNewLimbSetWide();

		//FP31Val GetFP31Val(bool sign, uint[] limbs);
	}
}