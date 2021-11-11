using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record RRectangleRecord(string Display, RRectangleDto CoordsDto)
	{
		public RRectangleRecord() : this(string.Empty, new RRectangleDto())
		{ }
	}
}
