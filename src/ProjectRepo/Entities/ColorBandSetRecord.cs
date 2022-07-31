using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(
		ObjectId? ParentId, 
		ObjectId ProjectId, 
		string? Name, 
		string? Description, 
		ColorBandRecord[] ColorBandRecords
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public ReservedColorBandRecord[]? ReservedColorBandRecords { get; set; }

		public Guid ColorBandsSerialNumber { get; set; }

		public DateTime DateCreatedUtc { get; set; }
		public DateTime LastAccessed { get; set; }
	}

}
