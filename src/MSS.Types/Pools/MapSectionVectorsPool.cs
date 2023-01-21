
namespace MSS.Types
{
	public class MapSectionVectorsPool : ObjectPool<MapSectionVectors>
	{
		private readonly SizeInt _blockSize;

		public MapSectionVectorsPool(SizeInt BlockSize, int initialSize = 16) : base(initialSize)
		{
			_blockSize = BlockSize;

			for (var i = 0; i < initialSize; i++)
			{
				_pool.Push(NewObject());
			}
		}

		protected override MapSectionVectors NewObject()
		{
			return new MapSectionVectors(_blockSize);
		}
	}
}
