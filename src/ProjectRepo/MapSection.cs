using FSTypes;
using System;

namespace ProjectRepo
{
	public record MapSection(Guid Id, DateTime DateCreated, string Name,
		int TargetIterationCount,
		int Zoom,
		int CoordValuePrecision,
		string StartingX, // TODO: Add double[] to hold "real" StartingX
		string StartingY,// TODO: Add double[] to hold "real" StartingY
		SizeInt Size,
		double SamplePointDeltaV,
		double SamplePointDeltaH
	) : RecordBase(Id, DateCreated, Name);

}
