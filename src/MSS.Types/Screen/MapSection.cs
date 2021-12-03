using MSS.Types.MSet;
using System;

namespace MSS.Types.Screen
{
	public class MapSection
	{
		public Subdivision Subdivision { get; init; }
		public PointInt BlockPosition { get; init; }
		public byte[] Pixels1d { get; init; }

		public MapSection(Subdivision subdivision, PointInt blockPosition, byte[] pixels1d)
		{
			Subdivision = subdivision ?? throw new ArgumentNullException(nameof(subdivision));
			BlockPosition = blockPosition;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));
		}

	}
}


