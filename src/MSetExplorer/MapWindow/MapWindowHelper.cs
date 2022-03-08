using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSetExplorer
{
	internal static class MapWindowHelper
	{
		#region Build Job

		public static Job BuildJob(Job parentJob, Project project, string jobName, SizeInt canvasSize, MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea, SizeInt blockSize, ProjectAdapter projectAdapter/*, bool clearExistingMapSections*/)
		{
			// Determine how much of the canvas control can be covered by the new map.
			if (newArea.Width == 0 || newArea.Height == 0)
			{
				throw new ArgumentException("When building a job, the new area's size cannot have a width or height = 0.");
			}

			var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(displaySize, blockSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var coords = mSetInfo.Coords;
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref coords, displaySize);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(coords, samplePointDelta, blockSize, projectAdapter);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(coords, subdivision.Position, samplePointDelta, blockSize, out var canvasControlOffset);

			var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			var job = new Job(parentJob, project, subdivision, jobName, transformType, newArea, updatedMSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset);

			return job;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		private static Subdivision GetSubdivision(RRectangle coords, RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			if (!projectAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var subdivision))
			{
				var subdivisionNotSaved = new Subdivision(samplePointDelta, blockSize);
				subdivision = projectAdapter.InsertSubdivision(subdivisionNotSaved);
			}

			return subdivision;
		}

		public static string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		#endregion

		#region Build Initial MSetInfo

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

		#endregion
	}
}
