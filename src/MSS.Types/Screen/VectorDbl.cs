﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct VectorDbl : IEquatable<VectorDbl>, IEqualityComparer<VectorDbl>
	{
		private static VectorDbl _zeroSingleton = new VectorDbl(0, 0);

		public static VectorDbl Zero => _zeroSingleton;

		public VectorDbl(PointDbl pointDbl) : this(pointDbl.X, pointDbl.Y)
		{ }

		//public VectorDbl(SizeDbl size) : this(size.Width, size.Height)
		//{ }

		public VectorDbl(double x, double y)
		{
			X = x;
			Y = y;
		}

		public VectorDbl(VectorInt vectorInt)
		{
			X = vectorInt.X;
			Y = vectorInt.Y;
		}

		public double X { get; set; }
		public double Y { get; set; }

		public bool IsNAN()
		{
			return double.IsNaN(X) || double.IsNaN(Y);
		}

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

		public VectorDbl Scale(double factor)
		{
			return new VectorDbl(X * factor, Y * factor);
		}

		public VectorDbl Invert()
		{
			return Scale(-1);
		}


		public VectorDbl Add(SizeDbl amount)
		{
			return new VectorDbl(X + amount.Width, Y + amount.Height);
		}

		public VectorDbl Sub(SizeDbl amount)
		{
			return new VectorDbl(X - amount.Width, Y - amount.Height);
		}

		public VectorDbl Diff(VectorDbl offset)
		{
			return new VectorDbl(X - offset.X, Y - offset.Y);
		}

		public VectorDbl Divide(double dividend)
		{
			var resultH = X / dividend;
			var resultV = X / dividend;

			var result = new VectorDbl(resultH, resultV);

			return result;
		}

		//public PointDbl Translate(SizeInt offset)
		//{
		//	return new PointDbl(X + offset.Width, Y + offset.Height);
		//}

		//public SizeDbl Diff(PointDbl amount)
		//{
		//	return new SizeDbl(X - amount.X, Y - amount.Y);
		//}

		//public PointDbl Min(PointDbl pointB)
		//{
		//	return new PointDbl(Math.Min(X, pointB.X), Math.Min(Y, pointB.Y));
		//}

		//public PointDbl Max(PointDbl pointB)
		//{
		//	return new PointDbl(Math.Max(X, pointB.X), Math.Max(Y, pointB.Y));
		//}

		public VectorInt Round()
		{
			return Round(MidpointRounding.ToEven);
		}

		public VectorInt Round(MidpointRounding midpointRounding)
		{
			var result = new VectorInt
				(
					(int)Math.Round(X, midpointRounding),
					(int)Math.Round(Y, midpointRounding)
				);

			return result;
		}

		public VectorDbl Abs()
		{
			return new VectorDbl(Math.Abs(X), Math.Abs(Y));
		}

		public bool IsNearZero(double threshold = 0.1)
		{
			return Math.Abs(X) < threshold && Math.Abs(Y) < threshold;
		}

		#region ToString, IEquatable and IEqualityComparer Support

		public override string? ToString()
		{
			return $"x:{X}, y:{Y}";
		}

		public string? ToString(string? format)
		{
			return $"x:{X.ToString(format)}, y:{Y.ToString(format)}";
		}

		public override bool Equals(object? obj)
		{
			return obj is PointDbl dp && Equals(dp);
		}

		public bool Equals(VectorDbl other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public bool Equals(VectorDbl a, VectorDbl b)
		{
			return a.Equals(b);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode([DisallowNull] VectorDbl obj)
		{
			return HashCode.Combine(obj.X, obj.Y);
		}

		public static bool operator ==(VectorDbl left, VectorDbl right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(VectorDbl left, VectorDbl right)
		{
			return !(left == right);
		}

		#endregion
	}
}
