
using MSS.Types.Base;

namespace MSS.Types
{
	public class RRectangle : Rectangle<long>
	{
		public int Exponent { get; init; }

		public RRectangle() : base()
		{
			Exponent = 0;
		}

		public RRectangle(long[] values, int exponent) : base(values)
		{
			Exponent = exponent;
		}

		public RRectangle(long x1, long x2, long y1, long y2, int exponent) : base(x1, x2, y1, y2)
		{
			Exponent = exponent;
		}

		public new RPoint LeftBot => new RPoint(X1, Y1, Exponent);

		public new RPoint RightTop => new RPoint(X2, Y2, Exponent);

		public RSize Size => new RSize(X2 - X1, Y2 - Y2, Exponent);

		public long WidthNumerator => X2 - X1;
		public long HeigthNumerator => Y2 - Y1;
	}
}
