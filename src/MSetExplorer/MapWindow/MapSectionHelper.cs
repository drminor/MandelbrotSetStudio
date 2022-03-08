using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	public static class MapSectionHelper
	{
		private static readonly DtoMapper _dtoMapper = new DtoMapper();

		#region Create MapSectionRequests

		public static IList<MapSectionRequest> CreateSectionRequests(Job job, IList<MapSection> emptyMapSections)
		{
			if (emptyMapSections == null)
			{
				return CreateSectionRequests(job);
			}
			else
			{
				var result = new List<MapSectionRequest>();

				Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

				foreach (var mapSection in emptyMapSections)
				{
					var screenPosition = mapSection.BlockPosition;
					var mapSectionRequest = CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
					result.Add(mapSectionRequest);
				}

				return result;
			}
		}

		public static IList<MapSectionRequest> CreateSectionRequests(Job job)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			//for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			//{
			//	for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
			//	{
			//		var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
			//		var mapSectionRequest = CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
			//		result.Add(mapSectionRequest);
			//	}
			//}

			foreach (var screenPosition in ScreenTypeHelper.Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public static IList<MapSection> CreateEmptyMapSections(Job job)
		{
			var emptyPixelData = new byte[0];
			var result = new List<MapSection>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			//for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			//{
			//	for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
			//	{
			//		var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
			//		var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, job.MapBlockOffset, out var isInverted);
			//		var mapSection = new MapSection(screenPosition, job.Subdivision.BlockSize, emptyPixelData, job.Subdivision.Id.ToString(), repoPosition, isInverted);
			//		result.Add(mapSection);
			//	}
			//}

			foreach (var screenPosition in ScreenTypeHelper.Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, job.MapBlockOffset, out var isInverted);
				var mapSection = new MapSection(screenPosition, job.Subdivision.BlockSize, emptyPixelData, job.Subdivision.Id.ToString(), repoPosition, isInverted);
				result.Add(mapSection);
			}

			return result;
		}

		#endregion

		#region Create Single MapSectionRequest

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

		#endregion

		#region Create MapSection

		public static MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, BigVector mapBlockOffset, ColorMap colorMap)
		{
			var repoBlockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var isInverted = mapSectionRequest.IsInverted;
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var blockSize = mapSectionRequest.BlockSize;
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, colorMap, !isInverted);
			var mapSection = new MapSection(screenPosition, blockSize, pixels1d, mapSectionRequest.SubdivisionId, repoBlockPosition, isInverted);

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

		#endregion
	}
}
