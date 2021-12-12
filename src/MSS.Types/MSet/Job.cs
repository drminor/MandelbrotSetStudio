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
		public SizeInt CanvasSizeInBlocks { get; init; }
		public PointInt CanvasBlockOffset { get; init; }
		public PointDbl CanvasControlOffset { get; init; }

		public Job(
			ObjectId id,
			Job? parentJob,
			Project project,
			Subdivision subdivision,
			string? label,
			MSetInfo mSetInfo,
			SizeInt canvasSizeInBlocks,
			PointInt canvasBlockOffset,
			PointDbl canvasControlOffset
			)
		{
			Id = id;
			ParentJob = parentJob;
			Project = project;
			Subdivision = subdivision;
			Label = label;
			MSetInfo = mSetInfo;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			CanvasBlockOffset = canvasBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
