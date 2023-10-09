using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class BigVector : IEquatable<BigVector>, IEqualityComparer<BigVector>, ICloneable
	{
		private const int DEFAULT_PRECISION = 55;

		// TODO: Add Constructors to the BigVector class to support initializing the precision property.
		#region Constructors

		public BigVector() : this(0, 0)
		{ }

		public BigVector(BigInteger[] values) : this(values[0], values[1])
		{ }

		// Square from single value
		public BigVector(BigInteger extent) : this(extent, extent)
		{ }

		//public BigVector(RVector rVector) : this(ConvertToBigVector(rVector).Values)
		//{ }

		public BigVector(BigInteger x, BigInteger y)
		{
			Values = new BigInteger[] { x, y };
			Precision = DEFAULT_PRECISION;
		}

		#endregion

		#region Public Properties

		public BigInteger[] Values { get; init; }

		public int Precision { get; set; }

		public BigInteger X => Values[0];
		public BigInteger Y => Values[1];

		#endregion

		#region Public Methods

		public BigVector Scale(SizeInt factor)
		{
			return new BigVector(X * factor.Width, Y * factor.Height);
		}

		public BigVector Scale(VectorLong factor)
		{
			return new BigVector(X * factor.X, Y * factor.Y);
		}

		public BigVector Scale(BigVector factor)
		{
			return new BigVector(X * factor.X, Y * factor.Y);
		}

		//public BigVector Scale(PointInt factor)
		//{
		//	return new BigVector(X * factor.X, Y * factor.Y);
		//}

		//public new BigVector Translate(RPoint amount)
		//{
		//	return new BigVector(X + amount.XNumerator, Y + amount.YNumerator);
		//}

		public BigVector Translate(BigVector amount)
		{
			return new BigVector(X + amount.X, Y + amount.Y);
		}

		public BigVector Diff(BigVector vector)
		{
			return new BigVector(X - vector.X, Y - vector.Y);
		}

		public BigVector Translate(PointInt amount)
		{
			return new BigVector(X + amount.X, Y + amount.Y);
		}

		public BigVector Translate(VectorInt amount)
		{
			return new BigVector(X + amount.X, Y + amount.Y);
		}

		public BigVector Translate(VectorLong amount)
		{
			return new BigVector(X + amount.X, Y + amount.Y);
		}

		public BigVector DivRem(SizeInt dividend, out SizeInt remainder)
		{
			var blocksH = BigInteger.DivRem(X, dividend.Width, out var remainderH);
			var blocksV = BigInteger.DivRem(Y, dividend.Height, out var remainderV);

			remainder = new SizeInt(remainderH, remainderV);
			var result = new BigVector(blocksH, blocksV);

			return result;
		}

		public BigVector DivRem(VectorLong dividend, out VectorLong remainder)
		{
			var blocksH = BigInteger.DivRem(X, dividend.X, out var remainderH);
			var blocksV = BigInteger.DivRem(Y, dividend.Y, out var remainderV);

			remainder = new VectorLong(remainderH, remainderV);
			var result = new BigVector(blocksH, blocksV);

			return result;
		}

		public BigVector DivRem(BigVector dividend, out BigVector remainder)
		{
			var blocksH = BigInteger.DivRem(X, dividend.X, out var remainderH);
			var blocksV = BigInteger.DivRem(Y, dividend.Y, out var remainderV);

			remainder = new BigVector(remainderH, remainderV);
			var result = new BigVector(blocksH, blocksV);

			return result;
		}

		public bool IsZero()
		{
			return X == 0 && Y == 0; 
		}

		public bool TryConvertToInt(out VectorInt value)
		{
			if (Values[0] > int.MaxValue || Values[0] < int.MinValue || Values[1] > int.MaxValue || Values[1] < int.MinValue)
			{
				value = new VectorInt();
				return false;
			}
			else
			{
				value = new VectorInt((int)Values[0], (int)Values[1]);
				return true;
			}
		}

		public bool TryConvertToLong(out VectorLong value)
		{
			if (Values[0] > long.MaxValue || Values[0] < long.MinValue || Values[1] > long.MaxValue || Values[1] < long.MinValue)
			{
				value = new VectorLong();
				return false;
			}
			else
			{
				value = new VectorLong((long)Values[0], (int)Values[1]);
				return true;
			}
		}

		public (long[] x, long[] y) GetLongPairs()
		{
			var x = BigIntegerHelper.ToLongPairs(Values[0]);
			var y = BigIntegerHelper.ToLongPairs(Values[1]);

			return (x, y);
		}

		#endregion

		public static BigVector ConvertToBigVector(RVector rVector)
		{
			var vector = Reducer.Reduce(rVector);

			if (vector.Exponent > 1)
			{
				throw new InvalidOperationException($"Cannot convert the rVector: {rVector} to a BigVector. Its exponent is {rVector.Exponent}.");
			}

			var factor = BigInteger.Pow(2, -1 * rVector.Exponent);
			return new BigVector(rVector.XNumerator * factor, rVector.YNumerator * factor);
		}

		#region ToString / ICloneable Support

		public override string ToString()
		{
			var result = $"{X}, {Y}";
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public BigVector Clone()
		{
			return new BigVector(X, Y);
		}

		#endregion

		#region IEqualityComparer / IEquatable Support

		public bool Equals(BigVector? a, BigVector? b)
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
			return Equals(obj as BigVector);
		}

		public bool Equals(BigVector? other)
		{
			return !(other is null) && X.Equals(other.X) && Y.Equals(other.Y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode(BigVector obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(BigVector p1, BigVector p2)
		{
			return EqualityComparer<BigVector>.Default.Equals(p1, p2);
		}

		public static bool operator !=(BigVector p1, BigVector p2)
		{
			return !(p1 == p2);
		}

		#endregion

	}
}
