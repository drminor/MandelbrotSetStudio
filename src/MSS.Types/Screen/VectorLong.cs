using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public class VectorLong : IEquatable<VectorLong?>, IEqualityComparer<VectorLong?>, ICloneable
	{
		private static VectorLong _zeroSingleton = new VectorLong(0 ,0);

		public static VectorLong Zero => _zeroSingleton;

		public VectorLong() : this(0, 0)
		{ }

		public VectorLong(long extent) : this(extent, extent)
		{ }

		public VectorLong(long x, long y)
		{
			X = x;
			Y = y;
		}

		public VectorLong(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public VectorLong(SizeInt sizeInt) : this(sizeInt.Width, sizeInt.Height)
		{ }

		public VectorLong(BigInteger x, BigInteger y) : this(ConvertToLong(x), ConvertToLong(y))
		{ }

		[ProtoMember(1)]
		public long X { get; set; }

		[ProtoMember(2)]
		public long Y { get; set; }

		private static long ConvertToLong(BigInteger n)
		{
			if (n < long.MaxValue && n > long.MinValue)
			{
				return (long)n;
			}
			else
			{
				throw new ArgumentException($"The BigInteger:{n} cannot be converted into a long.");
			}
		}

		public VectorLong Add(VectorLong amount)
		{
			return new VectorLong(X + amount.X, Y + amount.Y);
		}

		public VectorLong Translate(PointInt amount)
		{
			return new VectorLong(X + amount.X, Y + amount.Y);
		}

		public VectorLong Sub(VectorLong amount)
		{
			return new VectorLong(X - amount.X, Y - amount.Y);
		}

		public VectorLong Invert()
		{
			return new VectorLong(X * -1, Y * -1);
		}

		public VectorLong Mod(SizeInt dividend)
		{
			return new VectorLong(X % dividend.Width, Y % dividend.Height);
		}

		public VectorLong Divide(SizeInt dividend)
		{
			return new VectorLong(X / dividend.Width, Y / dividend.Height);
		}

		public VectorLong Scale(double factor)
		{
			return new VectorLong((int)Math.Round(X * factor), (int)Math.Round(Y * factor));
		}

		public RVector Scale(RSize factor)
		{
			var result = factor.Scale(this);
			return new RVector(result);
		}

		public bool EqualsZero => X == 0 && Y == 0;

		public bool TryConvertToInt(out VectorInt value)
		{
			if (X > int.MaxValue || X < int.MinValue || Y > int.MaxValue || Y < int.MinValue)
			{
				value = new VectorInt();
				return false;
			}
			else
			{
				value = new VectorInt((int)X, (int)Y);
				return true;
			}
		}

		public override string? ToString()
		{
			return $"x:{X}, y:{Y}";
		}

		public VectorLong Clone()
		{
			var result = new VectorLong(X, Y);
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is VectorLong other && Equals(other);
		}

		public bool Equals(VectorLong? other)
		{
			return other is not null &&
				X == other.X &&
				Y == other.Y;
		}

		public bool Equals(VectorLong? x, VectorLong? y)
		{
			if (x == null)
			{
				return y == null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode([DisallowNull] VectorLong? obj)
		{
			return HashCode.Combine(obj.X, obj.Y);
		}

		public static bool operator ==(VectorLong? left, VectorLong? right)
		{
			return EqualityComparer<VectorLong>.Default.Equals(left, right);
		}

		public static bool operator !=(VectorLong? left, VectorLong? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
