
namespace MSS.Types
{ 
	public class MapSectionValuesPool : ObjectPool<MapSectionValues>
	{
		private readonly SizeInt _blockSize;

		public MapSectionValuesPool(SizeInt BlockSize, int initialSize = 16) : base(initialSize)
		{
			_blockSize = BlockSize;
			Fill(initialSize);
		}

		protected override MapSectionValues NewObject()
		{
			return new MapSectionValues(_blockSize);
		}
	}
}
