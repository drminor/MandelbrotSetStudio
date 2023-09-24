using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record RPointAndDeltaRecord(string Display, RPointAndDeltaDto RPointAndDeltaDto)
	{
		public RPointAndDeltaRecord() : this(string.Empty, new RPointAndDeltaDto())
		{ }
	}
}
