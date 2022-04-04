namespace ProjectRepo.Entities
{
	public record ProjectRecord(string Name, string? Description, ColorBandSetRecord CurrentColorBandSetRecord) : RecordBase();
}
