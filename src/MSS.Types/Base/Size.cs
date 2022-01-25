using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MSS.Types.Base
{
	[Serializable]
	public class Size<T> : IEquatable<Size<T>>, IEqualityComparer<Size<T>>, ICloneable where T : struct
	{
		[JsonIgnore]
		[BsonIgnore]
		public T[] Values { get; init; }

		public Size() : this(default, default)
		{ }

		public Size(T[] values) : this(values[0], values[1])
		{ }

		[BsonConstructor]
		[JsonConstructor]
		public Size(T width, T height)
		{
			Values = new T[] { width, height };
		}

		public T Width
		{
			get => Values[0];
			//init => Values[0] = value;
		}

		public T Height
		{
			get => Values[1];
			//init => Values[1] = value;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Size<T> Clone()
		{
			return new Size<T>(Values[0], Values[1]);
		}

		#region IEqualityComparer / IEquatable Support

		public bool Equals(Size<T>? a, Size<T>? b)
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
			return Equals(obj as Size<T>);
		}

		public bool Equals(Size<T>? other)
		{
			return !(other is null)
				&& Width.Equals(other.Width)
				&& Height.Equals(other.Height);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Width, Height);
		}

		public int GetHashCode(Size<T> obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(Size<T> s1, Size<T> s2)
		{
			return EqualityComparer<Size<T>>.Default.Equals(s1, s2);
		}

		public static bool operator !=(Size<T> s1, Size<T> s2)
		{
			return !(s1 == s2);
		}

		#endregion
	}
}
