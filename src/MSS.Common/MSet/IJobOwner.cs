using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IJobOwner
	{
		ObjectId Id { get; init; }
		string Name { get; set; }
		string? Description { get; set; }

		ObjectId CurrentJobId { get; }
		ObjectId CurrentColorBandSetId { get; }

		List<Job> GetJobs();
		List<ColorBandSet> GetColorBandSets();

		bool OnFile { get; }
		bool IsDirty { get; }
		bool IsCurrentJobIdChanged { get; }

		DateTime DateCreated { get; }
		DateTime LastSavedUtc { get; }
		DateTime LastUpdatedUtc { get; }

		void MarkAsSaved();
	}
}