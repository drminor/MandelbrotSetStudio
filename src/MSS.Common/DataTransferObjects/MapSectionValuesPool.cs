using MSS.Types;

namespace MSS.Common.DataTransferObjects
{
	public class MapSectionValuesPool : ObjectPool<MapSectionValues>
	{
		private readonly SizeInt _blockSize;

		public MapSectionValuesPool(SizeInt BlockSize)
		{
			_blockSize = BlockSize;
		}

		protected override MapSectionValues NewObject()
		{
			return new MapSectionValues(_blockSize);
		}
	}
}
