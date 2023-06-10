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

		public MapCalcSettings(int targetIterations) : this(targetIterations, RMapConstants.DEFAULT_THRESHOLD, useEscapeVelocities: false, saveTheZValues: false)
		{ }

		public MapCalcSettings(int targetIterations, int threshold) : this(targetIterations, threshold, useEscapeVelocities: false, saveTheZValues: false)
		{ }

		public MapCalcSettings(int targetIterations, bool useEscapeVelocities, bool saveTheZValues) : this(targetIterations, RMapConstants.DEFAULT_THRESHOLD, useEscapeVelocities, saveTheZValues)
		{ }

		public MapCalcSettings(int targetIterations, int threshold, bool useEscapeVelocities, bool saveTheZValues)
		{
			TargetIterations = targetIterations;
			Threshold = threshold;
			UseEscapeVelocities = useEscapeVelocities;
			SaveTheZValues = saveTheZValues;		
		}

		#endregion

		#region Public Properties

		[DataMember(Order = 1)]
		public int TargetIterations { get; set; }

		[DataMember(Order = 2)]
		public int Threshold { get; set; }

		[DataMember(Order = 3)]
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(false)]
		public bool UseEscapeVelocities { get; set; }

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
			return new MapCalcSettings(targetIterations, mcs.Threshold, mcs.UseEscapeVelocities, mcs.SaveTheZValues);
		}


		#endregion

		#region ICloneable and ToString

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapCalcSettings Clone()
		{
			var result = new MapCalcSettings(TargetIterations, Threshold, UseEscapeVelocities, SaveTheZValues);
			return result;
		}

		public override string ToString()
		{
			return $"TargetIterations: {TargetIterations}, Threshold: {Threshold}, UseEscapeVelocities: {UseEscapeVelocities}, SaveTheZValues: {SaveTheZValues}.";
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
				&& UseEscapeVelocities == other.UseEscapeVelocities
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
			return HashCode.Combine(TargetIterations, Threshold, UseEscapeVelocities, SaveTheZValues);
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
