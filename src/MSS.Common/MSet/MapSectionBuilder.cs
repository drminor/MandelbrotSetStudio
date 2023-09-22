using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSS.Common
{
	public class MapSectionBuilder
	{
		#region Private Properties

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private int _currentPrecision;
		private int _currentLimbCount;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionBuilder()
		{
			_currentPrecision = -1;
			_currentLimbCount = 1;
		}

		#endregion

		#region Create MapSectionRequests

		public List<MapSectionRequest> CreateSectionRequests(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, int mapLoaderJobNumber)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLineIf(_useDetailedDebug, $"Creating section requests. The map extent is {mapExtentInBlocks}.");

			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			var requestNumber = 0;
			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);

				var mapSectionRequest = CreateRequest(jobType, screenPosition, screenPositionRelativeToCenter, mapAreaInfo.MapBlockOffset, precision, jobId, jobOwnerType, 
					mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId, mapCalcSettings, mapLoaderJobNumber, requestNumber++);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public List<MapSectionRequest> CreateSectionRequestsFromMapSections(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, 
			IList<MapSection> emptyMapSections)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLineIf(_useDetailedDebug, $"Creating section requests. The map extent is {mapExtentInBlocks}.");


			Debug.WriteLineIf(_useDetailedDebug, $"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			foreach (var mapSection in emptyMapSections)
			{
				var screenPosition = mapSection.ScreenPosition;
				var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);

				var mapSectionRequest = CreateRequest(jobType, screenPosition, screenPositionRelativeToCenter, mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, jobId, jobOwnerType, 
					mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId, mapCalcSettings, mapLoaderJobNumber: -1, mapSection.RequestNumber);

				result.Add(mapSectionRequest);
			}

			return result;
		}

		#endregion

		#region Create Single MapSectionRequest

		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="screenPosition"></param>
		/// <param name="jobMapBlockOffset"></param>
		/// <param name="precision"></param>
		/// <param name="jobId"></param>
		/// <param name="ownerType"></param>
		/// <param name="subdivision"></param>
		/// <param name="mapCalcSettings"></param>
		/// <returns></returns>
		public MapSectionRequest CreateRequest(JobType jobType, PointInt screenPosition, VectorInt screenPositionRelativeToCenter, BigVector jobMapBlockOffset, int precision, string jobId, OwnerType ownerType, 
			Subdivision subdivision, ObjectId originalSourceSubdivisionId, MapCalcSettings mapCalcSettings, int mapLoaderJobNumber, int requestNumber)
		{
			// Block Position, relative to the Subdivision's BaseMapPosition
			var localBlockPosition = RMapHelper.ToSubdivisionCoords(screenPosition, jobMapBlockOffset, out var isInverted);

			// Absolute position in Map Coordinates.
			var mapPosition = GetMapPosition(subdivision, localBlockPosition);

			var limbCount = GetLimbCount(precision);

			var mapSectionRequest = new MapSectionRequest
			(
				jobType: jobType,
				jobId: jobId,
				ownerType: ownerType,
				subdivisionId: subdivision.Id.ToString(),
				originalSourceSubdivisionId: originalSourceSubdivisionId.ToString(),
				screenPosition: screenPosition,
				screenPositionRelativeToCenter: screenPositionRelativeToCenter,
				mapBlockOffset: jobMapBlockOffset,
				blockPosition: localBlockPosition,
				mapPosition: mapPosition,
				isInverted: isInverted,
				precision: precision,
				limbCount: limbCount,
				blockSize: subdivision.BlockSize,
				samplePointDelta: subdivision.SamplePointDelta,
				mapCalcSettings: mapCalcSettings,
				mapLoaderJobNumber: mapLoaderJobNumber,
				requestNumber: requestNumber
			);

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, BigVector localBlockPosition)
		{
			RVector mapDistance;

			if (subdivision.BaseMapPosition.IsZero())
			{
				mapDistance = subdivision.SamplePointDelta.Scale(localBlockPosition.Scale(subdivision.BlockSize));
			}
			else
			{
				var mapBlockPosition = localBlockPosition.Tranlate(subdivision.BaseMapPosition);

				// Multiply the blockPosition by the blockSize
				var numberOfSamplePointsFromSubOrigin = mapBlockPosition.Scale(subdivision.BlockSize);

				// Convert sample points to map coordinates.
				mapDistance = subdivision.SamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);

			}

			var result = new RPoint(mapDistance);

			return result;
		}

		private int GetLimbCount(int precision)
		{
			if (precision != _currentPrecision)
			{
				var adjustedPrecision = precision + PRECSION_PADDING;
				var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: adjustedPrecision);

				var adjustedLimbCount = Math.Max(apFixedPointFormat.LimbCount, MIN_LIMB_COUNT);

				if (_currentLimbCount == adjustedLimbCount)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount remains the same at {adjustedLimbCount}.");
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount is being updated to {adjustedLimbCount}.");
				}

				_currentLimbCount = adjustedLimbCount;
				_currentPrecision = precision;
			}

			return _currentLimbCount;	
		}

		#endregion

		#region Create MapSections

		public List<MapSection> CreateEmptyMapSections(MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSection>();

			var subdivisionId = mapAreaInfo.Subdivision.Id.ToString();
			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var targetIterations = mapCalcSettings.TargetIterations;

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			//Debug.WriteLineIf(_useDetailedDebug, $"Creating empty MapSections. Returned: {mapExtentInBlocks}, For Size: {mapAreaInfo.CanvasSize} and Offset: {mapAreaInfo.CanvasControlOffset}.");

			//Debug.WriteLineIf(_useDetailedDebug, $"Creating {mapExtentInBlocks} empty MapSections. For CanvasSize: {mapAreaInfo.CanvasSize} and SamplePointDelta: {mapAreaInfo.SamplePointDelta}.");

			var ts = DateTime.Now.ToLongTimeString();
			Debug.WriteLineIf(_useDetailedDebug, $"Creating {mapExtentInBlocks} empty MapSections. For CanvasSize: {mapAreaInfo.CanvasSize} and SamplePointDelta: {mapAreaInfo.SamplePointDelta}. At ts={ts}.");

			if (mapExtentInBlocks.NumberOfCells > 400)
			{
				Debug.WriteLine($"About to request {mapExtentInBlocks.NumberOfCells} map sections!!");
			}

			var mapLoaderJobNumber = -1;
			var requestNumber = 0;

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapAreaInfo.MapBlockOffset, out var isInverted);

				var mapSection = new MapSection(
					jobNumber: mapLoaderJobNumber, 
					requestNumber: requestNumber++,
					subdivisionId: subdivisionId, 
					repoBlockPosition: repoPosition,
					jobMapBlockPosition: mapAreaInfo.MapBlockOffset,
					isInverted: isInverted,
					screenPosition: screenPosition, 
					size: blockSize, 
					targetIterations: targetIterations,
					isCancelled: false);

				result.Add(mapSection);
			}

			return result;
		}


		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors, int jobNumber)
		{
			Debug.Assert(mapSectionRequest.MapLoaderJobNumber == jobNumber, "MapLoaderJobNumber mismatch.");
			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;

			var mapBlockOffset = mapSectionRequest.MapBlockOffset;

			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, mapSectionVectors, mapSectionRequest.SubdivisionId, mapBlockOffset, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest);

			return mapSection;
		}

		[Conditional("PERF")]
		private void UpdateMapSectionWithProcInfo(MapSection mapSection, MapSectionRequest mapSectionRequest)
		{
			mapSection.MapSectionProcessInfo = new MapSectionProcessInfo(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.FoundInRepo, mapSectionRequest.RequestNumber, isLastSection: false, requestDuration: mapSectionRequest.TimeToCompleteGenRequest,
				processingDuration: mapSectionRequest.ProcessingDuration, generationDuration: mapSectionRequest.GenerationDuration);
		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, int jobNumber, bool isCancelled)
		{
			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;

			var mapBlockOffset = mapSectionRequest.MapBlockOffset;

			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(jobNumber, mapSectionRequest.RequestNumber, mapSectionRequest.SubdivisionId, mapBlockOffset, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, isCancelled);

			return mapSection;
		}

		private IHistogram BuildHistogram(ushort[] counts)
		{
			//return new HistogramALow(counts.Select(x => (int)Math.Round(x / (double)VALUE_FACTOR)));
			return new HistogramALow(counts);
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
