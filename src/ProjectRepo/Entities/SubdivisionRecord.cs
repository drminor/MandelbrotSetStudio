
namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(
		RPointRecord Position,
		RSizeRecord SamplePointDelta,
		int BlockWidth, 
		int BlockHeight 
	) : RecordBase();
}
