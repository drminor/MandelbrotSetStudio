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

		void Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> doneFlagsVec);
		void IterateFirstRound(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis, ref Vector256<int> escapedFlagsVec, ref Vector256<int> DoneFlagsVec);

		Vector256<uint>[] GetModulusSquared(Vector256<uint>[] zrs, Vector256<uint>[] zis);
	}
}