using System;
using System.Diagnostics;

namespace MSS.Types
{
    public class ApFixedPointFormat
    {
        // NOTE: Currently we are using the same default settting of 8 BITS_BEFORE_BP, 
        // for implementations that use unsigned values and signed values.
        // This means that effectively signed values have 1/2 the range of unsigned value.

        private const int EFFECTIVE_BITS_PER_LIMB = 31;

        public ApFixedPointFormat(int limbCount) : this(RMapConstants.BITS_BEFORE_BP, limbCount * EFFECTIVE_BITS_PER_LIMB - RMapConstants.BITS_BEFORE_BP)
        { }

        //public ApFixedPointFormat(float precision) : this(RMapConstants.BITS_BEFORE_BP, (int)precision)
        //{ }

        public ApFixedPointFormat(byte bitsBeforeBinaryPoint, int minimumFractionalBits)
        {
            BitsBeforeBinaryPoint = bitsBeforeBinaryPoint;

            var estimatedCountTotalBits = bitsBeforeBinaryPoint + minimumFractionalBits;
            LimbCount = GetLimbCount(estimatedCountTotalBits);

            NumberOfFractionalBits = LimbCount * EFFECTIVE_BITS_PER_LIMB - BitsBeforeBinaryPoint;

            CheckFractionalBitsValue(minimumFractionalBits);
        }

        public byte BitsBeforeBinaryPoint { get; init; }
        public int NumberOfFractionalBits { get; init; }
        public int LimbCount { get; init; }

        public int TotalBits => BitsBeforeBinaryPoint + NumberOfFractionalBits;

        public int TargetExponent => -1 * NumberOfFractionalBits;

        public override string ToString()
        {
            return $"fmt:{BitsBeforeBinaryPoint}:{NumberOfFractionalBits}(Limbs:{LimbCount})";
        }

        private int GetLimbCount(int totalNumberOfBits)
        {
            var dResult = totalNumberOfBits / (double)EFFECTIVE_BITS_PER_LIMB;
            var limbCount = (int)Math.Ceiling(dResult);

            return limbCount;
        }

        private void CheckFractionalBitsValue(int minimumFractionalBits)
        {
            if (NumberOfFractionalBits < minimumFractionalBits)
            {
                throw new InvalidOperationException("Using less fractional bits than the requested minimum.");
            }
            else if (NumberOfFractionalBits > minimumFractionalBits)
            {
                Debug.WriteLine($"Note: The number of fractional bits is being set to {NumberOfFractionalBits}. This is above the {minimumFractionalBits} requested.");
            }

        }

    }
}
