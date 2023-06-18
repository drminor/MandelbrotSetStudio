namespace ProjectRepo.Entities
{
	public record MapAreaInfo2Record(
		RPointAndDeltaRecord RPointAndDeltaRecord,
		SubdivisionRecord SubdivisionRecord,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset,
		int Precsion
		)
	{
	}

}
