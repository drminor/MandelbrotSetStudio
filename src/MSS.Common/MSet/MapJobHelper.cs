using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace MSS.Common
{
	public class MapJobHelper
	{
		#region Private Properties

		private readonly SubdivisonProvider _subdivisonProvider;

		private readonly bool _useDetailedDebug = false;

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

		public Job BuildHomeJob(OwnerType jobOwnerType, MapAreaInfo2 mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings)
		{
			ObjectId? parentJobId = null;
			ObjectId ownerId = ObjectId.Empty;
			var transformType = TransformType.Home;
			RectangleInt? newArea = null;

			var result = BuildJob(parentJobId, ownerId, jobOwnerType, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea);
			return result;
		}

		public Job BuildJob(ObjectId? parentJobId, ObjectId ownerId, OwnerType jobOwnerType, MapAreaInfo2 mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			var mapAreaInfoWithRegisteredSub = RegisterTheSubdivision(mapAreaInfo);

			var jobName = GetJobName(transformType);
			var job = new Job(ownerId, jobOwnerType, parentJobId, jobName, transformType, newArea, mapAreaInfoWithRegisteredSub, colorBandSetId, mapCalcSettings);

			return job;
		}

		public MapAreaInfo2 RegisterTheSubdivision(MapAreaInfo2 value)
		{
			if (value.Subdivision.Id == ObjectId.Empty)
			{
				var originalUnSavedSubdivision = value.Subdivision;
				var totalMapBlockOffset = value.MapBlockOffset.Tranlate(originalUnSavedSubdivision.BaseMapPosition);
				var newSubdivision = _subdivisonProvider.GetSubdivision(originalUnSavedSubdivision.SamplePointDelta, totalMapBlockOffset, out var localMapBlockOffset);
				var result = new MapAreaInfo2(value.PositionAndDelta, newSubdivision, value.Precision, localMapBlockOffset, value.CanvasControlOffset);

				return result;
			}
			else
			{
				return value;
			}
		}

		public static string GetJobName(TransformType transformType)
		{
			var result = transformType.ToString();
			return result;
		}

		#endregion

		#region GetMapAreaInfo

		// Pan
		public MapAreaInfo2 GetMapAreaInfoPan(MapAreaInfo2 currentArea, VectorInt panAmount)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var transPd = RMapHelper.GetNewCenterPoint(currentArea.PositionAndDelta, panAmount);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(transPd, blockSize, out var canvasControlOffset);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(transPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(transPd.Exponent);

			var result = new MapAreaInfo2(transPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		// Zoom
		public MapAreaInfo2 GetMapAreaInfoZoomCenter(MapAreaInfo2 currentArea, double factor, out double diagReciprocal)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var scaledPd = RMapHelper.GetNewSamplePointDelta(currentArea.PositionAndDelta, factor, out diagReciprocal);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(scaledPd, blockSize, out var canvasControlOffset);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(scaledPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(scaledPd.Exponent);

			var result = new MapAreaInfo2(scaledPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		// Pan and Zoom
		public MapAreaInfo2 GetMapAreaInfoZoomPoint(MapAreaInfo2 currentArea, VectorInt panAmount, double factor, out double diagReciprocal)
		{
			var blockSize = currentArea.Subdivision.BlockSize;

			var transPd = RMapHelper.GetNewCenterPoint(currentArea.PositionAndDelta, panAmount);
			var scaledAndTransPd = RMapHelper.GetNewSamplePointDelta(transPd, factor, out diagReciprocal);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(scaledAndTransPd, blockSize, out var canvasControlOffset);
			
			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(scaledAndTransPd.SamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(scaledAndTransPd.Exponent);

			var result = new MapAreaInfo2(scaledAndTransPd, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

			return result;
		}

		#endregion

		#region MapAreaInfo2 Support

		//public static MapAreaInfo GetMapAreaWithSize(MapAreaInfo2 mapAreaInfoV2, SizeDbl canvasSize)
		//{
		//	var rPointAndDelta = mapAreaInfoV2.PositionAndDelta;

		//	var rArea = ConvertScreenRectToMapCenterCoords(canvasSize, rPointAndDelta.SamplePointDelta);

		//	//var coords = rArea.Translate(rPointAndDelta.Position);

		//	//// Calculate the total number of sample points from the origin to the lower, left corner of the map's coordinates and the Subdivision origin (i.e., BaseMapBlockOffset.)
		//	//// Determine the number of full blocks, and number of samplePoints remaining
		//	//BigVector mapBlockOffset;
		//	//VectorInt canvasControlOffset;

		//	//if (coords.Position.IsZero())
		//	//{
		//	//	mapBlockOffset = new BigVector();
		//	//	canvasControlOffset = new VectorInt();
		//	//}
		//	//else
		//	//{
		//	//	var positionV = new RVector(coords.Position);
		//	//	var offsetInSamplePoints = positionV.Divide(rPointAndDelta.SamplePointDelta);
		//	//	var blockSize = mapAreaInfoV2.Subdivision.BlockSize;
		//	//	mapBlockOffset = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
		//	//}

		//	// Add to it the CenterPoint, to get a RRectangle which is the map's coordinates
		//	var nrmArea = RNormalizer.Normalize(rArea, rPointAndDelta.Position, out var nrmMapCenterPoint);
		//	var coords = nrmArea.Translate(nrmMapCenterPoint);

		//	// Calculate the total number of sample points from the origin to the lower, left corner of the map's coordinates and the Subdivision origin (i.e., BaseMapBlockOffset.)
		//	var adjCoords = RNormalizer.Normalize(coords, rPointAndDelta.SamplePointDelta, out var nrmSamplePointDelta);

		//	if (nrmSamplePointDelta.Exponent != rPointAndDelta.Exponent)
		//	{
		//		// Consider using the version of Normalize that attempts to keep one of the exponents the same. See method: GetMapAreaInfoScaleConstant
		//		throw new InvalidOperationException("Cannot create a MapAreaWithSize from the given mapAreaInfoV2 and CanvasSize: The existing subdivision is not compatible. Please use the GetMapAreaWithSizeFat method instead.");
		//	}

		//	var positionV = new RVector(adjCoords.Position);

		//	// Determine the number of full blocks, and number of samplePoints remaining
		//	BigVector mapBlockOffset;
		//	VectorInt canvasControlOffset;

		//	if (positionV.IsZero())
		//	{
		//		mapBlockOffset = new BigVector();
		//		canvasControlOffset = new VectorInt();
		//	}
		//	else
		//	{
		//		var offsetInSamplePoints = positionV.Divide(rPointAndDelta.SamplePointDelta);
		//		var blockSize = mapAreaInfoV2.Subdivision.BlockSize;
		//		mapBlockOffset = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
		//	}

		//	var binaryPrecision = Math.Abs(nrmSamplePointDelta.Exponent);

		//	var result = new MapAreaInfo(coords, canvasSize, mapAreaInfoV2.Subdivision, binaryPrecision, mapBlockOffset, canvasControlOffset);

		//	return result;
		//}

		public MapAreaInfo GetMapAreaWithSizeFat(MapAreaInfo2 mapAreaInfoV2, SizeDbl canvasSize)
		{
			var rPointAndDelta = mapAreaInfoV2.PositionAndDelta;

			var rArea = ConvertScreenRectToMapCenterCoords(canvasSize, rPointAndDelta.SamplePointDelta);

			// Add to it the CenterPoint, to get a RRectangle which is the map's coordinates
			var nrmArea = RNormalizer.Normalize(rArea, rPointAndDelta.Position, out var nrmMapCenterPoint);
			var coords = nrmArea.Translate(nrmMapCenterPoint);

			// Calculate the total number of sample points from the origin to the lower, left corner of the map's coordinates and the Subdivision origin (i.e., BaseMapBlockOffset.)
			var adjCoords = RNormalizer.Normalize(coords, rPointAndDelta.SamplePointDelta, out var nrmSamplePointDelta);

			if (nrmSamplePointDelta.Exponent != rPointAndDelta.Exponent)
			{
				Debug.WriteLine($"INFO: GetMapAreaWithSizeFat is not using the existing subdivision.");
			}

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
				var offsetInSamplePoints = positionV.Divide(nrmSamplePointDelta);
				var blockSize = mapAreaInfoV2.Subdivision.BlockSize;
				mapBlockOffset = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);
			}

			var binaryPrecision = Math.Abs(nrmSamplePointDelta.Exponent);

			// Find or create a subdivision record in the database.
			var subdivision = _subdivisonProvider.GetSubdivision(nrmSamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			if (_useDetailedDebug) CheckSubdivisionConsistency(mapAreaInfoV2.Subdivision, subdivision, nrmMapCenterPoint.Exponent, nrmSamplePointDelta.Exponent);

			var result = new MapAreaInfo(adjCoords, canvasSize, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset, mapAreaInfoV2.Subdivision.Id);

			return result;
		}

		public static MapAreaInfo2 Convert(MapAreaInfo mapAreaInfo)
		{
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var blockSize = mapAreaInfo.Subdivision.BlockSize;

			// The current SamplePointDelta remains the same.
			// The coords are updated to have the same exponent.
			var nrmCoords = NormalizeCoordsWithSPD(mapAreaInfo.Coords, samplePointDelta);

			var centerPoint = nrmCoords.GetCenter();
			var convertedPd = new RPointAndDelta(centerPoint, samplePointDelta);

			var mapBlockOffset = RMapHelper.GetMapBlockOffset(convertedPd, blockSize, out var canvasControlOffset);

			var result = new MapAreaInfo2(convertedPd, mapAreaInfo.Subdivision, mapAreaInfo.Precision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		public static RRectangle ConvertScreenRectToMapCenterCoords(SizeDbl canvasSize, RSize samplePointDelta)
		{
			// Create a rectangle centered at position: x = 0, y = 0
			// Having the same width and height as the given canvasSize.
			var half = new PointDbl(canvasSize.Width / 2, canvasSize.Height / 2);
			var area = new RectangleDbl(half.Invert(), canvasSize);

			// Multiply the area by the SamplePointDelta to get map coordiates
			var rArea = new RRectangle(
				new BigInteger(Math.Round(area.X1 * 2)),
				new BigInteger(Math.Round(area.X2 * 2)),
				new BigInteger(Math.Round(area.Y1 * 2)),
				new BigInteger(Math.Round(area.Y2 * 2)),
				-1,
				RMapConstants.DEFAULT_PRECISION
			);

			var result = rArea.Scale(samplePointDelta);

			return result;
		}

		private static RRectangle NormalizeCoordsWithSPD(RRectangle coords, RSize samplePointDelta)
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

			var factor = (long)Math.Pow(2, rCoords.Exponent - samplePointDelta.Exponent);

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

		[Conditional("DEBUG2")]
		private void CheckSubdivisionConsistency(Subdivision original, Subdivision result, int normalizedPositionExponent, int normalizedSpdExponent)
		{
			Debug.WriteLine($"While calculating the MapAreaWithSize. Original SubdivisionId: {original.Id}, Result SubdivisionId: {result.Id}. " +
				$"Exponents (Original-0, Position-1, SPD-2, Result-3): {original.SamplePointDelta.Exponent}, {normalizedPositionExponent}, {normalizedSpdExponent}, {result.SamplePointDelta.Exponent}.");
		}

		#endregion

		#region GetMapAreaInfo Methods - V1

		// Convert the screen coordinates given by screenArea into map coordinates,
		// then move these map coordiates by the x and y distances specified in the current MapPosition.
		public RRectangle GetMapCoords(RectangleInt screenArea, RPoint mapPosition, RSize samplePointDelta)
		{
			// Convert to map coordinates.

			//var rArea = ScaleByRsize(screenArea, samplePointDelta);
			var rArea = new RRectangle(screenArea);
			rArea = rArea.Scale(samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, mapPosition, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			//Debug.WriteLine($"GetMapCoords is receiving area: {screenArea}.");
			//Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		// Calculate the MapBlockOffset and CanvasControlOffset while keeping the SamplePointDelta, constant.
		public MapAreaInfo GetMapAreaInfoScaleConstant(RRectangle coords, Subdivision subdivision, ObjectId originalSourceSubdivisionId, SizeDbl canvasSize)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			//var updatedCoords = coords.Clone();

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = GetMapBlockOffset(coords.Position, samplePointDelta, BlockSize, out var canvasControlOffset);

			var newPosition = samplePointDelta.Scale(mapBlockOffset.Scale(BlockSize).Tranlate(canvasControlOffset));

			var adjPos = RNormalizer.Normalize(newPosition, coords.Size, out var adjMapSize);
			var newCoords = new RRectangle(new RPoint(adjPos), adjMapSize);

			if (_useDetailedDebug) ReportCoordsDiff(coords, newCoords, "for a new display size");

			//var localMapBlockOffset = GetLocalMapBlockOffset(mapBlockOffset, subdivision);
			var newSubdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			Debug.Assert(newSubdivision == subdivision, "GetMapAreaInfo for CanvasSize Update is producing a new Subdivision");

			var binaryPrecision = GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);

			var result = new MapAreaInfo(newCoords, canvasSize, newSubdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset, originalSourceSubdivisionId);

			return result;
		}

		public int GetBinaryPrecision(RRectangle coords, RSize samplePointDelta, out int decimalPrecision)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(coords.Right, coords.Left, out decimalPrecision);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			return binaryPrecision;
		}

		#endregion

		#region GetMapAreaInfo Methods - V1 - Depreciated

		// Calculate the SamplePointDelta, MapBlockOffset, CanvasControlOffset, using the specified coordinates and display size
		private MapAreaInfo GetMapAreaInfoV1(RRectangle coords, SizeInt canvasSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = GetSamplePointDelta(coords, displaySize, ToleranceFactor, out var wToHRatio);
			if (_useDetailedDebug) ReportSamplePointRatios(coords, displaySize, wToHRatio);

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

			if (_useDetailedDebug) ReportCoordsDiff(coords, newCoords, "for a new Job");

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(uSpd, mapBlockOffset, out var localMapBlockOffset);

			var binaryPrecision = RMapHelper.GetBinaryPrecision(newCoords, subdivision.SamplePointDelta, out _);
			var result = new MapAreaInfo(newCoords, new SizeDbl(canvasSize), subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset, subdivision.Id);

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
