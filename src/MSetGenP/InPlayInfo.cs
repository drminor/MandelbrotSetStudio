
namespace MSetGenP
{
	public class InPlayInfo
	{
		public InPlayInfo() : this(null)
		{ }

		public InPlayInfo(List<int>? inPlayList)
		{
			InPlayList = inPlayList ?? new List<int>();
		}

		public List<int> InPlayList;

		public int Count => InPlayList.Count;
	}
}
