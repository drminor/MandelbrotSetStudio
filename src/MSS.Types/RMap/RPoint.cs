
using MSS.Types.Base;

namespace MSS.Types
{
	public class RPoint : Point<long>
	{
		public int Exp { get; init; }

		public RPoint() : this(0, 0, 0) { }

		public RPoint(long x, long y, int exp) : base(x, y)
		{
			Exp = exp;
		}
	}
}
