using MSS.Types.DataTransferObjects;
using System;

namespace ProjectRepo.Entities
{
	[Serializable]
	public record RSizeRecord(string Display, RSizeDto SizeDto)
	{
		public RSizeRecord() : this(string.Empty, new RSizeDto())
		{ }
	}
}
