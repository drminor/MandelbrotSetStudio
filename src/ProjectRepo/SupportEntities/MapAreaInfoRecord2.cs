namespace ProjectRepo.Entities
{
	// TODO: Rename the MapAreaInfo2Record -> MapAreaInfoRecord
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
