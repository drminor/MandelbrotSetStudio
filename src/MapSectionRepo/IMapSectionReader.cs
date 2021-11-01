using FSTypes;

namespace MapSectionRepo
{
	public interface IMapSectionReader
	{
		bool ContainsKey(KPoint key);

		int[] GetCounts(KPoint key, int linePtr);
		SizeInt GetImageSizeInBlocks();
	}
}