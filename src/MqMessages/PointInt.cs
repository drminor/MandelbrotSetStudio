using System;
using System.Collections.Generic;

namespace MqMessages
{
	[Serializable]
	public class PointInt : IEquatable<PointInt>
	{
		public PointInt() : this(0,0) {	}

		public PointInt(int x, int y)
		{
			X = x;
			Y = y;
		}

		public int X { get; set; }
		public int Y { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as PointInt);
		}

		public bool Equals(PointInt other)
		{
			return other != null &&
				   X == other.X &&
				   Y == other.Y;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public static bool operator ==(PointInt int1, PointInt int2)
		{
			return EqualityComparer<PointInt>.Default.Equals(int1, int2);
		}

		public static bool operator !=(PointInt int1, PointInt int2)
		{
			return !(int1 == int2);
		}
	}
}
