using MSS.Types.Base;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public class RSize : Size<BigInteger>, ICloneable
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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public new RSize Clone()
		{
			return new RSize(Values, Exponent);
		}

		public RSize Scale(SizeInt factor)
		{
			return new RSize(Width * factor.Width, Height * factor.Height, Exponent);
		}

		public RSize Scale(PointInt factor)
		{
			return new RSize(Width * factor.X, Height * factor.Y, Exponent);
		}

		public RSize Translate(RPoint amount)
		{
			return amount.Exponent != Exponent
                ?                throw new InvalidOperationException($"Cannot translate an RSize with Exponent: {Exponent} using an RPoint with Exponent: {amount.Exponent}.")
				: new RSize(Width + amount.X, Height + amount.Y, Exponent);
		}

	}
}
