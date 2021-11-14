using MSS.Common;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record SubdivisionRecord(RPointRecord Position, int BlockWidth, int BlockHeight, RSizeRecord SamplePointDelta) : RecordBase()
	{
		public SubdivisionRecord(RPointRecord position, RSizeRecord samplePointDelta) : this(position, RMapConstants.BLOCK_SIZE.Width, RMapConstants.BLOCK_SIZE.Height, samplePointDelta)
		{ }
	}
}
