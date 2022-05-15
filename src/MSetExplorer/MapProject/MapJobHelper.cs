using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	internal static class MapJobHelper
	{
		#region Build Job

		public static Job BuildJob(ObjectId? parentJobId, ObjectId projectId, SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			if (!parentJobId.HasValue && transformType != TransformType.None)
			{
				throw new InvalidOperationException($"Attempting to create an new job with no parent and TransformType = {transformType}. Only jobs with TransformType = 'none' be parentless.");
			}

			var jobAreaInfo = GetJobAreaInfo(coords, canvasSize, blockSize);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(jobAreaInfo.SamplePointDelta, blockSize, projectAdapter);

			var isPreferredChild = transformType != TransformType.CanvasSizeUpdate;
			var jobName = GetJobName(transformType);

			var job = new Job(parentJobId, isPreferredChild, projectId, jobName, transformType, newArea, colorBandSetId, jobAreaInfo, subdivision, mapCalcSettings);

			return job;
		}

		public static JobAreaInfo GetJobAreaInfo(RRectangle coords, SizeInt canvasSize, SizeInt blockSize)
		{
			// Determine how much of the canvas control can be covered by the new map.
			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ration of the newArea.
			var displaySize = canvasSize;

			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(displaySize, blockSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var updatedCoords = coords.Clone();
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, displaySize);
			//Debug.WriteLine($"\nThe new coords are : {coordsWork},\n old = {mSetInfo.Coords}. (While calculating SamplePointDelta.)\n");

			//var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			//RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, mSetInfo.Coords, coordsWork, newDCoords);


			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			var result = new JobAreaInfo(updatedCoords, samplePointDelta, mapBlockOffset, canvasControlOffset, canvasSizeInBlocks);

			return result;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		private static Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			Subdivision result;

			if (projectAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var subdivision))
			{
				result = subdivision;
			}
			else
			{
				var subdivisionNotSaved = new Subdivision(samplePointDelta, blockSize);
				result = projectAdapter.InsertSubdivision(subdivisionNotSaved);
			}

			return result;
		}

		public static string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		[Conditional("DEBUG")]
		public static void CheckCanvasSize(SizeInt canvasSize, SizeInt blockSize)
		{
			var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(new SizeDbl(canvasSize), blockSize, keepSquare: true);

			if (sizeInWholeBlocks != new SizeInt(8))
			{
				Debug.WriteLine($"The canvas size is not 1024 x 1024.");
				//throw new InvalidOperationException("For testing we need the canvas size to be 1024 x 1024.");
			}
		}

		#endregion

		#region Build Initial MSetInfo

		public static ColorBandSet BuildInitialColorBandSet(int maxIterations)
		{
			var colorBands = new List<ColorBand>
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

			var result = new ColorBandSet(colorBands);

			return result;
		}

		#endregion
	}
}
