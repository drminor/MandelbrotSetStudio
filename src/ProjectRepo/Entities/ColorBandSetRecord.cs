using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(
		ObjectId? ParentId, 
		ObjectId OwnerId,
		string Name, 
		string? Description,
		Guid ColorBandsSerialNumber,
		ColorBandRecord[] ColorBandRecords,
		int TargetIterations,
		int Version
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public ReservedColorBandRecord[]? ReservedColorBandRecords { get; set; }

		public DateTime DateCreatedUtc { get; set; }
		public DateTime DateRecordLastSavedUtc { get; set; }
		public DateTime DateLastUsedUtc { get; set; }

		[BsonDefaultValue(false)]
		[BsonIgnoreIfDefault]
		public bool UsingPercentages { get; set; }
	}

}
