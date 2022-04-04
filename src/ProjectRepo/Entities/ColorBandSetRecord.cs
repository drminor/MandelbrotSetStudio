
using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(ObjectId? ParentId, string Name, string? Description, ColorBandRecord[] ColorBandRecords) : RecordBase()
	{ }

}
