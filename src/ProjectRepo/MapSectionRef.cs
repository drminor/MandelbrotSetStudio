using FSTypes;
using MongoDB.Bson;
using System;

namespace ProjectRepo
{
	public record MapSectionRef(
		string Name,
		ObjectId JobId,
		ObjectId MapSectionId,
		PointInt BlockIndex,
		int PrecisionIndex,
		RectangleInt ClippingRectangle
		) : RecordBase();
}
