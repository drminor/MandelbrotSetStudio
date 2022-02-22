using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record MapSectionRecord(
		ObjectId SubdivisionId,
		BigVectorRecord BlockPosition,
		int[] Counts
		//bool[] DoneFlags,
		//double[] ZValues
	) : RecordBase();

}
