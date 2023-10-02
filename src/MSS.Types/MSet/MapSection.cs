using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
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
				  requestNumber: -1,
				  mapSectionVectors: null,
				  subdivisionId: string.Empty,
				  jobMapBlockPosition: new BigVector(),
				  repoBlockPosition: new MapBlockOffset(),
				  isInverted: false,
				  screenPosition: new PointInt(),
				  size: new SizeInt(),
				  targetIterations: 0,
				  histogramBuilder: BuildHstFake
				  )
		{ }

		// Used when the Request was cancelled or the MapSectionsVectors was empty. 
		public MapSection(int jobNumber,
			int requestNumber,
			string subdivisionId,
			BigVector jobMapBlockPosition,
			MapBlockOffset repoBlockPosition, 
			bool isInverted, 
			PointInt screenPosition, 
			SizeInt size, 
			int targetIterations, 
			bool isCancelled)
			: this(
				  jobNumber,
				  requestNumber,
				  mapSectionVectors: null, 
				  subdivisionId,
				  jobMapBlockPosition,
				  repoBlockPosition, 
				  isInverted, 
				  screenPosition, 
				  size, 
				  targetIterations, 
				  BuildHstFake)
		{
			RequestCancelled = isCancelled;
		}

		public MapSection(int jobNumber, int requestNumber, MapSectionVectors? mapSectionVectors, string subdivisionId, BigVector jobMapBlockPosition,
			MapBlockOffset repoBlockPosition, bool isInverted, PointInt screenPosition, SizeInt size, int targetIterations, Func<ushort[], IHistogram> histogramBuilder)
		{
			JobNumber = jobNumber;
			RequestNumber = requestNumber;
			MapSectionVectors = mapSectionVectors;
			SubdivisionId = subdivisionId;
			JobMapBlockOffset = jobMapBlockPosition;
			RepoBlockPosition = repoBlockPosition;
			IsInverted = isInverted;
			ScreenPosition = screenPosition;
			Size = size;

			TargetIterations = targetIterations;
			_histogram = new Lazy<IHistogram>(() => histogramBuilder(MapSectionVectors?.Counts ?? new ushort[0]), System.Threading.LazyThreadSafetyMode.PublicationOnly);

			ScreenPosHasBeenUpdated = false;
		}

		#endregion

		#region Public Properties

		public int JobNumber { get; set; }
		public int RequestNumber { get; set; }
		public MapSectionVectors? MapSectionVectors { get; set; }

		public string SubdivisionId { get; init; }

		// TODO: Rename property RepoBlockPosition in class MapSection: SectionBlockOffset
		// X,Y coordinates of this section, relative to the Subdivision's Base Map Position in block-size units.
		public MapBlockOffset RepoBlockPosition { get; init; }

		public bool IsInverted { get; init; }

		public BigVector JobMapBlockOffset { get; private set; }

		// X,Y coordinates of this section relative to the JobMapBlockOffset in block-size units.
		public PointInt ScreenPosition { get; private set; }

		public SizeInt Size { get; init; }
		public int TargetIterations { get; init; }

		public IHistogram Histogram => _histogram.Value;

		public bool RequestCancelled { get; set; }
		public bool IsEmpty => MapSectionVectors == null;

		public bool IsLastSection { get; set; }
		public MapSectionProcessInfo? MapSectionProcessInfo { get; set; }
		public MathOpCounts? MathOpCounts { get; set; }

		public bool ScreenPosHasBeenUpdated { get; set; }

		#endregion

		public void UpdateJobMapBlockOffsetAndPos(BigVector blockOffset, PointInt screenPos)
		{
			JobMapBlockOffset = blockOffset;
			ScreenPosition = screenPos;
			ScreenPosHasBeenUpdated = true;
		}

		public override string? ToString()
		{
			return IsInverted
				? $"MapSection: {ScreenPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition} (Inverted)."
				: $"MapSection: {ScreenPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition}.";
		}

		public string ToString(PointInt adjustedPosition)
		{
			return IsInverted
				? $"MapSection: AdjBlockPos: {adjustedPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition} (Inverted)."
				: $"MapSection: AdjBlockPos: {adjustedPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition}.";
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
				&& EqualityComparer<MapBlockOffset>.Default.Equals(RepoBlockPosition, ms.RepoBlockPosition);
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


