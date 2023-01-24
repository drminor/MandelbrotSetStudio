using MSS.Common.APValues;
using MSS.Types;

namespace MSetGeneratorPrototype
{
	internal class IteratorCoords
	{
		public IteratorCoords(BigVector blockPos, PointInt screenPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta)
		{
			BlockPos = blockPos ?? throw new ArgumentNullException(nameof(blockPos));
			ScreenPos = screenPos;
			StartingCx = startingCx;
			StartingCy = startingCy;
			Delta = delta;
		}

		public BigVector BlockPos { get; init; }
		public PointInt ScreenPos { get; init; }
		public FP31Val StartingCx { get; init;  }
		public FP31Val StartingCy { get; init; }
		public FP31Val Delta { get; init; }



	}
}
