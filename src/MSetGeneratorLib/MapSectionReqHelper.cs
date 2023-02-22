using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Runtime.Intrinsics;

namespace MSetGeneratorLib
{
	internal static class MapSectionReqHelper
	{

		public static MapSectionRequestStruct GetRequestStruct(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, uint threshold)
		{
			var result = new MapSectionRequestStruct();

			if (!iterationState.RowNumber.HasValue)
			{
				throw new ArgumentException("The iteration state must have a non-null row number.");
			}

			result.RowNumber = iterationState.RowNumber.Value;

			result.BitsBeforeBinaryPoint = apFixedPointFormat.BitsBeforeBinaryPoint;
			result.LimbCount = apFixedPointFormat.LimbCount;
			result.NumberOfFractionalBits = apFixedPointFormat.NumberOfFractionalBits;
			result.TotalBits = apFixedPointFormat.TotalBits;
			result.TargetExponent = apFixedPointFormat.TargetExponent;

			result.Lanes = Vector256<int>.Count;
			result.VectorsPerRow = iterationState.VectorsPerRow;

			result.subdivisionId = ObjectId.Empty.ToString();

			result.blockSizeWidth = iterationState.ValuesPerRow;
			result.blockSizeHeight = iterationState.RowCount;

			result.maxIterations = iterationState.TargetIterationsVector.GetElement(0);
			result.threshold = (int)threshold;
			result.iterationsPerStep = -1;

			return result;
		}

		public static MapSectionRequestStruct GetRequestStruct(MapSectionRequest mapSectionRequest)
		{
			var result = new MapSectionRequestStruct();
			return result;
		}

		//public static MapSectionRequestStruct GetRequestStruct(MapSectionRequest mapSectionRequest)
		//{
		//	MapSectionRequestStruct result = new MapSectionRequestStruct();

		//	result.subdivisionId = mapSectionRequest.SubdivisionId;

		//	// BlockPosition

		//	var blockPositionLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.BlockPosition.Values);

		//	result.blockPositionX = blockPositionLongs[0];
		//	result.blockPositionY = blockPositionLongs[1];

		//	// RPointDto Position

		//	var positionLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.MapPosition.Values);

		//	result.positionX = positionLongs[0];
		//	result.positionY = positionLongs[1];
		//	result.positionExponent = mapSectionRequest.MapPosition.Exponent;
		//	result.positionPrecision = mapSectionRequest.Precision;

		//	// BlockSize
		//	result.blockSizeWidth = mapSectionRequest.BlockSize.Width;
		//	result.blockSizeHeight = mapSectionRequest.BlockSize.Height;

		//	// RSizeDto SamplePointDelta;

		//	var spdLongs = BigIntegerHelper.ToLongsDeprecated(mapSectionRequest.SamplePointDelta.Values);

		//	result.samplePointDeltaWidth = spdLongs[0];
		//	result.samplePointDeltaHeight = spdLongs[1];
		//	result.samplePointDeltaExponent = mapSectionRequest.SamplePointDelta.Exponent;

		//	// MapCalcSettings
		//	result.maxIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
		//	result.threshold = mapSectionRequest.MapCalcSettings.Threshold;
		//	result.iterationsPerStep = mapSectionRequest.MapCalcSettings.RequestsPerJob;

		//	return result;
		//}

	}
}
