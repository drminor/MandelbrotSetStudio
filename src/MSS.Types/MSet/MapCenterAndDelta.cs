using System;
using System.Collections.Generic;
using System.Text;

namespace MSS.Types.MSet
{
	public class MapCenterAndDelta : ICloneable, IEquatable<MapCenterAndDelta?>
	{
		private static readonly Lazy<MapCenterAndDelta> _lazyMapAreaInfo = new Lazy<MapCenterAndDelta>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly MapCenterAndDelta Empty = _lazyMapAreaInfo.Value;

		public RPointAndDelta PositionAndDelta { get; init; }

		public Subdivision Subdivision { get; init; }
		public int Precision { get; init; }

		public VectorLong MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public RPoint MapCenter => PositionAndDelta.Center;
		public RSize SamplePointDelta => PositionAndDelta.SamplePointDelta;

		public bool IsEmpty { get; init;}

		#region Constructors

		public MapCenterAndDelta()
		{
			PositionAndDelta = new RPointAndDelta();
			Subdivision = new Subdivision();
			Precision = 1;
			MapBlockOffset = new VectorLong();
			CanvasControlOffset = new VectorInt();
			IsEmpty = true;
		}

		public MapCenterAndDelta(RPoint mapCenter, Subdivision subdivision, int precision, VectorLong mapBlockOffset, VectorInt canvasControlOffset)
			: this(Combine(mapCenter, subdivision.SamplePointDelta), subdivision, precision, mapBlockOffset, canvasControlOffset)
		{ }

		public MapCenterAndDelta(RPointAndDelta rPointAndDelta, Subdivision subdivision, int precision, VectorLong mapBlockOffset, VectorInt canvasControlOffset)
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

		public MapCenterAndDelta Clone()
		{
			return new MapCenterAndDelta(PositionAndDelta.Clone(), Subdivision.Clone(), Precision, MapBlockOffset.Clone(), CanvasControlOffset)
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
			return Equals(obj as MapCenterAndDelta);
		}

		public bool Equals(MapCenterAndDelta? other)
		{
			return other is not null &&
				IsEmpty == other.IsEmpty &&
				EqualityComparer<RPointAndDelta>.Default.Equals(PositionAndDelta, other.PositionAndDelta) &&
				EqualityComparer<Subdivision>.Default.Equals(Subdivision, other.Subdivision) &&
				EqualityComparer<VectorLong>.Default.Equals(MapBlockOffset, other.MapBlockOffset) &&
				CanvasControlOffset.Equals(other.CanvasControlOffset);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(PositionAndDelta, Subdivision, MapBlockOffset, CanvasControlOffset, IsEmpty);
		}

		public static bool operator ==(MapCenterAndDelta? left, MapCenterAndDelta? right)
		{
			return EqualityComparer<MapCenterAndDelta>.Default.Equals(left, right);
		}

		public static bool operator !=(MapCenterAndDelta? left, MapCenterAndDelta? right)
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

