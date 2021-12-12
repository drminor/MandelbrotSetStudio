using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;

namespace ProjectRepo.Entities
{
	public record MSetInfoRecord(
		RRectangleRecord CoordsRecord,
		MapCalcSettings MapCalcSettings,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss
		) : RecordBase()
	{ }

}
