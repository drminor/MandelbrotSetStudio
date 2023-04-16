using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSS.Common
{
	public class MapSectionHelper
	{
		#region Private Properties

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;

		private int _currentPrecision;
		private int _currentLimbCount;

		#endregion

		#region Constructor

		public MapSectionHelper(MapSectionVectorsPool mapSectionVectorsPool, MapSectionZVectorsPool mapSectionZVectorsPool)
		{
			_mapSectionVectorsPool = mapSectionVectorsPool;
			_mapSectionZVectorsPool = mapSectionZVectorsPool;

			_currentPrecision = -1;
			_currentLimbCount = 1;
		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		public int MapSectionsVectorsInPool => _mapSectionVectorsPool.TotalFree;
		public int MapSectionsZVectorsInPool => _mapSectionZVectorsPool.TotalFree;

		public int MaxPeakSectionVectors => _mapSectionVectorsPool.MaxPeak;
		public int MaxPeakSectionZVectors => _mapSectionZVectorsPool.MaxPeak;

		#endregion

		#region Create MapSectionRequests

		public List<MapSectionRequest> CreateSectionRequests(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = GetBinaryPrecision(mapAreaInfo);

			var requestNumber = 0;
			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, precision, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings, requestNumber++);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public List<MapSectionRequest> CreateSectionRequestsFromMapSections(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections)
		{
			var result = new List<MapSectionRequest>();

			Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

			var requestNumber = 0;
			foreach (var mapSection in emptyMapSections)
			{
				var screenPosition = mapSection.ScreenPosition;
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings, requestNumber++);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		private int GetBinaryPrecision(MapAreaInfo mapAreaInfo)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(mapAreaInfo.Coords.Right, mapAreaInfo.Coords.Left, out _);

			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(mapAreaInfo.Subdivision.SamplePointDelta.Exponent));

			return binaryPrecision;
		}

		#endregion

		#region Create Single MapSectionRequest

		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="screenPosition"></param>
		/// <param name="mapBlockOffset"></param>
		/// <param name="precision"></param>
		/// <param name="ownerId"></param>
		/// <param name="jobOwnerType"></param>
		/// <param name="subdivision"></param>
		/// <param name="mapCalcSettings"></param>
		/// <returns></returns>
		public MapSectionRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, int precision, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings, int requestNumber)
		{
			var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapBlockOffset, out var isInverted);

			var mapPosition = GetMapPosition(subdivision, repoPosition);

			var limbCount = GetLimbCount(precision);

			var mapSectionRequest = new MapSectionRequest
			(
				ownerId: ownerId,
				jobOwnerType: jobOwnerType,
				subdivisionId: subdivision.Id.ToString(),
				screenPosition: screenPosition,
				mapBlockOffset: mapBlockOffset,
				blockPosition: repoPosition,
				mapPosition: mapPosition,
				isInverted: isInverted,
				precision: precision,
				limbCount: limbCount,
				blockSize: subdivision.BlockSize,
				samplePointDelta: subdivision.SamplePointDelta,
				mapCalcSettings: mapCalcSettings,
				requestNumber: requestNumber
			);

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, BigVector localBlockPosition)
		{
			var mapBlockPosition = subdivision.BaseMapPosition.Tranlate(localBlockPosition);

			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = mapBlockPosition.Scale(subdivision.BlockSize);

			// Convert sample points to map coordinates.
			var mapDistance = subdivision.SamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);

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
					Debug.WriteLine($"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount remains the same at {adjustedLimbCount}.");
				}
				else
				{
					Debug.WriteLine($"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount is being updated to {adjustedLimbCount}.");
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

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating empty MapSections. Returned: {mapExtentInBlocks}, For Size: {mapAreaInfo.CanvasSize} and Offset: {mapAreaInfo.CanvasControlOffset}.");

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapAreaInfo.MapBlockOffset, out var isInverted);

				var mapSection = new MapSection(
					jobNumber: -1, 
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
			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;

			var mapBlockOffset = mapSectionRequest.MapBlockOffset;

			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(jobNumber, mapSectionVectors, mapSectionRequest.SubdivisionId, mapBlockOffset, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest, jobNumber);

			return mapSection;
		}

		[Conditional("PERF")]
		private void UpdateMapSectionWithProcInfo(MapSection mapSection, MapSectionRequest mapSectionRequest, int jobNumber)
		{
			mapSection.MapSectionProcessInfo = new MapSectionProcessInfo(jobNumber, mapSectionRequest.FoundInRepo, mapSectionRequest.RequestNumber, isLastSection: false, requestDuration: mapSectionRequest.TimeToCompleteGenRequest,
				processingDuration: mapSectionRequest.ProcessingDuration, generationDuration: mapSectionRequest.GenerationDuration);

		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, int jobNumber, bool isCancelled)
		{
			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;

			var mapBlockOffset = mapSectionRequest.MapBlockOffset;

			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(jobNumber, mapSectionRequest.SubdivisionId, mapBlockOffset, repoBlockPosition, isInverted,
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

		#region MapSectionVectors

		public MapSectionVectors ObtainMapSectionVectors()
		{
			var result = _mapSectionVectorsPool.Obtain();

			//Debug.WriteLine($"Just obtained a MSVectors. Currently: {_mapSectionVectorsPool.TotalFree} available; {_mapSectionVectorsPool.MaxPeak} max allocated.");

			return result;
		}

		public MapSectionZVectors ObtainMapSectionZVectors(int limbCount)
		{
			var adjustedLimbCount = Math.Max(limbCount, MIN_LIMB_COUNT);
			var result = _mapSectionZVectorsPool.Obtain(adjustedLimbCount);
			return result;
		}

		//public MapSectionZVectors ObtainMapSectionZVectorsByPrecision(int precision)
		//{
		//	var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: precision);
		//	return ObtainMapSectionZVectors(apFixedPointFormat.LimbCount);
		//}

		public void ReturnMapSection(MapSection mapSection)
		{
			mapSection.MapSectionVectors = ReturnMapSectionVectors(mapSection.MapSectionVectors);
		}

		public void ReturnMapSectionRequest(MapSectionRequest mapSectionRequest)
		{
			mapSectionRequest.MapSectionVectors = ReturnMapSectionVectors(mapSectionRequest.MapSectionVectors);
			mapSectionRequest.MapSectionZVectors = ReturnMapSectionZVectors(mapSectionRequest.MapSectionZVectors);
		}

		public void ReturnMapSectionResponse(MapSectionResponse mapSectionResponse)
		{
			mapSectionResponse.MapSectionVectors = ReturnMapSectionVectors(mapSectionResponse.MapSectionVectors);
			mapSectionResponse.MapSectionZVectors= ReturnMapSectionZVectors(mapSectionResponse.MapSectionZVectors);
		}

		public MapSectionVectors? ReturnMapSectionVectors(MapSectionVectors? mapSectionVectors)
		{
			if (mapSectionVectors != null)
			{
				if (_mapSectionVectorsPool.Free(mapSectionVectors))
				{
					mapSectionVectors = null;
				}
			}

			return mapSectionVectors;
		}

		public MapSectionZVectors? ReturnMapSectionZVectors(MapSectionZVectors? mapSectionZVectors)
		{
			if (mapSectionZVectors != null)
			{
				if (_mapSectionZVectorsPool.Free(mapSectionZVectors))
				{
					mapSectionZVectors = null;
				}
			}

			return mapSectionZVectors;
		}

		#endregion
	}
}
