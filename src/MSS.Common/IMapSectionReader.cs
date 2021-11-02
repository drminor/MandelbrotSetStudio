using MSS.Types;

namespace MSS.Common
{
	public interface IMapSectionReader
	{
		bool ContainsKey(KPoint key);

		int[] GetCounts(KPoint key, int linePtr);
		SizeInt GetImageSizeInBlocks();
	}
}