
namespace MSS.Types
{
	public class MapSectionZVectorsPool : ObjectPool<MapSectionZVectors>
	{
		public MapSectionZVectorsPool(SizeInt blockSize, int limbCount, int initialSize = 16) : base(initialSize)
		{
			BlockSize = blockSize;
			LimbCount = limbCount;

			Fill(initialSize);
		}

		public SizeInt BlockSize { get; init; }
		public int LimbCount { get; init; }

		protected override MapSectionZVectors NewObject()
		{
			return new MapSectionZVectors(BlockSize, LimbCount);
		}
	}
}
