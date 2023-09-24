using MongoDB.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	public class ObjectIdComparer : IComparer<ObjectId>, IComparer, IEqualityComparer<ObjectId>
	{
		public int Compare(object? x, object? y)
		{
			return (x is ObjectId a && y is ObjectId b)
				? Compare(a, b)
				: 0;
		}

		public int Compare(ObjectId x, ObjectId y)
		{
			return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
		}

		public bool Equals(ObjectId x, ObjectId y)
		{
			return 0 == Compare(x, y);
		}

		public int GetHashCode([DisallowNull] ObjectId obj)
		{
			return obj.GetHashCode();
		}
	}




}
