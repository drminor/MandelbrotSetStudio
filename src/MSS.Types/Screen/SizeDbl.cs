﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public struct SizeDbl : IEquatable<SizeDbl>, IEqualityComparer<SizeDbl>
	{
		private static SizeDbl _zeroSingleton = new SizeDbl(0, 0);
		private static SizeDbl _naNSingleton = new SizeDbl(double.NaN, double.NaN);

		public static SizeDbl Zero => _zeroSingleton;
		public static SizeDbl NaN => _naNSingleton;

		// Square from single value
		public SizeDbl(double extent) : this(extent, extent)
		{ }

		public SizeDbl(SizeInt size) : this(size.Width, size.Height)
		{ }

		public SizeDbl(PointDbl point) : this(point.X, point.Y)
		{ }

		public SizeDbl(VectorDbl vector) : this(vector.X, vector.Y)
		{ }

		public SizeDbl(double width, double height)
		{
			Width = width;
			Height = height;
		}

		public double Width { get; set; }
		public double Height { get; set; }

		public double AspectRatio => Height == 0 ? 1 : Width / Height;

		
		public bool IsNAN()
		{
			return double.IsNaN(Width) || double.IsNaN(Height);
		}
		
		public SizeDbl Inflate(int amount)
		{
			return new SizeDbl(Width + amount, Height + amount);
		}

		public SizeDbl Inflate(SizeInt amount)
		{
			return new SizeDbl(Width + amount.Width, Height + amount.Height);
		}

		public SizeDbl Inflate(VectorDbl amount)
		{
			return new SizeDbl(Width + amount.X, Height + amount.Y);
		}

		public SizeDbl Deflate(int amount)
		{
			return new SizeDbl(Width - amount, Height - amount);
		}

		public SizeDbl Deflate(VectorDbl amount)
		{
			return new SizeDbl(Width - amount.X, Height - amount.Y);
		}

		//public SizeDbl Scale(PointDbl factor)
		//{
		//	return new SizeDbl(Width * factor.X, Height * factor.Y);
		//}

		//public SizeDbl Scale(SizeInt factor)
		//{
		//	return new SizeDbl(Width * factor.Width, Height * factor.Height);
		//}

		public SizeDbl Scale(double factor)
		{
			return new SizeDbl(Width * factor, Height * factor);
		}

		public SizeDbl Scale(SizeDbl factor)
		{
			return new SizeDbl(Width * factor.Width, Height * factor.Height);
		}

		public SizeDbl Translate(PointDbl offset)
		{
			return new SizeDbl(Width + offset.X, Height + offset.Y);
		}

		public SizeDbl Sub(SizeDbl offset)
		{
			return new SizeDbl(Width - offset.Width, Height - offset.Height);
		}

		public SizeDbl Scale(SizeInt factor)
		{
			return new SizeDbl(Width * factor.Width, Height * factor.Height);
		}

		//public SizeDbl Translate(SizeInt offset)
		//{
		//	return new SizeDbl(Width + offset.Width, Height + offset.Height);
		//}


		public SizeDbl Divide(double dividend)
		{
			var resultH = Width / dividend;
			var resultV = Height / dividend;

			var result = new SizeDbl(resultH, resultV);

			return result;
		}

		public SizeDbl Divide(SizeDbl dividend)
		{
			var resultH = Width / dividend.Width;
			var resultV = Height / dividend.Height;

			var result = new SizeDbl(resultH, resultV);

			return result;
		}

		public SizeDbl Divide(SizeInt dividend)
		{
			var resultH = Width / dividend.Width;
			var resultV = Height / dividend.Height;

			var result = new SizeDbl(resultH, resultV);

			return result;
		}

		public SizeInt DivRem(SizeInt dividend, out SizeDbl remainder)
		{
			var w = DivRem(Width, dividend.Width, out var remainderW);
			var h = DivRem(Height, dividend.Height, out var remainderH);

			remainder = new SizeDbl(remainderW, remainderH);
			return new SizeInt(w, h);
		}

		private int DivRem(double dividend, double divisor, out double remainder)
		{
			var rat = dividend / divisor;
			var result = Math.Truncate(rat);
			remainder = rat - result;

			return (int) result;
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

		public SizeInt Ceiling()
		{
			var result = new SizeInt
				(
					(int)Math.Ceiling(Width),
					(int)Math.Ceiling(Height)
				);

			return result;
		}

		//public SizeDbl Diff(SizeInt offset)
		//{
		//	return new SizeDbl(Width - offset.Width, Height - offset.Height);
		//}

		public VectorDbl Diff(SizeDbl offset)
		{
			return new VectorDbl(Width - offset.Width, Height - offset.Height);
		}

		public SizeDbl Abs()
		{
			return new SizeDbl(Math.Abs(Width), Math.Abs(Height));
		}

		public bool IsNearZero(double threshold = 0.1)
		{
			return !IsNAN() && Math.Abs(Width) < threshold && Math.Abs(Height) < threshold;
		}

		public RectangleDbl PlaceAtCenter(SizeDbl containerSize)
		{
			var diff = containerSize.Diff(this);
			var halfDiff = diff.Scale(0.5);
			var result = new RectangleDbl(halfDiff, this);

			return result;
		}

		public SizeDbl Min(SizeDbl sizeB)
		{
			return new SizeDbl(Math.Min(Width, sizeB.Width), Math.Min(Height, sizeB.Height));
		}

		public SizeDbl Max(double amount)
		{
			return new SizeDbl(Math.Max(Width, amount), Math.Max(Height, amount));

		}

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

		public string? ToString(string? format)
		{
			return $"w:{Width.ToString(format)}, h:{Height.ToString(format)}";
		}

	}
}
