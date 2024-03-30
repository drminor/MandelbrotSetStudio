namespace ProjectRepo.Entities
{
	// TODO_schema: Rename the MapAreaInfo2Record -> MapCenterAndDeltaRecord
	public record MapAreaInfo2Record(
		RPointAndDeltaRecord RPointAndDeltaRecord,
		SubdivisionRecord SubdivisionRecord,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset,
		int Precsion
		)
	{


		public static MapAreaInfo2Record CreateEmpty()
		{
			var result = new MapAreaInfo2Record(
			new RPointAndDeltaRecord(), new SubdivisionRecord(new BigVectorRecord(), new RSizeRecord(), new SizeIntRecord(0, 0)), new BigVectorRecord(), new VectorIntRecord(0, 0), 0);

			return result;
		}
	}

}
