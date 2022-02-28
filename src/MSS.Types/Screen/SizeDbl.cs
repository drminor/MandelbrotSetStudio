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
		// Square from single value
		public SizeDbl(double extent) : this(extent, extent)
		{ }

		public SizeDbl(SizeInt size) : this(size.Width, size.Height)
		{ }

		public double Width { get; set; }
		public double Height { get; set; }

		public SizeDbl Inflate(int amount)
		{
			return new SizeDbl(Width + amount, Height + amount);
		}

		//public SizeDbl Scale(PointDbl factor)
		//{
		//	return new SizeDbl(Width * factor.X, Height * factor.Y);
		//}

		//public SizeDbl Scale(SizeInt factor)
		//{
		//	return new SizeDbl(Width * factor.Width, Height * factor.Height);
		//}

		//public SizeDbl Scale(double factor)
		//{
		//	return new SizeDbl(Width * factor, Height * factor);
		//}

		//public SizeDbl Translate(PointDbl offset)
		//{
		//	return new SizeDbl(Width + offset.X, Height + offset.Y);
		//}

		//public SizeDbl Scale(SizeInt factor)
		//{
		//	return new SizeDbl(Width * factor.Width, Height * factor.Height);
		//}

		//public SizeDbl Translate(SizeInt offset)
		//{
		//	return new SizeDbl(Width + offset.Width, Height + offset.Height);
		//}

		public SizeDbl Divide(SizeInt dividend)
		{
			var resultH = Width / dividend.Width;
			var resultV = Height / dividend.Height;

			var result = new SizeDbl(resultH, resultV);

			return result;
		}

		//public SizeDbl Mod(SizeInt dividend)
		//{
		//	return new SizeDbl(Width % dividend.Width, Height % dividend.Height);
		//}

		public SizeInt Round()
		{
			return Round(MidpointRounding.ToEven);
		}

		public SizeInt Round(MidpointRounding midpointRounding)
		{
			int w = double.IsNaN(Width) ? 0 : (int)Math.Round(Width, midpointRounding);
			int h = double.IsNaN(Height) ? 0 : (int)Math.Round(Height, midpointRounding);

			var result = new SizeInt(w, h);

			return result;
		}

		public SizeInt Truncate()
		{
			var result = new SizeInt
				(
					(int)Math.Truncate(Width),
					(int)Math.Truncate(Height)
				);

			return result;
		}

		//public SizeDbl Diff(SizeInt offset)
		//{
		//	return new SizeDbl(Width - offset.Width, Height - offset.Height);
		//}

		//public SizeDbl Diff(SizeDbl offset)
		//{
		//	return new SizeDbl(Width - offset.Width, Height - offset.Height);
		//}

		//public SizeDbl Abs()
		//{
		//	return new SizeDbl(Math.Abs(Width), Math.Abs(Height));
		//}

		//public SizeInt Ceiling()
		//{
		//	return new SizeInt((int)Math.Ceiling(Width), (int)Math.Ceiling(Height));
		//}

		//public SizeInt Floor()
		//{
		//	return new SizeInt((int)Math.Floor(Width), (int)Math.Floor(Height));
		//}

		//public SizeInt GetSign()
		//{
		//	return new SizeInt(Math.Sign(Width), Math.Sign(Height));
		//}

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
