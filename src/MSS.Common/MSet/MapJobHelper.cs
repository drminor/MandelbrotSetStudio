using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Numerics;

namespace MSS.Common
{
	public class MapJobHelper
	{
		private const int TERMINAL_LIMB_COUNT = 2;

		//private static readonly BigVector TERMINAL_SUBDIV_SIZE = new BigVector(BigInteger.Pow(2, 62));
		private static readonly BigVector TERMINAL_SUBDIV_SIZE = new BigVector(BigInteger.Pow(2, TERMINAL_LIMB_COUNT * ApFixedPointFormat.EFFECTIVE_BITS_PER_LIMB));

		private readonly IMapSectionAdapter _mapSectionAdapter;

		public MapJobHelper(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
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

		public static Job BuildJob(ObjectId? parentJobId, ObjectId projectId, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			TransformType transformType, RectangleInt? newArea)
		{
			// Determine how much of the canvas control can be covered by the new map.
			var canvasSize = mapAreaInfo.CanvasSize;

			//var displaySize = RMapHelper.GetCanvasSize(newArea.Size, canvasSize);

			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize;

			var canvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(displaySize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);

			var jobName = GetJobName(transformType);

			var job = new Job(parentJobId, projectId, jobName, transformType, newArea, mapAreaInfo, canvasSizeInBlocks, colorBandSetId, mapCalcSettings);

			return job;
		}

		// NOTE: This is really the negative of what is happening (its the Y axis that needs flipping,
		// however this allows us to do a Translate (i.e., add, instead of a Sub or Negate and Tranlate.) 
		private static readonly SizeDbl FLIP_VERTICALLY_AND_HALVE = new SizeDbl(0.5, 0.5);

		public static MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeDbl canvasSize, SizeDbl newCanvasSize, Subdivision subdivision, SizeInt blockSize)
		{
			var samplePointDelta = subdivision.SamplePointDelta;
			//var diff = newCanvasSize.Sub(canvasSize);
			var diff = canvasSize.Sub(newCanvasSize);
			diff = diff.Scale(FLIP_VERTICALLY_AND_HALVE);
			var mapDiff = samplePointDelta.Scale(diff.Round());
			
			var mapPos = coords.Position;
			var nrmPos = RNormalizer.Normalize(mapPos, mapDiff, out var nrmDiff);
			var newPos = nrmPos.Translate(nrmDiff);

			Debug.WriteLine($"GetMapArea is moving the pos from {mapPos} to {newPos}.");

			var newCoords = RMapHelper.GetMapCoords(newCanvasSize.Round(), newPos, samplePointDelta);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			Debug.Assert(canvasControlOffset.X >= 0 && canvasControlOffset.Y >= 0, "GetMapBlockOffset is returning a canvasControlOffset with a negative w or h value.");

			// TODO: Check the calculated precision as the new Map Coordinates are calculated.
			var binaryPrecision = RValueHelper.GetBinaryPrecision(newCoords.Right, newCoords.Left, out _);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			// TODO: For now, assume that the localMapBlockOffset is the same as the mapBlockOffset.
			//var subdivision = GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);
			var localMapBlockOffset = mapBlockOffset;

			var result = new MapAreaInfo(newCoords, newCanvasSize.Round(), subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
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

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			// TODO: Check the calculated precision as the new Map Coordinates are calculated.
			var binaryPrecision = RValueHelper.GetBinaryPrecision(updatedCoords.Right, updatedCoords.Left, out _);

			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			var result = new MapAreaInfo(updatedCoords, canvasSize, subdivision, localMapBlockOffset, binaryPrecision, canvasControlOffset);

			return result;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		public Subdivision GetSubdivision(RSize samplePointDelta, BigVector mapBlockOffset, out BigVector localMapBlockOffset)
		{
			var estimatedBaseMapPosition = GetBaseMapPosition(mapBlockOffset, out localMapBlockOffset);

			if (! _mapSectionAdapter.TryGetSubdivision(samplePointDelta, estimatedBaseMapPosition, out var result))
			{
				var subdivision = new Subdivision(samplePointDelta, estimatedBaseMapPosition);
				result = _mapSectionAdapter.InsertSubdivision(subdivision);
			}

			return result;
		}

		public BigVector GetBaseMapPosition(BigVector mapBlockOffset, out BigVector localMapBlockOffset)
		{
			var quotient = mapBlockOffset.DivRem(TERMINAL_SUBDIV_SIZE, out localMapBlockOffset);

			var result = quotient.Scale(TERMINAL_SUBDIV_SIZE);

			return result;
		} 

		public static string GetJobName(TransformType transformType)
		{
			//var result = transformType == TransformType.Home ? "Home" : transformType.ToString();
			var result = transformType.ToString();
			return result;
		}

		//public BigVector GetLocalMapBlockOffset(BigVector mapBlockOffset, RVector baseMapPosition)
		//{
		//	var rMapBlockOffset = new RVector(mapBlockOffset);

		//	var scaledBaseMapPosition = baseMapPosition.Scale(new RSize(TERMINAL_LIMB_COUNT * ApFixedPointFormat.EFFECTIVE_BITS_PER_LIMB));

		//	var result = scaledBaseMapPosition.Diff(rMapBlockOffset);

		//	//return result;

		//	return new BigVector();
		//}

		//public RVector GetBaseMapPosition(BigVector mapBlockOffset, int precisionInBinaryDigits)
		//{
		//	RVector result;

		//	var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, precisionInBinaryDigits);

		//	if (apFixedPointFormat.LimbCount <= TERMINAL_LIMB_COUNT)
		//	{
		//		result = new RVector();
		//	}
		//	else
		//	{
		//		//var baseLength = apFixedPointFormat.LimbCount - 3;

		//		var bitsPerLimb = ApFixedPointFormat.EFFECTIVE_BITS_PER_LIMB;
		//		var currentExponent = apFixedPointFormat.TargetExponent;
		//		var baseExponent = currentExponent - TERMINAL_LIMB_COUNT * bitsPerLimb;

		//		var x = new RValue(mapBlockOffset.X, 0);
		//		var fp31ValX = FP31ValHelper.CreateFP31Val(x, apFixedPointFormat);
		//		var baseX = new FP31Val(fp31ValX.Mantissa.Skip(TERMINAL_LIMB_COUNT).ToArray(), baseExponent, fp31ValX.BitsBeforeBP, fp31ValX.Precision);

		//		var y = new RValue(mapBlockOffset.Y, 0);
		//		var fp31ValY = FP31ValHelper.CreateFP31Val(y, apFixedPointFormat);
		//		var baseY = new FP31Val(fp31ValY.Mantissa.Skip(TERMINAL_LIMB_COUNT).ToArray(), baseExponent, fp31ValY.BitsBeforeBP, fp31ValY.Precision);

		//		var baseRValX = FP31ValHelper.CreateRValue(baseX);
		//		var baseRValY = FP31ValHelper.CreateRValue(baseY);

		//		result = new RVector(baseRValX.Value, baseRValY.Value, fp31ValX.Exponent - 62);
		//	}

		//	return result;
		//}

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
