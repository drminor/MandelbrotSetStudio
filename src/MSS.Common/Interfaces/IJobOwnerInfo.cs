using MongoDB.Bson;
using MSS.Types.MSet;
using System;

namespace MSS.Common
{
	public interface IJobOwnerInfo 
	{
		//Project Project { get; }

		ObjectId OwnerId { get; }
		OwnerType OwnerType { get; }
		string Name { get; set; }
		string? Description { get; set; }
		ObjectId CurrentJobId { get; init; }

		int Bytes { get; set; }
		int NumberOfJobs { get; }
		int MinSamplePointDeltaExponent { get; }

		DateTime DateCreatedUtc { get; init; }
		DateTime LastSavedUtc { get; set; }
		DateTime LastAccessedUtc { get; set; }
	}

}