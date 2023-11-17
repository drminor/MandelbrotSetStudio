using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
				  jobMapBlockPosition: new VectorLong(),
				  sectionBlockPosition: new VectorLong(),
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
			VectorLong jobMapBlockPosition,
			VectorLong sectionBlockOffset, 
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
				  sectionBlockOffset, 
				  isInverted, 
				  screenPosition, 
				  size, 
				  targetIterations, 
				  BuildHstFake)
		{
			RequestCancelled = isCancelled;
		}

		public MapSection(MapSectionRequest req, MapSectionVectors mapSectionVectors, bool isInverted, PointInt screenPosition, Func<ushort[], IHistogram> histogramBuilder) 
			: this(req.MapLoaderJobNumber, req.RequestNumber, mapSectionVectors, req.Subdivision.Id.ToString(), req.JobBlockOffset, 
				  req.SectionBlockOffset, isInverted, screenPosition, req.BlockSize, req.MapCalcSettings.TargetIterations, histogramBuilder)
		{ }

		private MapSection(int jobNumber, int requestNumber, MapSectionVectors? mapSectionVectors, string subdivisionId, VectorLong jobMapBlockPosition,
			VectorLong sectionBlockPosition, bool isInverted, PointInt screenPosition, SizeInt size, int targetIterations, Func<ushort[], IHistogram> histogramBuilder)
		{
			JobNumber = jobNumber;
			RequestNumber = requestNumber;
			MapSectionVectors = mapSectionVectors;
			SubdivisionId = subdivisionId;
			JobMapBlockOffset = jobMapBlockPosition;
			SectionBlockOffset = sectionBlockPosition;
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
		public VectorLong SectionBlockOffset { get; init; }

		public bool IsInverted { get; init; }

		public VectorLong JobMapBlockOffset { get; private set; }

		// X,Y coordinates of this section relative to the JobMapBlockOffset in block-size units.
		public PointInt ScreenPosition { get; private set; }

		public SizeInt Size { get; init; }
		public int TargetIterations { get; init; }

		public IHistogram Histogram => _histogram.Value;

		public bool RequestCancelled { get; set; }

		public bool IsEmpty => MapSectionVectors == null;

		//public bool IsLastSection { get; set; }
		public MapSectionProcessInfo? MapSectionProcessInfo { get; set; }
		public MathOpCounts? MathOpCounts { get; set; }

		public bool ScreenPosHasBeenUpdated { get; set; }

		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateJobMapBlockOffsetAndPos(VectorLong blockOffset, PointInt screenPos)
		{
			JobMapBlockOffset = blockOffset;
			ScreenPosition = screenPos;
			ScreenPosHasBeenUpdated = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort[]? GetOneLineFromCountsBlock(int linePtr)
		{
			if (MapSectionVectors == null)
			{
				return null;
			}
			else
			{
				var stride = MapSectionVectors.BlockSize.Width;
				var result = new ushort[stride];

				Array.Copy(MapSectionVectors.Counts, linePtr * stride, result, 0, stride);
				return result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort[]? GetOneLineFromEscapeVelocitiesBlock(int linePtr)
		{
			if (MapSectionVectors == null)
			{
				return null;
			}
			else
			{
				var stride = MapSectionVectors.BlockSize.Width;
				var result = new ushort[stride];

				Array.Copy(MapSectionVectors.EscapeVelocities, linePtr * stride, result, 0, stride);
				return result;
			}
		}


		public override string? ToString()
		{
			return IsInverted
				? $"MapSection: {ScreenPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {SectionBlockOffset} (Inverted)."
				: $"MapSection: {ScreenPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {SectionBlockOffset}.";
		}

		public string ToString(PointInt adjustedPosition)
		{
			//return IsInverted
			//	? $"MapSection: AdjBlockPos: {adjustedPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition} (Inverted)."
			//	: $"MapSection: AdjBlockPos: {adjustedPosition}, JobNumber: {JobNumber}, Subdiv: {SubdivisionId}: MapPosition: {RepoBlockPosition}.";

			return IsInverted
				? $"MapSection: AdjBlockPos: {adjustedPosition}, (J/R:{JobNumber}/{RequestNumber}, MapPosition: {SectionBlockOffset} (Inverted from {ScreenPosition})."
				: $"MapSection: AdjBlockPos: {adjustedPosition}, (J/R:{JobNumber}/{RequestNumber}, MapPosition: {SectionBlockOffset}.";

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
				&& EqualityComparer<VectorLong>.Default.Equals(SectionBlockOffset, ms.SectionBlockOffset);
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
			return HashCode.Combine(SubdivisionId, IsInverted, SectionBlockOffset);
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


