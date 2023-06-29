using MongoDB.Bson;
using System;
using System.Text;

namespace MSS.Types.MSet
{
	// TODO: Consider creating a MapAreaInfoNoSub class that not require a registered subdivision but would contain all of the info necessary to register a subdivision.
	
	
	public class MapAreaInfo : ICloneable
	{
		private static readonly Lazy<MapAreaInfo> _lazyMapAreaInfo = new Lazy<MapAreaInfo>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly MapAreaInfo Empty = _lazyMapAreaInfo.Value;

		public RRectangle Coords { get; init; }
		public SizeDbl CanvasSize { get; init; }

		public Subdivision Subdivision { get; init; }
		public int Precision { get; init; }

		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public RPoint MapPosition => Coords.Position;
		public RSize SamplePointDelta => Subdivision.SamplePointDelta;

		public ObjectId OriginalSourceSubdivisionId { get; init; }

		public bool IsEmpty => Coords == RRectangle.Zero;

		public MapAreaInfo()
		{
			Coords = new RRectangle();
			CanvasSize = new SizeDbl();
			Subdivision = new Subdivision();
			Precision = 1;
			MapBlockOffset = new BigVector();
			CanvasControlOffset = new VectorInt();

			OriginalSourceSubdivisionId = ObjectId.Empty;
		}

		public MapAreaInfo(RRectangle coords, SizeDbl canvasSize, Subdivision subdivision, int precision, BigVector mapBlockOffset, VectorInt canvasControlOffset, ObjectId originalSourceSubdivisionId)
		{
			Coords = coords;
			CanvasSize = canvasSize;
			Subdivision = subdivision;
			MapBlockOffset = mapBlockOffset;
			Precision = precision;	
			CanvasControlOffset = canvasControlOffset;
			OriginalSourceSubdivisionId = originalSourceSubdivisionId;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapAreaInfo Clone()
		{
			return new MapAreaInfo(Coords.Clone(), CanvasSize, Subdivision.Clone(), Precision, MapBlockOffset.Clone(), CanvasControlOffset, OriginalSourceSubdivisionId);
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
