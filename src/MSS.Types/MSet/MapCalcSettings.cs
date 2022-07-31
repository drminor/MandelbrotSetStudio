using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapCalcSettings : IEquatable<MapCalcSettings>, IEqualityComparer<MapCalcSettings?>, ICloneable
	{
		private static readonly int DEFAULT_THRESHOLD = 4;

		[DataMember(Order = 1)]
		public int TargetIterations { get; set; }

		[DataMember(Order = 2)]
		public int Threshold { get; set; }

		[DataMember(Order = 3)]
		public int RequestsPerJob { get; set; }

		#region Constructor

		public MapCalcSettings() : this(0, 0, 0)
		{ }

		public MapCalcSettings(int targetIterations, int requestsPerJob) : this(targetIterations, DEFAULT_THRESHOLD, requestsPerJob)
		{ }

		public MapCalcSettings(int targetIterations, int threshold, int requestsPerJob)
		{
			TargetIterations = targetIterations;
			Threshold = threshold;
			RequestsPerJob = requestsPerJob;
		}

		#endregion

		#region Public Methods

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapCalcSettings Clone()
		{
			var result = new MapCalcSettings(TargetIterations, RequestsPerJob, Threshold);
			return result;
		}

		public override string ToString()
		{
			return $"TargetIterations: {TargetIterations}, RequestsPerJob: {RequestsPerJob}, Threshold: {Threshold}";
		}

		#endregion

		#region IEquatable and IEqualityComparer Support

		public override bool Equals(object? obj)
		{
			return obj is MapCalcSettings mcs && Equals(mcs);
		}

		public bool Equals(MapCalcSettings? other)
		{
			return !(other is null)
				&& TargetIterations == other.TargetIterations
				&& Threshold == other.Threshold
				&& RequestsPerJob == other.RequestsPerJob;
		}

		public bool Equals(MapCalcSettings? x, MapCalcSettings? y)
		{
			if(x is null)
			{
				return y is null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public int GetHashCode([DisallowNull] MapCalcSettings obj)
		{
			return obj.GetHashCode();
		}

		public bool Equals(RectangleInt x, RectangleInt y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(TargetIterations, Threshold, RequestsPerJob);
		}

		public static bool operator ==(MapCalcSettings left, MapCalcSettings right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MapCalcSettings left, MapCalcSettings right)
		{
			return !(left == right);
		}

		#endregion
	}

}
