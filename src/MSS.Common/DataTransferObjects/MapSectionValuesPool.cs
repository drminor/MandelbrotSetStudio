using MSS.Types;

namespace MSS.Common.DataTransferObjects
{
	public class MapSectionValuesPool : ObjectPool<MapSectionValues>
	{
		private readonly SizeInt _blockSize;

		public MapSectionValuesPool(SizeInt BlockSize, int initialSize = 16) : base(initialSize)
		{
			_blockSize = BlockSize;

			for (var i = 0; i < initialSize; i++)
			{
				_pool.Push(NewObject());
			}
		}

		protected override MapSectionValues NewObject()
		{
			return new MapSectionValues(_blockSize);
		}
	}
}
