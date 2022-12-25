using MSS.Common;
using System.Diagnostics;

namespace MSetGenP
{
	public class ApFixedPointFormat
	{
		// NOTE: Currently we are using the same default settting of 8 BITS_BEFORE_BP, 
		// for implementations that use unsigned values and signed values.
		// This means that effectively signed values have 1/2 the range of unsigned value.


		private const int BITS_PER_LIMB = 32;
		private const int EFFECTIVE_BITS_PER_LIMB = 31;

		public ApFixedPointFormat(int limbCount) : this(RMapConstants.BITS_BEFORE_BP, limbCount * EFFECTIVE_BITS_PER_LIMB - RMapConstants.BITS_BEFORE_BP)
		{ }

		public ApFixedPointFormat(float precision) : this(RMapConstants.BITS_BEFORE_BP, (int)precision)
		{
			//BitsBeforeBinaryPoint = RMapConstants.BITS_BEFORE_BP;

			//var estimatedCountTotalBits = (int) (precision + BitsBeforeBinaryPoint);


			//LimbCount = GetLimbCount(estimatedCountTotalBits);
			//NumberOfFractionalBits = LimbCount * BITS_PER_LIMB - BitsBeforeBinaryPoint;
		}

		public ApFixedPointFormat(byte bitsBeforeBinaryPoint, int minimumFractionalBits)
		{
			BitsBeforeBinaryPoint = bitsBeforeBinaryPoint;

			var estimatedCountTotalBits = bitsBeforeBinaryPoint + minimumFractionalBits;
			LimbCount = GetLimbCount(estimatedCountTotalBits);

			NumberOfFractionalBits = (LimbCount * EFFECTIVE_BITS_PER_LIMB) - BitsBeforeBinaryPoint;

			Debug.Assert(NumberOfFractionalBits >= minimumFractionalBits, "Using less fractional bits than the requested minimum.");

			if (NumberOfFractionalBits != minimumFractionalBits)
			{
				Debug.WriteLine($"Note: The number of fractional bits is being set to {NumberOfFractionalBits}. This is above the {minimumFractionalBits} requested.");
			}
		}

		public int TotalBits => LimbCount * BITS_PER_LIMB;
		public int TotalEffectiveBits => BitsBeforeBinaryPoint + NumberOfFractionalBits;
		public int NumberOfFractionalBits { get; init; }

		public int TargetExponent => -1 * NumberOfFractionalBits; // Adjusted to offset the fact that 8 bits before the BP, only gives us 6 bit of range due to using Signed values and reserving a bit for Carry Detection.
		public int LimbCount { get; init; }
		public byte BitsBeforeBinaryPoint { get; init; }

		public override string ToString()
		{
			return $"fmt:{BitsBeforeBinaryPoint}:{NumberOfFractionalBits}({TotalBits})";
		}


		private int GetLimbCount(int totalNumberOfBits)
		{
			var dResult = totalNumberOfBits / (double)EFFECTIVE_BITS_PER_LIMB;
			var limbCount = (int)Math.Ceiling(dResult);

			return limbCount;

		}

	}
}
