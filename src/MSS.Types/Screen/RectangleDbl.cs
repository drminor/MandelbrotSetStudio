using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct RectangleDbl : IEquatable<RectangleDbl>, IEqualityComparer<RectangleDbl>
	{
		#region Constructors

		public RectangleDbl(PointDbl p1, PointDbl p2) : this(p1.X, p2.X, p1.Y, p2.Y)
		{ }

		public RectangleDbl(PointDbl point, SizeDbl size) : this(point.X, point.X + size.Width, point.Y, point.Y + size.Height)
		{ }

		public RectangleDbl(VectorDbl point, SizeDbl size) : this(point.X, point.X + size.Width, point.Y, point.Y + size.Height)
		{ }

		public RectangleDbl(RectangleInt rectangleInt) : this(rectangleInt.X1, rectangleInt.X2, rectangleInt.Y1, rectangleInt.Y2)
		{ }

		public RectangleDbl(double x1, double x2, double y1, double y2)
		{
			X1 = x1;
			X2 = x2;
			Y1 = y1;
			Y2 = y2;

			Validate();
		}

		#endregion

		#region Public Properties

		public double X1 { get; init; }
		public double X2 { get; init; }
		public double Y1 { get; init; }
		public double Y2 { get; init; }

		public double Width => X2 - X1;
		public double Height => Y2 - Y1;

		public PointDbl Point1 => new PointDbl(X1, Y1);
		public PointDbl Point2 => new PointDbl(X2, Y2);
		public PointDbl Position => new PointDbl(X1, Y1);
		public SizeDbl Size => new SizeDbl(Width, Height);

		#endregion

		#region Public Methods

		public PointDbl GetCenter()
		{
			// Note: Width and Height are guaranteed to be > 0.

			var vectDbl = new VectorDbl(Width / 2, Height / 2);
			return Point1.Translate(vectDbl);
		}

		public RectangleDbl Scale(double factor)
		{
			RectangleDbl result = new(X1 * factor, X2 * factor, Y1 * factor, Y2 * factor);
			return result;
		}

		//public RectangleDbl Scale(PointDbl factor)
		//{
		//	RectangleDbl result = new(X1 * factor.X, X2 * factor.X, Y1 * factor.Y, Y2 * factor.Y);
		//	return result;
		//}

		public RectangleDbl Scale(SizeDbl factor)
		{
			RectangleDbl result = new(X1 * factor.Width, X2 * factor.Width, Y1 * factor.Height, Y2 * factor.Height);
			return result;
		}

		public RectangleDbl Translate(PointDbl amount)
		{
			RectangleDbl result = new(X1 + amount.X, X2 + amount.X, Y1 + amount.Y, Y2 + amount.Y);
			return result;
		}

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

		/// <summary>
		/// Transforms the rectangle to use a new Y origin where the range of Y values is from zero to maxY.
		/// If the rectangle values increase from 0 = bottom to maxY = top, then the new rectangle has values that increase from 0 = top to maxY = bottom.
		/// If the rectangle values increase from 0 = top to maxY = bottom, then the new rectangle has values that increase from 0 = bottom to maxY = top.
		/// </summary>
		/// <param name="maxY"></param>
		/// <returns>Example: FlipY(p1:(0,0), p2:(100,100), 200) -> p1:(0,200), p2:100,100) </returns>
		public RectangleDbl FlipY(double maxY)
		{
			return new RectangleDbl(X1, X2, maxY - Y2, maxY - Y1);
		}

		public RectangleDbl MakeSafe()
		{
			if (double.IsNaN(X1) || double.IsNaN(X2) || double.IsInfinity(X1) || double.IsInfinity(X2))
			{
				return new RectangleDbl();
			}

			if (double.IsNaN(Y1) || double.IsNaN(Y2) || double.IsInfinity(Y1) || double.IsInfinity(Y2))
			{
				return new RectangleDbl();
			}

			return this;
		}

		#endregion

		#region ToString, IEquatable and IEqualityComparer Support

		public override string? ToString()
		{
			return $"pos:{Position}, size:{Size}";
		}

		public string? ToString(string? format)
		{
			return $"pos:{Position.ToString(format)}, size:{Size.ToString(format)}";
		}

		public static string FormatNully(RectangleDbl? a)
		{
			return a.HasValue ? a.Value.ToString() ?? "RectangleDbl without a custom ToString Override.": "null";
		}

		public static string FormatNully(RectangleDbl? a, string? format)
		{
			return a.HasValue ? a.Value.ToString(format) ?? "RectangleDbl without a custom ToString Override." : "null";
		}

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
			return HashCode.Combine(Position, Size);
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

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void Validate()
		{
			if (!(X2 >= X1 || double.IsNaN(X1) || double.IsNaN(X2) || double.IsInfinity(X1) || double.IsInfinity(X2)))
			{
				throw new ArgumentException($"The beginning X must be less than or equal to the ending X.");
			}

			if (!(Y2 >= Y1 || double.IsNaN(Y1) || double.IsNaN(Y2) || double.IsInfinity(Y1) || double.IsInfinity(Y2)))
			{
				throw new ArgumentException($"The beginning Y must be less than or equal to the ending Y.");
			}
		}

		#endregion
	}
}
