using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, ObjectId CurrentJobId, DateTime LastSavedUtc) : RecordBase()
	{
		public ObjectId CurrentColorBandSetId { get; init; } = ObjectId.Empty;
	}
}
