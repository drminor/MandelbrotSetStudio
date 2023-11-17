using MSS.Types;

namespace MSS.Common
{
	public class MapExtent
	{
		public MapExtent(SizeInt blockSize, SizeInt extent, SizeInt sizeOfFirstBlock, SizeInt sizeOfLastBlock)
		{
			BlockSize = blockSize;
			Extent = extent;
			SizeOfFirstBlock = sizeOfFirstBlock;
			SizeOfLastBlock = sizeOfLastBlock;
		}

		public SizeInt BlockSize { get; init; }
		public SizeInt Extent { get; init; }
		public SizeInt SizeOfFirstBlock { get; init; }
		public SizeInt SizeOfLastBlock { get; init; }

		public int Width => Extent.Width;
		public int Height => Extent.Height;
	}
}
