using MSS.Types;

namespace MSetExplorer
{
	// Screen Coordinates with double precision accuracy.
	public class ScaledImageViewInfo
	{
		public SizeDbl ContentViewportSize { get; init; }
		public VectorDbl ContentOffset { get; init; }
		public double ContentScale { get; init; }	

		//public double OffsetX => PositionRelativeToPosterMapBlockOffset.X;
		//public double OffsetY => PositionRelativeToPosterMapBlockOffset.Y;

		public ScaledImageViewInfo(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			ContentViewportSize = contentViewportSize;
			ContentOffset = contentOffset;
			ContentScale = contentScale;
		}
	}

}
