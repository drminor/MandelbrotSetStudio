using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	//using MsrPosition = Tuple<PointInt, VectorLong, bool>;

	public class MapSectionBuilder
	{
		#region Private Fields

		//private const int PRECSION_PADDING = 4;
		//private const int MIN_LIMB_COUNT = 1;

		//private int _currentPrecision;
		//private int _currentLimbCount;

		//private DtoMapper _dtoMapper;
		//private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionBuilder()
		{
			//_dtoMapper = new DtoMapper();
			
			//_currentPrecision = -1;
			//_currentLimbCount = 1;
		}

		#endregion

		#region Create MapSectionRequests

		//public List<MapSectionRequest> CreateSectionRequests(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		//{
		//	var msrJob = CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo, mapCalcSettings);

		//	var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
		//	Debug.WriteLine($"Creating section requests. CanvasSize: {mapAreaInfo.CanvasSize.Round()}, CanvasControlOffset: {mapAreaInfo.CanvasControlOffset}, produces MapExtentInBlocks: {mapExtentInBlocks}.");

		//	var result = CreateSectionRequests(msrJob, mapExtentInBlocks);

		//	return result;
		//}

		//public MsrJob CreateMapSectionRequestJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		//{
		//	// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
		//	var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

		//	var limbCount = GetLimbCount(precision);

		//	var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset,
		//		precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesXZero);

		//	return msrJob;
		//}

		public List<MapSectionRequest> CreateSectionRequests(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			List<MapSectionRequest> result;

			if (msrJob.CrossesYZero)
			{
				result = CreateSectionRequestsMixedYVals(msrJob, mapExtentInBlocks);
			}
			else
			{
				result = CreateSectionRequestsSameYVals(msrJob, mapExtentInBlocks);
			}

			ReportCreateMapSectionRequests(result);

			return result;
		}

		public List<MapSectionRequest> CreateSectionRequestsSameYVals(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			var result = new List<MapSectionRequest>();
			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));
			var requestNumber = 0;

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
				var mapSectionRequest = CreateRequest(msrJob, requestNumber++, screenPosition, screenPositionRelativeToCenter);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public List<MapSectionRequest> CreateSectionRequestsMixedYVals(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			var subCoords = GetSubdivisionCoords(msrJob, mapExtentInBlocks);

			var notInvertedSubCoords = subCoords.Select((x, i) => new Tuple<MsrPosition, int>(x, i)).Where(x => !x.Item1.IsInverted).ToArray();
			var matchedNotInvertedSubCoords = new bool[notInvertedSubCoords.Length];

			var tempCoordPairs = new List<Tuple<MsrPosition, Tuple<MsrPosition, int>?>>();

			foreach (var subCoord in subCoords)
			{
				var indexOfMiror = subCoord.IsInverted ? GetIndexOfMirror(subCoord, notInvertedSubCoords) : -1;

				if (indexOfMiror != -1)
				{
					var mirror = notInvertedSubCoords[indexOfMiror];
					matchedNotInvertedSubCoords[indexOfMiror] = true;
					tempCoordPairs.Add(new Tuple<MsrPosition, Tuple<MsrPosition, int>?>(subCoord, mirror));
				}
				else
				{
					tempCoordPairs.Add(new Tuple<MsrPosition, Tuple<MsrPosition, int>?>(subCoord, null));
				}
			}

			var result = new List<MapSectionRequest>();
			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			var invertedPtr = 0;

			for (var requestNumber = 0; requestNumber < tempCoordPairs.Count; requestNumber++)
			{
				var primary = tempCoordPairs[requestNumber].Item1;

				if (!primary.IsInverted)
				{
					var matched = matchedNotInvertedSubCoords[invertedPtr];

					if (!matched)
					{
						var screenPosition = primary.ScreenPosition;
						var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
						var mapSectionRequest = CreateRequest(msrJob, requestNumber, screenPosition, screenPositionRelativeToCenter, primary);
						result.Add(mapSectionRequest);
					}
					else
					{
						// Not adding this item, it is being used as Mirror for some other item.
					}

					// Advance the invertedPtr so that the next time we find a primary that is Inverted, we will be pointing to the next elements from the matchedInvertedSubCoords array.
					invertedPtr++;
				}
				else
				{
					var screenPosition = primary.ScreenPosition;
					var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
					var mapSectionRequest = CreateRequest(msrJob, requestNumber, screenPosition, screenPositionRelativeToCenter, primary);

					var mirrorAndIndex = tempCoordPairs[requestNumber].Item2;

					if (mirrorAndIndex != null)
					{
						var mirror = mirrorAndIndex.Item1;
						var requestNumber2 = mirrorAndIndex.Item2;
						var screenPosition2 = mirror.ScreenPosition;
						var screenPositionRelativeToCenter2 = screenPosition2.Sub(centerBlockIndex);
						var mapSectionRequest2 = CreateRequest(msrJob, requestNumber2, screenPosition2, screenPositionRelativeToCenter2, mirror);

						mapSectionRequest.Mirror = mapSectionRequest2;
					}

					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		private List<MsrPosition> GetSubdivisionCoords(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			var result = new List<MsrPosition>();

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);
				result.Add(new MsrPosition(screenPosition, sectionBlockOffset, isInverted));
			}

			return result;
		}

		private int GetIndexOfMirror(MsrPosition primary, Tuple<MsrPosition, int>[] notInvertedSubCoords)
		{
			for (var i = 0; i < notInvertedSubCoords.Length; i++)
			{
				if (notInvertedSubCoords[i].Item1.SectionBlockOffset == primary.SectionBlockOffset)
				{
					return i;
				}
			}

			return -1;
		}

		private MsrPosition? GetMirror(MsrPosition primary, MsrPosition[] notInvertedSubCoords, bool[] matched)
		{
			if (primary.IsInverted)
			{
				for (var i = 0; i < notInvertedSubCoords.Length; i++)
				{
					// TODO: Consider only comparing those that have not yet been matched.
					if (notInvertedSubCoords[i].SectionBlockOffset == primary.SectionBlockOffset)
					{
						Debug.Assert(!matched[i], $"Item at index position: i, is being matched more than once.");
						matched[i] = true;
						return notInvertedSubCoords[i];
					}
				}
			}

			return null;
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
		public MapSectionRequest CreateRequest(MsrJob msrJob, int requestNumber, PointInt screenPosition, VectorInt screenPositionRelativeToCenter, MsrPosition? reqPos = null)
		{
			// Block Position, relative to the Subdivision's BaseMapPosition
			if (reqPos is null)
			{
				var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);
				reqPos = new MsrPosition(screenPosition, sectionBlockOffset, isInverted);
			}

			// Absolute position in Map Coordinates.
			var mapPosition = GetMapPosition(msrJob.Subdivision, reqPos.SectionBlockOffset);

			var mapSectionRequest = new MapSectionRequest
			(
				msrJob: msrJob,
				requestNumber: requestNumber,
				screenPosition: screenPosition,
				screenPositionRelativeToCenter: screenPositionRelativeToCenter,
				sectionBlockOffset: reqPos.SectionBlockOffset,
				mapPosition: mapPosition,
				isInverted: reqPos.IsInverted);

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, VectorLong sectionBlockOffset)
		{
			RVector mapPosition;

			if (subdivision.BaseMapPosition.IsZero())
			{
				var sectionBlockOffsetBigV = new BigVector(sectionBlockOffset.X, sectionBlockOffset.Y);
				mapPosition = subdivision.SamplePointDelta.Scale(sectionBlockOffsetBigV.Scale(subdivision.BlockSize));
			}
			else
			{
				var mapBlockPosition = subdivision.BaseMapPosition.Translate(sectionBlockOffset);

				// Multiply the blockPosition by the blockSize
				var numberOfSamplePointsFromSubOrigin = mapBlockPosition.Scale(subdivision.BlockSize);

				// Convert sample points to map coordinates.
				mapPosition = subdivision.SamplePointDelta.Scale(numberOfSamplePointsFromSubOrigin);
			}

			var result = new RPoint(mapPosition);

			return result;
		}

		//public int GetLimbCount(int precision)
		//{
		//	if (precision != _currentPrecision)
		//	{
		//		var adjustedPrecision = precision + PRECSION_PADDING;
		//		var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: adjustedPrecision);

		//		var adjustedLimbCount = Math.Max(apFixedPointFormat.LimbCount, MIN_LIMB_COUNT);

		//		if (_currentLimbCount == adjustedLimbCount)
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount remains the same at {adjustedLimbCount}.");
		//		}
		//		else
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount is being updated to {adjustedLimbCount}.");
		//		}

		//		_currentLimbCount = adjustedLimbCount;
		//		_currentPrecision = precision;
		//	}

		//	return _currentLimbCount;	
		//}

		#endregion

		#region Create MapSections

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors)
		{
			var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			//var sectionBlockOffsetBigV = _dtoMapper.MapFrom(sectionBlockOffset);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);
			
			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, mapSectionVectors, 
				mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, sectionBlockOffset, isInverted, screenPosition, 
				mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, histogramBuilder: BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest);

			return mapSection;
		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, bool isCancelled)
		{
			var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			//var sectionBlockOffsetBigV = _dtoMapper.MapFrom(sectionBlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);

			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, 
				mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, sectionBlockOffset, isInverted, screenPosition, 
				mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, isCancelled);

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

		[Conditional("PERF")]
		private void UpdateMapSectionWithProcInfo(MapSection mapSection, MapSectionRequest mapSectionRequest)
		{
			mapSection.MapSectionProcessInfo = new MapSectionProcessInfo(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.FoundInRepo, mapSectionRequest.RequestNumber, isLastSection: false, requestDuration: mapSectionRequest.TimeToCompleteGenRequest,
				processingDuration: mapSectionRequest.ProcessingDuration, generationDuration: mapSectionRequest.GenerationDuration);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG")]
		private void ReportCreateMapSectionRequests(List<MapSectionRequest> mapSectionRequests)
		{
			var countRequestsReport = GetCountRequestsReport(mapSectionRequests);
			Debug.WriteLine(countRequestsReport);
		}

		public string GetCountRequestsReport(List<MapSectionRequest> mapSectionRequests)
		{
			var (s, p) = CountRequests(mapSectionRequests);
			var t = p * 2 + s;
			var result = $"Created {p} request pairs and {s} single requests, for a total of {p * 2} + {s} = {t}.";

			return result;
		}

		public (int singles, int pairs) CountRequests(List<MapSectionRequest> result)
		{
			//var total = 0;
			var pairs = 0;
			var singles = 0;

			for (var i = 0; i < result.Count; i++)
			{
				var ms = result[i];

				if (ms.Mirror != null)
				{
					//total += 2;
					pairs += 1;
				}
				else
				{
					//total += 1;
					singles += 1;
				}
			}

			return (singles, pairs);
		}


		//[Conditional("DEBUG")]
		//private void ReportCreateMapSectionRequests(int numberOfSections, int numberOfSingles, int numberOfPairs, List<MapSectionRequest> mapSectionRequests)
		//{

		//	var diff = numberOfSections - ((2 * numberOfPairs) + numberOfSingles);

		//	if (diff > 0)
		//	{
		//		Debug.WriteLine($"CreateSectionRequests processed {numberOfSections} points and produced {numberOfPairs} pairs and {numberOfSingles} singles. Missing {diff} requests.");
		//	}
		//	else if (diff < 0)
		//	{
		//		Debug.WriteLine($"CreateSectionRequests processed {numberOfSections} points and produced {numberOfPairs} pairs and {numberOfSingles} singles. Found {diff} extra requests.");
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"CreateSectionRequests processed {numberOfSections} points and produced {numberOfPairs} pairs and {numberOfSingles} singles. As expected.");
		//	}

		//	var countRequestsReport = GetCountRequestsReport(mapSectionRequests);
		//	Debug.WriteLine(countRequestsReport);
		//}

		#endregion
	}

	public class MsrPosition
	{
		public PointInt ScreenPosition { get; init; }
		public VectorLong SectionBlockOffset { get; init; }
		public bool IsInverted { get; init; }

		public MsrPosition(PointInt screenPosition, VectorLong sectionBlockOffset, bool isInverted)
		{
			ScreenPosition = screenPosition;
			SectionBlockOffset = sectionBlockOffset;
			IsInverted = isInverted;
		}
	}

}
