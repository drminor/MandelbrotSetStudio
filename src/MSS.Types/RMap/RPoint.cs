
namespace MSS.Types
{
	public class RPoint
	{
		public long XN { get; init; }
		public long YN { get; init; }
		public int Exp { get; init; }

		public RPoint() : this(0, 0, 0) { }

		public RPoint(long xN, long yN, int exp)
		{
			XN = xN;
			YN = yN;
			Exp = exp;
		}
	}
}
