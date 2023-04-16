using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSS.Common
{
	public class MapJobHelper
	{
		private readonly SubdivisonProvider _subdivisonProvider;

		public MapJobHelper(SubdivisonProvider subdivisonProvider)
		{
			_subdivisonProvider = subdivisonProvider;
			ToleranceFactor = 10; // SamplePointDelta values are calculated to within 10 pixels of the display area.
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

		public Job BuildHomeJob(SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, SizeInt blockSize)
		{
			ObjectId? parentJobId = null;
			ObjectId projectId = ObjectId.Empty;
			RectangleInt? newArea = null;

			var mapAreaInfo = GetMapAreaInfo(coords, canvasSize, blockSize);
			var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea)
		{
			// Determine how much of the canvas control can be covered by the new map.
			var canvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);

			var jobName = GetJobName(transformType);
			var job = new Job(parentJobId, projectId, jobName, transformType, newArea, mapAreaInfo, canvasSizeInBlocks, colorBandSetId, mapCalcSettings);

			return job;
		}

		/// <summary>
		/// Calculate new coordinates, MapBlockOffset and CanvasControlOffset, for a new display size, while keeping the SamplePointDelta, constant.
		/// </summary>
		/// <param name="mapAreaInfo"></param>
		/// <param name="canvasSize"></param>
		/// <param name="newCanvasSize"></param>
		/// <returns></returns>
		public MapAreaInfo GetMapAreaInfo(RPoint mapPosition, Subdivision subdivision, SizeDbl canvasSize, SizeDbl newCanvasSize)
		{
			//var subdivision = mapAreaInfo.Subdivision;
			var samplePointDelta = subdivision.SamplePointDelta;
			var blockSize = subdivision.BlockSize;

			var diff = canvasSize.Sub(newCanvasSize);

			// Take 1/2 of the distance
			diff = diff.Scale(0.5);

			//var mapPos = mapAreaInfo.Coords.Position;

			var rectangleDbl = new RectangleDbl(new PointDbl(diff), newCanvasSize);
			var newCoords = RMapHelper.GetMapCoords(rectangleDbl.Round(), mapPosition, samplePointDelta);

			var newPos = newCoords.Position;
			Debug.WriteLine($"GetMapArea is moving the pos from {mapPosition} to {newPos}.");

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			Debug.Assert(canvasControlOffset.X >= 0 && canvasControlOffset.Y >= 0, "GetMapBlockOffset is returning a canvasControlOffset with a negative w or h value.");

			// TODO: Check the calculated precision as the new Map Coordinates are calculated.
			var binaryPrecision = RValueHelper.GetBinaryPrecision(newCoords.Right, newCoords.Left, out _);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			var checkSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			Debug.Assert(checkSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			var result = new MapAreaInfo(newCoords, newCanvasSize.Round(), subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
		}

		//public static RRectangle GetNewCoordsForNewCanvasSize(RRectangle currentCoords, SizeInt currentSizeInBlocks, SizeInt newSizeInBlocks, Subdivision subdivision)
		//{
		//	var diff = newSizeInBlocks.Sub(currentSizeInBlocks);

		//	if (diff == SizeInt.Zero)
		//	{
		//		return currentCoords;
		//	}

		//	diff = diff.Scale(subdivision.BlockSize);
		//	var rDiff = subdivision.SamplePointDelta.Scale(diff);
		//	rDiff = rDiff.DivideBy2();

		//	var result = AdjustCoords(currentCoords, rDiff);
		//	return result;
		//}

		//private static RRectangle AdjustCoords(RRectangle coords, RSize rDiff)
		//{
		//	var nrmArea = RNormalizer.Normalize(coords, rDiff, out var nrmDiff);

		//	var x1 = nrmArea.X1 - nrmDiff.Width.Value;
		//	var x2 = nrmArea.X2 + nrmDiff.Width.Value;

		//	var y1 = nrmArea.Y1 - nrmDiff.Height.Value;
		//	var y2 = nrmArea.Y2 + nrmDiff.Height.Value;

		//	var result = new RRectangle(x1, x2, y1, y2, nrmArea.Exponent);

		//	return result;
		//}

		/// <summary>
		/// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		/// </summary>
		/// <param name="coords"></param>
		/// <param name="canvasSize"></param>
		/// <param name="blockSize"></param>
		/// <returns></returns>
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

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			// TODO: Check the calculated precision as the new Map Coordinates are calculated.
			var binaryPrecision = RValueHelper.GetBinaryPrecision(updatedCoords.Right, updatedCoords.Left, out _);

			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			var result = new MapAreaInfo(updatedCoords, canvasSize, subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
		}

		public static string GetJobName(TransformType transformType)
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
