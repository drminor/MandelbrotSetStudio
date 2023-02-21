﻿using MSS.Types.MSet;
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

		public MapSection(MapSectionRequest mapSectionRequest, bool isCancelled = false)
			: this(
				  jobId: -1,
				  mapSectionVectors: null,
				  subdivisionId: mapSectionRequest.SubdivisionId,
				  repoBlockPosition: mapSectionRequest.BlockPosition,
				  isInverted: false,
				  blockPosition: new PointInt(),
				  size: new SizeInt(),
				  targetIterations: 0,
				  histogramBuilder: BuildHstFake
				  )
		{
			RequestCancelled = isCancelled;
		}


		public MapSection()
			: this(
				  jobId: -1,
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

		public MapSection(int jobId, MapSectionVectors? mapSectionVectors, string subdivisionId,
			BigVector repoBlockPosition, bool isInverted, PointInt blockPosition, SizeInt size, int targetIterations, Func<ushort[], IHistogram> histogramBuilder)
		{
			JobId = jobId;
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

		public int JobId { get; init; }
		public MapSectionVectors? MapSectionVectors { get; set; }

		public bool IsEmpty => MapSectionVectors == null;

		public PointInt BlockPosition { get; set; }
		public SizeInt Size { get; init; }

		public int TargetIterations { get; init; }

		public string SubdivisionId { get; init; }
		public BigVector RepoBlockPosition { get; init; }
		public bool IsInverted { get; init; }

		public IHistogram Histogram => _histogram.Value;

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

