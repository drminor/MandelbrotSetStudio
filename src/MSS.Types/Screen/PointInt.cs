using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types
{
	[DataContract]
	public struct PointInt : IEquatable<PointInt>, IEqualityComparer<PointInt>
	{
		public PointInt(int x, int y)
		{
			X = x;
			Y = y;
		}

		[DataMember(Order = 1)]
		public int X { get; set; }

		[DataMember(Order = 2)]
		public int Y { get; set; }

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is PointInt pi && Equals(pi);
		}

		public bool Equals(PointInt other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public bool Equals(PointInt x, PointInt y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode([DisallowNull] PointInt obj)
		{
			return HashCode.Combine(obj.X, obj.Y);
		}

		public static bool operator ==(PointInt left, PointInt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(PointInt left, PointInt right)
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
