using System.Runtime.InteropServices;

namespace MEngineService
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MapSectionRequestStruct
    {
        public string subdivisionId;

        // RVectorDto BlockPosition
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] blockPositionX;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] blockPositionY;
        public int blockPositionExponent;

        // RPointDto Position
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] positionX;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] positionY;
        public int positionExponent;

        // BlockSize
        public int blockSizeWidth;
        public int blockSizeHeight;

        // RSizeDto SamplePointsDelta;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] samplePointDeltaWidth;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public long[] samplePointDeltaHeight;
        public int samplePointDeltaExponent;

        // MapCalcSettings;
        public int maxIterations;
        public int threshold;
        public int iterationsPerStep;
    }


}
