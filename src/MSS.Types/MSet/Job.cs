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
		public MapCalcSettings MapCalcSettings { get; init; }
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
			MapCalcSettings mapCalcSettings,
			IList<ColorMapEntry> colorMapEntries,
			string highColorCss
			)
		{
			Id = id;
			ProjectId = projectId;
			ParentJobId = parentJobId;
			Label = label;
			CanvasSize = canvasSize;
			Coords = coords;
			SubdivisionId = subdivisionId;
			MapCalcSettings = mapCalcSettings;
			ColorMapEntries = colorMapEntries;
			HighColorCss = highColorCss;
		}

		public DateTime DateCreated => Id.CreationTime;

	}

}
