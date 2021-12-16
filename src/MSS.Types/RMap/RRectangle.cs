using MSS.Types.Base;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public class RRectangle : Rectangle<BigInteger>, ICloneable
	{
		public int Exponent { get; init; }

		public RRectangle() : base()
		{
			Exponent = 0;
		}

		public RRectangle(BigInteger[] values, int exponent) : base(values)
		{
			Exponent = exponent;
			Validate();
		}

		public RRectangle(BigInteger[] xValues, BigInteger[] yValues, int exponent) : base(xValues[0], xValues[1], yValues[0], yValues[1])
		{
			Exponent = exponent;
			Validate();
		}

		public RRectangle(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent) : base(x1, x2, y1, y2)
		{
			Exponent = exponent;
			Validate();
		}

		public new RPoint LeftBot => new RPoint(X1, Y1, Exponent);

		public new RPoint RightTop => new RPoint(X2, Y2, Exponent);

		public BigInteger[] XValues => new BigInteger[] { X1, X2 };
		public BigInteger[] YValues => new BigInteger[] { Y1, Y2 };

		public BigInteger WidthNumerator => X2 - X1;
		public BigInteger HeightNumerator => Y2 - Y1; 
		public RSize Size => new RSize(WidthNumerator, HeightNumerator, Exponent);

		object ICloneable.Clone()
		{
			return Clone();
		}

		public new RRectangle Clone()
		{
			return new RRectangle(Values, Exponent);
		}

		public RRectangle Scale(RPoint factor)
		{
			return factor.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot scale a RRectangle with Exponent: {Exponent} using a RPoint with Exponent: {factor.Exponent}.")
				: new RRectangle(X1 * factor.X, X2 * factor.X, Y1 * factor.Y, Y2 * factor.Y, Exponent);
		}
		
		public RRectangle Scale(RSize factor)
		{
			return factor.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot scale a RRectangle with Exponent: {Exponent} using a RSize with Exponent: {factor.Exponent}.")
				: new RRectangle(X1 * factor.Width, X2 * factor.Width, Y1 * factor.Height, Y2 * factor.Height, Exponent);
		}

		public RRectangle Translate(RPoint amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RRectangle with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
				: new RRectangle(X1 + amount.X, X2 + amount.X, Y1 + amount.Y, Y2 + amount.Y, Exponent);
		}

		public RRectangle Translate(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RRectangle with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
				: new RRectangle(X1 + amount.Width, X2 + amount.Width, Y1 + amount.Height, Y2 + amount.Height, Exponent);
		}

		public RRectangle ScaleB(int exponentDelta)
		{
			if (exponentDelta == 0)
			{
				return this;
			}

			var factor = (long)Math.Pow(2, -1 * exponentDelta);
			var result = new RRectangle(Values.Select(v => v * factor).ToArray(), Exponent - exponentDelta);

			return result;
		}

		[Conditional("Debug")]
		private void Validate()
		{
			if (X1 > X2)
			{
				throw new ArgumentException($"The beginning X must be less than or equal to the ending X.");
			}
			
			if (Y1 > Y2)
			{ 
				throw new ArgumentException($"The beginning Y must be less than or equal to the ending Y.");
			}
		}
	}

}
