
namespace MSS.Types
{
	public class RSize
	{
		public long WN { get; init; }
		public long HN { get; init; }
		public int Exp { get; init; }

		public RSize() : this(0, 0, 0) { }

		public RSize(long wN, long hN, int exp)
		{
			WN = wN;
			HN = hN;
			Exp = exp;
		}
	}
}
