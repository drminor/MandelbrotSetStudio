using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterator
	{
		bool IncreasingIterations { get; set; }
		//MathOpCounts MathOpCounts { get; }

		Vector256<uint>[] IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> DoneFlagsVec);
		Vector256<uint>[] Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> DoneFlagsVec);
	}
}