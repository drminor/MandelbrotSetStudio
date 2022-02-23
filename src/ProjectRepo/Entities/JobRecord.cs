using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		int TransformType,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		MSetInfoRecord MSetInfo,

		SizeIntRecord CanvasSizeInBlocks,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset
		) : RecordBase()
	{ }

}
