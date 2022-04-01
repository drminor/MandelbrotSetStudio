using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSS.Types
{
	public class MapSection : IEquatable<MapSection>, IEqualityComparer<MapSection>
	{
		private Lazy<IHistogram> _histogram;

		public PointInt BlockPosition { get; set; }
		public SizeInt Size { get; init; }

		public int[] Counts { get; set; }

		public string SubdivisionId { get; init; }
		public BigVector RepoBlockPosition { get; init; }
		public bool IsInverted { get; init; }

		public MapSection(PointInt blockPosition, SizeInt size, int[] counts, string subdivisionId, BigVector repoBlockPosition, bool isInverted)
		{
			BlockPosition = blockPosition;
			Size = size;
			Counts = counts ?? throw new ArgumentNullException(nameof(counts));

			SubdivisionId = subdivisionId;
			RepoBlockPosition = repoBlockPosition;
			IsInverted = isInverted;

			_histogram = new Lazy<IHistogram>(() => new HistogramALow(counts.Select(x => (int)Math.Round(x / 1000d))), System.Threading.LazyThreadSafetyMode.PublicationOnly);
		}

		public IHistogram Histogram => _histogram.Value;

		public override string? ToString()
		{
			return IsInverted
				? $"MapSection:{SubdivisionId}:Pos:{RepoBlockPosition} (Inverted)."
				: $"MapSection:{SubdivisionId}:Pos:{RepoBlockPosition}.";
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
				   IsInverted == ms.IsInverted &&
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
			return HashCode.Combine(SubdivisionId, IsInverted, RepoBlockPosition);
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


