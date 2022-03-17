using MSS.Types;

namespace ProjectRepo.Entities
{
	public record ColorBandSetRecord(ColorBand[] ColorBands, byte[] SerialNumber) : RecordBase()
	{ }

}
