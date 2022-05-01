using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	public static class MapSectionHelper
	{
		private const int VALUE_FACTOR = 10000;

		private static readonly DtoMapper _dtoMapper = new DtoMapper();

		#region Create MapSectionRequests

		public static IList<MapSectionRequest> CreateSectionRequests(Job job, IList<MapSection>? emptyMapSections)
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

		private static IList<MapSectionRequest> CreateSectionRequests(Job job)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			foreach (var screenPosition in ScreenTypeHelper.Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public static IList<MapSection> CreateEmptyMapSections(Job job)
		{
			var emptyCountsData = new int[0];

			var result = new List<MapSection>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			var subdivisionId = job.Subdivision.Id.ToString();
			var targetIterations = job.MSetInfo.MapCalcSettings.TargetIterations;

			foreach (var screenPosition in ScreenTypeHelper.Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, job.MapBlockOffset, out var isInverted);
				var mapSection = new MapSection(screenPosition, job.Subdivision.BlockSize, emptyCountsData, targetIterations, 
					subdivisionId, repoPosition, isInverted, BuildHistogram);
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
				Counts = null,
				DoneFlags = null,
				ZValues = null,
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

		public static MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, BigVector mapBlockOffset)
		{
			var repoBlockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var isInverted = mapSectionRequest.IsInverted;
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var blockSize = mapSectionRequest.BlockSize;
			var mapSection = new MapSection(screenPosition, blockSize, mapSectionResponse.Counts, mapSectionResponse.MapCalcSettings.TargetIterations, 
				mapSectionRequest.SubdivisionId, repoBlockPosition, isInverted, BuildHistogram);

			return mapSection;
		}

		public static byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap, bool invert, bool useEscapeVelocities)
		{
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
					countVal = Math.DivRem(countVal, VALUE_FACTOR, out var ev);

					//var escapeVel = useEscapeVelocities ? Math.Max(1, ev / (double)VALUE_FACTOR) : 0;
					var escapeVel = useEscapeVelocities ? ev / (double)VALUE_FACTOR : 0;

					if (escapeVel > 1.0)
					{
						Debug.WriteLine($"The Escape Velocity is greater that 1.0");
					}

					colorMap.PlaceColor(countVal, escapeVel, new Span<byte>(result, curResultPtr, 4));
					curResultPtr += 4;
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

		private static IHistogram BuildHistogram(int[] counts)
		{
			return new HistogramALow(counts.Select(x => (int)Math.Round(x / (double)VALUE_FACTOR)));
		}

		#endregion
	}
}
