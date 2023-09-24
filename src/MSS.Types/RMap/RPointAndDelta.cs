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

		public int Precision { get; set; }  // Number of binary digits

		#region Constructors

		public RPointAndDelta() : this(0, 0, 0, 0, 0)
		{ }

		public RPointAndDelta(BigInteger[] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public RPointAndDelta(BigInteger[] posValues, BigInteger[] sizeValues, int exponent) : this(posValues[0], posValues[1], sizeValues[0], sizeValues[1], exponent)
		{ }

		public RPointAndDelta(RPoint p, RSize s) : this(p.XNumerator, p.YNumerator, s.WidthNumerator, s.HeightNumerator, s.Exponent)
		{
			if (p.Exponent != s.Exponent)
			{
				throw new ArgumentException($"Cannot create a RPointAndDelta from a RPoint with Exponent: {p.Exponent} and a RSize with Exponent: {s.Exponent}.");
			}
		}

		public RPointAndDelta(RVector r, RSize s) : this(r.XNumerator, r.YNumerator, s.WidthNumerator, s.HeightNumerator, s.Exponent)
		{
			if (r.Exponent != s.Exponent)
			{
				throw new ArgumentException($"Cannot create a RPointAndDelta from a RVector with Exponent: {r.Exponent} and a RSize with Exponent: {s.Exponent}.");
			}
		}

		public RPointAndDelta(BigInteger x1, BigInteger x2, BigInteger y1, BigInteger y2, int exponent, int? precision = null)
		{
			Values = new BigInteger[] { x1, x2, y1, y2 };
			Exponent = exponent;
			Precision = precision ?? RMapConstants.DEFAULT_PRECISION;
		}

		#endregion

		public BigInteger XNumerator
		{
			get => Values[0];
			init => Values[0] = value;
		}

		public BigInteger YNumerator
		{
			get => Values[1];
			init => Values[1] = value;
		}

		public BigInteger DeltaNumerator
		{
			get => Values[2];
			init
			{
				Values[2] = value;
				Values[3] = value;
			}
		}

		public RPoint Position => new(XNumerator, YNumerator, Exponent);
		public RSize SamplePointDelta => new(DeltaNumerator, DeltaNumerator, Exponent);

		public RPoint Center => Position;
		public RSize Width => new RSize(DeltaNumerator, DeltaNumerator, Exponent);
		public RSize Height => new RSize(DeltaNumerator, DeltaNumerator, Exponent);

		#region Public Methods

		public RPointAndDelta ScaleDelta(RValue rValue)
		{
			var deltaNumerator = DeltaNumerator * rValue.Value;
			var exponent = Exponent + rValue.Exponent;
			var precision = Math.Min(Precision, rValue.Precision);

			var xNumerator = XNumerator * BigInteger.Pow(2, -1 * rValue.Exponent);
			var yNumerator = YNumerator * BigInteger.Pow(2, -1 * rValue.Exponent);

			var result = new RPointAndDelta(xNumerator, yNumerator, deltaNumerator, deltaNumerator, exponent, precision);

			return result;
		}


		//newExponent = exponent + reductionFactor;
		//	var result = value / BigInteger.Pow(2, reductionFactor);


		#endregion

		#region ToString / ICloneable / IEqualityComparer / IEquatable Support

		public override string ToString()
		{
			return ToString(reduce: true);
		}

		public string ToString(bool reduce)
		{
			return $"Position: {Center.ToString(reduce)}, SamplePointDelta: {SamplePointDelta.ToString(reduce)}";
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
				&& XNumerator.Equals(other.XNumerator)
				&& YNumerator.Equals(other.YNumerator)
				&& DeltaNumerator.Equals(other.DeltaNumerator)
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
			return HashCode.Combine(XNumerator, YNumerator, DeltaNumerator, Exponent);
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
