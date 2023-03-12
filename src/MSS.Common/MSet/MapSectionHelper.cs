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

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;

		private SizeInt _blockSize;
		private readonly int _rowCount;
		private readonly int _sourceStride;
		private readonly int _maxRowIndex;

		private readonly int _pixelArraySize;
		private readonly int _pixelStride;

		private int _currentPrecision;
		private int _currentLimbCount;

		#endregion

		#region Constructor

		public MapSectionHelper(MapSectionVectorsPool mapSectionVectorsPool, MapSectionZVectorsPool mapSectionZVectorsPool)
		{
			_mapSectionVectorsPool = mapSectionVectorsPool;
			_mapSectionZVectorsPool = mapSectionZVectorsPool;

			_blockSize = mapSectionVectorsPool.BlockSize;
			_rowCount = _blockSize.Height;
			_sourceStride = _blockSize.Width;
			_maxRowIndex = _blockSize.Height - 1;

			_pixelArraySize = _blockSize.NumberOfCells * BYTES_PER_PIXEL;
			_pixelStride = _sourceStride * BYTES_PER_PIXEL;

			_currentPrecision = -1;
			_currentLimbCount = 1;
		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		public int MaxPeakSectionVectors => _mapSectionVectorsPool.MaxPeak;
		public int MaxPeakSectionZVectors => _mapSectionZVectorsPool.MaxPeak;

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

			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = GetBinaryPrecision(mapAreaInfo);

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, precision, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public IList<MapSection> CreateEmptyMapSections(MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSection>();

			var targetIterations = mapCalcSettings.TargetIterations;

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating empty MapSections. The map extent is {mapExtentInBlocks}.");

			var subdivisionId = mapAreaInfo.Subdivision.Id.ToString();

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapAreaInfo.MapBlockOffset, out var isInverted);

				var mapSection = new MapSection(jobId: -1, mapSectionVectors: null, subdivisionId: subdivisionId, repoBlockPosition: repoPosition, isInverted: isInverted,
					blockPosition: screenPosition, size: mapAreaInfo.Subdivision.BlockSize, targetIterations: targetIterations, histogramBuilder: BuildHistogram);


				result.Add(mapSection);
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
		public MapSectionRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, int precision, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings)
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
				isInverted: isInverted,
				mapPosition: mapPosition,
				precision: precision,
				limbCount: limbCount,
				blockSize: subdivision.BlockSize,
				samplePointDelta: subdivision.SamplePointDelta,
				mapCalcSettings: mapCalcSettings
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

		#region Create MapSection

		public MapSection CreateMapSection(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors, int jobId, BigVector mapBlockOffset)
		{
			var repoBlockPosition = mapSectionRequest.BlockPosition;
			var isInverted = mapSectionRequest.IsInverted;
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			var mapSection = new MapSection(jobId, mapSectionVectors, mapSectionRequest.SubdivisionId, repoBlockPosition, isInverted,
				screenPosition, mapSectionRequest.BlockSize, mapSectionRequest.MapCalcSettings.TargetIterations, BuildHistogram);


			return mapSection;
		}

		public byte[] GetPixelArray(MapSectionVectors mapSectionVectors, SizeInt blockSize, ColorMap colorMap, bool invert, bool useEscapeVelocities)
		{
			// Currently EscapeVelocities are not supported.
			useEscapeVelocities = false;

			Debug.Assert(blockSize == _blockSize, "The block sizes do not match.");

			var result = new byte[_pixelArraySize];
			var counts = mapSectionVectors.Counts;
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

		#region MapSectionVectors

		public MapSectionVectors ObtainMapSectionVectors()
		{
			var result = _mapSectionVectorsPool.Obtain();
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
			if (mapSection.MapSectionVectors != null)
			{
				_mapSectionVectorsPool.Free(mapSection.MapSectionVectors);
				mapSection.MapSectionVectors = null;
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

		public MapSectionVectors Duplicate(MapSectionVectors mapSectionVectors)
		{
			var result = _mapSectionVectorsPool.DuplicateFrom(mapSectionVectors);
			return result;
		}

		#endregion
	}
}
