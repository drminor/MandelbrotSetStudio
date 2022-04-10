using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class RVector : IBigRatShape, IEquatable<RVector>, IEqualityComparer<RVector>
	{
		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public RVector() : this(0, 0, 0)
		{ }

		public RVector(BigInteger[] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		// Square from single value
		public RVector(RValue extent) : this(extent.Value, extent.Value, extent.Exponent)
		{ }

		public RVector(BigInteger width, BigInteger height, int exponent)
		{
			Values = new BigInteger[] { width, height };
			Exponent = exponent;
		}

		public BigInteger XNumerator => Values[0];

		public BigInteger YNumerator => Values[1];

		public RValue X => new(XNumerator, Exponent);
		public RValue Y => new(YNumerator, Exponent);

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RVector Clone()
		{
			return Reducer.Reduce(this);
		}

		public BigVector Divide(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot InvScale a RSize with Exponent: {Exponent} using an RSize with Exponent: {amount.Exponent}.")
				: new BigVector(XNumerator / amount.WidthNumerator, YNumerator / amount.HeightNumerator);
		}

		public override string ToString()
		{
			var result = BigIntegerHelper.GetDisplay(Reducer.Reduce(this));
			return result;
		}

		#region IEqualityComparer / IEquatable Support

		public bool Equals(RVector? a, RVector? b)
		{
			if (a is null)
			{
				return b is null;
			}
			else
			{
				return a.Equals(b);
			}
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as RVector);
		}

		public bool Equals(RVector? other)
		{
			return !(other is null) && XNumerator.Equals(other.XNumerator) && YNumerator.Equals(other.YNumerator);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(XNumerator, YNumerator);
		}

		public int GetHashCode(RVector obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(RVector p1, RVector p2)
		{
			return EqualityComparer<RVector>.Default.Equals(p1, p2);
		}

		public static bool operator !=(RVector p1, RVector p2)
		{
			return !(p1 == p2);
		}

		#endregion

	}
}
