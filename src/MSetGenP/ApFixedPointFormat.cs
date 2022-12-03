
namespace MSetGenP
{
	public class ApFixedPointFormat
	{
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
