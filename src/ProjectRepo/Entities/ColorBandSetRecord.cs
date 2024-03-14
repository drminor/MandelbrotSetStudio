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
		ColorBandRecord[] ColorBandRecords
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public ObjectId ProjectId { get; set; }			// TODO: Delete the ProjectId property from the ColorBandSetRecord

		public ReservedColorBandRecord[]? ReservedColorBandRecords { get; set; }

		public Guid ColorBandsSerialNumber { get; set; } = Guid.NewGuid();

		public DateTime DateCreatedUtc { get; set; }
		public DateTime DateRecordLastSavedUtc { get; set; }
		public DateTime DateLastUsedUtc { get; set; }

		
		public DateTime LastAccessed { get; set; }		// TODO: Delete LastAccessed to LastAccessedUTC on the ColorBandRecord

		public int TargetIterations { get; set; }


		[BsonDefaultValue(false)]
		[BsonIgnoreIfDefault]
		public bool UsingPercentages { get; set; }

	}

}
