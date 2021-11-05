
namespace MSS.Types
{
	public class RPoint
	{
		public long XN { get; init; }
		public long YN { get; init; }
		public uint Exp { get; init; }

		public RPoint() : this(0, 0, 0) { }

		public RPoint(long xN, long yN, uint exp)
		{
			XN = xN;
			YN = yN;
			Exp = exp;
		}
	}
}
