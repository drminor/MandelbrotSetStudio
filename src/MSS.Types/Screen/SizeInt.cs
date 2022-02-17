﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	[ProtoContract(SkipConstructor = true)]
	public struct SizeInt : IEquatable<SizeInt>, IEqualityComparer<SizeInt>
	{
		//public SizeInt(SizeDbl size) : this(size.Width, size.Height)
		//{ }

		//public SizeInt(double width, double height) : this((int)Math.Round(width), (int)Math.Round(height))
		//{ }

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
