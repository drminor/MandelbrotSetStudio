using MongoDB.Bson;
using System;

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
		ObjectId ColorBandSetId,

		SizeIntRecord CanvasSizeInBlocks,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset,
		DateTime LastSaved
		) : RecordBase()
	{ }

	public record JobModel1
	(
		DateTime DateCreated,
		int TransformType,
		ObjectId SubDivisionId,
		int MapCoordExponent
	)
	{ }

}
