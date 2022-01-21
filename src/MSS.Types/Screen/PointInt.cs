using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public struct PointInt : IEquatable<PointInt>, IEqualityComparer<PointInt>
	{
		public PointInt(int x, int y)
		{
			X = x;
			Y = y;
		}

		[ProtoMember(1)]
		public int X { get; set; }

		[ProtoMember(2)]
		public int Y { get; set; }

		public PointInt Scale(SizeInt factor)
		{
			return new PointInt(X * factor.Width, Y * factor.Height);
		}

		public PointInt Translate(SizeInt amount)
		{
			return new PointInt(X + amount.Width, Y + amount.Height);
		}

		public PointInt Diff(SizeInt amount)
		{
			return new PointInt(X - amount.Width, Y - amount.Height);
		}

		public PointInt Abs()
		{
			return new PointInt(Math.Abs(X), Math.Abs(Y));
		}

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
