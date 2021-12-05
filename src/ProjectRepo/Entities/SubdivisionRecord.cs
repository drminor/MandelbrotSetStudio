
namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(RPointRecord Position, int BlockWidth, int BlockHeight, RSizeRecord SamplePointDelta) : RecordBase();
}
