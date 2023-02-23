using System.Runtime.InteropServices;

namespace MSetGeneratorLib
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct MapSectionRequestStruct
	{
		// The row to calculate

		public int RowNumber;

        // ApFixedPointFormat
        public int BitsBeforeBinaryPoint;
        public int LimbCount;
        public int NumberOfFractionalBits;
        public int TotalBits;
        public int TargetExponent;

		public int Lanes;
		public int VectorsPerRow;

        // Subdivision
		public string subdivisionId;

		// BlockSize
		public int blockSizeWidth;
		public int blockSizeHeight;

		// MapCalcSettings;
		public int maxIterations;
		public int threshold;
		public int iterationsPerStep;
	}
}
