using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		int TransformType,
		int NewAreaX,
		int NewAreaY,
		int NewAreaWidth,
		int NewAreaHeight,
		MSetInfoRecord MSetInfo,
		int CanvasSizeInBlocksWidth,
		int CanvasSizeInBlocksHeight,

		BigVectorRecord MapBlockOffset,

		int CanvasControlOffsetWidth,
		int CanvasControlOffsetHeight
		) : RecordBase()
	{ }

}
