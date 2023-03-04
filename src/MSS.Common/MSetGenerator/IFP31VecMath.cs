using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IFP31VecMath
	{
		ApFixedPointFormat ApFixedPointFormat { get; init; }
		int LimbCount { get; }
		MathOpCounts MathOpCounts { get; init; }

		void Add(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result);
		void Sub(Vector256<uint>[] left, Vector256<uint>[] right, Vector256<uint>[] result);

		void Square(Vector256<uint>[] a, Vector256<uint>[] result);

		Vector256<int> CreateVectorForComparison(uint value);
		void IsGreaterOrEqThan(Vector256<uint>[] left, ref Vector256<int> right, ref Vector256<int> escapedFlagsVec);

		string Implementation { get; }

		//Vector256<uint>[] CreateNewLimbSet();
		//Vector256<ulong>[] CreateNewLimbSetWide();
	}
}