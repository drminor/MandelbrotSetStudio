namespace ProjectRepo.Entities
{
	public record JobAreaInfoRecord(
		RRectangleRecord CoordsRecord,
		SizeIntRecord CanvasSize,
		SubdivisionRecord SubdivisionRecord,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset
		)
	{
	}



}
