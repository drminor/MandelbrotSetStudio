namespace ProjectRepo.Entities
{
	public record MapAreaInfoRecord(
		RRectangleRecord CoordsRecord,
		SizeIntRecord CanvasSize,
		SubdivisionRecord SubdivisionRecord,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset
		)
	{
	}



}
