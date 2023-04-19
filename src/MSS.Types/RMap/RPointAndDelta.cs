using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	public class RPointAndDelta : IBigRatShape, IEquatable<RPointAndDelta>, IEqualityComparer<RPointAndDelta?>
	{
		public static readonly RPointAndDelta Zero = new RPointAndDelta();

		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public int Precision { get; set; }	// Number of binary digits

		public RPointAndDelta() : this(0, 0, 0, 0, 0)
		{ }

		public RPointAndDelta(BigInteger[] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public RPointAndDelta(BigInteger[] posValues, BigInteger[] sizeValues, int exponent) : this(posValues[0], posValues[1], sizeValues[0], sizeValues[1], exponent)
		{ }

		public RPointAndDelta(RPoint p, RSize s) : this(p.XNumerator, p.YNumerator, s.WidthNumerator, s.HeightNumerator, p.Exponent)
		{
			if (p.Exponent != s.Exponent)
			{
				throw new ArgumentException($"Cannot create a RPointAndDelta from a Point with Exponent: {p.Exponent} and a Size with Exponent: {s.Exponent}.");
			}
		}

		public RPointAndDelta(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent, int? precision = null)
		{
			Values = new BigInteger[] { x1, x2, y1, y2 };
			Exponent = exponent;
			Precision = precision ?? BigIntegerHelper.DEFAULT_PRECISION;
		}

		public BigInteger X
		{
			get => Values[0];
			init => Values[0] = value;
		}

		public BigInteger Y
		{
			get => Values[1];
			init => Values[1] = value;
		}

		public BigInteger Width
		{
			get => Values[2];
			init => Values[2] = value;
		}

		public BigInteger Height
		{
			get => Values[3];
			init => Values[3] = value;
		}

		public RPoint Center => new(X, Y, Exponent);
		public RPoint Position => Center; 

		public BigInteger[] PositionValues => new BigInteger[] { X, Y };
		public BigInteger[] SizeValues => new BigInteger[] { Width, Height };

		//public RValue[] GetRValues() => new RValue[] { Left, Right, Bottom, Top };

		public RSize Size => new(Width, Height, Exponent);

		#region ToString / ICloneable / IEqualityComparer / IEquatable Support

		public override string ToString()
		{
			return ToString(reduce: true);
		}

		public string ToString(bool reduce)
		{
			return $"Position: {Center.ToString(reduce)}, Size: {Size.ToString(reduce)}";
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RPointAndDelta Clone()
		{
			return Reducer.Reduce(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as RPointAndDelta);
		}

		public bool Equals(RPointAndDelta? other)
		{
			return !(other is null)
				&& X.Equals(other.X)
				&& Y.Equals(other.Y)
				&& Width.Equals(other.Width)
				&& Height.Equals(other.Height)
				&& Exponent.Equals(other.Exponent);
		}

		public bool Equals(RPointAndDelta? x, RPointAndDelta? y)
		{
			if (x is null)
			{
				return y is null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public int GetHashCode([DisallowNull] RPointAndDelta obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y, Width, Height, Exponent);
		}

		public static bool operator ==(RPointAndDelta? p1, RPointAndDelta? p2)
		{
			return EqualityComparer<RPointAndDelta?>.Default.Equals(p1, p2);
		}

		public static bool operator !=(RPointAndDelta? p1, RPointAndDelta? p2)
		{
			return !(p1 == p2);
		}

		#endregion
	}
}
