using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public record TargetIterationColorMapRecord(int TargetIterations, ObjectId ColorBandSetId, Guid ColorBandSerialNumber, DateTime DateTimeUtc);
}
