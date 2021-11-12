using MSS.Common;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(RPointRecord Position, SizeInt BlockSize, RSizeRecord SamplePointDelta) : RecordBase()
	{
		public SubdivisionRecord(RPointRecord position, RSizeRecord samplePointDelta) : this(position, RMapConstants.BLOCK_SIZE, samplePointDelta)
		{ }
	}
}
