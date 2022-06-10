using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public class MapSectionHelper
	{
		private const int VALUE_FACTOR = 10000;
		private readonly DtoMapper _dtoMapper = new();

		#region Create MapSectionRequests

		public IList<MapSectionRequest> CreateSectionRequests(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection>? emptyMapSections)
		{
			if (emptyMapSections == null)
			{
				return CreateSectionRequests(jobAreaAndCalcSettings);
			}
			else
			{
				var result = new List<MapSectionRequest>();

				Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

				var jobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
				var mapCalcSettings = jobAreaAndCalcSettings.MapCalcSettings;

				foreach (var mapSection in emptyMapSections)
				{
					var screenPosition = mapSection.BlockPosition;
					var mapSectionRequest = CreateRequest(screenPosition, jobAreaInfo.MapBlockOffset, jobAreaInfo.Subdivision, mapCalcSettings);
					result.Add(mapSectionRequest);
				}

				return result;
			}
		}

		public IList<MapSectionRequest> CreateSectionRequests(JobAreaAndCalcSettings jobAreaAndCalcSettings)
		{
			var result = new List<MapSectionRequest>();

			var jobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
			var mapCalcSettings = jobAreaAndCalcSettings.MapCalcSettings;

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(jobAreaInfo.CanvasSize, jobAreaInfo.CanvasControlOffset, jobAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, jobAreaInfo.MapBlockOffset, jobAreaInfo.Subdivision, mapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public IList<MapSection> CreateEmptyMapSections(JobAreaAndCalcSettings jobAreaAndCalcSettings)
		{
			var emptyCountsData = new int[0];

			var result = new List<MapSection>();

			var jobAreaInfo = jobAreaAndCalcSettings.JobAreaInfo;
			var targetIterations = jobAreaAndCalcSettings.MapCalcSettings.TargetIterations;

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(jobAreaInfo.CanvasSize, jobAreaInfo.CanvasControlOffset, jobAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			var subdivisionId = jobAreaInfo.Subdivision.Id.ToString();

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, jobAreaInfo.MapBlockOffset, out var isInverted);
				var mapSection = new MapSection(screenPosition, jobAreaInfo.Subdivision.BlockSize, emptyCountsData, targetIterations,
					subdivisionId, repoPosition, isInverted, BuildHistogram);
				result.Add(mapSection);
			}

			return result;
		}

		#endregion

		#region Create Single MapSectionRequest

		public MapSectionRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings)
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
		public MapSectionRequest CreateRequest(BigVector repoPosition, bool isInverted, Subdivision subdivision, MapCalcSettings mapCalcSettings)
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
				IsInverted = isInverted,
				TimeToCompleteGenRequest = null
			};

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, BigVector blockPosition)
		{
			//var nrmSubdivisionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);

			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = blockPosition.Scale(subdivision.BlockSize);

			// Convert sample points to map coordinates.
			//var mapDistance = nrmSamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);
			var mapDistance = subdivision.SamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);

			// Add the map distance to the sub division origin
			//var result = nrmSubdivisionPosition.Translate(mapDistance);

			var result = new RPoint(mapDistance);

			return result;
		}

		#endregion

		#region Create MapSection

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, BigVector mapBlockOffset)
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

		public byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap, bool invert, bool useEscapeVelocities)
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

		private int GetResultRowPtr(int maxRowIndex, int rowPtr, bool invert)
		{
			// The Source's origin is at the bottom, left.
			// If inverted, the Destination's origin is at the top, left, otherwise bottom, left. 
			var result = invert ? maxRowIndex - rowPtr : rowPtr;
			return result;
		}

		private IHistogram BuildHistogram(int[] counts)
		{
			return new HistogramALow(counts.Select(x => (int)Math.Round(x / (double)VALUE_FACTOR)));
		}

		private IEnumerable<PointInt> Points(SizeInt size)
		{
			for (var yBlockPtr = 0; yBlockPtr < size.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < size.Width; xBlockPtr++)
				{
					yield return new PointInt(xBlockPtr, yBlockPtr);
				}
			}
		}

		#endregion
	}
}
