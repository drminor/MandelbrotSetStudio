using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	public class RValue : IBigRatShape, ICloneable, IEquatable<RValue?>, IEqualityComparer<RValue>
	{
		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		//public BigInteger[] Values => new BigInteger[] { Value };

		public RValue() : this(0,0)
		{ }

		public RValue(BigInteger[] values, int exponent) : this(values[0], exponent)
		{ }

		public RValue(BigInteger value, int exponent)
		{
			Values = new BigInteger[] { value };
			Exponent = exponent;
		}

		public BigInteger Value => Values[0];

		public RValue Add(RValue rValue)
		{
			if (Exponent != rValue.Exponent)
			{
				throw new InvalidOperationException("Cannot add two RValues if there Exponents do not agree.");
			}

			return new RValue(Value + rValue.Value, Exponent);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RValue Clone()
		{
			return Reducer.Reduce(this);
		}

		public override string ToString()
		{
			var reducedVal = Reducer.Reduce(this);
			var result = BigIntegerHelper.GetDisplay(reducedVal.Value, reducedVal.Exponent);
			
			return result;
		}

		#region IEqualityComparer / IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as RValue);
		}

		public bool Equals(RValue? other)
		{
			return other != null &&
				   Value.Equals(other.Value) &&
				   Exponent == other.Exponent;
		}

		public bool Equals(RValue? x, RValue? y)
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

		public int GetHashCode([DisallowNull] RValue obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Value, Exponent);
		}

		public static bool operator ==(RValue? left, RValue? right)
		{
			return EqualityComparer<RValue>.Default.Equals(left, right);
		}

		public static bool operator !=(RValue? left, RValue? right)
		{
			return !(left == right);
		}

		public static implicit operator RValue(int value)
		{
			return new RValue(value, 0);
		}

		#endregion
	}
}
