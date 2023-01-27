using MEngineDataContracts;
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
		private const double VALUE_FACTOR = 10000;
		private readonly DtoMapper _dtoMapper;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionValuesPool _mapSectionValuesPool;

		private SizeInt _blockSize;
		private readonly int _rowCount;
		private readonly int _sourceStride;
		private readonly int _maxRowIndex;

		private readonly int _pixelArraySize;
		private readonly int _pixelStride;	


		#region Constructor

		public MapSectionHelper(MapSectionVectorsPool mapSectionVectorsPool, MapSectionValuesPool mapSectionValuesPool)
		{
			_dtoMapper = new DtoMapper();
			_mapSectionVectorsPool = mapSectionVectorsPool;
			_mapSectionValuesPool = mapSectionValuesPool;

			_blockSize = mapSectionVectorsPool.BlockSize;
			_rowCount = _blockSize.Height;
			_sourceStride = _blockSize.Width;
			_maxRowIndex = _blockSize.Height - 1;

			_pixelArraySize = _blockSize.NumberOfCells * 4;
			_pixelStride = _sourceStride * 4;

		}

		#endregion

		public long NumberOfCountValSwitches { get; private set; }

		public void ReturnMapSection(MapSection mapSection)
		{
			if (mapSection.MapSectionValues != null)
			{
				if (!_mapSectionValuesPool.Free(mapSection.MapSectionValues))
				{
					mapSection.MapSectionValues.Dispose();
				}

				mapSection.Dispose();
			}
		}

		public void ReturnMapSectionResponse(MapSectionServiceResponse mapSectionResponse)
		{
			if (mapSectionResponse.MapSectionVectors != null)
			{
				var mapSectionVectors = mapSectionResponse.MapSectionVectors;
				mapSectionResponse.MapSectionVectors = null;

				if (!_mapSectionVectorsPool.Free(mapSectionVectors))
				{
					mapSectionVectors.Dispose();
				}
			}
		}

		#region Create MapSectionRequests

		public IList<MapSectionServiceRequest> CreateSectionRequests(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections)
		{
			var result = new List<MapSectionServiceRequest>();

			Debug.WriteLine($"Creating section requests from the given list of {emptyMapSections.Count} empty MapSections.");

			foreach (var mapSection in emptyMapSections)
			{
				var screenPosition = mapSection.BlockPosition;
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings);
				result.Add(mapSectionRequest);
			}

			return result;
		}

		public IList<MapSectionServiceRequest> CreateSectionRequests(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			var result = new List<MapSectionServiceRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(mapAreaInfo.CanvasSize, mapAreaInfo.CanvasControlOffset, mapAreaInfo.Subdivision.BlockSize);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			foreach (var screenPosition in Points(mapExtentInBlocks))
			{
				var mapSectionRequest = CreateRequest(screenPosition, mapAreaInfo.MapBlockOffset, ownerId, jobOwnerType, mapAreaInfo.Subdivision, mapCalcSettings);
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

				var mapSection = new MapSection(mapSectionValues: null, subdivisionId: subdivisionId, repoBlockPosition: repoPosition, isInverted: isInverted,
					blockPosition: screenPosition, size: mapAreaInfo.Subdivision.BlockSize, targetIterations: targetIterations, histogramBuilder: BuildHistogram);


				result.Add(mapSection);
			}

			return result;
		}

		#endregion

		#region Create Single MapSectionRequest

		public MapSectionServiceRequest CreateRequest(PointInt screenPosition, BigVector mapBlockOffset, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, mapBlockOffset, out var isInverted);
			var result = CreateRequest(screenPosition, repoPosition, isInverted, ownerId, jobOwnerType, subdivision, mapCalcSettings);

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
		public MapSectionServiceRequest CreateRequest(PointInt screenPosition, BigVector repoPosition, bool isInverted, string ownerId, JobOwnerType jobOwnerType, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var mapPosition = GetMapPosition(subdivision, repoPosition);
			var mapSectionRequest = new MapSectionServiceRequest
			{
				OwnerId = ownerId,
				JobOwnerType = jobOwnerType,
				SubdivisionId = subdivision.Id.ToString(),
				ScreenPosition = screenPosition,
				BlockPosition = _dtoMapper.MapTo(repoPosition),
				BlockSize = subdivision.BlockSize,
				Position = _dtoMapper.MapTo(mapPosition),
				Precision = repoPosition.Precision,
				SamplePointDelta = _dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings,
				//Counts = null,
				//HasEscapedFlags = null,
				MapSectionVectors = null,
				ZValues = null,
				IsInverted = isInverted,
				TimeToCompleteGenRequest = null,
				ProcessingStartTime = DateTime.UtcNow
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

		public MapSection CreateMapSection(MapSectionServiceRequest mapSectionRequest, MapSectionServiceResponse mapSectionResponse, BigVector mapBlockOffset)
		{
			var repoBlockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var isInverted = mapSectionRequest.IsInverted;
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, isInverted, mapBlockOffset);
			//Debug.WriteLine($"Creating MapSection for response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {isInverted}.");

			if (mapSectionResponse.MapSectionVectors != null)
			{
				var mapSectionValues = _mapSectionValuesPool.Obtain();
				mapSectionValues.Load(mapSectionResponse.MapSectionVectors);

				var mapSection = new MapSection(mapSectionValues, mapSectionRequest.SubdivisionId, repoBlockPosition, isInverted,
					screenPosition, mapSectionRequest.BlockSize, mapSectionResponse.MapCalcSettings.TargetIterations, BuildHistogram);

				ReturnMapSectionResponse(mapSectionResponse);

				return mapSection;
			}
			else
			{
				throw new InvalidOperationException("Could not create the MapSection, the MapSectionVectors had a null value.");
				//var mapSection = new MapSection();
				//return mapSection;
			}
		}

		public byte[] GetPixelArray(MapSectionValues mapSectionValues, SizeInt blockSize, ColorMap colorMap, bool invert, bool useEscapeVelocities)
		{
			Debug.Assert(blockSize == _blockSize, "The block sizes do not match.");

			//var numberofCells = blockSize.NumberOfCells;
			var result = new byte[_pixelArraySize];

			var counts = mapSectionValues.Counts;
			var escapeVelocities = mapSectionValues.EscapeVelocities;

			var previousCountVal = counts[0];

			for (var rowPtr = 0; rowPtr < _rowCount; rowPtr++)
			{
				// Calculate the array index for the beginning of this destination and source row.
				//var resultRowPtr = GetResultRowPtr(blockSize.Height - 1, rowPtr, invert);
				var resultRowPtr = invert ? _maxRowIndex - rowPtr : rowPtr;

				var curSourcePtr = rowPtr * _sourceStride;
				var curResultPtr = resultRowPtr * _pixelStride;

				for (var colPtr = 0; colPtr < _sourceStride; colPtr++)
				{
					var countVal = counts[curSourcePtr];

					if (countVal != previousCountVal)
					{
						NumberOfCountValSwitches++;
						previousCountVal = countVal;
					}

					var escapeVelocity = useEscapeVelocities ? escapeVelocities[curSourcePtr] / VALUE_FACTOR : 0;

					if (escapeVelocity > 1.0)
					{
						Debug.WriteLine($"The Escape Velocity is greater than 1.0");
					}

					escapeVelocity = 0;
					//var ccv = Convert.ToUInt16(countVal);

					colorMap.PlaceColor(countVal, escapeVelocity, new Span<byte>(result, curResultPtr, 4));
					curResultPtr += 4;

					curSourcePtr++;
				}
			}

			return result;
		}

		//private int GetResultRowPtr(int maxRowIndex, int rowPtr, bool invert)
		//{
		//	// The Source's origin is at the bottom, left.
		//	// If inverted, the Destination's origin is at the top, left, otherwise bottom, left. 
		//	var result = invert ? maxRowIndex - rowPtr : rowPtr;
		//	return result;
		//}

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
