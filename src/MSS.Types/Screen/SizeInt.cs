using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public struct SizeInt : IEquatable<SizeInt>, IEqualityComparer<SizeInt>
	{
		// Square from single value
		public SizeInt(int extent) : this(extent, extent)
		{ }

		public SizeInt(BigInteger width, BigInteger height) : this(ConvertToInt(width), ConvertToInt(height))
		{ }

		public SizeInt(int width, int height)
		{
			Width = width;
			Height = height;
		}

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

		[ProtoMember(1)]
		public int Width { get; set; }

		[ProtoMember(2)]
		public int Height { get; set; }

		public int NumberOfCells => Width * Height;

		public SizeInt Translate(SizeInt amount)
		{
			return new SizeInt(Width + amount.Width, Height + amount.Height);
		}

		public SizeInt Diff(SizeInt amount)
		{
			return new SizeInt(Width - amount.Width, Height - amount.Height);
		}

		public SizeInt Scale(SizeInt factor)
		{
			return new SizeInt(Width * factor.Width, Height * factor.Height);
		}

		public SizeInt Scale(double factor)
		{
			return new SizeInt((int)Math.Round(Width * factor), (int)Math.Round(Height * factor));
		}

		public SizeInt Scale(int factor)
		{
			return new SizeInt(Width * factor, Height * factor);
		}

		public SizeDbl Divide(SizeInt dividend)
		{
			var resultH = Width / (double)dividend.Width;
			var resultV = Height / (double)dividend.Height;

			var result = new SizeDbl(resultH, resultV);

			return result;
		}

		public SizeInt DivRem(SizeInt dividend, out SizeInt remainder)
		{
			var blocksH = Math.DivRem(Width, dividend.Width, out var remainderH);
			var blocksV = Math.DivRem(Height, dividend.Height, out var remainderV);

			remainder = new SizeInt(remainderH, remainderV);
			var result = new SizeInt(blocksH, blocksV);

			return result;
		}

		public SizeInt Mod(SizeInt dividend)
		{
			return new SizeInt(Width % dividend.Width, Height % dividend.Height);
		}

		public SizeInt Abs()
		{
			return new SizeInt(Math.Abs(Width), Math.Abs(Height));
		}

		public SizeInt GetSquare()
		{
			var result = new SizeInt(Math.Min(Width, Height));
			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is SizeInt si && Equals(si);
		}

		public bool Equals(SizeInt other)
		{
			return Width == other.Width &&
				   Height == other.Height;
		}

		public bool Equals(SizeInt x, SizeInt y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Width, Height);
		}

		public int GetHashCode([DisallowNull] SizeInt obj)
		{
			return HashCode.Combine(obj.Width, obj.Height);
		}

		public override string? ToString()
		{
			return $"w:{Width}, h:{Height}";
		}

		public static bool operator ==(SizeInt left, SizeInt right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SizeInt left, SizeInt right)
		{
			return !(left == right);
		}


	}
}
