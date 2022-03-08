using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record RPointRecord(string Display, RPointDto PointDto)
	{
		public RPointRecord() : this(string.Empty, new RPointDto())
		{ }
	}
}
