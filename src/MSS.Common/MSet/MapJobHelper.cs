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

		public Job BuildHomeJob(MapAreaInfo2 mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings)
		{
			ObjectId? parentJobId = null;
			ObjectId projectId = ObjectId.Empty;
			var transformType = TransformType.Home;
			RectangleInt? newArea = null;

			var result = BuildJob(parentJobId, projectId, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		public Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo2 mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			var jobName = GetJobName(transformType);
			var job = new Job(parentJobId, projectId, jobName, transformType, newArea, mapAreaInfo, colorBandSetId, mapCalcSettings);

			return job;
		}

		#endregion

		#region GetMapAreaInfo Methods - New

		// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		public MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt canvasSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = RMapHelper.GetSamplePointDelta(coords, displaySize, ToleranceFactor, out var wToHRatio);
			ReportSamplePointRatios(coords, displaySize, wToHRatio);

			// The samplePointDelta may require the coordinates to be adjusted.
			var mapSize = samplePointDelta.Scale(displaySize);
			var adjPos1 = RNormalizer.Normalize(coords.Position, mapSize, out var adjMapSize1);
			var adjCoords1 = new RRectangle(adjPos1, adjMapSize1);

			var uCoords = RNormalizer.Normalize(adjCoords1, samplePointDelta, out var uSpd);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(uCoords.Position, uSpd, BlockSize, out var canvasControlOffset);

			var newPosition = samplePointDelta.Scale(mapBlockOffset.Scale(BlockSize).Tranlate(canvasControlOffset));

			var adjPos2 = RNormalizer.Normalize(newPosition, uCoords.Size, out var adjMapSize2);
			var newCoords = new RRectangle(new RPoint(adjPos2), adjMapSize2);

			ReportCoordsDiff(coords, newCoords, "for a new Job");

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(uSpd, mapBlockOffset, out var localMapBlockOffset);

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);
			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		public MapAreaInfo UpdateSize(MapAreaInfo previousMapAreaInfo, SizeDbl previousSize, SizeDbl newSize)
		{
			var newScreenArea = GetNewScreenArea(previousSize, newSize);

			var mapPosition = previousMapAreaInfo.Coords.Position;
			var samplePointDelta = previousMapAreaInfo.Subdivision.SamplePointDelta;

			var newCoords = RMapHelper.GetMapCoords(newScreenArea.Round(), mapPosition, samplePointDelta);
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(newCoords.Position, samplePointDelta, BlockSize, out var canvasControlOffset);

			var result = new MapAreaInfo(newCoords, newSize.Round(), previousMapAreaInfo.Subdivision, previousMapAreaInfo.Precision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		// Calculate new coordinates for a new display size
		private RectangleDbl GetNewScreenArea(SizeDbl canvasSize, SizeDbl newCanvasSize)
		{
			var diff = canvasSize.Sub(newCanvasSize);
			diff = diff.Scale(0.5);
			var rectangleDbl = new RectangleDbl(new PointDbl(diff), newCanvasSize);

			return rectangleDbl;
		}

		public int GetBinaryPrecision(RRectangle coords, RSize samplePointDelta, out int decimalPrecision)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(coords.Right, coords.Left, out decimalPrecision);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			return binaryPrecision;
		}

		public static string GetJobName(TransformType transformType)
		{
			//var result = transformType == TransformType.Home ? "Home" : transformType.ToString();
			var result = transformType.ToString();
			return result;
		}

		public MapAreaInfo UpdateSizeWithDiagnostics(MapAreaInfo previousMapAreaInfo, SizeDbl previousSize, SizeDbl newSize)
		{
			var newScreenArea = GetNewScreenArea(previousSize, newSize);

			var mapPosition = previousMapAreaInfo.Coords.Position;
			var subdivision = previousMapAreaInfo.Subdivision;
			var samplePointDelta = subdivision.SamplePointDelta;

			var newCoords = RMapHelper.GetMapCoords(newScreenArea.Round(), mapPosition, samplePointDelta);

			//var newMapAreaInfo = GetMapAreaInfo(newCoords, subdivision, newSize.Round());

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(newCoords.Position, samplePointDelta, BlockSize, out var canvasControlOffset);

			var newPosition = samplePointDelta.Scale(mapBlockOffset.Scale(BlockSize).Tranlate(canvasControlOffset));

			var adjPos = RNormalizer.Normalize(newPosition, newCoords.Size, out var adjMapSize);
			var adjCoords = new RRectangle(new RPoint(adjPos), adjMapSize);

			ReportCoordsDiff(previousMapAreaInfo.Coords, adjCoords, "for a new display size");

			var checkSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			Debug.Assert(checkSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			var binaryPrecision = GetBinaryPrecision(adjCoords, samplePointDelta, out _);

			var result = new MapAreaInfo(adjCoords, newSize.Round(), subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		private void ReportSamplePointRatios(RRectangle coords, SizeInt displaySize, double samplePointWToHRatio)
		{
			var coordsWToHRatio = coords.Height.DivideLimitedPrecision(coords.Width);
			var canvasSizeWToHRatio = displaySize.Height / (double)displaySize.Width;
			Debug.WriteLine($"While calculating the SamplePointDelta, we got wToHRatio: {samplePointWToHRatio}, coordsWToHRatio: {coordsWToHRatio} and displayWToHRatio: {canvasSizeWToHRatio}.");
		}

		private void ReportCoordsDiff(RRectangle coords, RRectangle newCoords, string desc)
		{
			var pos = coords.Position;
			var newPos = newCoords.Position;
			var nrmPos = RNormalizer.Normalize(pos, newPos, out var nrmNewPos);
			var nrmSize = RNormalizer.Normalize(coords.Size, newCoords.Size, out var nrmNewSize);

			var diffW = nrmSize.Width.Sub(nrmNewSize.Width);
			var diffP = nrmPos.Diff(nrmNewPos);

			Debug.WriteLine($"While getting the MapAreaInfo {desc}, the coordinates were adjusted by diffW: {diffW}, diffP: {diffP}.");
		}

		#endregion

		#region GetMapAreaInfo Methods - Previous / Ref

		// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		public MapAreaInfo GetMapAreaInfoRef(RRectangle coords, SizeInt canvasSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = RMapHelper.GetSamplePointDelta(coords, displaySize, ToleranceFactor, out var wToHRatio);

			// The samplePointDelta may require the coordinates to be adjusted.
			var updatedCoords = RMapHelper.AdjustCoordsWithNewSPD(coords, samplePointDelta, displaySize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffsetRef(updatedCoords, samplePointDelta, BlockSize, out var canvasControlOffset, out RPoint newPosition);

			var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);
			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		// Calculate the MapBlockOffset and CanvasControlOffset while keeping the SamplePointDelta, constant.
		public MapAreaInfo GetMapAreaInfoRef(RRectangle coords, Subdivision subdivision, SizeInt canvasSize)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			var updatedCoords = coords.Clone();

			var mapBlockOffset = RMapHelper.GetMapBlockOffsetRef(updatedCoords, samplePointDelta, BlockSize, out var canvasControlOffset, out RPoint newPosition);
			var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

			//var localMapBlockOffset = GetLocalMapBlockOffset(mapBlockOffset, subdivision);

			var checkSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			Debug.Assert(checkSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);

			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

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
			rDiff = rDiff.ScaleByHalf();

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

		// Calculate the MapBlockOffset and CanvasControlOffset while keeping the SamplePointDelta, constant.
		public MapAreaInfo GetMapAreaInfoScaleConstant(RRectangle coords, Subdivision subdivision, SizeInt canvasSize)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			//var updatedCoords = coords.Clone();

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(coords.Position, samplePointDelta, BlockSize, out var canvasControlOffset);

			var newPosition = samplePointDelta.Scale(mapBlockOffset.Scale(BlockSize).Tranlate(canvasControlOffset));

			var adjPos = RNormalizer.Normalize(newPosition, coords.Size, out var adjMapSize);
			var newCoords = new RRectangle(new RPoint(adjPos), adjMapSize);

			ReportCoordsDiff(coords, newCoords, "for a new display size");

			//var localMapBlockOffset = GetLocalMapBlockOffset(mapBlockOffset, subdivision);
			var checkSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			Debug.Assert(checkSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);

			var result = new MapAreaInfo(newCoords, canvasSize, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

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
