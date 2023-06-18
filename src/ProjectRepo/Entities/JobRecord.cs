using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		ObjectId? ParentJobId,

		ObjectId OwnerId,
		JobOwnerType? JobOwnerType,

		ObjectId SubDivisionId,     // Do we really need to have a SubdivisionId field here, it is included in the MapAreaInfoRecord.
		string Label,
		int TransformType,

		MapAreaInfo2Record MapAreaInfo2Record,
		string TransformTypeString,

		PointIntRecord NewAreaPosition,
		SizeIntRecord NewAreaSize,

		ObjectId ColorBandSetId,
		MapCalcSettings MapCalcSettings,

		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public DateTime? LastSavedUtc { get; set; }
		public DateTime? LastSaved { get; set; }

		public IterationUpdateRecord[]? IterationUpdates { get; set; }
		public ColorMapUpdateRecord[]? ColorMapUpdates { get; set; }



	}

}
