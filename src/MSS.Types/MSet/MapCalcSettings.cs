using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapCalcSettings : IEquatable<MapCalcSettings>, IEqualityComparer<MapCalcSettings?>
	{
		public const int DEFAULT_THRESHOLD = 4;

		[DataMember(Order = 1)]
		public int TargetIterations { get; init; }

		[DataMember(Order = 2)]
		public int Threshold { get; init; }

		[DataMember(Order = 3)]
		public int IterationsPerRequest { get; init; }

		public MapCalcSettings()
		{
			TargetIterations = 0;
			Threshold = 0;
			IterationsPerRequest = 0;
		}

		public MapCalcSettings(int targetIterations, int iterationsPerRequest) : this(targetIterations, DEFAULT_THRESHOLD, iterationsPerRequest)
		{ }

		public MapCalcSettings(int targetIterations, int threshold, int iterationsPerRequest)
		{
			TargetIterations = targetIterations;
			Threshold = threshold;
			IterationsPerRequest = iterationsPerRequest;
		}


		public override string ToString()
		{
			return $"TargetIterations: {TargetIterations}";
		}

		public override bool Equals(object? obj)
		{
			return obj is MapCalcSettings mcs && Equals(mcs);
		}

		public bool Equals(MapCalcSettings? other)
		{
			return !(other is null)
				&& TargetIterations == other.TargetIterations
				&& Threshold == other.Threshold
				&& IterationsPerRequest == other.IterationsPerRequest;
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
			return HashCode.Combine(TargetIterations, Threshold, IterationsPerRequest);
		}

		public static bool operator ==(MapCalcSettings left, MapCalcSettings right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(MapCalcSettings left, MapCalcSettings right)
		{
			return !(left == right);
		}
	}

}
