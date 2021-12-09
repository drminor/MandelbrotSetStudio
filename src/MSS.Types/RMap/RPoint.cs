using MSS.Types.Base;
using System;
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

		public RPoint(PointInt pi, int exponent) : base(pi.X, pi.Y)
		{
			Exponent = exponent;
		}

		public RPoint Scale(SizeInt factor)
		{
			return new RPoint(X * factor.Width, Y * factor.Height, Exponent);
		}

		public RPoint Scale(PointInt factor)
		{
			return new RPoint(X * factor.X, Y * factor.Y, Exponent);
		}

		public RPoint Scale(RSize factor)
		{
			if (factor.Exponent != Exponent)
			{
				throw new InvalidOperationException($"Cannot scale a RPoint with Exponent: {Exponent} using a RSize with Exponent: {factor.Exponent}.");
			}

			return new RPoint(X * factor.Width, Y * factor.Height, Exponent);
		}

		public RPoint Translate(RPoint amount)
		{
			if (amount.Exponent != Exponent)
			{
				throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.");
			}

			return new RPoint(X + amount.X, Y + amount.Y, Exponent);
		}
	}
}
