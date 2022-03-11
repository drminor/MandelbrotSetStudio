﻿using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	internal static class MapJobHelper
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
			var mapCalcSettings = new MapCalcSettings(targetIterations: maxIterations, threshold: 4, iterationsPerRequest: 100);

			IList<ColorBand> colorBands = new List<ColorBand>
			{
				new ColorBand(1, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(2, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(3, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(5, "#ccccff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(10, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(25, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(50, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(60, "#ccccff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(70, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(120, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(300, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(500, "#e95ee8", ColorBandBlendStyle.End, "#758cb7")
			};

			var highColorCss = "#000000";
			colorBands.Add(new ColorBand(maxIterations, highColorCss, ColorBandBlendStyle.None, highColorCss));

			var result = new MSetInfo(coords, mapCalcSettings, new ColorBandSet(colorBands));

			return result;
		}

		#endregion
	}
}