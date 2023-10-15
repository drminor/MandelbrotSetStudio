using MongoDB.Bson;
using System;
using System.Diagnostics;
using System.Text;

namespace MSS.Types.MSet
{
	// TODO: Consider creating a MapAreaInfoNoSub class that not require a registered subdivision but would contain all of the info necessary to register a subdivision.
	
	
	public class MapPositionSizeAndDelta : ICloneable
	{
		private static readonly Lazy<MapPositionSizeAndDelta> _lazyMapAreaInfo = new Lazy<MapPositionSizeAndDelta>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly MapPositionSizeAndDelta Empty = _lazyMapAreaInfo.Value;

		public RRectangle Coords { get; init; }
		public SizeDbl CanvasSize { get; init; }

		public Subdivision Subdivision { get; init; }
		public int Precision { get; init; }

		public VectorLong MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public RPoint MapPosition => Coords.Position;
		public RSize SamplePointDelta => Subdivision.SamplePointDelta;

		public ObjectId OriginalSourceSubdivisionId { get; set; }

		public bool IsEmpty => Coords == RRectangle.Zero;

		public MapPositionSizeAndDelta()
		{
			Coords = new RRectangle();
			CanvasSize = new SizeDbl();
			Subdivision = new Subdivision();
			Precision = 1;
			MapBlockOffset = new VectorLong();
			CanvasControlOffset = new VectorInt();

			OriginalSourceSubdivisionId = ObjectId.Empty;
		}

		public MapPositionSizeAndDelta(RRectangle coords, SizeDbl canvasSize, Subdivision subdivision, int precision, VectorLong mapBlockOffset, VectorInt canvasControlOffset, ObjectId originalSourceSubdivisionId)
		{
			if (originalSourceSubdivisionId == ObjectId.Empty)
			{
				Debug.WriteLine($"The originalSourceSubdivisionId is blank during MapAreaInfo construction.");
			}

			Coords = coords;
			CanvasSize = canvasSize;
			Subdivision = subdivision;
			Precision = precision;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
			OriginalSourceSubdivisionId = originalSourceSubdivisionId;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapPositionSizeAndDelta Clone()
		{
			return new MapPositionSizeAndDelta(Coords.Clone(), CanvasSize, Subdivision.Clone(), Precision, MapBlockOffset.Clone(), CanvasControlOffset, OriginalSourceSubdivisionId);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Coords: {Coords}");
			sb.AppendLine($"CanvasSize: {CanvasSize}");
			sb.AppendLine($"Subdivision: Pos:{Subdivision.Position}, Delta: {Subdivision.SamplePointDelta.WidthNumerator} / {Subdivision.SamplePointDelta.Exponent}.");
			sb.AppendLine($"MapBlockOffset: X:{MapBlockOffset.X}, Y:{MapBlockOffset.Y}");
			sb.AppendLine($"CanvasControlOffset: {CanvasControlOffset}");
			sb.AppendLine($"OriginalSubdivisionId: {OriginalSourceSubdivisionId}");


			return sb.ToString();
		}
	}
}
