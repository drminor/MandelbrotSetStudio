
namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(byte[] SerialNumber, ColorBandRecord[] ColorBandRecords) : RecordBase()
	{ }

}
