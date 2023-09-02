using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterator
	{
		bool IncreasingIterations { get; set; }
		MathOpCounts MathOpCounts { get; }
		uint Threshold { get; set; }
		uint ThresholdForEscVel { get; set; }

		void IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> DoneFlagsVec);
		void Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> doneFlagsVec);

		Vector256<uint>[] IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis);
		Vector256<uint>[] Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis);


		Vector256<uint>[] GetModulusSquared(Vector256<uint>[] zrs, Vector256<uint>[] zis);
	}
}