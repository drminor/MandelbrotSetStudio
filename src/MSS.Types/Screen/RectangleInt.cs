using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{

	//: IEqualityComparer<RectangleInt>, IEquatable<RectangleInt>


	public struct RectangleInt : IEquatable<RectangleInt>, IEqualityComparer<RectangleInt>
	{
		public RectangleInt(PointInt point, SizeInt size)
		{
			Point = point;
			Size = size;
		}

		public PointInt Point { get; set; }
		public SizeInt Size { get; set; }

		public int Width => Size.Width;
		public int Height => Size.Height;

		public RectangleInt Translate(PointInt amount)
		{
			RectangleInt result = new(new PointInt(Point.X + amount.X, Point.Y + amount.Y), new SizeInt(Size.Width, Size.Height));
			return result;
		}



		public override bool Equals(object? obj)
		{
			return obj is RectangleInt @int && Equals(@int);
		}

		public bool Equals(RectangleInt other)
		{
			return Point.Equals(other.Point) &&
				   Size.Equals(other.Size);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Point, Size);
		}

		public bool Equals(RectangleInt x, RectangleInt y)
		{
			return x.Equals(y);
		}

		public int GetHashCode([DisallowNull] RectangleInt obj)
		{
			return HashCode.Combine(obj.Point, obj.Size);
		}

		public static bool operator ==(RectangleInt left, RectangleInt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RectangleInt left, RectangleInt right)
		{
			return !(left == right);
		}
	}
}
