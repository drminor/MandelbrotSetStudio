
namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(ColorBandRecord[] ColorBandRecords, byte[] SerialNumber) : RecordBase()
	{ }

}
