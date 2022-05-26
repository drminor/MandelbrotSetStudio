namespace MSS.Types.MSet
{
	public class JobAreaInfo
	{
		public RRectangle Coords { get; init; }
		public SizeInt CanvasSize { get; init; }
		public Subdivision Subdivision { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public JobAreaInfo(RRectangle coords, SizeInt canvasSize, Subdivision subdivision, BigVector mapBlockOffset, VectorInt canvasControlOffset)
		{
			Coords = coords;
			CanvasSize = canvasSize;
			Subdivision = subdivision;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}
	}

	public class JobAreaInfoOld
	{
		private static SizeInt DefaultBlockSize => new SizeInt(128);

		public RRectangle Coords { get; init; }
		public RSize SamplePointDelta { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public SizeInt CanvasSize { get; init; }
		public VectorInt CanvasControlOffset { get; init; }
		public SizeInt CanvasSizeInBlocks { get; init; }

		public SizeInt BlockSize { get; init; }

		public JobAreaInfoOld(RRectangle coords, RSize samplePointDelta, BigVector mapBlockOffset, SizeInt canvasSize, VectorInt canvasControlOffset,
			SizeInt canvasSizeInBlocks, SizeInt blockSize = default)
		{
			Coords = coords;
			SamplePointDelta = samplePointDelta;
			MapBlockOffset = mapBlockOffset;
			CanvasSize = canvasSize;
			CanvasControlOffset = canvasControlOffset;
			CanvasSizeInBlocks = canvasSizeInBlocks;

			if (blockSize.Width == default)
			{
				BlockSize = DefaultBlockSize;
			}
		}
	}
}
