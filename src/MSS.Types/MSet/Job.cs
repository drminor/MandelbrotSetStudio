using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Job
	{
		public ObjectId Id { get; init; }
		public Job? ParentJob { get; init; }
		public Project Project { get; init; }
		public Subdivision Subdivision { get; init; }
		public string? Label { get; init; }
		public MSetInfo MSetInfo { get; init; }
		public PointDbl CanvasOffset { get; init; }

		public Job(
			ObjectId id,
			Job? parentJob,
			Project project,
			Subdivision subdivision,
			string? label,
			MSetInfo mSetInfo,
			PointDbl canvasOffset
			)
		{
			Id = id;
			ParentJob = parentJob;
			Project = project;
			Subdivision = subdivision;
			Label = label;
			MSetInfo = mSetInfo;
			CanvasOffset = canvasOffset;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
