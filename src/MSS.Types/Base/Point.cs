using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace MSS.Types.Base
{
	[Serializable]
	public class Point<T> : IEquatable<Point<T>>, IEqualityComparer<Point<T>> where T: struct
	{
		[JsonIgnore]
		[BsonIgnore]
		public T[] Values;

		public Point() : this(default, default) 
		{ }

		public Point(T[] values) : this(values[0], values[1])
		{ }

		[BsonConstructor]
		[JsonConstructor]
		public Point(T x, T y)
		{
			Values = new T[] { x, y };
		}

		public T X 
		{
			get => Values[0]; 
			init => Values[0] = value;
		}

		public T Y
		{
			get => Values[1]; 
			init => Values[1] = value;
		}

		#region IEqualityComparer / IEquatable Support

		public bool Equals(Point<T>? a, Point<T>? b)
		{
			if (a is null)
			{
				return b is null;
			}
			else
			{
				return a.Equals(b);
			}
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Point<T>);
		}

		public bool Equals(Point<T>? other)
		{
			return !(other is null) && X.Equals(other.X) && Y.Equals(other.Y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}

		public int GetHashCode(Point<T> obj)
		{
			return obj.GetHashCode();
		}

		public override string? ToString()
		{
			return $"X: {X}, Y: {Y}";
		}

		public static bool operator ==(Point<T> p1, Point<T> p2)
		{
			return EqualityComparer<Point<T>>.Default.Equals(p1, p2);
		}

		public static bool operator !=(Point<T> p1, Point<T> p2)
		{
			return !(p1 == p2);
		}

		#endregion

	}
}

