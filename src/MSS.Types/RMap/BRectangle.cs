using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class BRectangle : Rectangle<BigInteger>
	{
		public int Exponent { get; init; }

		public BRectangle() : base()
		{
			Exponent = 0;
		}

		public BRectangle(BigInteger[] values, int exponent) : base(values)
		{
			Exponent = exponent;
		}

		public BRectangle(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent) : base(x1, x2, y1, y2)
		{
			Exponent = exponent;
		}

		public new BPoint LeftBot => new BPoint(X1, Y1, Exponent);

		public new BPoint RightTop => new BPoint(X2, Y2, Exponent);

		public BSize Size => new BSize(X2 - X1, Y2 - Y2, Exponent);

		public BigInteger WidthNumerator => X2 - X1;
		public BigInteger HeigthNumerator => Y2 - Y1;
	}

}
