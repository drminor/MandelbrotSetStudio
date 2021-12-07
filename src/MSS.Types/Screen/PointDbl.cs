using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types
{
	[DataContract]
	public struct PointDbl : IEquatable<PointDbl>, IEqualityComparer<PointDbl>
	{
		public PointDbl(double x, double y)
		{
			X = x;
			Y = y;
		}

		[DataMember(Order = 1)]
		public double X { get; set; }

		[DataMember(Order = 2)]
		public double Y { get; set; }

		public PointDbl(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public PointDbl Scale(PointDbl factor)
		{
			return new PointDbl(X * factor.X, Y * factor.Y);
		}

		public PointDbl Translate(PointDbl offset)
		{
			return new PointDbl(X + offset.X, Y + offset.Y);
		}

		public PointDbl Scale(SizeInt factor)
		{
			return new PointDbl(X * factor.Width, Y * factor.Height);
		}

		public PointDbl Translate(SizeInt offset)
		{
			return new PointDbl(X + offset.Width, Y + offset.Height);
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
