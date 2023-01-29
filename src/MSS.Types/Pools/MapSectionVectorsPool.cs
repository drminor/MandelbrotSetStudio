
namespace MSS.Types
{
	public class MapSectionVectorsPool : ObjectPool<MapSectionVectors>
	{
		public MapSectionVectorsPool(SizeInt blockSize, int initialSize = 16) : base(initialSize)
		{
			BlockSize = blockSize;
			Fill(initialSize);
		}

		public SizeInt BlockSize { get; init; }

		protected override MapSectionVectors NewObject()
		{
			return new MapSectionVectors(BlockSize);
		}
	}
}
