using MSS.Types;
using MSS.Types.MSet;

namespace ProjectRepo.Entities
{
	public record MSetInfoRecord(
		RRectangleRecord CoordsRecord,
		MapCalcSettings MapCalcSettings,
		ColorMapEntry[] ColorMapEntries
		)/* : RecordBase()*/
	{ }

}
