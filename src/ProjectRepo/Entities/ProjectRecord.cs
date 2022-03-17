
using MongoDB.Bson;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, byte[][] ColorBandSetIds, ColorBandSetRecord CurrentColorBandSetRecord) : RecordBase();
}
