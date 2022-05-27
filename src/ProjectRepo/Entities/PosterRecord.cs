using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record PosterRecord(

		string Name, 
		string? Description,

		ObjectId? SourceJobId,
		JobAreaInfoRecord  JobAreaInfoRecord,
		ObjectId ColorBandSetId,
		MapCalcSettings MapCalcSettings,

		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,
		DateTime LastAccessedUtc

		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;
	}
}
