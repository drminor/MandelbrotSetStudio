using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSS.Common
{
	public class MapSectionVectorProvider
	{
		#region Private Properties

		private const int MIN_LIMB_COUNT = 1;

		private readonly MapSectionVectorsPool _mapSectionVectorsPool;
		//private readonly MapSectionVectorsPool2 _mapSectionVectorsPool2;
		private readonly MapSectionZVectorsPool _mapSectionZVectorsPool;

		//private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapSectionVectorProvider(MapSectionVectorsPool mapSectionVectorsPool, MapSectionZVectorsPool mapSectionZVectorsPool)
		{
			_mapSectionVectorsPool = mapSectionVectorsPool;
			//_mapSectionVectorsPool2 = new MapSectionVectorsPool2(RMapConstants.BLOCK_SIZE);
			_mapSectionZVectorsPool = mapSectionZVectorsPool;
		}

		#endregion

		#region Public Properties

		public int MapSectionsVectorsInPool => _mapSectionVectorsPool.TotalFree;
		public int MapSectionsZVectorsInPool => _mapSectionZVectorsPool.TotalFree;

		public int MaxPeakSectionVectors => _mapSectionVectorsPool.MaxPeak;
		public int MaxPeakSectionZVectors => _mapSectionZVectorsPool.MaxPeak;

		public int NumberOfRefusedMapSectionReturns { get; set; }
		public int NumberOfMapSectionVectorsLeased { get; set; }
		public int NumberOfMapSectionVectors2Leased { get; set; }
		public int NumberOfMapSectionZVectorsLeased { get; set; }

		#endregion

		#region Request / Response / MapSection

		public void ReturnToPool(MapSectionRequest mapSectionRequest)
		{
			if (mapSectionRequest.MapSectionVectors2 != null)
			{
				Debug.WriteLine("CHECK THIS, Returning a MapSectionRequest that has a non-null MapSectionVectors2 instance.");
			}

			if (mapSectionRequest.MapSectionZVectors != null)
			{
				Debug.WriteLine("CHECK THIS, Returning a MapSectionRequest that has a non-null MapSectionZVectors instance.");
			}

			//_ = mapSectionRequest.MapSectionVectors2 = ReturnMapSectionVectors2(mapSectionRequest.MapSectionVectors2);
			_ = mapSectionRequest.MapSectionZVectors = ReturnMapSectionZVectors(mapSectionRequest.MapSectionZVectors);
		}

		public void ReturnToPool(MapSectionResponse mapSectionResponse)
		{
			_ = mapSectionResponse.MapSectionVectors = ReturnMapSectionVectors(mapSectionResponse.MapSectionVectors);
			//_ = mapSectionResponse.MapSectionVectors2 = ReturnMapSectionVectors2(mapSectionResponse.MapSectionVectors2);
			_ = mapSectionResponse.MapSectionZVectors = ReturnMapSectionZVectors(mapSectionResponse.MapSectionZVectors);
		}

		public void ReturnToPool(MapSection mapSection)
		{
			mapSection.MapSectionVectors = ReturnMapSectionVectors(mapSection.MapSectionVectors);
			if (mapSection.MapSectionVectors != null)
			{
				NumberOfRefusedMapSectionReturns++;
			}

			//mapSection.MapSectionVectors?.Dispose();
		}

		#endregion

		#region MapSectionVectors

		public MapSectionVectors ObtainMapSectionVectors()
		{
			var result = _mapSectionVectorsPool.Obtain();
			result.ResetObject();
			NumberOfMapSectionVectorsLeased++;

			//Debug.WriteLine($"Just obtained a MSVectors. Currently: {_mapSectionVectorsPool.TotalFree} available; {_mapSectionVectorsPool.MaxPeak} max allocated.");

			return result;
		}

		public MapSectionVectors? ReturnMapSectionVectors(MapSectionVectors? mapSectionVectors)
		{
			if (mapSectionVectors != null)
			{
				if (_mapSectionVectorsPool.Free(mapSectionVectors))
				{
					mapSectionVectors = null;
					NumberOfMapSectionVectorsLeased--;
				}
			}

			return mapSectionVectors;
		}

		#endregion

		#region MapSectionVectors2

		//public MapSectionVectors2 ObtainMapSectionVectors2()
		//{
		//	var result = _mapSectionVectorsPool2.Obtain();
		//	NumberOfMapSectionVectors2Leased++;

		//	//Debug.WriteLine($"Just obtained a MSVectors. Currently: {_mapSectionVectorsPool.TotalFree} available; {_mapSectionVectorsPool.MaxPeak} max allocated.");

		//	return result;
		//}

		//public MapSectionVectors2? ReturnMapSectionVectors2(MapSectionVectors2? mapSectionVectors2)
		//{
		//	if (mapSectionVectors2 != null)
		//	{
		//		//if (_mapSectionVectorsPool2.Free(mapSectionVectors2))
		//		//{
		//		//	mapSectionVectors2 = null;
		//		//	NumberOfMapSectionVectors2Leased--;
		//		//}

		//		mapSectionVectors2.DecreaseRefCount();

		//		if (mapSectionVectors2.ReferenceCount < 1)
		//		{
		//			mapSectionVectors2.Dispose();
		//			mapSectionVectors2 = null;
		//		}
		//	}

		//	return mapSectionVectors2;
		//}

		#endregion

		#region MapSectionZVectors

		public MapSectionZVectors ObtainMapSectionZVectors(int limbCount)
		{
			var adjustedLimbCount = Math.Max(limbCount, MIN_LIMB_COUNT);
			var result = _mapSectionZVectorsPool.Obtain(adjustedLimbCount);

			NumberOfMapSectionZVectorsLeased++;

			return result;
		}

		public MapSectionZVectors? ReturnMapSectionZVectors(MapSectionZVectors? mapSectionZVectors)
		{
			if (mapSectionZVectors != null)
			{
				if (_mapSectionZVectorsPool.Free(mapSectionZVectors))
				{
					mapSectionZVectors = null;
					NumberOfMapSectionZVectorsLeased--;

				}
			}

			return mapSectionZVectors;
		}

		//public MapSectionZVectors ObtainMapSectionZVectorsByPrecision(int precision)
		//{
		//	var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: precision);
		//	return ObtainMapSectionZVectors(apFixedPointFormat.LimbCount);
		//}

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

		//public (MapSection dest2, MapSection dest3) SplitLow(MapCenterAndDelta mapCenterAndDelta, MapCenterAndDelta x2, int jobNumber, MapSection source)
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

		//	//var subdivision = mapCenterAndDelta.Subdivision;

		//	//// Absolute position in Map Coordinates.
		//	//var mapPosition = GetMapPosition(subdivision, localBlockPosition);


		//	var dest2 = new MapSection(jobNumber: jobNumber, subdivisionId: "", jobMapBlockPosition: new BigVector(), repoBlockPosition: new BigVector(), isInverted: false, screenPosition: new PointInt(), size: new SizeInt(), targetIterations: source.TargetIterations, isCancelled: false);
		//	var dest3 = new MapSection(jobNumber: jobNumber, subdivisionId: "", jobMapBlockPosition: new BigVector(), repoBlockPosition: new BigVector(), isInverted: false, screenPosition: new PointInt(), size: new SizeInt(), targetIterations: source.TargetIterations, isCancelled: false);

		//	return (dest2, dest3);
		//}

		#endregion
	}
}
