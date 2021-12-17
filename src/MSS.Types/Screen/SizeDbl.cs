using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct SizeDbl : IEquatable<SizeDbl>, IEqualityComparer<SizeDbl>
	{
		public SizeDbl(double width, double height)
		{
			Width = width;
			Height = height;
		}

		public double Width { get; set; }

		public double Height { get; set; }

		public SizeDbl(PointInt pointInt) : this(pointInt.X, pointInt.Y)
		{ }

		public SizeDbl Scale(PointDbl factor)
		{
			return new SizeDbl(Width * factor.X, Height * factor.Y);
		}

		//public SizeDbl Translate(PointDbl offset)
		//{
		//	return new SizeDbl(Width + offset.X, Height + offset.Y);
		//}

		//public SizeDbl Scale(SizeInt factor)
		//{
		//	return new SizeDbl(Width * factor.Width, Height * factor.Height);
		//}

		public SizeDbl Translate(SizeInt offset)
		{
			return new SizeDbl(Width + offset.Width, Height + offset.Height);
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is SizeDbl dp && Equals(dp);
		}

		public bool Equals(SizeDbl other)
		{
			return Width == other.Width &&
				   Height == other.Height;
		}

		public bool Equals(SizeDbl width, SizeDbl height)
		{
			return width.Equals(height);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Width, Height);
		}

		public int GetHashCode([DisallowNull] SizeDbl obj)
		{
			return HashCode.Combine(obj.Width, obj.Height);
		}

		public static bool operator ==(SizeDbl left, SizeDbl right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SizeDbl left, SizeDbl right)
		{
			return !(left == right);
		}

		#endregion

		public override string? ToString()
		{
			return $"w:{Width}, h:{Height}";
		}
	}
}
