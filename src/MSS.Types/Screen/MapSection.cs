using System;

namespace MSS.Types.Screen
{
	public class MapSection
	{
		public PointDbl CanvasPosition { get; init; }
		public SizeInt Size { get; init; }
		public byte[] Pixels1d { get; init; }

		public MapSection(PointDbl canvasPosition, SizeInt size, byte[] pixels1d)
		{	
			CanvasPosition = canvasPosition;
			Size = size;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));
		}

	}
}


