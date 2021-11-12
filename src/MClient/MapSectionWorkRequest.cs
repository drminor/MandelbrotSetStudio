using MSS.Types;
using System;

namespace MClient
{
	public class MapSectionWorkRequest
	{
		public readonly RectangleInt MapSection;
		public int MaxIterations;

		public readonly int HPtr;
		public readonly int VPtr;

		public MapSectionWorkRequest(RectangleInt mapSection, int maxIterations, int hPtr, int vPtr)
		{
			MaxIterations = maxIterations;
			MapSection = mapSection;
			HPtr = hPtr;
			VPtr = vPtr;
		}
	}
}
