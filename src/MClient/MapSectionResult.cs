using FSTypes;
using System;

namespace MClient
{
	public class MapSectionResult
	{
		public readonly int JobId;

		public readonly RectangleInt MapSection;

		public readonly int[] ImageData;

		public MapSectionResult(int jobId, RectangleInt mapSection, int[] imageData)
		{
			JobId = jobId;
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
		}
	}
}
