using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct PointDbl : IEquatable<PointDbl>, IEqualityComparer<PointDbl>
	{
		private static PointDbl ZeroSingleton = new PointDbl();

		public static PointDbl Zero => ZeroSingleton;

		public PointDbl(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public PointDbl(VectorDbl vectorDbl) : this(vectorDbl.X, vectorDbl.Y)
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

		public PointDbl Translate(VectorDbl offset)
		{
			return new PointDbl(X + offset.X, Y + offset.Y);
		}

		//public PointDbl Translate(SizeDbl offset)
		//{
		//	return new PointDbl(X + offset.Width, Y + offset.Height);
		//}

		//public PointDbl Scale(SizeInt factor)
		//{
		//	return new PointDbl(X * factor.Width, Y * factor.Height);
		//}

		public PointDbl Scale(double factor)
		{
			return new PointDbl(X * factor, Y * factor);
		}

		public PointDbl Invert()
		{
			return Scale(-1);
		}

		//public PointDbl Translate(SizeInt offset)
		//{
		//	return new PointDbl(X + offset.Width, Y + offset.Height);
		//}

		public VectorDbl Diff(PointDbl amount)
		{
			return new VectorDbl(X - amount.X, Y - amount.Y);
		}

		public PointDbl Min(PointDbl pointB)
		{
			return new PointDbl(Math.Min(X, pointB.X), Math.Min(Y, pointB.Y));
		}

		public PointDbl Max(PointDbl pointB)
		{
			return new PointDbl(Math.Max(X, pointB.X), Math.Max(Y, pointB.Y));
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

		public PointDbl Abs()
		{
			return new PointDbl(Math.Abs(X), Math.Abs(Y));
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

		public bool Equals(PointDbl a, PointDbl b)
		{
			return a.Equals(b);
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

		public string? ToString(string? format)
		{
			return $"x:{X.ToString(format)}, y:{Y.ToString(format)}";
		}

	}
}
