using System;
using System.Collections.Generic;

namespace MqMessages
{
	[Serializable]
	public class SizeInt : IEquatable<SizeInt>
	{
		public SizeInt() : this(0, 0) { }

		public SizeInt(int w, int h)
		{
			W = w;
			H = h;
		}

		public int W { get; set; }
		public int H { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as SizeInt);
		}

		public bool Equals(SizeInt other)
		{
			return other != null &&
				   W == other.W &&
				   H == other.H;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(W, H);
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
