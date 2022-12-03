
using MSS.Common;

namespace MSetGenP
{
	public class ApFixedPointFormat
	{
		private const int BITS_PER_LIMB = 32;

		public ApFixedPointFormat(int limbCount): this(RMapConstants.BITS_BEFORE_BP, limbCount * BITS_PER_LIMB - RMapConstants.BITS_BEFORE_BP)
		{ }

		public ApFixedPointFormat(int bitsBeforeBinaryPoint, int numberOfFractionalBits)
		{
			BitsBeforeBinaryPoint = bitsBeforeBinaryPoint;
			NumberOfFractionalBits = numberOfFractionalBits;
		}

		public int BitsBeforeBinaryPoint { get; init; }
		public int NumberOfFractionalBits { get; init; }

		public int TotalBits => BitsBeforeBinaryPoint + NumberOfFractionalBits;

		public override string ToString()
		{
			return $"fmt:{BitsBeforeBinaryPoint}:{NumberOfFractionalBits}";
		}
	}
}
