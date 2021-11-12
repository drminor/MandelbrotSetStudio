using MongoDB.Bson;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record MapSectionRecord(
		ObjectId SubdivisionId,
		PointInt BlockPosition,
		RPointRecord Position
		//int[] Counts,
		//bool[] DoneFlags,
		//double[] ZValues
	) : RecordBase();

}
