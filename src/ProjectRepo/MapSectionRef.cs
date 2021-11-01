using FSTypes;
using System;

namespace ProjectRepo
{
	public record MapSectionRef(
		string Name,
		Guid JobId,
		Guid MapSectionId,
		PointInt BlockIndex,
		int PrecisionIndex,
		RectangleInt ClippingRectangle
		) : RecordBase();
}
