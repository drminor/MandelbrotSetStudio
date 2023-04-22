using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public class MapJobHelper
	{
		#region Private Properties

		private readonly SubdivisonProvider _subdivisonProvider;

		//private readonly MapJobHelper _oldJobHelper;

		#endregion

		#region Constructor

		public MapJobHelper(SubdivisonProvider subdivisonProvider, double toleranceFactor, SizeInt blockSize)
		{
			_subdivisonProvider = subdivisonProvider;
			BlockSize = blockSize;
			ToleranceFactor = toleranceFactor; // SamplePointDelta values are calculated to within 10 pixels of the display area.

			//_oldJobHelper = new MapJobHelper(subdivisonProvider, toleranceFactor, blockSize);
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

		#region GetMapAreaInfo

		// Pan
		public MapAreaInfo2 GetMapAreaInfoPan(MapAreaInfo2 currentArea, VectorInt panAmount)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var rPanAmount = panAmount.Scale(currentArea.SamplePointDelta);
			var newMapCenter = currentArea.MapCenter.Translate(rPanAmount);

			var transPd = new RPointAndDelta(newMapCenter, currentArea.SamplePointDelta);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(transPd, blockSize, out var canvasControlOffset);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(transPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(transPd.Exponent);

			var result = new MapAreaInfo2(transPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		// Zoom
		public MapAreaInfo2 GetMapAreaInfoZoomCenter(MapAreaInfo2 currentArea, double factor)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var scaledPd = GetNewSamplePointDelta(currentArea.PositionAndDelta, factor);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(scaledPd, blockSize, out var canvasControlOffset);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(scaledPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(scaledPd.Exponent);

			var result = new MapAreaInfo2(scaledPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		// Pan and Zoom
		public MapAreaInfo2 GetMapAreaInfoZoomPoint(MapAreaInfo2 currentArea, VectorInt panAmount, double factor)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var rPanAmount = panAmount.Scale(currentArea.SamplePointDelta);
			var newMapCenter = currentArea.MapCenter.Translate(rPanAmount);

			var transPd = new RPointAndDelta(newMapCenter, currentArea.SamplePointDelta);
			var scaledAndTransPd = GetNewSamplePointDelta(transPd, factor);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(scaledAndTransPd, blockSize, out var canvasControlOffset);
			
			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(scaledAndTransPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(scaledAndTransPd.Exponent);

			var result = new MapAreaInfo2(scaledAndTransPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);


			return result;
		}

		private RPointAndDelta GetNewSamplePointDelta(RPointAndDelta pointAndDelta, double factor)
		{
			var kFactor = (int)Math.Round(1 / factor * 1024);

			var rKFactor = new RValue(kFactor, -10);

			var rawResult = pointAndDelta.ScaleDelta(rKFactor);

			var result = Reducer.Reduce(rawResult);

			return result;
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

		#endregion

		#region MapAreaInfo2 Support

		public MapAreaInfo Convert(MapAreaInfo2 mapAreaInfo2, SizeInt canvasSize)
		{
			var mapCenterPoint = mapAreaInfo2.MapCenter;
			var samplePointDelta = mapAreaInfo2.PositionAndDelta.SamplePointDelta;
			var blockSize = mapAreaInfo2.Subdivision.BlockSize;

			// Create a rectangle centered at position: x = 0, y = 0
			// Having the same width and height as the given canvasSize.
			var half = new PointInt(canvasSize.Width / 2, canvasSize.Height / 2);
			var area = new RectangleInt(half.Invert(), canvasSize);

			// Multiply the area by the SamplePointDelta to get map coordiates
			// add to it the CenterPoint, to get a RRectangle which is the map's coordinates

			//var coords = RMapHelper.GetMapCoords(area, mapCenterPoint, samplePointDelta);

			//var rArea = RMapHelper.ScaleByRsize(area, samplePointDelta);
			var rArea = new RRectangle(area);
			rArea = rArea.Scale(samplePointDelta);

			var nrmArea = RNormalizer.Normalize(rArea, mapCenterPoint, out var nrmMapCenterPoint);
			var coords = nrmArea.Translate(nrmMapCenterPoint);

			//var mapBlockOffset = RMapHelper.GetMapBlockOffset(coords.Position, mapAreaInfo2.SamplePointDelta, mapAreaInfo2.Subdivision.BlockSize, out var canvasControlOffset);

			var adjCoords = RNormalizer.Normalize(coords, samplePointDelta, out var nrmSamplePointDelta);

			// Calculate the total number of sample points from the origin to the lower, left corner of the map's coordinates and the Subdivision origin (i.e., BaseMapBlockOffset.)
			var positionV = new RVector(adjCoords.Position);

			// Determine the number of full blocks, and number of samplePoints remaining
			BigVector mapBlockOffset;
			VectorInt canvasControlOffset;

			if (positionV.IsZero())
			{
				mapBlockOffset = new BigVector();
				canvasControlOffset = new VectorInt();
			}
			else
			{
				var offsetInSamplePoints = positionV.Divide(samplePointDelta);
				mapBlockOffset = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
			}

			var subdivision = _subdivisonProvider.GetSubdivision(nrmSamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(nrmSamplePointDelta.Exponent);

			var result = new MapAreaInfo(adjCoords, canvasSize, subdivision, binaryPrecision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		public static MapAreaInfo2 Convert(MapAreaInfo mapAreaInfo)
		{
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var blockSize = mapAreaInfo.Subdivision.BlockSize;

			// The current SamplePointDelta remains the same.
			// The coords are updated to have the same exponent.
			var nrmCoords = NormalizeCoordsWithSPD(mapAreaInfo.Coords, samplePointDelta);

			// The center of the coords = pos.x + width / 2, pos.y + height / 2;
			//var offset = new RSize(nrmCoords.WidthNumerator / 2, nrmCoords.HeightNumerator / 2, nrmCoords.Exponent);

			// Get the center of the coords in map units
			var offset = nrmCoords.Size.DivideBy2();
			//var invertedOffset = offset.Invert();
			var centerV = nrmCoords.Position.Translate(offset);

			//var mapBlockOffset = RMapHelper.GetMapBlockOffset(center, samplePointDelta, blockSize, out var canvasControlOffset);

			// Calculate the total number of sample pointg between the center and the Subdivision origin (i.e., BaseMapBlockOffset.)

			BigVector mapBlockOffset;
			VectorInt canvasControlOffset;

			if (centerV.IsZero())
			{
				mapBlockOffset = new BigVector();
				canvasControlOffset = new VectorInt();
			}
			else
			{
				var offsetInSamplePoints = centerV.Divide(samplePointDelta);

				// Determine the number of full blocks, and number of samplePoints remaining
				mapBlockOffset = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
			}

			// Create a MapAreaInfo2 using the center point, mapBlockOffset and canvasControlOffset.
			var centerP = new RPoint(centerV);
			var result = new MapAreaInfo2(centerP, mapAreaInfo.Subdivision, mapAreaInfo.Precision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		public static RRectangle NormalizeCoordsWithSPD(RRectangle coords, RSize samplePointDelta)
		{
			RRectangle rCoords;

			if (coords.Exponent < samplePointDelta.Exponent)
			{
				// If the numerator is even, we can make the exponent, less negative.
				rCoords = Reducer.Reduce(coords);

				if (rCoords.Exponent < samplePointDelta.Exponent)
				{
					throw new InvalidOperationException("Cannot Normalize coords having a higher (more negative) exponent than the SamplePointDelta.");
				}
			}
			else
			{
				rCoords = coords;
			}

			if (rCoords.Exponent == samplePointDelta.Exponent)
			{
				return rCoords;
			}

			var factor = (long)Math.Pow(2, samplePointDelta.Exponent - rCoords.Exponent);

			var newVals = rCoords.Values.Select(v => v * factor).ToArray();

			var result = new RRectangle(newVals, samplePointDelta.Exponent);

			return result;
		}

		public MapAreaInfo2 GetMapAreaInfo(RRectangle coords, SizeInt canvasSize)
		{
			var oldAreaInfo = GetMapAreaInfoV1(coords, canvasSize);
			var mapAreaInfo = Convert(oldAreaInfo);
			return mapAreaInfo;
		}

		#endregion

		#region GetMapAreaInfo Methods - V1

		// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		public MapAreaInfo GetMapAreaInfoV1(RRectangle coords, SizeInt canvasSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = GetSamplePointDelta(coords, displaySize, ToleranceFactor, out var wToHRatio);
			ReportSamplePointRatios(coords, displaySize, wToHRatio);

			// The samplePointDelta may require the coordinates to be adjusted.
			var mapSize = samplePointDelta.Scale(displaySize);
			var adjPos1 = RNormalizer.Normalize(coords.Position, mapSize, out var adjMapSize1);
			var adjCoords1 = new RRectangle(adjPos1, adjMapSize1);

			var uCoords = RNormalizer.Normalize(adjCoords1, samplePointDelta, out var uSpd);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = GetMapBlockOffset(uCoords.Position, uSpd, BlockSize, out var canvasControlOffset);

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

		private RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize, double toleranceFactor, out double wToHRatio)
		{
			var spdH = BigIntegerHelper.Divide(coords.Width, canvasSize.Width, toleranceFactor);
			var spdV = BigIntegerHelper.Divide(coords.Height, canvasSize.Height, toleranceFactor);

			var nH = RNormalizer.Normalize(spdH, spdV, out var nV);

			// Take the smallest value
			var result = new RSize(RValue.Min(nH, nV));

			wToHRatio = nH.DivideLimitedPrecision(nV);

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

		private BigVector GetMapBlockOffset(RPoint mapPosition, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset/*, out RPoint newPosition*/)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin = left, bottom. Map origin = left, bottom.

			if (mapPosition.IsZero())
			{
				canvasControlOffset = new VectorInt();
				return new BigVector();
			}

			var distance = new RVector(mapPosition);
			var offsetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta/*, out newPosition*/);

			var result = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);

			return result;
		}

		private BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta/*, out RPoint newPosition*/)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			return offsetInSamplePoints;
		}

		#endregion
	}
}
