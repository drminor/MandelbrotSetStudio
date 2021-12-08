using MongoDB.Bson;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record MapSectionRecord(
		ObjectId SubdivisionId,
		int BlockPositionX,
		int BlockPositionY,
		int[] Counts
		//bool[] DoneFlags,
		//double[] ZValues
	) : RecordBase();

}
