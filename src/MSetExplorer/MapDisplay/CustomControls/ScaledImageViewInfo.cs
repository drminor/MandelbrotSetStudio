using MSS.Types;

namespace MSetExplorer
{
	// Screen Coordinates with double precision accuracy.
	public class ScaledImageViewInfo
	{
		public VectorDbl PositionRelativeToPosterMapBlockOffset { get; init; }
		public SizeDbl ContentViewPortSize { get; init; }

		public double OffsetX => PositionRelativeToPosterMapBlockOffset.X;
		public double OffsetY => PositionRelativeToPosterMapBlockOffset.Y;

		public ScaledImageViewInfo(VectorDbl offsets, SizeDbl viewPortSize)
		{
			PositionRelativeToPosterMapBlockOffset = offsets;
			ContentViewPortSize = viewPortSize;
		}
	}



}
