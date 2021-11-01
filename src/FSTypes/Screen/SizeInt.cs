using System;
using System.Collections.Generic;

namespace FSTypes
{
	[Serializable]
	public class SizeInt : IEquatable<SizeInt>
	{
		public SizeInt() : this(0, 0) { }

		public SizeInt(int width, int heigth)
		{
			Width = width;
			Height = heigth;
		}

		public int Width { get; set; }
		public int Height { get; set; }

		public int NumberOfCells => Width * Height;

		public override bool Equals(object obj)
		{
			return Equals(obj as SizeInt);
		}

		public bool Equals(SizeInt other)
		{
			return other != null &&
				   Width == other.Width &&
				   Height == other.Height;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Width, Height);
		}

		public static bool operator ==(SizeInt int1, SizeInt int2)
		{
			return EqualityComparer<SizeInt>.Default.Equals(int1, int2);
		}

		public static bool operator !=(SizeInt int1, SizeInt int2)
		{
			return !(int1 == int2);
		}
	}
}
