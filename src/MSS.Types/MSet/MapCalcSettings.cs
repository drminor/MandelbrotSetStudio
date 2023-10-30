using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace MSS.Types.MSet
{
	[DataContract]
	public class MapCalcSettings : IEquatable<MapCalcSettings>, IEqualityComparer<MapCalcSettings?>, ICloneable
	{
		#region Constructor

		public MapCalcSettings() : this(0, 0, false, false)
		{ }

		public MapCalcSettings(int targetIterations, int threshold, bool calculateEscapeVelocities, bool saveTheZValues)
		{
			TargetIterations = targetIterations;
			Threshold = threshold;
			CalculateEscapeVelocities = calculateEscapeVelocities;
			SaveTheZValues = saveTheZValues;		
		}

		#endregion

		#region Public Properties

		[DataMember(Order = 1)]
		public int TargetIterations { get; set; }

		[DataMember(Order = 2)]
		public int Threshold { get; set; }

		// TODO: Remove the UseEscapeVelocities on the MapCalcSettings class.
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(false)]
		public bool UseEscapeVelocities { get; set; } 

		[DataMember(Order = 3)]
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(false)]
		public bool CalculateEscapeVelocities { get; set; }

		[DataMember(Order = 4)]
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(false)]
		public bool SaveTheZValues { get; set; }

		// TODO: Remove the RequestsPerJob Property on the MapCalcSettings class.
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0)]
		public int RequestsPerJob { get; set; } = 0;

		#endregion

		#region Static Convenience Methods

		public static MapCalcSettings UpdateTargetIterations(MapCalcSettings mcs, int targetIterations)
		{
			return new MapCalcSettings(targetIterations, mcs.Threshold, mcs.CalculateEscapeVelocities, mcs.SaveTheZValues);
		}

		public static MapCalcSettings UpdateSaveTheZValues(MapCalcSettings mcs, bool saveTheZValues)
		{
			return new MapCalcSettings(mcs.TargetIterations, mcs.Threshold, mcs.CalculateEscapeVelocities, saveTheZValues);
		}

		public static MapCalcSettings UpdateCalculateEscapeVelocities(MapCalcSettings mcs, bool calculateEscapeVelocities)
		{
			return new MapCalcSettings(mcs.TargetIterations, mcs.Threshold, calculateEscapeVelocities, mcs.SaveTheZValues);
		}

		#endregion

		#region ICloneable and ToString

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapCalcSettings Clone()
		{
			var result = new MapCalcSettings(TargetIterations, Threshold, CalculateEscapeVelocities, SaveTheZValues);
			return result;
		}

		public override string ToString()
		{
			return $"TargetIterations: {TargetIterations}, Threshold: {Threshold}, CalculateEscapeVelocities: {CalculateEscapeVelocities}, SaveTheZValues: {SaveTheZValues}.";
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
				&& CalculateEscapeVelocities == other.CalculateEscapeVelocities
				&& SaveTheZValues == other.SaveTheZValues;
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
			return HashCode.Combine(TargetIterations, Threshold, CalculateEscapeVelocities, SaveTheZValues);
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
