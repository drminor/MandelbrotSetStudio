using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace MSS.Types
{
	public class RValue : IBigRatShape, ICloneable, IEquatable<RValue?>, IEqualityComparer<RValue>
	{
		#region Contructors

		public RValue() : this(0, 0, null)
		{ }

		public RValue(BigInteger[] values, int exponent) : this(values[0], exponent, null)
		{ }

		public RValue(BigInteger value, int exponent) : this(value, exponent, null)
		{ }

		public RValue(BigInteger value, int exponent, int? precision)
		{
			Values = new BigInteger[] { value };
			Exponent = exponent;
			Precision = precision ?? GetPrecision(value, exponent);
		}

		private int GetPrecision(BigInteger value, int exponent)
		{
			var precision = BigInteger.Abs(value).ToString(CultureInfo.InvariantCulture).Length;

			var be = BigInteger.Pow(2, Math.Abs(exponent));
			var ePrecision = BigInteger.Abs(be).ToString(CultureInfo.InvariantCulture).Length;

			precision = Math.Max(precision, ePrecision);
			precision += 5;

			return precision;
		}

		#endregion

		#region Public Properties

		public BigInteger[] Values { get; init; }
		public int Exponent { get; init; }

		public int Precision { get; init; }

		public BigInteger Value => Values[0];

		#endregion

		#region Public Methods

		public RValue Add(RValue rValue)
		{
			if (Exponent != rValue.Exponent)
			{
				throw new InvalidOperationException("Cannot add two RValues if their Exponents do not agree.");
			}

			return new RValue(Value + rValue.Value, Exponent);
		}

		public RValue Sub(RValue rValue)
		{
			if (Exponent != rValue.Exponent)
			{
				throw new InvalidOperationException("Cannot add two RValues if their Exponents do not agree.");
			}

			return new RValue(Value - rValue.Value, Exponent);
		}

		public static RValue Min(RValue a, RValue b)
		{
			if (a.Exponent != b.Exponent)
			{
				throw new InvalidOperationException("Cannot take the Minimum of two RValues if their Exponents do not agree.");
			}

			return a.Value <= b.Value ? a : b;
		}


		#endregion

		#region Clone and To String

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
			var result = BigIntegerHelper.GetDisplay(reducedVal);
			
			return result;
		}

		#endregion

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
