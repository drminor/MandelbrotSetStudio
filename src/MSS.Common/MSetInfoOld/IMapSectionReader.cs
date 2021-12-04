using MSS.Types;
using MSS.Types.MSetOld;

namespace MSS.Common
{
	public interface IMapSectionReader
	{
		bool ContainsKey(KPoint key);

		int[] GetCounts(KPoint key, int linePtr);
		SizeInt GetImageSizeInBlocks();
	}
}