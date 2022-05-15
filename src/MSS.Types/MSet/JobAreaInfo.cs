namespace MSS.Types.MSet
{
	public class JobAreaInfo
	{
		public RRectangle Coords { get; init; }
		public RSize SamplePointDelta { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }
		public SizeInt CanvasSizeInBlocks { get; init; }

		public JobAreaInfo(RRectangle coords, RSize samplePointDelta, BigVector mapBlockOffset, VectorInt canvasControlOffset, SizeInt canvasSizeInBlocks)
		{
			Coords = coords;
			SamplePointDelta = samplePointDelta;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

	}
}
