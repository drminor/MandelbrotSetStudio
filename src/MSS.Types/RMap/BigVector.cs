using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class BigVector : RVector, IEquatable<BigVector>, IEqualityComparer<BigVector>, ICloneable
	{
		public BigVector() : this(0, 0)
		{ }

		public BigVector(BigInteger[] values) : this(values[0], values[1])
		{ }

		// Square from single value
		public BigVector(BigInteger extent) : this(extent, extent)
		{ }

		public BigVector(RVector rVector) : this(ConvertToBigVector(rVector).Values)
		{ }

		public BigVector(BigInteger width, BigInteger height) : base(width, height, 0)
		{ }

		public new BigInteger X => XNumerator;
		public new BigInteger Y => YNumerator;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public new BigVector Clone()
		{
			return (BigVector) base.Clone();
		}

		public BigVector Scale(SizeInt factor)
		{
			return new BigVector(X * factor.Width, Y * factor.Height);
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
			return new BigVector(X + amount.XNumerator, Y + amount.YNumerator);
		}

		public BigVector Diff(BigVector vector)
		{
			return new BigVector(X - vector.X, Y - vector.Y);
		}

		public BigVector Tranlate(PointInt factor)
		{
			return new BigVector(X + factor.X, Y + factor.Y);
		}

		public BigVector DivRem(SizeInt dividend, out SizeInt remainder)
		{
			var blocksH = BigInteger.DivRem(X, dividend.Width, out var remainderH);
			var blocksV = BigInteger.DivRem(Y, dividend.Height, out var remainderV);

			remainder = new SizeInt(remainderH, remainderV);
			var result = new BigVector(blocksH, blocksV);

			return result;
		}

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

		public override string? ToString()
		{
			var result = $"{X}, {Y}";
			return result;
		}

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
			return HashCode.Combine(XNumerator, YNumerator);
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
