using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class RSize : Size<BigInteger>
	{
		public int Exponent { get; init; }

		public RSize() : this(0, 0, 0)
		{ }

		public RSize(BigInteger[] values, int exponent) : base(values)
		{
			Exponent = exponent;
		}

		public RSize(BigInteger width, BigInteger height, int exponent) : base(width, height)
		{
			Exponent = exponent;
		}
	}
}
