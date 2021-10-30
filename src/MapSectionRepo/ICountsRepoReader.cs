namespace MapSectionRepo
{
	public interface ICountsRepoReader
	{
		bool ContainsKey(KPoint key);
		int[] GetCounts(KPoint key, int linePtr);
	}
}