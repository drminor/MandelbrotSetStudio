using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct SizeInt : IEquatable<SizeInt>, IEqualityComparer<SizeInt>
	{
		public SizeInt(int width, int heigth)
		{
			Width = width;
			Height = heigth;
		}

		public int Width { get; set; }

		public int Height { get; set; }

		public int NumberOfCells => Width * Height;

		public SizeInt Scale(double factor)
		{
			return new SizeInt((int)Math.Round(Width * factor), (int)Math.Round(Height * factor));
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
