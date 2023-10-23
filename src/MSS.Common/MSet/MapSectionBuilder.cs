﻿using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public class MapSectionBuilder
	{
		#region Private Fields

		#endregion

		#region Constructor

		public MapSectionBuilder()
		{
		}

		#endregion

		#region Create MapSectionRequests

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

		private List<MapSectionRequest> CreateSectionRequestsSameYVals(MsrJob msrJob, SizeInt mapExtentInBlocks)
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

		private List<MapSectionRequest> CreateSectionRequestsMixedYVals(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			// All positions being requested
			var subCoords = GetSubdivisionCoords(msrJob, mapExtentInBlocks);

			// Collect all of the non-inverted requests
			var regularPositions = subCoords.Where(x => !x.IsInverted).ToArray();
			var matchedRegularPositions = new bool[regularPositions.Length];

			// Prepare the result
			//		The first item in each Tuple is the item from the list being processed
			//		The second item in each Tuple is the matched item, if extant.
			var coordPairs = new List<Tuple<MsrPosition, MsrPosition?>>();

			foreach (var subCoord in subCoords)
			{
				// If this item is Inverted, find the corresponding non-inverted item, if extant
				var indexOfRegular = subCoord.IsInverted ? GetIndexOfMirror(subCoord, regularPositions) : -1;

				if (indexOfRegular != -1)
				{
					// subCoord is Inverted and we found the matching non-inverted item
					var mirror = regularPositions[indexOfRegular];
					
					// Keep track of which non-inverted items are part of a pair
					matchedRegularPositions[indexOfRegular] = true;

					coordPairs.Add(new Tuple<MsrPosition, MsrPosition?>(subCoord, mirror));
				}
				else
				{
					// subCoord is Inverted and we have no match,
					// or subCoord is non-Inverted.
					coordPairs.Add(new Tuple<MsrPosition, MsrPosition?>(subCoord, null));
				}
			}

			// Prepare the result list of MapSectionRequests.
			// Each request will be for a single non-inverted or for a single inverted, or for a pair
			// In the case of a pair, the non-inverted specified in the first argument and 
			// the inverted item specified as the second argument.
			var result = new List<MapSectionRequest>();
			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			// Keep track of how many pairs have been processed
			var invertedPtr = 0;

			// Iterate over the temp result
			for (var coordPairPtr = 0; coordPairPtr < coordPairs.Count; coordPairPtr++)
			{
				var primary = coordPairs[coordPairPtr].Item1;
				var mirror = coordPairs[coordPairPtr].Item2;

				MapSectionRequest mapSectionRequest;

				if (!primary.IsInverted)
				{
					// The main request is regular -- include this item, only if this item is not a mirror of some other (inverted) MapSection.
					var matched = matchedRegularPositions[invertedPtr];

					if (!matched)
					{
						// Single request for a non-inverted item.
						mapSectionRequest = CreateRequest(msrJob, primary);
						result.Add(mapSectionRequest);
					}
					else
					{
						// Not adding this item, it is being used as Mirror for some other item.
					}

					// Advance the invertedPtr so that the next time we find a pair, we will be pointing to the next matchedRegularPosition ptr.
					invertedPtr++;
				}
				else
				{
					// The primary request is inverted.
					if (mirror != null)
					{
						// We have a matching non-inverted item, forming a pair of requests for the same MapSection
						mapSectionRequest = CreateRequest(msrJob, mirror, primary);
					}
					else
					{
						// Single request for an inverted item.
						mapSectionRequest = CreateRequest(msrJob, primary);
					}

					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		private List<MsrPosition> GetSubdivisionCoords(MsrJob msrJob, SizeInt mapExtentInBlocks)
		{
			var result = new List<MsrPosition>();

			var centerBlockIndex = new PointInt(mapExtentInBlocks.DivInt(new SizeInt(2)));

			var requestNumber = 0;

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var screenPositionRelativeToCenter = screenPosition.Sub(centerBlockIndex);
				var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);
				result.Add(new MsrPosition(requestNumber++, screenPosition, screenPositionRelativeToCenter, sectionBlockOffset, isInverted));
			}

			return result;
		}

		private int GetIndexOfMirror(MsrPosition primary, MsrPosition[] notInvertedSubCoords)
		{
			for (var i = 0; i < notInvertedSubCoords.Length; i++)
			{
				if (notInvertedSubCoords[i].SectionBlockOffset == primary.SectionBlockOffset)
				{
					return i;
				}
			}

			return -1;
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

		public MapSectionRequest CreateRequest(MsrJob msrJob, MsrPosition requestPosition)
		{
			//var mapPosition = GetMapPosition(msrJob.Subdivision, requestPosition.SectionBlockOffset);

			//var mapSectionRequest = new MapSectionRequest(msrJob, mapPosition, requestPosition);

			var mapSectionRequest = CreateRequest(msrJob, requestPosition.RequestNumber, requestPosition.ScreenPosition, requestPosition.ScreenPositionReleativeToCenter);
			return mapSectionRequest;
		}

		public MapSectionRequest CreateRequest(MsrJob msrJob, MsrPosition regularPosition, MsrPosition invertedPosition)
		{
			var mapPosition = GetMapPosition(msrJob.Subdivision, regularPosition.SectionBlockOffset);

			var mapSectionRequest = new MapSectionRequest(msrJob, mapPosition, regularPosition, invertedPosition);

			return mapSectionRequest;
		}

		public MapSectionRequest CreateRequest(MsrJob msrJob, int requestNumber, PointInt screenPosition, VectorInt screenPositionRelativeToCenter)
		{
			var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(screenPosition, msrJob.JobBlockOffset, out var isInverted);
			var requestPosition = new MsrPosition(requestNumber, screenPosition, screenPositionRelativeToCenter, sectionBlockOffset, isInverted);

			var mapPosition = GetMapPosition(msrJob.Subdivision, requestPosition.SectionBlockOffset);

			var mapSectionRequest = new MapSectionRequest(msrJob, mapPosition, requestPosition);

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


		public static string GetMapSectionRequestId(int mapLoaderJobNumber, int mapSectionRequestNumber)
		{
			return mapLoaderJobNumber.ToString() + "/" + mapSectionRequestNumber.ToString();
		}

		#endregion

		#region Create MapSections

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, bool isInverted, MapSectionVectors mapSectionVectors)
		{
			//MapSection result;

			//var jobBlockOffset = mapSectionRequest.JobBlockOffset;
			//var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			//var isInvertedD = mapSectionRequest.IsInverted;

			//var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInvertedD, jobBlockOffset);
			//result = new MapSection(mapSectionRequest, mapSectionVectors, isInverted, screenPosition, BuildHistogram);

			////if (isInverted)
			////{
			////	var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			////	var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);

			////	//var screenPosition = mapSectionRequest.InvertedPosition!.ScreenPosition;

			////	//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");
			////	result = new MapSection(mapSectionRequest, mapSectionVectors, isInverted, screenPosition, BuildHistogram);

			////}
			////else
			////{
			////	//var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);
			////	var screenPosition = mapSectionRequest.RegularPosition!.ScreenPosition;

			////	//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");
			////	result = new MapSection(mapSectionRequest, mapSectionVectors, isInverted, screenPosition, BuildHistogram);
			////}

			////var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			//////var isInverted = mapSectionRequest.IsInverted;

			////var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);

			//////Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");
			////var mapSection = new MapSection(mapSectionRequest, mapSectionVectors, isInverted, screenPosition, BuildHistogram);

			//UpdateMapSectionWithProcInfo(result, mapSectionRequest);




			var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			//var isInvertedD = mapSectionRequest.IsInverted;

			var jobBlockOffset = mapSectionRequest.JobBlockOffset;
			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);

			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");
			var mapSection = new MapSection(mapSectionRequest, mapSectionVectors, isInverted, screenPosition, BuildHistogram);

			UpdateMapSectionWithProcInfo(mapSection, mapSectionRequest);

			return mapSection;
		}

		public List<MapSection> CreateEmptyMapSections(MapSectionRequest mapSectionRequest, bool isCancelled)
		{
			var result = new List<MapSection>();

			if (mapSectionRequest.IsPaired)
			{
				var ms = CreateEmptyMapSection(mapSectionRequest, isInverted: false, isCancelled);
				result.Add(ms);

				ms = CreateEmptyMapSection(mapSectionRequest, isInverted: true, isCancelled);
				result.Add(ms);
			}
			else if(mapSectionRequest.RegularPosition != null)
			{
				var ms = CreateEmptyMapSection(mapSectionRequest, isInverted: false, isCancelled);
				result.Add(ms);
			}
			else if(mapSectionRequest.InvertedPosition != null)
			{
				var ms = CreateEmptyMapSection(mapSectionRequest, isInverted: true, isCancelled);
				result.Add(ms);
			}
			else
			{
				throw new InvalidOperationException("The MapSectionRequest has neither a Regular or Inverted Position.");
			}

			return result;
		}

		public MapSection CreateEmptyMapSection(MapSectionRequest mapSectionRequest, bool isInverted, bool isCancelled)
		{
			var sectionBlockOffset = mapSectionRequest.SectionBlockOffset;
			var jobBlockOffset = mapSectionRequest.JobBlockOffset;

			var screenPosition = RMapHelper.ToScreenCoords(sectionBlockOffset, isInverted, jobBlockOffset);

			//Debug.WriteLine($"Creating MapSection: SectionBlockOffset: {sectionBlockPosition}, ScreenBlkPos: {screenPosition}, Inverted = {isInverted}.");

			var requestNumber = isInverted ? mapSectionRequest.InvertedPosition!.RequestNumber : mapSectionRequest.RegularPosition!.RequestNumber;

			var mapSection = new MapSection(mapSectionRequest.MapLoaderJobNumber, requestNumber,
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
			mapSection.MapSectionProcessInfo = new MapSectionProcessInfo
				(
				mapSectionRequest.MapLoaderJobNumber, 
				mapSectionRequest.RequestNumber, 
				mapSectionRequest.FoundInRepo, 
				mapSectionRequest.Completed, 
				mapSectionRequest.IsCancelled, 
				requestDuration: mapSectionRequest.TimeToCompleteGenRequest,
				processingDuration: mapSectionRequest.ProcessingDuration, 
				generationDuration: mapSectionRequest.GenerationDuration
				);
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

				if (ms.RegularPosition != null && ms.InvertedPosition != null)
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

		public int GetTotalNumberOfRequests(List<MapSectionRequest> result)
		{
			var total = 0;

			for (var i = 0; i < result.Count; i++)
			{
				var ms = result[i];

				if (ms.RegularPosition != null && ms.InvertedPosition != null)
				{
					total += 2;
				}
				else
				{
					total += 1;
				}
			}

			return total;
		}

		public int GetNumberOfSectionsCancelled(List<MapSectionRequest> requests)
		{
			var result = 0;

			for (var i = 0; i < requests.Count; i++)
			{
				var ms = requests[i];

				if (ms.RegularPosition?.IsCancelled == true)
				{
					result += 1;
				}

				if (ms.InvertedPosition?.IsCancelled == true)
				{
					result += 1;
				}
			}

			return result;
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
