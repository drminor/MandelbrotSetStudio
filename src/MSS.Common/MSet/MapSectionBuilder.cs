using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	using ReqPosType = Tuple<PointInt, BigVector, bool>;

	public class MapSectionBuilder
	{
		#region Private Fields

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private DtoMapper _dtoMapper;

		private int _currentPrecision;
		private int _currentLimbCount;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionBuilder()
		{
			_dtoMapper = new DtoMapper();
			
			_currentPrecision = -1;
			_currentLimbCount = 1;
		}

		#endregion

		#region Create MapSectionRequests

		public List<MapSectionRequest> CreateSectionRequests(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var msrJob = CreateMapSectionRequestJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo, mapCalcSettings);

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize.Round(), mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating section requests. CanvasSize: {mapAreaInfo.CanvasSize.Round()}, CanvasControlOffset: {mapAreaInfo.CanvasControlOffset}, produces MapExtentInBlocks: {mapExtentInBlocks}.");

			var result = CreateSectionRequests(msrJob, mapExtentInBlocks);

			return result;
		}

		public MsrJob CreateMapSectionRequestJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

			var limbCount = GetLimbCount(precision);

			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset,
				precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesXZero);

			return msrJob;
		}

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

			var invertedSubCoords = subCoords.Where(x => x.Item3).ToArray();
			var matchedInvertedSubCoords = new bool[invertedSubCoords.Length];

			var tempCoordPairs = new List<Tuple<ReqPosType, ReqPosType?>>();

			foreach (var subCoord in subCoords)
			{
				//var mirror = GetMirror(subCoord, invertedSubCoords, matchedInvertedSubCoords);
				//tempCoordPairs.Add(new Tuple<ReqPosType, ReqPosType?>(subCoord, mirror));

				var indexOfMiror = GetIndexOfMirror(subCoord, invertedSubCoords);

				if (indexOfMiror != -1)
				{
					var mirror = invertedSubCoords[indexOfMiror];
					matchedInvertedSubCoords[indexOfMiror] = true;
					tempCoordPairs.Add(new Tuple<ReqPosType, ReqPosType?>(subCoord, mirror));
				}
				else
				{
					tempCoordPairs.Add(new Tuple<ReqPosType, ReqPosType?>(subCoord, null));
				}
			}

			var result = new List<MapSectionRequest>();
			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));
			var requestNumber = 0;

			var invertedPtr = 0;

			foreach (var coordPair in tempCoordPairs)
			{
				var primary = coordPair.Item1;

				if (primary.Item3)
				{
					var matched = matchedInvertedSubCoords[invertedPtr];

					if (!matched)
					{
						var screenPosition = primary.Item1;
						var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
						var mapSectionRequest = CreateRequest(msrJob, requestNumber++, screenPosition, screenPositionRelativeToCenter, primary);
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
					var screenPosition = primary.Item1;
					var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
					var mapSectionRequest = CreateRequest(msrJob, requestNumber++, screenPosition, screenPositionRelativeToCenter, primary);

					var mirror = coordPair.Item2;

					if (mirror != null)
					{
						var screenPosition2 = mirror.Item1;
						var screenPositionRelativeToCenter2 = screenPosition2.Sub(centerBlockIndex);
						var mapSectionRequest2 = CreateRequest(msrJob, requestNumber++, screenPosition2, screenPositionRelativeToCenter2, mirror);

						mapSectionRequest.Mirror = mapSectionRequest2;
					}

					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		private List<ReqPosType> GetSubdivisionCoords(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			var result = new List<ReqPosType>();

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);
				result.Add(new ReqPosType(screenPosition, sectionBlockOffset, isInverted));
			}

			return result;
		}

		private int GetIndexOfMirror(ReqPosType primary, ReqPosType[] secondaries)
		{
			if (!primary.Item3)
			{
				for (var i = 0; i < secondaries.Length; i++)
				{
					var second = secondaries[i];
					if (second.Item2.Y == primary.Item2.Y && second.Item2.X == primary.Item2.X)
					{
						return i;
					}
				}
			}

			return -1;
		}

		private ReqPosType? GetMirror(ReqPosType primary, ReqPosType[] secondaries, bool[] matched)
		{
			if (primary.Item2.Y >= 0)
			{
				for (var i = 0; i < secondaries.Length; i++)
				{
					// TODO: Consider only comparing those that have not yet been matched.
					var second = secondaries[i];
					if (second.Item2.Y == primary.Item2.Y && second.Item2.X == primary.Item2.X)
					{
						Debug.Assert(!matched[i], $"Item at index position: i, is being matched more than once.");
						matched[i] = true;
						return second;
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
		public MapSectionRequest CreateRequest(MsrJob msrJob, int requestNumber, PointInt screenPosition, VectorInt screenPositionRelativeToCenter, ReqPosType? reqPos = null)
		{
			// Block Position, relative to the Subdivision's BaseMapPosition

			bool isInverted;
			BigVector sectionBlockOffsetBigV;

			if (reqPos is null)
			{
				sectionBlockOffsetBigV = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out isInverted);
			}
			else
			{
				sectionBlockOffsetBigV = reqPos.Item2;
				isInverted = reqPos.Item3;
			}

			// Absolute position in Map Coordinates.
			var mapPosition = GetMapPosition(msrJob.Subdivision, sectionBlockOffsetBigV);

			var mapSectionRequest = new MapSectionRequest
			(
				msrJob: msrJob,
				requestNumber: requestNumber,
				screenPosition: screenPosition,
				screenPositionRelativeToCenter: screenPositionRelativeToCenter,
				sectionBlockOffset: _dtoMapper.Convert(sectionBlockOffsetBigV),
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

		#endregion

		#region Create MapSections

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors)
		{
			var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			var sectionBlockOffsetBigV = _dtoMapper.MapFrom(sectionBlockOffset);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffsetBigV, isInverted, jobBlockOffset);
			
			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, mapSectionVectors, 
				mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, sectionBlockOffset, isInverted, screenPosition, 
				mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, histogramBuilder: BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest);

			return mapSection;
		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, bool isCancelled)
		{
			var sectionBlockPosition = mapSectionRequest.SectionBlockOffset;
			var isInverted = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			var sectionBlockOffsetBigV = _dtoMapper.MapFrom(sectionBlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffsetBigV, isInverted, jobBlockOffset);

			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, mapSectionRequest.RequestNumber, 
				mapSectionRequest.Subdivision.Id.ToString(), jobBlockOffset, sectionBlockPosition, isInverted, screenPosition, 
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
}
