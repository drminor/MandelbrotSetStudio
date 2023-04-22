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

		public RPointAndDelta PositionAndDelta { get; init; }

		public Subdivision Subdivision { get; init; }
		public int Precision { get; init; }

		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public RPoint MapCenter => PositionAndDelta.Center;
		public RSize SamplePointDelta => PositionAndDelta.SamplePointDelta;

		public bool IsEmpty => PositionAndDelta == RPointAndDelta.Zero;

		#region Constructors

		public MapAreaInfo2()
		{
			PositionAndDelta = new RPointAndDelta();
			Subdivision = new Subdivision();
			Precision = 1;
			MapBlockOffset = new BigVector();
			CanvasControlOffset = new VectorInt();
		}

		public MapAreaInfo2(RPoint mapCenter, Subdivision subdivision, int precision, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(Combine(mapCenter, subdivision.SamplePointDelta), subdivision, precision, mapBlockOffset, canvasControlOffset)
		{ }

		public MapAreaInfo2(RPointAndDelta rPointAndDelta, Subdivision subdivision, int precision, BigVector mapBlockOffset, VectorInt canvasControlOffset)
		{
			PositionAndDelta = rPointAndDelta;
			Subdivision = subdivision;
			MapBlockOffset = mapBlockOffset;
			Precision = precision;
			CanvasControlOffset = canvasControlOffset;
		}

		private static RPointAndDelta Combine(RPoint mapCenter, RSize samplePointDelta)
		{
			if (mapCenter.Exponent != samplePointDelta.Exponent)
			{
				throw new ArgumentException("The MapCenter and SamplePointDelta must have the same exponent.");
			}

			return new RPointAndDelta(mapCenter, samplePointDelta);
		}

		#endregion

		#region ICloneable Support 

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapAreaInfo2 Clone()
		{
			return new MapAreaInfo2(PositionAndDelta.Clone(), Subdivision.Clone(), Precision, MapBlockOffset.Clone(), CanvasControlOffset);
		}

		#endregion
	}
}

