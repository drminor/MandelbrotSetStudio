using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class BPoint : Point<BigInteger>
	{
		public int Exp { get; init; }

		public BPoint() : this(0, 0, 0) { }

		public BPoint(BigInteger x, BigInteger y, int exp) : base(x, y)
		{
			Exp = exp;
		}
	}
}
