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
		// TODO: Define a precision property on all JobRecord.MapAreaInfoRecord instances in MongoDb 
		public int? Precision { get; set; }
	}

}
