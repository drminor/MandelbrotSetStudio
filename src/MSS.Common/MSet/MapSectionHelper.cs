using MSS.Common.DataTransferObjects;
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

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionValuesPool _mapSectionValuesPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;

		private SizeInt _blockSize;
		private readonly int _rowCount;
		private readonly int _sourceStride;
		private readonly int _maxRowIndex;

		private readonly int _pixelArraySize;
		private readonly int _pixelStride;

		#endregion

		#region Constructor

		public MapSectionHelper(MapSectionVectorsPool mapSectionVectorsPool, MapSectionValuesPool mapSectionValuesPool, MapSectionZVectorsPool mapSectionZVectorsPool)
		{
			_mapSectionVectorsPool = mapSectionVectorsPool;
			_mapSectionValuesPool = mapSectionValuesPool;
			_mapSectionZVectorsPool = mapSectionZVectorsPool;

			_blockSize = mapSectionVectorsPool.BlockSize;
			_rowCount = _blockSize.Height;
			_sourceStride = _blockSize.Width;
			_maxRowIndex = _blockSize.Height - 1;

			_pixelArraySize = _blockSize.NumberOfCells * BYTES_PER_PIXEL;
			_pixelStride = _sourceStride * BYTES_PER_PIXEL;

		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Create MapSectionRequests

		public IList<MapSectionRequest> CreateSectionRequests(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections)
		{
			var result = new List<MapSectionRequest>();

			Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

			foreach (var mapSection in emptyMapSections)
			{
				var screenPosition = mapSection.BlockPosition;
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public IList<MapSectionRequest> CreateSectionRequests(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public IList<MapSection> CreateEmptyMapSections(MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			//var emptyCountsData = new ushort[0];
			//var emptyEscapeVelocities = new ushort[0];

			var result = new List<MapSection>();

			var targetIterations = mapCalcSettings.TargetIterations;

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			var subdivisionId = mapAreaInfo.Subdivision.Id.ToString();

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapAreaInfo.MapBlockOffset, out var isInverted);

				//var mapSection = new MapSection(screenPosition, mapAreaInfo.Subdivision.BlockSize, emptyCountsData, emptyEscapeVelocities, targetIterations,
				//	subdivisionId, repoPosition, isInverted, BuildHistogram);

				var mapSection = new MapSection(jobId: -1, mapSectionValues: null, subdivisionId: subdivisionId, repoBlockPosition: repoPosition, isInverted: isInverted,
					blockPosition: screenPosition, size: mapAreaInfo.Subdivision.BlockSize, targetIterations: targetIterations, histogramBuilder: BuildHistogram);


				result.Add(mapSection);
			}

			return result;
		}

		#endregion

		#region Create Single MapSectionRequest

		public MapSectionRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, int precision, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapBlockOffset, out var isInverted);

			var result = CreateRequest(screenPosition, repoPosition, precision, isInverted, ownerId, jobOwnerType, subdivision, mapCalcSettings);

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
		public MapSectionRequest CreateRequest(PointInt screenPosition, BigVector repoPosition, int precision, bool isInverted, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var mapPosition = GetMapPosition(subdivision, repoPosition);

			var mapSectionRequest = new MapSectionRequest
			(
				ownerId: ownerId,
				jobOwnerType: jobOwnerType,
				subdivisionId: subdivision.Id.ToString(),
				screenPosition: screenPosition,
				blockPosition: repoPosition,
				isInverted: isInverted,
				mapPosition: mapPosition,
				precision: repoPosition.Precision,
				blockSize: subdivision.BlockSize,
				samplePointDelta: subdivision.SamplePointDelta,
				mapCalcSettings: mapCalcSettings
			);

			return mapSectionRequest;
		}

		private RPoint GetMapPosition(Subdivision subdivision, BigVector localBlockPosition)
		{
			//var nrmSubdivisionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);

			var mapBlockPosition = subdivision.BaseMapPosition.Tranlate(localBlockPosition);

			// Multiply the blockPosition by the blockSize
			var numberOfSamplePointsFromSubOrigin = mapBlockPosition.Scale(subdivision.BlockSize);

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

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse, int jobId, BigVector mapBlockOffset)
		{
			var mapSectionVectors = mapSectionResponse.MapSectionVectors;
			if (mapSectionVectors == null)
			{
				throw new InvalidOperationException("Cannot create the MapSection: the MapSectionResponse is empty.");
			}

			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSectionValues = ObtainMapSectionValues();
			mapSectionValues.Load(mapSectionVectors);

			var mapSection = new MapSection(jobId, mapSectionValues, mapSectionRequest.SubdivisionId, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, BuildHistogram);

			ReturnMapSectionResponse(mapSectionResponse);
			//mapSectionResponse.MapSectionValues = null;

			return mapSection;
		}

		//private void ConvertVecToVals(MapSectionResponse mapSectionResponse)
		//{
		//	var mapSectionVectors = mapSectionResponse.MapSectionVectors;
		//	if (mapSectionVectors == null) return;

		//	if (!mapSectionResponse.RequestCancelled)
		//	{
		//		var mapSectionValues = _mapSectionHelper.ObtainMapSectionValues();
		//		mapSectionValues.Load(mapSectionVectors);
		//		mapSectionResponse.MapSectionValues = mapSectionValues;
		//	}

		//	_mapSectionHelper.ReturnMapSectionResponse(mapSectionResponse);
		//}


		public byte[] GetPixelArray(MapSectionValues mapSectionValues, SizeInt blockSize, ColorMap colorMap, bool invert, bool useEscapeVelocities)
		{
			// Currently EscapeVelocities are not supported.
			useEscapeVelocities = false;

			Debug.Assert(blockSize == _blockSize, "The block sizes do not match.");

			var result = new byte[_pixelArraySize];
			var counts = mapSectionValues.Counts;
			var previousCountVal = counts[0];

			var sourcePtr = 0;
			var resultRowPtr = invert ? _maxRowIndex * _pixelStride : 0;
			var resultRowPtrIncrement = invert ? -1 * _pixelStride : _pixelStride;
			var sourcePtrUpperBound = _rowCount * _sourceStride;

			if (useEscapeVelocities)
			{
				var escapeVelocities = new ushort[counts.Length]; // mapSectionValues.EscapeVelocities;
				for (; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < _sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, previousCountVal);

						var escapeVelocity = escapeVelocities[sourcePtr] / VALUE_FACTOR;
						CheckEscapeVelocity(escapeVelocity);

						colorMap.PlaceColor(countVal, escapeVelocity, new Span<byte>(result, resultPtr, BYTES_PER_PIXEL));

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}
			else
			{
				for (; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < _sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, previousCountVal);

						colorMap.PlaceColor(countVal, escapeVelocity:0, new Span<byte>(result, resultPtr, BYTES_PER_PIXEL));
						
						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}

			return result;
		}

		[Conditional("DEBUG2")]
		private void TrackValueSwitches(ushort countVal, ushort previousCountVal)
		{
			if (countVal != previousCountVal)
			{
				NumberOfCountValSwitches++;
				previousCountVal = countVal;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckEscapeVelocity(double escapeVelocity)
		{
			if (escapeVelocity > 1.0)
			{
				Debug.WriteLine($"The Escape Velocity is greater than 1.0");
			}
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

		#region MapSectionValues / MapSectionVectors

		public MapSectionVectors ObtainMapSectionVectors()
		{
			var result = _mapSectionVectorsPool.Obtain();
			return result;
		}

		public MapSectionValues ObtainMapSectionValues()
		{
			var result = _mapSectionValuesPool.Obtain();
			return result;
		}

		public MapSectionZVectors ObtainMapSectionZVectors(int limbCount)
		{
			if (limbCount != 3)
			{
				Debug.WriteLine($"Getting Zvectors with Lc = {limbCount}.");
			}

			var result = _mapSectionZVectorsPool.Obtain(limbCount);
			return result;
		}

		public void ReturnMapSection(MapSection mapSection)
		{
			if (mapSection.MapSectionValues != null)
			{
				_mapSectionValuesPool.Free(mapSection.MapSectionValues);
				mapSection.MapSectionValues = null;
			}
		}

		public void ReturnMapSectionRequest(MapSectionRequest mapSectionRequest)
		{
			if (mapSectionRequest.MapSectionVectors != null)
			{
				_mapSectionVectorsPool.Free(mapSectionRequest.MapSectionVectors);
				mapSectionRequest.MapSectionVectors = null;
			}

			if (mapSectionRequest.MapSectionZVectors != null)
			{
				_mapSectionZVectorsPool.Free(mapSectionRequest.MapSectionZVectors);
				mapSectionRequest.MapSectionZVectors = null;
			}
		}

		public void ReturnMapSectionResponse(MapSectionResponse mapSectionResponse)
		{
			if (mapSectionResponse.MapSectionVectors != null)
			{
				_mapSectionVectorsPool.Free(mapSectionResponse.MapSectionVectors);
				mapSectionResponse.MapSectionVectors = null;
			}

			if (mapSectionResponse.MapSectionZVectors != null)
			{
				_mapSectionZVectorsPool.Free(mapSectionResponse.MapSectionZVectors);
				mapSectionResponse.MapSectionZVectors = null;
			}
		}

		public MapSectionResponse Duplicate(MapSectionResponse mapSectionResponse)
		{
			var result = new MapSectionResponse(mapSectionResponse.MapSectionId, mapSectionResponse.OwnerId, mapSectionResponse.JobOwnerType, mapSectionResponse.SubdivisionId,
				mapSectionResponse.BlockPosition, mapSectionResponse.MapCalcSettings, mapSectionResponse.AllRowsHaveEscaped);

			if (mapSectionResponse.MapSectionVectors != null)
			{
				var newCopyOfMapSectionVectors = _mapSectionVectorsPool.DuplicateFrom(mapSectionResponse.MapSectionVectors);
				result.MapSectionVectors = newCopyOfMapSectionVectors;
			}

			//if (mapSectionResponse.MapSectionZVectors != null)
			//{
			//	var newCopyOfMapSectionZVectors = _mapSectionZVectorsPool.DuplicateFrom(mapSectionResponse.MapSectionZVectors);
			//	result.MapSectionZVectors = newCopyOfMapSectionZVectors;
			//}

			return result;
		}

		#endregion

		#region DTO Support



		#endregion
	}
}
