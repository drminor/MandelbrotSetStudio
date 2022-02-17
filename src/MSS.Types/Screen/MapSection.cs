using System;

namespace MSS.Types.Screen
{
	public class MapSection
	{
		public PointInt BlockPosition { get; init; }
		//public PointInt CanvasPosition { get; init; }
		public SizeInt Size { get; init; }
		public byte[] Pixels1d { get; init; }

		public MapSection(PointInt blockPosition, /*PointInt canvasPosition, */SizeInt size, byte[] pixels1d)
		{
			BlockPosition = blockPosition;
			//CanvasPosition = canvasPosition;
			Size = size;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));
		}

	}
}


