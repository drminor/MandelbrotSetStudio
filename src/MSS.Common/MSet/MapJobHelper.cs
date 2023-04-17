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
		#region Private Properties

		private readonly SubdivisonProvider _subdivisonProvider;

		#endregion

		#region Constructor

		public MapJobHelper(SubdivisonProvider subdivisonProvider, double toleranceFactor, SizeInt blockSize)
		{
			_subdivisonProvider = subdivisonProvider;
			BlockSize = blockSize;
			ToleranceFactor = toleranceFactor; // SamplePointDelta values are calculated to within 10 pixels of the display area.
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public double ToleranceFactor { get; set; }

		#endregion

		#region Build Job Methods

		public Job BuildHomeJob(MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings)
		{
			ObjectId? parentJobId = null;
			ObjectId projectId = ObjectId.Empty;
			var transformType = TransformType.Home;
			RectangleInt? newArea = null;

			var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			var jobName = GetJobName(transformType);
			var job = new Job(parentJobId, projectId, jobName, transformType, newArea, mapAreaInfo, colorBandSetId, mapCalcSettings);

			return job;
		}

		#endregion

		#region GetMapAreaInfo Methods

		// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		public MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt canvasSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = RMapHelper.GetSamplePointDelta(coords, displaySize, ToleranceFactor, out var wToHRatio);

			// The samplePointDelta may require the coordinates to be adjusted.
			var updatedCoords = RMapHelper.AdjustCoordsWithNewSPD(coords, samplePointDelta, displaySize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(updatedCoords, samplePointDelta, BlockSize, out var canvasControlOffset, out RPoint newPosition);

			var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);
			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
		}

		// Calculate the MapBlockOffset and CanvasControlOffset while keeping the SamplePointDelta, constant.
		public MapAreaInfo GetMapAreaInfo(RRectangle coords, Subdivision subdivision, SizeInt canvasSize)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			var updatedCoords = coords.Clone();

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(updatedCoords, samplePointDelta, BlockSize, out var canvasControlOffset, out RPoint newPosition);
			var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

			var localMapBlockOffset = GetLocalMapBlockOffset(mapBlockOffset, subdivision);

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);

			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
		}

		public int GetBinaryPrecision(RRectangle coords, RSize samplePointDelta, out int decimalPrecision)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(coords.Right, coords.Left, out decimalPrecision);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			return binaryPrecision;
		}

		private BigVector GetLocalMapBlockOffset(BigVector mapBlockOffset, Subdivision subdivision)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			var checkSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			Debug.Assert(checkSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			return localMapBlockOffset;
		}

		public static string GetJobName(TransformType transformType)
		{
			//var result = transformType == TransformType.Home ? "Home" : transformType.ToString();
			var result = transformType.ToString();
			return result;
		}

		#endregion

		#region Old Methods

		//public Job BuildHomeJob(SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
		//	TransformType transformType, SizeInt blockSize)
		//{
		//	ObjectId? parentJobId = null;
		//	ObjectId projectId = ObjectId.Empty;
		//	RectangleInt? newArea = null;

		//	var mapAreaInfo = GetMapAreaInfo(coords, canvasSize, blockSize);
		//	var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
		//	return result;
		//}

		//public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, SizeInt canvasSize, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
		//	TransformType transformType, RectangleInt? newArea, SizeInt blockSize)
		//{
		//	var mapAreaInfo = GetMapAreaInfo(coords, canvasSize);
		//	var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
		//	return result;
		//}

		public static RRectangle GetNewCoordsForNewCanvasSize(RRectangle currentCoords, SizeInt currentSizeInBlocks, SizeInt newSizeInBlocks, Subdivision subdivision)
		{
			var diff = newSizeInBlocks.Sub(currentSizeInBlocks);

			if (diff == SizeInt.Zero)
			{
				return currentCoords;
			}

			diff = diff.Scale(subdivision.BlockSize);
			var rDiff = subdivision.SamplePointDelta.Scale(diff);
			rDiff = rDiff.DivideBy2();

			var result = AdjustCoords(currentCoords, rDiff);
			return result;
		}

		private static RRectangle AdjustCoords(RRectangle coords, RSize rDiff)
		{
			var nrmArea = RNormalizer.Normalize(coords, rDiff, out var nrmDiff);

			var x1 = nrmArea.X1 - nrmDiff.Width.Value;
			var x2 = nrmArea.X2 + nrmDiff.Width.Value;

			var y1 = nrmArea.Y1 - nrmDiff.Height.Value;
			var y2 = nrmArea.Y2 + nrmDiff.Height.Value;

			var result = new RRectangle(x1, x2, y1, y2, nrmArea.Exponent);

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
