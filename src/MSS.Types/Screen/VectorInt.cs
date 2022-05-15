using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public struct VectorInt : IEquatable<VectorInt>, IEqualityComparer<VectorInt>
	{
		public VectorInt(int x, int y)
		{
			X = x;
			Y = y;
		}

		public VectorInt(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public VectorInt(SizeInt sizeInt) : this(sizeInt.Width, sizeInt.Height)
		{ }

		public VectorInt(BigInteger x, BigInteger y) : this(ConvertToInt(x), ConvertToInt(y))
		{ }

		[ProtoMember(1)]
		public int X { get; set; }

		[ProtoMember(2)]
		public int Y { get; set; }

		private static int ConvertToInt(BigInteger n)
		{
			if (n < int.MaxValue && n > int.MinValue)
			{
				return (int)n;
			}
			else
			{
				throw new ArgumentException($"The BigInteger:{n} cannot be converted into an integer.");
			}
		}

		public VectorInt Add(VectorInt amount)
		{
			return new VectorInt(X + amount.X, Y + amount.Y);
		}

		public VectorInt Sub(VectorInt amount)
		{
			return new VectorInt(X - amount.X, Y - amount.Y);
		}

		public VectorInt Invert()
		{
			return new VectorInt(X * -1, Y * -1);
		}

		public VectorInt Mod(SizeInt dividend)
		{
			return new VectorInt(X % dividend.Width, Y % dividend.Height);
		}

		public VectorInt Divide(SizeInt dividend)
		{
			return new VectorInt(X / dividend.Width, Y / dividend.Height);
		}

		public bool EqualsZero => X == 0 && Y == 0;

		public override string? ToString()
		{
			return $"x:{X}, y:{Y}";
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is VectorInt vectorInt && Equals(vectorInt);
		}

		public bool Equals(VectorInt other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public bool Equals(VectorInt x, VectorInt y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode([DisallowNull] VectorInt obj)
		{
			return HashCode.Combine(obj.X, obj.Y);
		}

		public static bool operator ==(VectorInt left, VectorInt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(VectorInt left, VectorInt right)
		{
			return !(left == right);
		}

		#endregion
	}
}
