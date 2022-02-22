using MongoDB.Bson;
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
		public static Job BuildJob(Job parentJob, Project project, string jobName, SizeInt canvasSize, MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea, SizeInt blockSize, ProjectAdapter projectAdapter/*, bool clearExistingMapSections*/)
		{
			// Determine how much of the canvas control can be covered by the new map.

			SizeInt displaySize;

			//displaySize = newArea.Width == 0 || newArea.Height == 0
			//	? RMapHelper.GetCanvasSize(mSetInfo.Coords.Size, canvasSize)
			//	: RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			if (newArea.Width == 0 || newArea.Height == 0)
			{
				throw new ArgumentException("When building a job, the new area's size cannot have a width or height = 0.");
			}

			displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Get the number of blocks
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(displaySize, blockSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			//var samplePointDelta = RMapHelper.GetSamplePointDelta2(ref coords, newArea, screenSizeToMapRat, canvasSize);
			var coords = mSetInfo.Coords;
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref coords, displaySize);

			// Get a subdivision record from the database.
			//var subdivision = GetSubdivision(coords, samplePointDelta, blockSize, projectAdapter, deleteExisting: false);
			var subdivision = GetSubdivision(coords, samplePointDelta, blockSize, projectAdapter);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(coords, subdivision.Position, samplePointDelta, blockSize, out var canvasControlOffset);

			var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			var job = new Job(ObjectId.GenerateNewId(), parentJob, project, subdivision, jobName, transformType, newArea, updatedMSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset);
			return job;
		}

		private static Subdivision GetSubdivision(RRectangle coords, RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			// Find an existing subdivision record that has a SamplePointDelta "close to" the given samplePointDelta
			// and that is "in the neighborhood of our Map Set.

			if (!projectAdapter.TryGetSubdivision(coords.Position, samplePointDelta, blockSize, out var subdivision))
			{
				var position = GetPositionForNewSubdivision(coords);
				var subdivisionNotSaved = new Subdivision(ObjectId.GenerateNewId(), position, samplePointDelta, blockSize);
				subdivision = projectAdapter.InsertSubdivision(subdivisionNotSaved);
			}

			return subdivision;
		}

		private static RPoint GetPositionForNewSubdivision(RRectangle coords)
		{
			//var x = coords.X1.Sign != coords.X2.Sign ? 0 : coords.X1;
			////var y = coords.Y1.Sign != coords.Y2.Sign ? 0 : BigInteger.Abs(coords.Y1);
			//var y = coords.Y1.Sign != coords.Y2.Sign ? 0 : coords.Y1;
			//var exponent = x == 0 && y == 0 ? 0 : coords.Exponent;

			//return new RPoint(x, y, exponent);

			return new RPoint();

			//return coords.Position;
		}

		//public static Point GetBlockPosition(Point screenPosition, SizeInt blockSize)
		//{
		//	var pos = new PointDbl(screenPosition.X, screenPosition.Y).Round();

		//	var left = Math.DivRem(pos.X, blockSize.Width, out var remainder);
		//	if (remainder == 0 && left > 0)
		//	{
		//		left--;
		//	}

		//	var bottom = Math.DivRem(pos.Y, blockSize.Height, out remainder);
		//	if (remainder == 0 && bottom > 0)
		//	{
		//		bottom--;
		//	}

		//	var botRight = new PointInt(left, bottom).Scale(blockSize);
		//	var center = botRight.Translate(new SizeInt(blockSize.Width / 2, blockSize.Height / 2));
		//	return new Point(center.X, center.Y);
		//}

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
