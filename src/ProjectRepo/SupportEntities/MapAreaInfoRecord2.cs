namespace ProjectRepo.Entities
{
	// TODO_schema: Rename the MapAreaInfo2Record -> MapAreaInfoRecord
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
