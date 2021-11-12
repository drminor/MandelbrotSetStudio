using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;

namespace ProjectRepo.Entities
{
	public record JobRecord(
		string? Label,
		ObjectId ProjectId,
		ObjectId? ParentJobId,
		SizeInt CanvasSize,
		RRectangleRecord CoordsRecord,
		ObjectId SubDivisionId,
		MapCalcSettings MapCalcSettings,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss
		) : RecordBase()
	{ }

}
