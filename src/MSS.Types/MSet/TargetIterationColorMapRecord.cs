using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public record TargetIterationColorMapRecord(
		int TargetIterations, 
		ObjectId ColorBandSetId,
		DateTime DateCreated
		)
	{
		public Guid ColorBandSerialNumber { get; set; } = Guid.NewGuid();
		public DateTime DateTimeUtc { get; set; } = DateTime.UtcNow;
	}
}
