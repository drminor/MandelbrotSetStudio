using FSTypes;
using System;

namespace ProjectRepo
{
	public record MapSectionRef(Guid Id, DateTime DateCreated, string Name,
		Guid JobId,
		Guid MapSectionId,
		PointInt BlockIndex,
		int PrecisionIndex,
		RectangleInt ClippingRectangle
		) : RecordBase(Id, DateCreated, Name);

}
