using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, ObjectId CurrentColorBandSetId) : RecordBase();
}
