using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, ObjectId CurrentJobId, DateTime LastSavedUtc, ObjectId CurrentColorBandSetId) : RecordBase();
}
