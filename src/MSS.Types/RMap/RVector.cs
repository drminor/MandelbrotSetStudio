﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class RVector : IBigRatShape, IEquatable<RVector>, IEqualityComparer<RVector>
	{
		public static readonly RVector Zero = new RVector();

		#region Constructors

		public RVector() : this(0, 0, 0)
		{ }

		public RVector(RPoint rPoint) : this(rPoint.Values, rPoint.Exponent)
		{ }

		public RVector(RSize rSize) : this(rSize.Values, rSize.Exponent)
		{ }

		public RVector(BigVector bigVector) : this(bigVector.Values, 0)
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

		#endregion

		#region Public Properties

		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public BigInteger XNumerator => Values[0];

		public BigInteger YNumerator => Values[1];

		public RValue X => new(XNumerator, Exponent);
		public RValue Y => new(YNumerator, Exponent);

		#endregion

		#region Public Methods

		public RVector Diff(RVector amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot take the Difference using an RVector with Exponent: {amount.Exponent}, this RVector has exponent: {Exponent}.")
				: new RVector(XNumerator - amount.XNumerator, YNumerator - amount.YNumerator, Exponent);
		}

		public BigVector Divide(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot Divide an RVector with Exponent: {Exponent} using an RSize with Exponent: {amount.Exponent}.")
				: new BigVector(XNumerator / amount.WidthNumerator, YNumerator / amount.HeightNumerator);
		}

		public RVector Scale(RSize amount)
		{
			var result = new RVector(XNumerator * amount.WidthNumerator, YNumerator * amount.HeightNumerator, Exponent + amount.Exponent);
			return result;
		}

		public bool IsZero()
		{
			return XNumerator == 0 && YNumerator == 0;
		}

		#endregion

		#region ToString and ICloneable

		public override string ToString()
		{
			return ToString(reduce: true);
		}

		public string ToString(bool reduce)
		{
			if (reduce)
			{
				return BigIntegerHelper.GetDisplay(Reducer.Reduce(this));
			}
			else
			{
				return BigIntegerHelper.GetDisplay(this);
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RVector Clone()
		{
			return Reducer.Reduce(this);
		}

		#endregion

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
			if (other is null)
			{
				return false;
			}

			var reducedOther = Reducer.Reduce(other);
			var reduced = Reducer.Reduce(this);

			return reduced.XNumerator == reducedOther.XNumerator && reduced.YNumerator == reducedOther.YNumerator;
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
