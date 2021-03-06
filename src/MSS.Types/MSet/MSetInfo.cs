
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public class MSetInfo : IEquatable<MSetInfo?>, IEqualityComparer<MSetInfo>, ICloneable
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public MSetInfo()
		{
			Coords = new RRectangle();
			MapCalcSettings = new MapCalcSettings();
		}

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords.Clone(), source.MapCalcSettings);
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations)
		{
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, source.MapCalcSettings.Threshold, source.MapCalcSettings.RequestsPerJob));
		}

		public static MSetInfo UpdateWithNewRequestsPerJob(MSetInfo source, int requestsPerJob)
		{
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(source.MapCalcSettings.TargetIterations, source.MapCalcSettings.Threshold, requestsPerJob));
		}

		public static MSetInfo UpdateWithNewThreshold(MSetInfo source, int threshold)
		{
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(source.MapCalcSettings.TargetIterations, threshold, source.MapCalcSettings.RequestsPerJob));
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MSetInfo Clone()
		{
			var result = new MSetInfo(Coords.Clone(), new MapCalcSettings(MapCalcSettings.TargetIterations, MapCalcSettings.Threshold, MapCalcSettings.RequestsPerJob));
			return result;
		}

		public override string ToString()
		{
			return $"{Coords}, {MapCalcSettings}";
		}

		#region IEquality and IEqualityComparer Implementation

		public override bool Equals(object? obj)
		{
			return Equals(obj as MSetInfo);
		}

		public bool Equals(MSetInfo? other)
		{
			return other != null
				&& Coords == other.Coords
				&& MapCalcSettings == other.MapCalcSettings;
		}

		public bool Equals(MSetInfo? x, MSetInfo? y)
		{
			if (x is null)
			{
				return y is null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public int GetHashCode([DisallowNull] MSetInfo obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Coords, MapCalcSettings);
		}

		public static bool operator ==(MSetInfo? left, MSetInfo? right)
		{
			return EqualityComparer<MSetInfo>.Default.Equals(left, right);
		}

		public static bool operator !=(MSetInfo? left, MSetInfo? right)
		{
			return !(left == right);
		}

		#endregion
	}
}
