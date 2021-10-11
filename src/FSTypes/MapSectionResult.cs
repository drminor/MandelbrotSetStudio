﻿using System;

namespace FSTypes
{
	public class MapSectionResult
	{
		public readonly int JobId;

		public readonly MapSection MapSection;

		public readonly int[] ImageData;

		public MapSectionResult(int jobId, MapSection mapSection, int[] imageData)
		{
			JobId = jobId;
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
		}
	}
}
