using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	internal interface IIterator
	{
		ApFixedPointFormat ApFixedPointFormat { get; }
		uint Threshold { get; set; } 
		MathOpCounts MathOpCounts { get; }

		void SetCoords(FP31Val[] samplePointsX, FP31Val samplePointY);
		void SetCoords(FP31Deck cRs, FP31Deck cIs, FP31Deck zRs, FP31Deck zIs);


		Vector256<int>[] Iterate(int[] inPlayList);
	}
}