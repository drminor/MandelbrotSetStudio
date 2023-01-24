using MSS.Common.APValues;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal interface IIterator
	{
		uint Threshold { get; set; }

		int LimbCount { get; }
		int ValueCount { get; }
		int VectorCount { get; }

		FP31Vectors Crs { get; }
		FP31Vectors Cis { get; }
		FP31Vectors Zrs { get; }
		FP31Vectors Zis { get; }

		bool ZValuesAreZero { get; set; }

		//MathOpCounts MathOpCounts { get; }

		Vector256<int>[] Iterate(int[] inPlayList, int[] inPlayListNarrow);

		Vector256<int> Iterate(Vector256<uint>[] crs, Vector256<uint>[] cis, Vector256<uint>[] zrs, Vector256<uint>[] zis);
	}
}