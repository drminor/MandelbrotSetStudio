﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct RectangleInt : IEquatable<RectangleInt>, IEqualityComparer<RectangleInt>
	{
		public RectangleInt(PointInt point, SizeInt size) : this(point.X, point.X + size.Width, point.Y, point.Y + size.Height)
		{ }

		public RectangleInt(int[] vals) : this(vals[0], vals[1], vals[2], vals[3])
		{ }

		public RectangleInt(int x1, int x2, int y1, int y2)
		{
			X1 = x1;
			X2 = x2;
			Y1 = y1;
			Y2 = y2;

			Validate();
		}

		public int X1 { get; init; }
		public int X2 { get; init; }
		public int Y1 { get; init; }
		public int Y2 { get; init; }

		public int Width => X2 - X1;
		public int Height => Y2 - Y1;

		public PointInt Point => new PointInt(X1, Y1);
		public PointInt Position => new PointInt(X1, Y1);
		public SizeInt Size => new SizeInt(Width, Height);

		//public RectangleInt Scale(PointInt factor)
		//{
		//	RectangleInt result = new(X1 * factor.X, X2 * factor.X, Y1 * factor.Y, Y2 * factor.Y);
		//	return result;
		//}

		//public RectangleInt Scale(SizeInt factor)
		//{
		//	RectangleInt result = new(X1 * factor.Width, X2 * factor.Width, Y1 * factor.Height, Y2 * factor.Height);
		//	return result;
		//}

		public RectangleInt Translate(VectorInt amount)
		{
			RectangleInt result = new(X1 + amount.X, X2 + amount.X, Y1 + amount.Y, Y2 + amount.Y);
			return result;
		}

		//public RectangleInt Translate(SizeInt amount)
		//{
		//	RectangleInt result = new(X1 + amount.Width, X2 + amount.Width, Y1 + amount.Height, Y2 + amount.Height);
		//	return result;
		//}

		public RectangleInt Expand(SizeInt amount)
		{
			var newPos = new PointInt(Position.Sub(new VectorInt(amount.Width / 2, amount.Height / 2)));
			var newSize = Size.Inflate(amount);
			var result = new RectangleInt(newPos, newSize);

			return result;
		}

		public VectorInt GetCenter()
		{
			var half = new VectorInt(Width / 2, Height / 2);
			var center = Point.Translate(half);
			return new VectorInt(center);
		}

		public override string? ToString()
		{
			return $"pos:{Position}, size:{Size}";
		}

		[Conditional("DEBUG2")]
		private void Validate()
		{
			if (X1 > X2)
			{
				throw new ArgumentException($"The beginning X must be less than or equal to the ending X.");
			}

			if (Y1 > Y2)
			{
				throw new ArgumentException($"The beginning Y must be less than or equal to the ending Y.");
			}
		}

		#region IEquality and IEqualityComparer Implementation

		public override bool Equals(object? obj)
		{
			return obj is RectangleInt r && Equals(r);
		}

		public bool Equals(RectangleInt other)
		{
			return X1.Equals(other.X1)
				&& X2.Equals(other.X2)
				&& Y1.Equals(other.Y1)
				&& Y2.Equals(other.Y2);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X1, X2, Y1, Y2);
		}

		public bool Equals(RectangleInt x, RectangleInt y)
		{
			return x.Equals(y);
		}

		public int GetHashCode([DisallowNull] RectangleInt obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(RectangleInt left, RectangleInt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RectangleInt left, RectangleInt right)
		{
			return !(left == right);
		}

		#endregion
	}
}
