using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.Screen
{
	public class MapSection : IEquatable<MapSection>, IEqualityComparer<MapSection>
	{
		public PointInt BlockPosition { get; init; }
		public SizeInt Size { get; init; }
		public byte[] Pixels1d { get; init; }

		public string SubdivisionId { get; init; }
		public BigVector RepoBlockPosition { get; init; }

		public MapSection(PointInt blockPosition, SizeInt size, byte[] pixels1d, string subdivisionId, BigVector repoBlockPosition)
		{
			BlockPosition = blockPosition;
			Size = size;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));

			SubdivisionId = subdivisionId;
			RepoBlockPosition = repoBlockPosition;
		}

		public override string? ToString()
		{
			return $"MapSection: {SubdivisionId}::Pos: {RepoBlockPosition}.";
		}

		#region IEqualityComparer / IEquatable Support

		public override bool Equals(object? obj)
		{
			return obj is MapSection ms && Equals(ms);
		}

		public bool Equals(MapSection? other)
		{
			return other is MapSection ms &&
				   SubdivisionId == ms.SubdivisionId &&
				   EqualityComparer<BigVector>.Default.Equals(RepoBlockPosition, ms.RepoBlockPosition);
		}

		public bool Equals(MapSection? x, MapSection? y)
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

		public int GetHashCode([DisallowNull] MapSection obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(SubdivisionId, RepoBlockPosition);
		}

		public static bool operator ==(MapSection? left, MapSection? right)
		{
			return EqualityComparer<MapSection>.Default.Equals(left, right);
		}

		public static bool operator !=(MapSection? left, MapSection? right)
		{
			return !(left == right);
		}

		#endregion
	}
}


