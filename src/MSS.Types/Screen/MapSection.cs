using MSS.Types.MSet;
using System;

namespace MSS.Types.Screen
{
	public class MapSection
	{
		public Subdivision Subdivision { get; init; }
		public PointDbl CanvasPosition { get; init; }
		public byte[] Pixels1d { get; init; }

		public MapSection(Subdivision subdivision, PointDbl canvasPosition, byte[] pixels1d)
		{
			Subdivision = subdivision ?? throw new ArgumentNullException(nameof(subdivision));
			CanvasPosition = canvasPosition;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));
		}

	}
}


