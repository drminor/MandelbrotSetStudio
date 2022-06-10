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

		[ProtoMember(1)]
		public int Width { get; set; }

		[ProtoMember(2)]
		public int Height { get; set; }

		public int NumberOfCells => Width * Height;

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

		#region Public Methods

		public SizeInt Inflate(SizeInt amount)
		{
			return new SizeInt(Width + amount.Width, Height + amount.Height);
		}

		//public SizeInt Deflate(SizeInt amount)
		//{
		//	return new SizeInt(Width - amount.Width, Height - amount.Height);
		//}

		public SizeInt Scale(SizeInt factor)
		{
			return new SizeInt(Width * factor.Width, Height * factor.Height);
		}

		public SizeInt Inflate(int amount)
		{
			return new SizeInt(Width + amount, Height + amount);
		}

		public SizeInt Scale(int factor)
		{
			return new SizeInt(Width * factor, Height * factor);
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

		public SizeInt DivInt(SizeInt dividend)
		{
			var resultH = Width / dividend.Width;
			var resultV = Height / dividend.Height;

			var result = new SizeInt(resultH, resultV);

			return result;
		}

		public SizeInt DivRem(SizeInt dividend, out SizeInt remainder)
		{
			var w = Math.DivRem(Width, dividend.Width, out var remainderW);
			var h = Math.DivRem(Height, dividend.Height, out var remainderH);

			remainder = new SizeInt(remainderW, remainderH);
			var result = new SizeInt(w, h);

			return result;
		}

		public SizeInt Mod(VectorInt dividend)
		{
			return new SizeInt(Width % dividend.X, Height % dividend.Y);
		}

		public SizeInt Mod(SizeInt dividend)
		{
			return new SizeInt(Width % dividend.Width, Height % dividend.Height);
		}

		//public SizeInt Abs()
		//{
		//	return new SizeInt(Math.Abs(Width), Math.Abs(Height));
		//}

		public SizeInt GetSquare()
		{
			var result = new SizeInt(Math.Min(Width, Height));
			return result;
		}

		public SizeInt Sub(SizeInt amount)
		{
			var result = new SizeInt(Width - amount.Width, Height - amount.Height);
			return result;
		}


		public SizeInt Sub(VectorInt amount)
		{
			var result = new SizeInt(Width - amount.X, Height - amount.Y);
			return result;
		}


		public override string? ToString()
		{
			return $"w:{Width}, h:{Height}";
		}

		#endregion

		#region IEquatable and IEqualityComparer Support

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

		#endregion
	}
}
