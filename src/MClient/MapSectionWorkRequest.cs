using MapSectionRepo;
using System;

namespace MClient
{
	public class MapSectionWorkRequest
	{
		public readonly MapSection MapSection;
		public int MaxIterations;

		public readonly int HPtr;
		public readonly int VPtr;

		public MapSectionWorkRequest(MapSection mapSection, int maxIterations, int hPtr, int vPtr)
		{
			MaxIterations = maxIterations;
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			HPtr = hPtr;
			VPtr = vPtr;
		}
	}
}
