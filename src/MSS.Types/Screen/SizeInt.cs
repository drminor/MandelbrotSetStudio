using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types
{
	[DataContract]
	public struct SizeInt : IEquatable<SizeInt>, IEqualityComparer<SizeInt>
	{
		public SizeInt(int width, int heigth)
		{
			Width = width;
			Height = heigth;
		}

		[DataMember(Order = 1)]
		public int Width { get; set; }

		[DataMember(Order = 2)]
		public int Height { get; set; }

		public int NumberOfCells => Width * Height;

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
