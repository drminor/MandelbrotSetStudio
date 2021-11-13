using qdDotNet;
using System;
using System.Collections.Generic;

namespace FGenConsole
{
	[Serializable]
	public struct KPoint : IEquatable<KPoint>, IEqualityComparer<KPoint>
	{
		public int X { get; set; }
		public int Y { get; set; }

		public KPoint(int x, int y)
		{
			X = x;
			Y = y;
		}

		public KPoint ToGlobal(KPoint amount)
		{
			KPoint result = new KPoint(X + amount.X, Y + amount.Y);
			return result;
		}

		public KPoint FromGlobal(KPoint amount)
		{
			KPoint result = new KPoint(X - amount.X, Y - amount.Y);
			return result;
		}

		public PointInt GetPointInt()
		{
			PointInt result = new PointInt(X, Y);
			return result;
		}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object obj)
		{
			return obj is KPoint && Equals((KPoint)obj);
		}

		public bool Equals(KPoint other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public override int GetHashCode()
		{
			var hashCode = 1861411795;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			return hashCode;
		}

		public bool Equals(KPoint x, KPoint y)
		{
			return (x.Equals(y));
		}

		public int GetHashCode(KPoint obj)
		{
			return obj.GetHashCode();
		}

		public override string ToString()
		{
			return $"{X}, {Y}";
		}

		public static bool operator ==(KPoint point1, KPoint point2)
		{
			return point1.Equals(point2);
		}

		public static bool operator !=(KPoint point1, KPoint point2)
		{
			return !(point1 == point2);
		}

		#endregion
	}
}
