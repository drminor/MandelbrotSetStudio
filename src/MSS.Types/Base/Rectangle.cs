using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MSS.Types.Base
{
	[Serializable]
	public class Rectangle<T> : IEquatable<Rectangle<T>>, IEqualityComparer<Rectangle<T>> where T: struct
	{
		[JsonIgnore]
		[BsonIgnore]
		public T[] Values;

		public Rectangle() : this(default, default, default, default)
		{ }

		public Rectangle(Point<T> leftBot, Point<T> rightTop) : this(leftBot.X, rightTop.X, leftBot.Y, rightTop.Y)
		{ }

		public Rectangle(T[] values)
		{
			Values = new T[] { values[0], values[1], values[2], values[3] };
		}

		[BsonConstructor]
		[JsonConstructor]
		public Rectangle(T x1, T x2, T y1, T y2)
		{
			Values = new T[] { x1, x2, y1, y2 };
		}

		public T X1
		{
			get => Values[0];
			init => Values[0] = value;
		}

		public T X2
		{
			get => Values[1];
			init => Values[1] = value;
		}

		public T Y1
		{
			get => Values[2];
			init => Values[2] = value;
		}

		public T Y2
		{
			get => Values[3];
			init => Values[3] = value;
		}

		public Point<T> LeftBot => new Point<T>(Values[0], Values[2]);
		public Point<T> RightTop => new Point<T>(Values[1], Values[3]);

		public override string ToString()
		{
			string result = $"X1: {Values[0]}, X2: {Values[1]}, Y1: {Values[2]}, Y2: {Values[3]}";
			return result;
		}

		#region IEqualityComparer / IEquatable Support

		public bool Equals(Rectangle<T>? a, Rectangle<T>? b)
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
			return Equals(obj as Rectangle<T>);
		}

		public bool Equals(Rectangle<T>? other)
		{
			return !(other is null)
				&& X1.Equals(other.X1)
				&& X2.Equals(other.X2)
				&& Y1.Equals(other.Y1)
				&& Y2.Equals(other.Y2);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X1, X2, Y1, Y2);
		}

		public int GetHashCode(Rectangle<T> obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(Rectangle<T> r1, Rectangle<T> r2)
		{
			return EqualityComparer<Rectangle<T>>.Default.Equals(r1, r2);
		}

		public static bool operator !=(Rectangle<T> r1, Rectangle<T> r2)
		{
			return !(r1 == r2);
		}

		#endregion
	}

}
