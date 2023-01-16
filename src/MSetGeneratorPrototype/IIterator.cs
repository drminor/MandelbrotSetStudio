using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal interface IIterator
	{
		ApFixedPointFormat ApFixedPointFormat { get; }
		MathOpCounts MathOpCounts { get; }

		Vector256<int>[] Iterate(int[] inPlayList, out FP31Deck sumOfSquares);
		void SetCoords(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs);
	}
}