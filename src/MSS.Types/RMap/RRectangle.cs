using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	public class RRectangle : IBigRatShape, IEquatable<RRectangle>, IEqualityComparer<RRectangle?>
	{
		public static readonly RRectangle Zero = new RRectangle();

		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public int Precision { get; set; }	// Number of binary digits

		public RRectangle() : this(0, 0, 0, 0, 0)
		{ }

		public RRectangle(BigInteger[] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public RRectangle(BigInteger[] xValues, BigInteger[] yValues, int exponent) : this(xValues[0], xValues[1], yValues[0], yValues[1], exponent)
		{ }

		public RRectangle(RPoint p, RSize s) : this(p.XNumerator, p.XNumerator + s.WidthNumerator, p.YNumerator, p.YNumerator + s.HeightNumerator, p.Exponent)
		{
			if (p.Exponent != s.Exponent)
			{
				throw new ArgumentException($"Cannot create a RRectangle from a Point with Exponent: {p.Exponent} and a Size with Exponent: {s.Exponent}.");
			}
		}

		public RRectangle(RPoint p1, RPoint p2) : this(p1.XNumerator, p2.XNumerator, p1.YNumerator, p2.YNumerator, p1.Exponent)
		{
			if (p1.Exponent != p2.Exponent)
			{
				throw new ArgumentException($"Cannot create a RRectangle from a Point with Exponent: {p1.Exponent} and a second Point with Exponent: {p2.Exponent}.");
			}
		}

		public RRectangle(RectangleInt rect) : this(rect.X1, rect.X2, rect.Y1, rect.Y2, 0, null)
		{ }

		public RRectangle(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent, int? precision = null)
		{
			Values = new BigInteger[] { x1, x2, y1, y2 };
			Exponent = exponent;
			Precision = precision ?? RMapConstants.DEFAULT_PRECISION;
			Validate();
		}

		public BigInteger X1
		{
			get => Values[0];
			init => Values[0] = value;
		}

		public BigInteger X2
		{
			get => Values[1];
			init => Values[1] = value;
		}

		public BigInteger Y1
		{
			get => Values[2];
			init => Values[2] = value;
		}

		public BigInteger Y2
		{
			get => Values[3];
			init => Values[3] = value;
		}

		public RValue Left => new RValue(X1, Exponent);
		public RValue Right => new RValue(X2, Exponent);
		public RValue Bottom => new RValue(Y1, Exponent);
		public RValue Top => new RValue(Y2, Exponent);

		public RPoint LeftBot => new(X1, Y1, Exponent);
		public RPoint Position => LeftBot; 

		public RPoint RightTop => new(X2, Y2, Exponent);

		public BigInteger[] XValues => new BigInteger[] { X1, X2 };
		public BigInteger[] YValues => new BigInteger[] { Y1, Y2 };

		public RValue[] GetRValues() => new RValue[] { Left, Right, Bottom, Top };

		public BigInteger WidthNumerator => X2 - X1;
		public BigInteger HeightNumerator => Y2 - Y1;

		public RValue Width => new(WidthNumerator, Exponent);
		public RValue Height => new(HeightNumerator, Exponent);
		public RSize Size => new(WidthNumerator, HeightNumerator, Exponent);

		public bool CrossesXZero => Values[2].Sign != Values[3].Sign && Values[2].Sign != 0 && Values[3].Sign != 0;


		//public RRectangle Scale(RPoint factor)
		//{
		//	return factor.Exponent != Exponent
		//		? throw new InvalidOperationException($"Cannot scale a RRectangle with Exponent: {Exponent} using a RPoint with Exponent: {factor.Exponent}.")
		//		: new RRectangle(X1 * factor.X, X2 * factor.X, Y1 * factor.Y, Y2 * factor.Y, factor.Exponent);
		//}

		public RRectangle Scale(RSize factor)
		{
			return new RRectangle(X1 * factor.WidthNumerator, X2 * factor.WidthNumerator, Y1 * factor.HeightNumerator, Y2 * factor.HeightNumerator, Exponent + factor.Exponent);
		}

		public RRectangle Translate(RPoint amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RRectangle with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
				: new RRectangle(X1 + amount.XNumerator, X2 + amount.XNumerator, Y1 + amount.YNumerator, Y2 + amount.YNumerator, amount.Exponent);
		}

		//public RRectangle Translate(RSize amount)
		//{
		//	return amount.Exponent != Exponent
		//		? throw new InvalidOperationException($"Cannot translate a RRectangle with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
		//		: new RRectangle(X1 + amount.WidthNumerator , X2 + amount.WidthNumerator, Y1 + amount.HeightNumerator, Y2 + amount.HeightNumerator, amount.Exponent);
		//}


		public RPoint GetCenter()
		{
			var offset = Size.DivideBy2();
			var centerP = new RPoint(Position.Translate(offset));

			return centerP;
		}

		[Conditional("DEBUG2")]
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

		#region ToString / ICloneable / IEqualityComparer / IEquatable Support

		public override string ToString()
		{
			var reduced = Reducer.Reduce(this);

			var result = $"P1: {reduced.LeftBot.ToString(reduce: false)}, P2: {reduced.RightTop.ToString(reduce: false)}";
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RRectangle Clone()
		{
			return Reducer.Reduce(this);
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as RRectangle);
		}

		public bool Equals(RRectangle? other)
		{
			return !(other is null)
				&& X1.Equals(other.X1)
				&& X2.Equals(other.X2)
				&& Y1.Equals(other.Y1)
				&& Y2.Equals(other.Y2)
				&& Exponent.Equals(other.Exponent);
		}

		public bool Equals(RRectangle? x, RRectangle? y)
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

		public int GetHashCode([DisallowNull] RRectangle obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X1, X2, Y1, Y2, Exponent);
		}

		public static bool operator ==(RRectangle? p1, RRectangle? p2)
		{
			return EqualityComparer<RRectangle?>.Default.Equals(p1, p2);
		}

		public static bool operator !=(RRectangle? p1, RRectangle? p2)
		{
			return !(p1 == p2);
		}

		#endregion
	}
}
