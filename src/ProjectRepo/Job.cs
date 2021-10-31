using FSTypes;
using System;
using System.Collections.Generic;

namespace ProjectRepo
{
	public record Job(Guid Id, DateTime DateCreated, string Name,
		Guid ProjectId,
		Guid ParentJobId,
		string Operation,
		double OperationAmount,
		bool Saved,
		string Label,
		int Zoom,
		int CoordValuePrecision,
		MFile.SCoords SCoords,
		int MaxInterations,
		int Threshold,
		int IterationsPerStep,
		//IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss
		) : RecordBase(Id, DateCreated, Name);


}
