using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, DateTime LastSavedUtc, ObjectId CurrentJobId, ObjectId CurrentColorBandSetId) : RecordBase();
}
