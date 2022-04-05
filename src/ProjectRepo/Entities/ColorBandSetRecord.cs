
using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(ObjectId? ParentId, ObjectId ProjectId, string? Name, string? Description, ColorBandRecord[] ColorBandRecords) : RecordBase()
	{ }

}
