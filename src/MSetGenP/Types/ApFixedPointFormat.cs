﻿
using MSS.Common;

namespace MSetGenP
{
	public class ApFixedPointFormat
	{
		private const int BITS_PER_LIMB = 32;

		public ApFixedPointFormat(int limbCount): this(RMapConstants.BITS_BEFORE_BP, limbCount * BITS_PER_LIMB - RMapConstants.BITS_BEFORE_BP)
		{ }

		public ApFixedPointFormat(byte bitsBeforeBinaryPoint, int numberOfFractionalBits)
		{
			BitsBeforeBinaryPoint = bitsBeforeBinaryPoint;
			NumberOfFractionalBits = numberOfFractionalBits;

			LimbCount = GetLimbCount(bitsBeforeBinaryPoint + numberOfFractionalBits);
		}

		public int TotalBits => BitsBeforeBinaryPoint + NumberOfFractionalBits;
		public int NumberOfFractionalBits { get; init; }

		public int TargetExponent => -1 * NumberOfFractionalBits;
		public int LimbCount { get; init; }
		public byte BitsBeforeBinaryPoint { get; init; }

		public override string ToString()
		{
			return $"fmt:{BitsBeforeBinaryPoint}:{NumberOfFractionalBits}";
		}


		private int GetLimbCount(int totalNumberOfBits)
		{
			var dResult = totalNumberOfBits / (double)BITS_PER_LIMB;
			var limbCount = (int)Math.Ceiling(dResult);

			return limbCount;

		}

	}
}
