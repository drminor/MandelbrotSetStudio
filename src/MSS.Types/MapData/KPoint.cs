using System;
using System.Collections.Generic;

namespace MSS.Types
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

		/// <summary>
		/// Take this instance (which should be a local KPoint) and creates a global KPoint.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public KPoint ToGlobal(KPoint amount)
		{
			KPoint result = new(X + amount.X, Y + amount.Y);
			return result;
		}

		/// <summary>
		/// Takes this instance (which should be global KPoint) and creates a local KPoint. 
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public KPoint FromGlobal(KPoint amount)
		{
			KPoint result = new(X - amount.X, Y - amount.Y);
			return result;
		}

		//public PointInt GetPointInt()
		//{
		//	PointInt result = new PointInt(X, Y);
		//	return result;
		//}

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object obj)
		{
			return obj is KPoint point && Equals(point);
		}

		public bool Equals(KPoint other)
		{
			return X == other.X &&
				   Y == other.Y;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
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
