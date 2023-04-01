using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types
{
	public class MapSection : IEquatable<MapSection>, IEqualityComparer<MapSection>//, IDisposable
	{
		//private static readonly Lazy<MapSection> _lazyMapSection = new Lazy<MapSection>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		//public static readonly MapSection Empty = _lazyMapSection.Value;

		private Lazy<IHistogram> _histogram;

		#region Constructor

		public MapSection()
			: this(
				  jobNumber: -1,
				  mapSectionVectors: null,
				  subdivisionId: string.Empty,
				  repoBlockPosition: new BigVector(),
				  isInverted: false,
				  blockPosition: new PointInt(),
				  size: new SizeInt(),
				  targetIterations: 0,
				  histogramBuilder: BuildHstFake
				  )
		{ }

		// Used when the Request was cancelled or the MapSectionsVectors was empty. 
		public MapSection(int jobNumber, 
			string subdivisionId, 
			BigVector repoBlockPosition, 
			bool isInverted, 
			PointInt blockPosition, 
			SizeInt size, 
			int targetIterations, 
			bool isCancelled)
			: this(
				  jobNumber, 
				  mapSectionVectors: null, 
				  subdivisionId, 
				  repoBlockPosition, 
				  isInverted, 
				  blockPosition, 
				  size, 
				  targetIterations, 
				  BuildHstFake)
		{
			RequestCancelled = isCancelled;
		}

		public MapSection(int jobNumber, MapSectionVectors? mapSectionVectors, string subdivisionId,
			BigVector repoBlockPosition, bool isInverted, PointInt blockPosition, SizeInt size, int targetIterations, Func<ushort[], IHistogram> histogramBuilder)
		{
			JobNumber = jobNumber;
			MapSectionVectors = mapSectionVectors;
			SubdivisionId = subdivisionId;
			RepoBlockPosition = repoBlockPosition;
			IsInverted = isInverted;
			BlockPosition = blockPosition;
			Size = size;

			TargetIterations = targetIterations;
			_histogram = new Lazy<IHistogram>(() => histogramBuilder(MapSectionVectors?.Counts ?? new ushort[0]), System.Threading.LazyThreadSafetyMode.PublicationOnly);
		}

		#endregion

		#region Public Properties

		public bool RequestCancelled { get; set; }

		public int JobNumber { get; init; }
		public MapSectionVectors? MapSectionVectors { get; set; }

		public bool IsEmpty => MapSectionVectors == null;

		public PointInt BlockPosition { get; set; }
		public SizeInt Size { get; init; }

		public int TargetIterations { get; init; }

		public bool IsInverted { get; init; }
		public string SubdivisionId { get; init; }

		// TODO: Create a new type: LongVector to hold the MapSectionBlockPosition, instead of using a pair of longs as does the BigVector

		// X,Y coordinates of this section relative to the Job's MapBlockOffset in block-size units.
		public BigVector RepoBlockPosition { get; init; }

		public IHistogram Histogram => _histogram.Value;

		public bool IsLastSection { get; set; }
		public MapSectionProcessInfo? MapSectionProcessInfo { get; set; }
		public MathOpCounts? MathOpCounts { get; set; }

		#endregion

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

		private static IHistogram BuildHstFake(ushort[] dummy)
		{
			return new HistogramALow(dummy);
		}

		#region IDisposable Support

		//private bool _disposedValue;

		//protected virtual void Dispose(bool disposing)
		//{
		//	if (!_disposedValue)
		//	{
		//		if (disposing)
		//		{
		//			// Dispose managed state (managed objects)
		//			if (MapSectionValues != null)
		//			{
		//				MapSectionValues = null;
		//			}
		//			//_histogram.Value.di = null;
		//		}

		//		// Set large fields to null
		//		_disposedValue = true;
		//	}
		//}

		//// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		//// ~MapSection()
		//// {
		////     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		////     Dispose(disposing: false);
		//// }

		//public void Dispose()
		//{
		//	// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//	Dispose(disposing: true);
		//	GC.SuppressFinalize(this);
		//}

		#endregion
	}
}


