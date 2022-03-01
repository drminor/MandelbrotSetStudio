using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;

namespace MSetExplorer
{
	public static class MapSectionHelper
	{
		private static readonly DtoMapper _dtoMapper = new DtoMapper();

		//var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, _job.MapBlockOffset, out var isInverted);
		public static MapSectionRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapBlockOffset, out var isInverted);
			var result = CreateRequest(repoPosition, isInverted, subdivision, mapCalcSettings);

			return result;
		}

		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="subdivision"></param>
		/// <param name="repoPosition"></param>
		/// <param name="isInverted"></param>
		/// <param name="mapCalcSettings"></param>
		/// <param name="mapPosition"></param>
		/// <returns></returns>
		public static MapSectionRequest CreateRequest(BigVector repoPosition, bool isInverted, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var mapPosition = GetMapPosition(subdivision, repoPosition);
			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = _dtoMapper.MapTo(repoPosition),
				BlockSize = subdivision.BlockSize,
				Position = _dtoMapper.MapTo(mapPosition),
				SamplePointsDelta = _dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings,
				IsInverted = isInverted
			};

			return mapSectionRequest;
		}

		private static RPoint GetMapPosition(Subdivision subdivision, BigVector blockPosition)
		{
			var nrmSubdivionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);

			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = blockPosition.Scale(subdivision.BlockSize);

			// Convert sample points to map coordinates.
			var mapDistance = nrmSamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);

			// Add the map distance to the sub division origin
			var mapPosition = nrmSubdivionPosition.Translate(mapDistance);

			return mapPosition;
		}

		public static MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, BigVector mapBlockOffset, ColorMap colorMap)
		{
			var repoBlockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, mapSectionRequest.IsInverted, mapBlockOffset);
			//Debug.WriteLine($"MapLoader handling response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {mapSectionRequest.IsInverted}.");

			var blockSize = mapSectionRequest.BlockSize;
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, colorMap, !mapSectionRequest.IsInverted);
			var mapSection = new MapSection(screenPosition, blockSize, pixels1d, mapSectionRequest.SubdivisionId, repoBlockPosition);

			return mapSection;
		}

		private static byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap, bool invert)
		{
			if (counts == null)
			{
				return null;
			}

			var numberofCells = blockSize.NumberOfCells;
			var result = new byte[4 * numberofCells];

			for (var rowPtr = 0; rowPtr < blockSize.Height; rowPtr++)
			{
				// Calculate the array index for the beginning of this destination and source row.
				var resultRowPtr = GetResultRowPtr(blockSize.Height - 1, rowPtr, invert);

				var curResultPtr = resultRowPtr * blockSize.Width * 4;
				var curSourcePtr = rowPtr * blockSize.Width;

				for (var colPtr = 0; colPtr < blockSize.Width; colPtr++)
				{
					var countVal = counts[curSourcePtr++];
					countVal = Math.DivRem(countVal, 1000, out var ev);
					var escapeVel = ev / 1000d;
					var colorComps = colorMap.GetColor(countVal, escapeVel);

					for (var j = 2; j > -1; j--)
					{
						result[curResultPtr++] = colorComps[j];
					}
					result[curResultPtr++] = 255;
				}
			}

			return result;
		}

		private static int GetResultRowPtr(int maxRowIndex, int rowPtr, bool invert)
		{
			// The Source's origin is at the bottom, left.
			// If inverted, the Destination's origin is at the top, left, otherwise bottom, left. 
			var result = invert ? maxRowIndex - rowPtr : rowPtr;
			return result;
		}



	}
}
