using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public record ColorMapUpdateRecord(ObjectId ColorBandSetId, Guid ColorBandSerialNumber, int TargetIterations, DateTime DateTimeUtc);
}
