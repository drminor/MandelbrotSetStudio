using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSS.Common
{
	public class MapSectionVectorProvider
	{
		#region Private Properties

		private const int MIN_LIMB_COUNT = 1;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		private readonly MapSectionVectorsPool2 _mapSectionVectorsPool2;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;

		//private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionVectorProvider(MapSectionVectorsPool2 mapSectionVectorsPool2, MapSectionZVectorsPool mapSectionZVectorsPool)
		{
			_mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE);
			_mapSectionVectorsPool2 = mapSectionVectorsPool2;
			_mapSectionZVectorsPool = mapSectionZVectorsPool;

		}

		#endregion

		#region Public Properties

		public int MapSectionsVectorsInPool => _mapSectionVectorsPool.TotalFree;
		public int MapSectionsZVectorsInPool => _mapSectionZVectorsPool.TotalFree;

		public int MaxPeakSectionVectors => _mapSectionVectorsPool.MaxPeak;
		public int MaxPeakSectionZVectors => _mapSectionZVectorsPool.MaxPeak;

		#endregion

		#region MapSectionVectors

		public MapSectionVectors ObtainMapSectionVectors()
		{
			var result = _mapSectionVectorsPool.Obtain();

			//Debug.WriteLine($"Just obtained a MSVectors. Currently: {_mapSectionVectorsPool.TotalFree} available; {_mapSectionVectorsPool.MaxPeak} max allocated.");

			return result;
		}

		public MapSectionVectors2 ObtainMapSectionVectors2()
		{
			var result = _mapSectionVectorsPool2.Obtain();

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
			//mapSection.MapSectionVectors = ReturnMapSectionVectors(mapSection.MapSectionVectors);
			mapSection.MapSectionVectors?.Dispose();
		}

		public void ReturnMapSectionRequest(MapSectionRequest mapSectionRequest)
		{
			_ = mapSectionRequest.MapSectionVectors2 = ReturnMapSectionVectors2(mapSectionRequest.MapSectionVectors2);
			_ =mapSectionRequest.MapSectionZVectors = ReturnMapSectionZVectors(mapSectionRequest.MapSectionZVectors);
		}

		public void ReturnMapSectionResponse(MapSectionResponse mapSectionResponse)
		{
			_ = mapSectionResponse.MapSectionVectors = ReturnMapSectionVectors(mapSectionResponse.MapSectionVectors);
			_ = mapSectionResponse.MapSectionVectors2 = ReturnMapSectionVectors2(mapSectionResponse.MapSectionVectors2);
			_ = mapSectionResponse.MapSectionZVectors = ReturnMapSectionZVectors(mapSectionResponse.MapSectionZVectors);
		}

		public MapSectionVectors2? ReturnMapSectionVectors2(MapSectionVectors2? mapSectionVectors2)
		{
			if (mapSectionVectors2 != null)
			{
				if (_mapSectionVectorsPool2.Free(mapSectionVectors2))
				{
					mapSectionVectors2 = null;
				}
				//mapSectionVectors2.Dispose();
				//mapSectionVectors2 = null;
			}

			return mapSectionVectors2;
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

		#region Merge and Split Methods

		/*
					-----------------
					|		|		|
		Hi			|	0	|	1	|
					|		|		|
					-----------------
					|		|		|
		Low			|	2	|	3	|
					|		|		|
					-----------------
		*/

		//public (MapSection dest2, MapSection dest3) SplitLow(MapAreaInfo2 mapAreaInfo, MapAreaInfo2 x2, int jobNumber, MapSection source)
		//{
		//	var mapSectionVectors = source.MapSectionVectors ?? throw new ArgumentException("The source MapSection must have a non-null MapSectionVectors.");


		//	var dest2Vecs = ObtainMapSectionVectors();
		//	var dest3Vecs = ObtainMapSectionVectors();

		//	var rowCount = mapSectionVectors.BlockSize.Height;
		//	var sourceStride = mapSectionVectors.BlockSize.Width;
		//	var halfSourceStride = sourceStride / 2;
		//	var doubleResultStride = mapSectionVectors.BlockSize.Width * 2;

		//	var sourceCounts = mapSectionVectors.Counts;
		//	var sourceEscapeVelocities = mapSectionVectors.EscapeVelocities;

		//	var dest2Counts = dest2Vecs.Counts;
		//	var dest2EscapeVelocities = dest2Vecs.EscapeVelocities;

		//	var dest3Counts = dest3Vecs.Counts;
		//	var dest3EscapeVelocities = dest2Vecs.EscapeVelocities;

		//	var resultRowPtr = 0;
		//	var sourcePtrUpperBound = rowCount / 2 * sourceStride;

		//	for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += doubleResultStride)
		//	{
		//		var resultPtr = resultRowPtr;

		//		for (var colPtr = 0; colPtr < halfSourceStride; colPtr++)
		//		{
		//			dest2Counts[resultPtr] = sourceCounts[sourcePtr];
		//			dest2EscapeVelocities[resultPtr] = sourceEscapeVelocities[sourcePtr];

		//			resultPtr += 2;
		//			sourcePtr += 1;
		//		}

		//		resultPtr = resultRowPtr;

		//		for (var colPtr = 0; colPtr < halfSourceStride; colPtr++)
		//		{
		//			dest3Counts[resultPtr] = sourceCounts[sourcePtr];
		//			dest3EscapeVelocities[resultPtr] = sourceEscapeVelocities[sourcePtr];

		//			resultPtr += 2;
		//			sourcePtr += 1;
		//		}
		//	}


		//	//// Block Position, relative to the Subdivision's BaseMapPosition
		//	//var localBlockPosition = RMapHelper.ToSubdivisionCoords(source.ScreenPosition, source.JobMapBlockOffset, out var isInverted);

		//	//var subdivision = mapAreaInfo.Subdivision;

		//	//// Absolute position in Map Coordinates.
		//	//var mapPosition = GetMapPosition(subdivision, localBlockPosition);


		//	var dest2 = new MapSection(jobNumber: jobNumber, subdivisionId: "", jobMapBlockPosition: new BigVector(), repoBlockPosition: new BigVector(), isInverted: false, screenPosition: new PointInt(), size: new SizeInt(), targetIterations: source.TargetIterations, isCancelled: false);
		//	var dest3 = new MapSection(jobNumber: jobNumber, subdivisionId: "", jobMapBlockPosition: new BigVector(), repoBlockPosition: new BigVector(), isInverted: false, screenPosition: new PointInt(), size: new SizeInt(), targetIterations: source.TargetIterations, isCancelled: false);

		//	return (dest2, dest3);
		//}

		#endregion
	}
}
