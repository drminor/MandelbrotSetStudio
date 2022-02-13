using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Linq;

namespace MSetExplorer
{
	internal static class MapWindowHelper
	{
		public static Job BuildJob(Project project, string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, SizeInt blockSize, ProjectAdapter projectAdapter, bool clearExistingMapSections)
		{
			var job = BuildJob(project, jobName, canvasControlSize, mSetInfo, new SizeInt(), blockSize, projectAdapter, clearExistingMapSections);

			return job;
		}

		public static Job BuildJob(Project project, string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, SizeInt? newArea, SizeInt blockSize, ProjectAdapter projectAdapter, bool clearExistingMapSections)
		{
			// Determine how much of the canvas control can be covered by the new map.

			SizeInt canvasSize;

			var selectedArea = newArea.HasValue ? newArea.Value : new SizeInt();

			canvasSize = selectedArea.Width == 0 && selectedArea.Height == 0
				? RMapHelper.GetCanvasSize(mSetInfo.Coords, canvasControlSize)
				: RMapHelper.GetCanvasSize(selectedArea, canvasControlSize);

			var coords = mSetInfo.Coords;

			// Get the number of blocks
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, blockSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			//var samplePointDelta = RMapHelper.GetSamplePointDelta2(ref coords, newArea, screenSizeToMapRat, canvasSize);
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref coords, canvasSize);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(coords, samplePointDelta, blockSize, projectAdapter, deleteExisting: clearExistingMapSections);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref coords, subdivision.Position, samplePointDelta, blockSize, out var canvasControlOffset);

			var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, jobName, updatedMSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset);
			return job;
		}

		private static Subdivision GetSubdivision(RRectangle coords, RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter, bool deleteExisting)
		{
			// Find an existing subdivision record that has a SamplePointDelta "close to" the given samplePointDelta
			// and that is "in the neighborhood of our Map Set.

			var result = projectAdapter.GetOrCreateSubdivision(coords.Position, samplePointDelta, blockSize, out var created);

			//while(deleteExisting && result.DateCreated < DateTime.Parse("1/25/2022 5:47", CultureInfo.InvariantCulture))
			//{
			//	_ = projectAdapter.DeleteSubdivision(result);
			//	result = projectAdapter.GetOrCreateSubdivision(position, samplePointDelta, blockSize, out var _);
			//}

			while (deleteExisting && !created)
			{
				_ = projectAdapter.DeleteSubdivision(result);
				result = projectAdapter.GetOrCreateSubdivision(coords.Position, samplePointDelta, blockSize, out created);
			}

			return result;
		}

		public static MSetInfo BuildInitialMSetInfo(int maxIterations)
		{
			var coords = RMapConstants.ENTIRE_SET_RECTANGLE;
			var mapCalcSettings = new MapCalcSettings(maxIterations: maxIterations, threshold: 4, iterationsPerStep: 100);

			IList<ColorMapEntry> colorMapEntries = new List<ColorMapEntry>
			{
				new ColorMapEntry(1, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(2, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(3, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(5, "#ccccff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(10, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(25, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(50, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(60, "#ccccff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(70, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(120, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(300, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(500, "#e95ee8", ColorMapBlendStyle.End, "#758cb7")
			};

			var highColorCss = "#000000";
			colorMapEntries.Add(new ColorMapEntry(maxIterations, highColorCss, ColorMapBlendStyle.None, highColorCss));

			var result = new MSetInfo(coords, mapCalcSettings, colorMapEntries.ToArray());

			return result;
		}

	}
}
