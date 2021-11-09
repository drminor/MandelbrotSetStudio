using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class RPoint : Point<BigInteger>
	{
		public int Exp { get; init; }

		public RPoint() : this(0, 0, 0) { }

		public RPoint(BigInteger x, BigInteger y, int exp) : base(x, y)
		{
			Exp = exp;
		}
	}
}
