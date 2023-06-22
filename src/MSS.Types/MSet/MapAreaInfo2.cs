using System;
using System.Collections.Generic;
using System.Text;

namespace MSS.Types.MSet
{

	/// <remarks>
	/// Same as the MapAreaInfo class, but this one records the MapCenter instead of a RRectangle
	/// </remarks>

	public class MapAreaInfo2 : ICloneable, IEquatable<MapAreaInfo2?>
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

		public bool IsEmpty { get; init;}

		#region Constructors

		public MapAreaInfo2()
		{
			PositionAndDelta = new RPointAndDelta();
			Subdivision = new Subdivision();
			Precision = 1;
			MapBlockOffset = new BigVector();
			CanvasControlOffset = new VectorInt();
			IsEmpty = true;
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
			IsEmpty = false;
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
			return new MapAreaInfo2(PositionAndDelta.Clone(), Subdivision.Clone(), Precision, MapBlockOffset.Clone(), CanvasControlOffset)
			{
				IsEmpty = IsEmpty
			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Center: {MapCenter}, Delta: {SamplePointDelta.WidthNumerator} / {SamplePointDelta.Exponent} (Subdivision: Pos:{Subdivision.Position}, Delta: {Subdivision.SamplePointDelta.WidthNumerator} / {Subdivision.SamplePointDelta.Exponent}.)");
			sb.AppendLine($"MapBlockOffset: X:{MapBlockOffset.X}, Y:{MapBlockOffset.Y}");
			sb.AppendLine($"CanvasControlOffset: {CanvasControlOffset}");

			return sb.ToString();
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as MapAreaInfo2);
		}

		public bool Equals(MapAreaInfo2? other)
		{
			return other is not null &&
				IsEmpty == other.IsEmpty &&
				EqualityComparer<RPointAndDelta>.Default.Equals(PositionAndDelta, other.PositionAndDelta) &&
				EqualityComparer<Subdivision>.Default.Equals(Subdivision, other.Subdivision) &&
				EqualityComparer<BigVector>.Default.Equals(MapBlockOffset, other.MapBlockOffset) &&
				CanvasControlOffset.Equals(other.CanvasControlOffset);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(PositionAndDelta, Subdivision, MapBlockOffset, CanvasControlOffset, IsEmpty);
		}

		public static bool operator ==(MapAreaInfo2? left, MapAreaInfo2? right)
		{
			return EqualityComparer<MapAreaInfo2>.Default.Equals(left, right);
		}

		public static bool operator !=(MapAreaInfo2? left, MapAreaInfo2? right)
		{
			return !(left == right);
		}

		#endregion

		/*
	
		Center: -9589679/2^23; 1776682/2^23, Delta: 1 / -30Subdivision: Pos:0/2^0; 0/2^0, Delta: 1 / -30.
		MapBlockOffset: X:-9589679, Y:1776682
		CanvasControlOffset: x:0, y:0

		*/
	}
}

