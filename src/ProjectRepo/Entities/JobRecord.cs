using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		bool IsPreferredChild,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		int TransformType,			// TODO: Change the JobRecord's TransformType (enum) from an int to a string.


		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		MSetInfoRecord MSetInfo,
		ObjectId ColorBandSetId,

		SizeIntRecord CanvasSizeInBlocks,
		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;

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
