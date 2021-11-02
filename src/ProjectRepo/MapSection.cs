using MSS.Types;
using System;

namespace ProjectRepo
{
	public record MapSection(
		string Name,
		int TargetIterationCount,
		int Zoom,
		int CoordValuePrecision,
		string StartingX, // TODO: Add double[] to hold "real" StartingX
		string StartingY,// TODO: Add double[] to hold "real" StartingY
		SizeInt Size,
		double SamplePointDeltaV,
		double SamplePointDeltaH
	) : RecordBase();

}
