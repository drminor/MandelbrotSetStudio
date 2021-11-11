using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class RPoint : Point<BigInteger>
	{
		public int Exponent { get; init; }

		public RPoint() : this(0, 0, 0)
		{ }

		public RPoint(BigInteger[] values, int exponent) : base(values)
		{
			Exponent = exponent;
		}

		public RPoint(BigInteger x, BigInteger y, int exponent) : base(x, y)
		{
			Exponent = exponent;
		}
	}
}
