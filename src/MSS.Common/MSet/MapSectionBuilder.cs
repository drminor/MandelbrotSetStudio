using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

		public List<MapSectionRequest> CreateSectionRequests(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLineIf(_useDetailedDebug, $"Creating section requests. The map extent is {mapExtentInBlocks}.");

			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

			var limbCount = GetLimbCount(precision);

			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset, 
				precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesXZero);

			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			bool firstSectionIsInverted = false;
			var requestNumber = 0;

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);

				var mapSectionRequest = CreateRequest(msrJob, requestNumber++, screenPosition, screenPositionRelativeToCenter);

				if(requestNumber == 1)
				{
					firstSectionIsInverted = mapSectionRequest.IsInverted;
				}
				else
				{
					if (mapSectionRequest.IsInverted != firstSectionIsInverted)
					{
						var mirror = result.FirstOrDefault(x => x.SectionBlockOffset.Equals(mapSectionRequest.SectionBlockOffset));
						if (mirror != null)
						{
							mapSectionRequest.Mirror = mirror;

							mirror.Mirror = mapSectionRequest;

							// Do not add this mapSectionRequest to the result since it is included as a mirror.
							continue;
						}
					}
				}

				result.Add(mapSectionRequest);
			}

			return result;
		}

		/* Compare this logic from above with the logic within the BitmapGrid to determine if a section is in bounds.

		The logic above uses 
			a rectangle in MapCoordinates,
			to get the size of the area to cover in whole blocks
		then
			calculates the database address of each block


		The logic in the BitmapGrid
			takes the map coordinates of each block,
			compares it to the map coordinates of the block in the lower, left
			and calculates the screen coordinates, relative to that 'anchor' block
		then
			determines if the block's lower-left is > upper-right of the display
			or if the block's upper-right is > 0.
		

		var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);

		
		  
		// New position, same size and scale
		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			// -- Scale the Position and Size together.
			var invertedY = GetInvertedYPos(newDisplayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, ContentViewportSize);
			var scaledNewScreenArea = newScreenArea.Scale(BaseScale);

			var result = GetUpdatedMapAreaInfo(scaledNewScreenArea, _scaledMapAreaInfo);

			return result;
		}

		private MapAreaInfo GetUpdatedMapAreaInfo(RectangleDbl newScreenArea, MapAreaInfo mapAreaInfoWithSize)
		{
			var newCoords = _mapJobHelper.GetMapCoords(newScreenArea.Round(), mapAreaInfoWithSize.MapPosition, mapAreaInfoWithSize.SamplePointDelta);
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, mapAreaInfoWithSize.Subdivision, mapAreaInfoWithSize.OriginalSourceSubdivisionId, newScreenArea.Size);

			//Debug.WriteLineIf(_useDetailedDebug, $"Getting Updated MapAreaInfo for newPos: {newScreenArea.Position}, newSize: {newScreenArea.Size}. " +
			//		$"Result: BlockOffset {mapAreaInfoV1.MapBlockOffset}, spd: {mapAreaInfoV1.SamplePointDelta.Width} , CanvasControlOffset: {mapAreaInfoV1.CanvasControlOffset} " +				
			//		$"From MapAreaInfo with CanvasSize: {mapAreaInfoWithSize.CanvasSize}, BlockOffset: {mapAreaInfoWithSize.MapBlockOffset}, spd: {mapAreaInfoWithSize.SamplePointDelta.Width}.");


			Debug.WriteLineIf(_useDetailedDebug, $"\nGetting Updated MapAreaInfo for newPos: {newScreenArea.Position.ToString("F2")}, newSize: {newScreenArea.Size.ToString("F2")}. " +
					$"Result: CanvasSize: {mapAreaInfoV1.CanvasSize.ToString("F2")}, BlockOffset: {mapAreaInfoV1.MapBlockOffset}, CanvasControlOffset: {mapAreaInfoV1.CanvasControlOffset}, spd: {mapAreaInfoV1.SamplePointDelta.Width}." +
					$"From MapAreaInfo with CanvasSize: {mapAreaInfoWithSize.CanvasSize.ToString("F2")}, BlockOffset: {mapAreaInfoWithSize.MapBlockOffset}, CanvasControlOffset: {mapAreaInfoWithSize.CanvasControlOffset}, spd: {mapAreaInfoWithSize.SamplePointDelta.Width}.");

			return mapAreaInfoV1;
		}

		*/

		#endregion

		#region Create A Single MapSectionRequest

		/// <summary>
		/// Calculate the map position of the section being requested 
		/// and prepare a MapSectionRequest
		/// </summary>
		/// <param name="screenPosition"></param>
		/// <param name="jobBlockOffset"></param>
		/// <param name="precision"></param>
		/// <param name="jobId"></param>
		/// <param name="ownerType"></param>
		/// <param name="subdivision"></param>
		/// <param name="mapCalcSettings"></param>
		/// <returns></returns>
		public MapSectionRequest CreateRequest(MsrJob msrJob, int requestNumber, PointInt screenPosition, VectorInt screenPositionRelativeToCenter)
		{
			// Block Position, relative to the Subdivision's BaseMapPosition
			var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);

			// Absolute position in Map Coordinates.
			var mapPosition = GetMapPosition(msrJob.Subdivision, sectionBlockOffset);

			var mapSectionRequest = new MapSectionRequest
			(
				msrJob: msrJob,
				requestNumber: requestNumber,
				screenPosition: screenPosition,
				screenPositionRelativeToCenter: screenPositionRelativeToCenter,
				sectionBlockOffset: MapTo(sectionBlockOffset),
				mapPosition: mapPosition,
				isInverted: isInverted);

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, BigVector sectionBlockOffset)
		{
			RVector mapPosition;

			if (subdivision.BaseMapPosition.IsZero())
			{
				mapPosition = subdivision.SamplePointDelta.Scale(sectionBlockOffset.Scale(subdivision.BlockSize));
			}
			else
			{
				var mapBlockPosition = sectionBlockOffset.Tranlate(subdivision.BaseMapPosition);

				// Multiply the blockPosition by the blockSize
				var numberOfSamplePointsFromSubOrigin = mapBlockPosition.Scale(subdivision.BlockSize);

				// Convert sample points to map coordinates.
				mapPosition = subdivision.SamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);
			}

			var result = new RPoint(mapPosition);

			return result;
		}

		public int GetLimbCount(int precision)
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

		private MapBlockOffset MapTo(BigVector bigVector)
		{
			var (x, y) = bigVector.GetLongPairs();
			var mapBlockOffset = new MapBlockOffset(x, y);
			return mapBlockOffset;
		}

		#endregion

		#region Create MapSections

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors)
		{
			var repoBlockPosition = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			var sectionBlockOffset = MapFrom(repoBlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, mapSectionVectors, mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, histogramBuilder: BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest);

			return mapSection;
		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, bool isCancelled)
		{
			var repoBlockPosition = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			var sectionBlockOffset = MapFrom(repoBlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, repoBlockPosition, isInverted,
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

		private BigVector MapFrom(MapBlockOffset mapBlockOffset)
		{
			var (x, y) = mapBlockOffset.GetBigIntegers();
			var result = new BigVector(x, y);

			return result;
		}

		[Conditional("PERF")]
		private void UpdateMapSectionWithProcInfo(MapSection mapSection, MapSectionRequest mapSectionRequest)
		{
			mapSection.MapSectionProcessInfo = new MapSectionProcessInfo(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.FoundInRepo, mapSectionRequest.RequestNumber, isLastSection: false, requestDuration: mapSectionRequest.TimeToCompleteGenRequest,
				processingDuration: mapSectionRequest.ProcessingDuration, generationDuration: mapSectionRequest.GenerationDuration);
		}

		#endregion
	}
}
