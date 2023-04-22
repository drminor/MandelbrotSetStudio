using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Linq;

namespace MSS.Common
{
	public class MapJobHelper2
	{
		#region Private Properties

		private readonly SubdivisonProvider _subdivisonProvider;

		private readonly MapJobHelper _oldJobHelper;

		#endregion

		#region Constructor

		public MapJobHelper2(SubdivisonProvider subdivisonProvider, double toleranceFactor, SizeInt blockSize)
		{
			_subdivisonProvider = subdivisonProvider;
			BlockSize = blockSize;
			ToleranceFactor = toleranceFactor; // SamplePointDelta values are calculated to within 10 pixels of the display area.

			_oldJobHelper = new MapJobHelper(subdivisonProvider, toleranceFactor, blockSize);
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
			var currentMapCenter = currentArea.MapCenter;
			var currentSamplePointDelta = currentArea.SamplePointDelta;
			var blockSize = currentArea.Subdivision.BlockSize;

			var rPanAmount = panAmount.Scale(currentSamplePointDelta);
			var newMapCenter = currentMapCenter.Translate(rPanAmount);
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(newMapCenter, currentSamplePointDelta, blockSize, out var canvasControlOffset);

			// Get a subdivision record from the database.
			var subdivision = _subdivisonProvider.GetSubdivision(currentSamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var binaryPrecision = Math.Abs(currentSamplePointDelta.Exponent);

			var result = new MapAreaInfo2(newMapCenter, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

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

		// Pan and Zoom
		//public MapAreaInfo2 GetMapAreaInfo(MapAreaInfo2 currentArea, VectorInt zoomPoint, int factor)
		//{
		//	var currentMapCenter = currentArea.MapCenter;
		//	var currentSamplePointDelta = currentArea.SamplePointDelta;
		//	var blockSize = currentArea.Subdivision.BlockSize;

		//	var rZoomPoint = zoomPoint.Scale(currentSamplePointDelta);
		//	var newMapCenter = currentMapCenter.Translate(rZoomPoint);

		//	var newSamplePointDelta = new RSize(currentSamplePointDelta.Values, currentSamplePointDelta.Exponent - factor);

		//	var adjMapCenter = RNormalizer.Normalize(newMapCenter, newSamplePointDelta, out var adjSamplePointDelta);
		//	var mapBlockOffset = RMapHelper.GetMapBlockOffset(adjMapCenter, adjSamplePointDelta, blockSize, out var canvasControlOffset);

		//	// Get a subdivision record from the database.
		//	var subdivision = _subdivisonProvider.GetSubdivision(adjSamplePointDelta, mapBlockOffset, out var localMapBlockOffset);
		//	var binaryPrecision = Math.Abs(adjSamplePointDelta.Exponent);

		//	var result = new MapAreaInfo2(adjMapCenter, subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset);

		//	return result;
		//}

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
			var oldAreaInfo = _oldJobHelper.GetMapAreaInfo(coords, canvasSize);
			var mapAreaInfo = Convert(oldAreaInfo);
			return mapAreaInfo;
		}

		#endregion
	}
}
