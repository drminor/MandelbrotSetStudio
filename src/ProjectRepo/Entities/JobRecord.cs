using MongoDB.Bson;
using MSS.Types;
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
		int MaxInterations,
		int Threshold,
		int IterationsPerStep,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss
		) : RecordBase()
	{ }

}
