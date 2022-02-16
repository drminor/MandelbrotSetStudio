using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public struct SizeInt : IEquatable<SizeInt>, IEqualityComparer<SizeInt>
	{
		public SizeInt(SizeDbl size) : this(size.Width, size.Height)
		{ }

		public SizeInt(double width, double height) : this((int)Math.Round(width), (int)Math.Round(height))
		{ }

		public SizeInt(int width, int heigth)
		{
			Width = width;
			Height = heigth;
		}

		[ProtoMember(1)]
		public int Width { get; set; }

		[ProtoMember(2)]
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
