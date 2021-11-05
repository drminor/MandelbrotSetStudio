using MSS.Types;
using MongoDB.Bson;
using System;

namespace MSS.Types.MSetDatabase
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
