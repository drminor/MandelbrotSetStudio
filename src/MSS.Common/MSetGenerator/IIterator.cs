using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterator
	{
		Vector256<uint>[] IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> DoneFlagsVec);
		Vector256<uint>[] IterateFirstRoundForIncreasingIterations(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> doneFlags);

		Vector256<uint>[] Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> DoneFlagsVec);
	}
}