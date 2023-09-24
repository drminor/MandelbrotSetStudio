
namespace MSS.Types
{
	public class MapSectionVectors2Pool : ObjectPool<MapSectionVectors2>
	{
		public MapSectionVectors2Pool(SizeInt blockSize, int initialSize = 16) : base(initialSize)
		{
			BlockSize = blockSize;
			Fill(initialSize);
		}

		public SizeInt BlockSize { get; init; }

		protected override MapSectionVectors2 NewObject()
		{
			return new MapSectionVectors2(BlockSize);
		}
	}
}
