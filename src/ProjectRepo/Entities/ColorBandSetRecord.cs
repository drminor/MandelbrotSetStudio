
namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(string Name, string? Description, int VersionNumber, byte[] SerialNumber, ColorBandRecord[] ColorBandRecords) : RecordBase()
	{ }

}
