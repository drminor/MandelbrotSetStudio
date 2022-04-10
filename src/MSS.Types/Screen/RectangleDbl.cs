using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct RectangleDbl : IEquatable<RectangleDbl>, IEqualityComparer<RectangleDbl>
	{
		public RectangleDbl(double x1, double x2, double y1, double y2)
		{
			X1 = x1;
			X2 = x2;
			Y1 = y1;
			Y2 = y2;
		}

		public RectangleDbl(PointDbl point, SizeDbl size)
		{
			X1 = point.X;
			X2 = point.X + size.Width;
			Y1 = point.Y;
			Y2 = point.Y + size.Height;
		}

		public RectangleDbl(RectangleInt rectangleInt) : this(rectangleInt.X1, rectangleInt.X2, rectangleInt.Y1, rectangleInt.Y2)
		{ }


		public double X1 { get; init; }
		public double X2 { get; init; }
		public double Y1 { get; init; }
		public double Y2 { get; init; }

		public double Width => X2 - X1;
		public double Height => Y2 - Y1;

		public PointDbl Point => new PointDbl(X1, Y1);
		public PointDbl Position => new PointDbl(X1, Y1);
		public SizeDbl Size => new SizeDbl(Width, Height);

		//public RectangleDbl Scale(PointDbl factor)
		//{
		//	RectangleDbl result = new(X1 * factor.X, X2 * factor.X, Y1 * factor.Y, Y2 * factor.Y);
		//	return result;
		//}

		//public RectangleDbl Scale(SizeDbl factor)
		//{
		//	RectangleDbl result = new(X1 * factor.Width, X2 * factor.Width, Y1 * factor.Height, Y2 * factor.Height);
		//	return result;
		//}

		//public RectangleDbl Translate(PointDbl amount)
		//{
		//	RectangleDbl result = new(X1 + amount.X, X2 + amount.X, Y1 + amount.Y, Y2 + amount.Y);
		//	return result;
		//}

		//public RectangleDbl Translate(SizeDbl amount)
		//{
		//	RectangleDbl result = new(X1 + amount.Width, X2 + amount.Width, Y1 + amount.Height, Y2 + amount.Height);
		//	return result;
		//}

		public RectangleInt Round()
		{
			var result = new RectangleInt((int)Math.Round(X1), (int)Math.Round(X2), (int)Math.Round(Y1), (int)Math.Round(Y2));
			return result;
		}

		public RectangleDbl Diff(RectangleDbl other)
		{
			var result = new RectangleDbl(X1 - other.X1, X2 - other.X2, Y1 - other.Y1, Y2 - other.Y2);

			return result;
		}

		public RectangleDbl Abs()
		{
			return new RectangleDbl(Math.Abs(X1), Math.Abs(X2), Math.Abs(Y1), Math.Abs(Y2));
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is RectangleDbl dp && Equals(dp);
		}

		public bool Equals(RectangleDbl other)
		{
			return X1.Equals(other.X1)
				&& X2.Equals(other.X2)
				&& Y1.Equals(other.Y1)
				&& Y2.Equals(other.Y2);
		}

		public bool Equals(RectangleDbl x, RectangleDbl y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Point, Size);
		}

		public int GetHashCode([DisallowNull] RectangleDbl obj)
		{
			return HashCode.Combine(obj.X1, obj.X2, obj.Y1, obj.Y2);
		}

		public static bool operator ==(RectangleDbl left, RectangleDbl right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RectangleDbl left, RectangleDbl right)
		{
			return !(left == right);
		}

		#endregion

		public override string? ToString()
		{
			return $"pos:{Position}, size:{Size}";
		}
	}
}
