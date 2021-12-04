using MongoDB.Bson;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record MapSectionRecord(
		ObjectId SubdivisionId,
		PointInt BlockPosition,
		int[] Counts
		//bool[] DoneFlags,
		//double[] ZValues
	) : RecordBase();

}
