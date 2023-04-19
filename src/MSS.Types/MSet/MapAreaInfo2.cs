using System;

namespace MSS.Types.MSet
{

	/// <remarks>
	/// Same as the MapAreaInfo class, but this one records the MapCenter instead of a RRectangle
	/// </remarks>

	public class MapAreaInfo2 : ICloneable
	{
		private static readonly Lazy<MapAreaInfo2> _lazyMapAreaInfo = new Lazy<MapAreaInfo2>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly MapAreaInfo2 Empty = _lazyMapAreaInfo.Value;

		public RPoint MapCenter { get; init; }
		public Subdivision Subdivision { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public int Precision { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public bool IsEmpty { get; init; }

		public MapAreaInfo2()
		{
			MapCenter = new RPoint();
			Subdivision = new Subdivision();
			MapBlockOffset = new BigVector();
			Precision = 1;
			IsEmpty = true;
		}

		public MapAreaInfo2(RPoint mapCenter, Subdivision subdivision, BigVector mapBlockOffset, int precision, VectorInt canvasControlOffset)
		{
			if (mapCenter.Exponent != subdivision.SamplePointDelta.Exponent)
			{
				throw new ArgumentException("The MapCenter and SamplePointDelta must have the same exponent.");
			}

			MapCenter = mapCenter;
			Subdivision = subdivision;
			MapBlockOffset = mapBlockOffset;
			Precision = precision;
			CanvasControlOffset = canvasControlOffset;
			IsEmpty = false;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapAreaInfo2 Clone()
		{
			return new MapAreaInfo2(MapCenter.Clone(), Subdivision.Clone(), MapBlockOffset.Clone(), Precision, CanvasControlOffset);
		}
	}
}

