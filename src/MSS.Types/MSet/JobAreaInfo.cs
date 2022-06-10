using System;

namespace MSS.Types.MSet
{
	public class JobAreaInfo : ICloneable
	{
		private static readonly Lazy<JobAreaInfo> _lazyJobAreaInfo = new Lazy<JobAreaInfo>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly JobAreaInfo Empty = _lazyJobAreaInfo.Value;

		public RRectangle Coords { get; init; }
		public SizeInt CanvasSize { get; init; }
		public Subdivision Subdivision { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public bool IsEmpty => Subdivision.BlockSize.Width == 0;

		public JobAreaInfo()
		{
			Coords = new RRectangle();
			Subdivision = new Subdivision();
			MapBlockOffset = new BigVector();
		}

		public JobAreaInfo(RRectangle coords, SizeInt canvasSize, Subdivision subdivision, BigVector mapBlockOffset, VectorInt canvasControlOffset)
		{
			Coords = coords;
			CanvasSize = canvasSize;
			Subdivision = subdivision;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		// TODO: Implement ICloneable for Coords, Subdivision and BigVector
		public JobAreaInfo Clone()
		{
			return new JobAreaInfo(Coords, CanvasSize, Subdivision, MapBlockOffset, CanvasControlOffset);
		}
	}
}
