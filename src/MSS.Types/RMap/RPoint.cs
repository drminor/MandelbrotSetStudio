using MSS.Types.Base;
using System;
using System.Numerics;

namespace MSS.Types
{
	public class RPoint : Point<BigInteger>, IBigRatShape
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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public new RPoint Clone()
		{
			return Reducer.Reduce(this);
		}

		public RPoint Scale(SizeInt factor)
		{
			return new RPoint(X * factor.Width, Y * factor.Height, Exponent);
		}

		public RPoint Scale(PointInt factor)
		{
			return new RPoint(X * factor.X, Y * factor.Y, Exponent);
		}

		//public RPoint Scale(RSize factor)
		//{
		//	return factor.Exponent != Exponent
		//      ? throw new InvalidOperationException($"Cannot scale a RPoint with Exponent: {Exponent} using a RSize with Exponent: {factor.Exponent}.")
		//		: new RPoint(X * factor.Width, Y * factor.Height, factor.Exponent);
		//}

		public RPoint Translate(RPoint amount)
		{
			return Exponent != 0 && amount.Exponent != Exponent
                ?                throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
				: new RPoint(X + amount.X, Y + amount.Y, amount.Exponent);
		}

		public RPoint Translate(RSize amount)
		{
			return Exponent != 0 && amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
				: new RPoint(X + amount.WidthNumerator, Y + amount.HeightNumerator, amount.Exponent);
		}

		/// <summary>
		/// Returns the distance from this point to the given point.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public RSize Diff(RPoint amount)
		{
			return Exponent != 0 && amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot find the diff from a RPoint with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
				: new RSize(X - amount.X, Y - amount.Y, amount.Exponent);
		}

		public override string? ToString()
		{
			var result = BigIntegerHelper.GetDisplay(Reducer.Reduce(this)); 
			return result;
		}
	}
}
