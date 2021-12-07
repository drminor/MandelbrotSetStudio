using MongoDB.Bson;
using MSS.Types;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		MSetInfoRecord MSetInfo,
		PointDbl CanvasOffset
		) : RecordBase()
	{ }

}
