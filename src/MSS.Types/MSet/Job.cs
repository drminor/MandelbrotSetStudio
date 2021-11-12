using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class Job
	{
		public ObjectId Id { get; init; }
		public string? Label { get; init; }
		public ObjectId ProjectId { get; init; }
		public ObjectId? ParentJobId { get; init; }
		public SizeInt CanvasSize { get; init; }
		public RRectangle Coords { get; init; }
		public ObjectId SubdivisionId { get; init; }
		public int MaxInterations { get; init; }
		public int Threshold { get; init; }
		public int IterationsPerStep { get; init; }
		public IList<ColorMapEntry> ColorMapEntries { get; init; }
		public string HighColorCss { get; init; }

		public Job(
			ObjectId id,
			string? label,
			ObjectId projectId,
			ObjectId? parentJobId,
			SizeInt canvasSize,
			RRectangle coords,
			ObjectId subdivisionId,
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
			Label = label;
			CanvasSize = canvasSize;
			Coords = coords;
			SubdivisionId = subdivisionId;
			MaxInterations = maxInterations;
			Threshold = threshold;
			IterationsPerStep = iterationsPerStep;
			ColorMapEntries = colorMapEntries;
			HighColorCss = highColorCss;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
