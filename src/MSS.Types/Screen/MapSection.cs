using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public class MapSection : IEquatable<MapSection>, IEqualityComparer<MapSection>
	{
		private readonly Lazy<IHistogram> _histogram;

		public MapSection(PointInt blockPosition, SizeInt size, ushort[] counts, ushort[] escapeVelocities, int targetIterations, string subdivisionId
			, BigVector repoBlockPosition, bool isInverted, Func<ushort[], IHistogram> histogramBuilder)
		{
			BlockPosition = blockPosition;
			Size = size;
			Counts = counts ?? throw new ArgumentNullException(nameof(counts));
			EscapeVelocities = escapeVelocities ?? throw new ArgumentNullException(nameof(escapeVelocities));
			TargetIterations = targetIterations;

			SubdivisionId = subdivisionId;
			RepoBlockPosition = repoBlockPosition;
			IsInverted = isInverted;

			_histogram = new Lazy<IHistogram>(() => histogramBuilder(Counts), System.Threading.LazyThreadSafetyMode.PublicationOnly);
		}

		public PointInt BlockPosition { get; set; }
		public SizeInt Size { get; init; }

		public ushort[] Counts { get; init; }
		public ushort[] EscapeVelocities { get; init; }
		public int TargetIterations { get; init; }

		public string SubdivisionId { get; init; }
		public BigVector RepoBlockPosition { get; init; }
		public bool IsInverted { get; init; }


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
			return other is MapSection ms
				&& SubdivisionId == ms.SubdivisionId
				&& IsInverted == ms.IsInverted
				&& EqualityComparer<BigVector>.Default.Equals(RepoBlockPosition, ms.RepoBlockPosition);
				//&& TargetIterations == ms.TargetIterations;
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


