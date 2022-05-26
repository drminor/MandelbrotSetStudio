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

		BigVectorRecord MapBlockOffset,
		VectorIntRecord CanvasControlOffset,
		SizeIntRecord CanvasSizeInBlocks
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;

		public DateTime LastSaved { get; set; }

		public SizeIntRecord? CanvasSize { get; set; } // TODO: Make sure every JobRecord has a value for CanvasSize
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
