using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class Job
	{
		public ObjectId Id { get; init; }
		public ObjectId ProjectId { get; init; }
		public ObjectId? ParentJobId { get; init; }
		public TransformType? Operation { get; init; }
		public int OperationAmount { get; init; }
		public bool Saved { get; init; }
		public string? Label { get; init; }
		public SizeInt CanvasSize { get; init; }
		public RRectangle Coords { get; init; }
		public int MaxInterations { get; init; }
		public int Threshold { get; init; }
		public int IterationsPerStep { get; init; }
		public IList<ColorMapEntry> ColorMapEntries { get; init; }
		public string HighColorCss { get; init; }

		public Job(
			ObjectId id,
			ObjectId projectId,
			ObjectId? parentJobId,
			TransformType? operation,
			int operationAmount,
			string? label,
			SizeInt canvasSize,
			RRectangle coords,
			int maxInterations,
			int threshold,
			int iterationsPerStep,
			IList<ColorMapEntry> colorMapEntries,
			string highColorCss
			//,
			//IList<MapSectionPtr>? mapSectionPtrs
			)
		{
			Id = id;
			ProjectId = projectId;
			ParentJobId = parentJobId;
			Operation = operation;
			OperationAmount = operationAmount;
			Label = label;
			CanvasSize = canvasSize;
			Coords = coords;
			MaxInterations = maxInterations;
			Threshold = threshold;
			IterationsPerStep = iterationsPerStep;
			ColorMapEntries = colorMapEntries;
			HighColorCss = highColorCss;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
