
namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(
		RSizeRecord SamplePointDelta,
		SizeIntRecord BlockSize
	) : RecordBase();
}
