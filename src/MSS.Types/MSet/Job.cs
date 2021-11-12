using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Job
	{
		public ObjectId Id { get; init; }
		public ObjectId? ParentJobId { get; init; }
		public ObjectId ProjectId { get; init; }
		public ObjectId SubdivisionId { get; init; }
		public string? Label { get; init; }
		public MSetInfo MSetInfo { get; init; }

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			ObjectId projectId,
			ObjectId subdivisionId,
			string? label,
			MSetInfo mSetInfo
			)
		{
			Id = id;
			ParentJobId = parentJobId;
			ProjectId = projectId;
			SubdivisionId = subdivisionId;
			Label = label;
			MSetInfo = mSetInfo;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
