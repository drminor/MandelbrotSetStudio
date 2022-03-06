
namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(
		RSizeRecord SamplePointDelta,
		int BlockWidth, 
		int BlockHeight 
	) : RecordBase();
}
