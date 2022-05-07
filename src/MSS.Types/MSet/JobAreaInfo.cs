namespace MSS.Types.MSet
{
	public class JobAreaInfo
	{
		public RRectangle Coords { get; init; }
		public Subdivision Subdivision { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }
		public SizeInt CanvasSizeInBlocks { get; init; }

		public JobAreaInfo(RRectangle coords, Subdivision subdivision, BigVector mapBlockOffset, VectorInt canvasControlOffset, SizeInt canvasSizeInBlocks)
		{
			Subdivision = subdivision;
			Coords = coords;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

	}
}
