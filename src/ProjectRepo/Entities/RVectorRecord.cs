using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record RVectorRecord(string Display, RVectorDto VectorDto)
	{
		public RVectorRecord() : this(string.Empty, new RVectorDto())
		{ }
	}
}
