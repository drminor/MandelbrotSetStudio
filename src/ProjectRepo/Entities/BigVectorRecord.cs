using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record BigVectorRecord(string Display, BigVectorDto BigVector)
	{
		public BigVectorRecord() : this(string.Empty, new BigVectorDto())
		{ }
	}
}
