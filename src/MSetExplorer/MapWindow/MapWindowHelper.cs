using MEngineDataContracts;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				var position = GetPositionForNewSubdivision(coords);
				var subdivisionNotSaved = new Subdivision( position, samplePointDelta, blockSize);
				subdivision = projectAdapter.InsertSubdivision(subdivisionNotSaved);
			}

			return subdivision;
		}

		private static RPoint GetPositionForNewSubdivision(RRectangle coords)
		{
			//var x = coords.X1.Sign != coords.X2.Sign ? 0 : coords.X1;
			//var y = coords.Y1.Sign != coords.Y2.Sign ? 0 : coords.Y1;
			//var exponent = x == 0 && y == 0 ? 0 : coords.Exponent;
			//return new RPoint(x, y, exponent);

			return new RPoint();
		}

		#endregion

		#region Map Loader Support

		public static IList<MapSectionRequest> CreateSectionRequests(Job job, IList<MapSection> emptyMapSections)
		{
			if (emptyMapSections == null)
			{
				return CreateSectionRequests(job);
			}
			else
			{
				var result = new List<MapSectionRequest>();

				Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

				foreach (var mapSection in emptyMapSections)
				{
					var screenPosition = mapSection.BlockPosition;
					var mapSectionRequest = MapSectionHelper.CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
					result.Add(mapSectionRequest);
				}
				return result;
			}
		}

		public static IList<MapSectionRequest> CreateSectionRequests(Job job)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
				{
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var mapSectionRequest = MapSectionHelper.CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		public static IList<MapSection> CreateEmptyMapSections(Job job)
		{
			var emptyPixelData = new byte[0];
			var result = new List<MapSection>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
				{
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, job.MapBlockOffset, out var _);
					var mapSection = new MapSection(screenPosition, job.Subdivision.BlockSize, emptyPixelData, job.Subdivision.Id.ToString(), repoBlockPosition: repoPosition);
					result.Add(mapSection);
				}
			}

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
