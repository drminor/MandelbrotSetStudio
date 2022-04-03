using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapCalcSettings : IEquatable<MapCalcSettings>, IEqualityComparer<MapCalcSettings?>
	{
		private static readonly int DEFAULT_THRESHOLD = 4;

		[DataMember(Order = 1)]
		public int TargetIterations { get; init; }

		[DataMember(Order = 2)]
		public int Threshold { get; init; }

		[DataMember(Order = 3)]
		public int RequestsPerJob { get; init; }

		#region Constructor

		public MapCalcSettings()
		{
			TargetIterations = 0;
			Threshold = 0;
			RequestsPerJob = 0;
		}

		public MapCalcSettings(int targetIterations, int requestsPerJob) : this(targetIterations, DEFAULT_THRESHOLD, requestsPerJob)
		{ }

		public MapCalcSettings(int targetIterations, int threshold, int requestsPerJob)
		{
			TargetIterations = targetIterations;
			Threshold = threshold;
			RequestsPerJob = requestsPerJob;
		}

		#endregion

		public override string ToString()
		{
			return $"TargetIterations: {TargetIterations}, RequestsPerJob: {RequestsPerJob}, Threshold: {Threshold}";
		}

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
