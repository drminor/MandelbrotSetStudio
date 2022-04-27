using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		bool IsPreferredChild,
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
		VectorIntRecord CanvasControlOffset
		) : RecordBase()
	{
		public DateTime LastSaved { get; set; }
	}

	public record JobModel1
	(
		DateTime DateCreated,
		int TransformType,
		ObjectId SubDivisionId,
		int MapCoordExponent
	)
	{ }



}
