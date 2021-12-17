using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		MSetInfoRecord MSetInfo,
		int CanvasSizeInBlocksWidth,
		int CanvasSizeInBlocksHeight,
		int MapBlockOffsetWidth,
		int MapBlockOffsetHeight,
		double CanvasControlOffsetWidth,
		double CanvasControlOffsetHeight
		) : RecordBase()
	{ }

}
