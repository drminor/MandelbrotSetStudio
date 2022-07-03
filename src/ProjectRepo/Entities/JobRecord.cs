using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,
		bool IsPreferredChild,
		ObjectId ProjectId,
		ObjectId SubDivisionId,
		string? Label,
		int TransformType,          // TODO: Change the JobRecord's TransformType (enum) from an int to a string.

		MapAreaInfoRecord MapAreaInfoRecord,
		string TransformTypeString,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		//MSetInfoRecord MSetInfo,
		ObjectId ColorBandSetId,
		MapCalcSettings MapCalcSettings,


		//BigVectorRecord MapBlockOffset,
		//VectorIntRecord CanvasControlOffset,
		SizeIntRecord CanvasSizeInBlocks
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;

		public DateTime LastSaved { get; set; }

		//public MapCalcSettings? MapCalcSettings { get; set; }

		//public MSetInfoRecord? MSetInfo { get; set; }
		//public BigVectorRecord? MapBlockOffset { get; set; }

		//public VectorIntRecord? CanvasControlOffset { get; set; }
		//public SizeIntRecord? CanvasSize { get; set; } // TODO: Make sure every JobRecord has a value for CanvasSize

	}

	public record JobInfoRecord
	(
		DateTime DateCreated,
		int TransformType,
		ObjectId SubDivisionId,
		int MapCoordExponent
	);

}
