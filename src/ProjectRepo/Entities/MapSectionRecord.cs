using MongoDB.Bson;
using MSS.Types.MSet;

namespace ProjectRepo.Entities
{
	/// <summary>
	/// Record used to store the data found in a MapSectionResponse
	/// </summary>
	public record MapSectionRecord(
		ObjectId SubdivisionId,
		BigVectorRecord BlockPosition,
		MapCalcSettings MapCalcSettings,
		int[] Counts
		//bool[] DoneFlags,
		//double[] ZValues
	) : RecordBase();

}
