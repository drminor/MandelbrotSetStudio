namespace ProjectRepo.Entities
{
	public record JobAreaInfoRecord(
		RRectangleRecord CoordsRecord,
		RSizeRecord SamplePointDelta,
		BigVectorRecord MapBlockOffset,
		SizeIntRecord CanvasSize,
		VectorIntRecord CanvasControlOffset,
		SizeIntRecord CanvasSizeInBlocks,
		SizeIntRecord BlockSize
		)
	{
	}



}
