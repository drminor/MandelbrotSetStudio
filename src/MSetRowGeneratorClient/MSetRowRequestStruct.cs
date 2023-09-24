using System.Runtime.InteropServices;

namespace MSetRowGeneratorClient
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct MSetRowRequestStruct
	{
		// BlockSize
		public int BlockSizeWidth;
		public int BlockSizeHeight;
		
		// ApFixedPointFormat
		public int BitsBeforeBinaryPoint;
        public int LimbCount;
        public int NumberOfFractionalBits;
        public int TotalBits;
        public int TargetExponent;

		public int Lanes;
		public int VectorsPerRow;

		// Subdivision
		//public string subdivisionId;

		// The row to calculate
		public int RowNumber;

		// MapCalcSettings;
		public int TargetIterations;
		public int ThresholdForComparison;
		public int IterationsPerStep;
	}
}
