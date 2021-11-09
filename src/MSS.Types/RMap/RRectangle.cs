using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class RRectangle : Rectangle<BigInteger>
	{
		public int Exponent { get; init; }

		public RRectangle() : base()
		{
			Exponent = 0;
		}

		public RRectangle(BigInteger[] values, int exponent) : base(values)
		{
			Exponent = exponent;
		}

		public RRectangle(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent) : base(x1, x2, y1, y2)
		{
			Exponent = exponent;
		}

		public new RPoint LeftBot => new RPoint(X1, Y1, Exponent);

		public new RPoint RightTop => new RPoint(X2, Y2, Exponent);

		public RSize Size => new RSize(X2 - X1, Y2 - Y2, Exponent);

		public BigInteger WidthNumerator => X2 - X1;
		public BigInteger HeigthNumerator => Y2 - Y1;
	}

}
