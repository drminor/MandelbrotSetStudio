namespace MSS.Types.MSet
{
	public interface IMsrJobNew
	{
		SizeInt BlockSize { get; }
		bool CrossesYZero { get; init; }
		VectorLong JobBlockOffset { get; init; }
		string JobId { get; init; }
		JobType JobType { get; init; }
		int LimbCount { get; set; }
		MapCalcSettings MapCalcSettings { get; init; }
		int MapLoaderJobNumber { get; set; }
		MathOpCounts? MathOpCounts { get; }
		string OriginalSourceSubdivisionId { get; init; }
		OwnerType OwnerType { get; init; }
		int Precision { get; set; }
		RSize SamplePointDelta { get; }
		Subdivision Subdivision { get; init; }
	}
}