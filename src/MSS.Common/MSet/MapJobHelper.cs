﻿using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSS.Common
{
	public class MapJobHelper
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;

		public MapJobHelper(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
			ToleranceFactor = 10; // Sample Delta Sizes are calculated to within 10 pixels of the diplay area.
		}

		#region Public Properties

		public double ToleranceFactor { get; set; }

		#endregion

		#region Public Methods

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea, SizeInt blockSize)
		{
			var mapAreaInfo = GetMapAreaInfo(coords, canvasSize, blockSize);
			var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		private Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea)
		{
			// Determine how much of the canvas control can be covered by the new map.
			var canvasSize = mapAreaInfo.CanvasSize;

			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			var canvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(displaySize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);

			var isPreferredChild = transformType != TransformType.CanvasSizeUpdate;
			var jobName = GetJobName(transformType);

			var job = new Job(parentJobId, isPreferredChild, projectId, jobName, transformType, newArea, mapAreaInfo, canvasSizeInBlocks, colorBandSetId, mapCalcSettings);

			return job;
		}

		public MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt canvasSize, SizeInt blockSize)
		{
			// Determine how much of the canvas control can be covered by the new map.
			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var updatedCoords = coords.Clone();
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, displaySize, ToleranceFactor);
			//Debug.WriteLine($"\nThe new coords are : {coordsWork},\n old = {mSetInfo.Coords}. (While calculating SamplePointDelta.)\n");

			//var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			//RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, mSetInfo.Coords, coordsWork, newDCoords);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(samplePointDelta, blockSize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, subdivision, out var canvasControlOffset);

			var result = new MapAreaInfo(updatedCoords, canvasSize, subdivision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		#endregion

		#region Private Methods

		// Find an existing subdivision record that the same SamplePointDelta
		private Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize)
		{
			if (! _mapSectionAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var result))
			{
				result = new Subdivision(samplePointDelta, blockSize);
				_mapSectionAdapter.InsertSubdivision(result);
			}

			return result;
		}

		private string GetJobName(TransformType transformType)
		{
			//var result = transformType == TransformType.Home ? "Home" : transformType.ToString();
			var result = transformType.ToString();
			return result;
		}

		//[Conditional("DEBUG")]
		//public static void CheckCanvasSize(SizeInt canvasSize, SizeInt blockSize)
		//{
		//	var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInBlocks(new SizeDbl(canvasSize), blockSize, keepSquare: true);

		//	if (sizeInWholeBlocks != new SizeInt(8))
		//	{
		//		Debug.WriteLine($"The canvas size is not 1024 x 1024.");
		//		//throw new InvalidOperationException("For testing we need the canvas size to be 1024 x 1024.");
		//	}
		//}

		#endregion
	}
}
