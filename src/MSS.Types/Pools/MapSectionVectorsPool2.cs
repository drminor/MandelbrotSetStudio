
namespace MSS.Types
{
	public class MapSectionVectorsPool2 : ObjectPool<MapSectionVectors2>
	{
		public MapSectionVectorsPool2(SizeInt blockSize, int initialSize = 16) : base(initialSize)
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
