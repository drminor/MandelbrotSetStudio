using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct PointDbl : IEquatable<PointDbl>, IEqualityComparer<PointDbl>
	{
		public PointDbl(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public PointDbl(SizeDbl size) : this(size.Width, size.Height)
		{ }

		public PointDbl(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; set; }
		public double Y { get; set; }

		//public PointDbl Scale(PointDbl factor)
		//{
		//	return new PointDbl(X * factor.X, Y * factor.Y);
		//}

		//public PointDbl Translate(PointDbl offset)
		//{
		//	return new PointDbl(X + offset.X, Y + offset.Y);
		//}

		//public PointDbl Translate(SizeDbl offset)
		//{
		//	return new PointDbl(X + offset.Width, Y + offset.Height);
		//}

		//public PointDbl Scale(SizeInt factor)
		//{
		//	return new PointDbl(X * factor.Width, Y * factor.Height);
		//}

		//public PointDbl Scale(double factor)
		//{
		//	return new PointDbl(X * factor, Y * factor);
		//}

		//public PointDbl Translate(SizeInt offset)
		//{
		//	return new PointDbl(X + offset.Width, Y + offset.Height);
		//}

		public SizeDbl Diff(PointDbl amount)
		{
			return new SizeDbl(X - amount.X, Y - amount.Y);
		}

		public PointInt Round()
		{
			return Round(MidpointRounding.ToEven);
		}

		public PointInt Round(MidpointRounding midpointRounding)
		{
			var result = new PointInt
				(
					(int)Math.Round(X, midpointRounding),
					(int)Math.Round(Y, midpointRounding)
				);

			return result;
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is PointDbl dp && Equals(dp);
		}

		public bool Equals(PointDbl other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public bool Equals(PointDbl x, PointDbl y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode([DisallowNull] PointDbl obj)
		{
			return HashCode.Combine(obj.X, obj.Y);
		}

		public static bool operator ==(PointDbl left, PointDbl right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(PointDbl left, PointDbl right)
		{
			return !(left == right);
		}

		#endregion

		public override string? ToString()
		{
			return $"x:{X}, y:{Y}";
		}
	}
}
