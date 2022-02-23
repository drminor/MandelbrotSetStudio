using System;

namespace MSS.Types.Screen
{
	public class MapSection
	{
		public PointInt BlockPosition { get; init; }
		public SizeInt Size { get; init; }
		public byte[] Pixels1d { get; init; }

		public string? SubdivisionId { get; init; }
		public BigVector RepoBlockPosition { get; init; }

		public MapSection(PointInt blockPosition, SizeInt size, byte[] pixels1d)
		{
			BlockPosition = blockPosition;
			Size = size;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));

			SubdivisionId = null;
			RepoBlockPosition = new BigVector();
		}

		public MapSection(PointInt blockPosition, SizeInt size, byte[] pixels1d, string subdivisionId, BigVector repoBlockPosition)
		{
			BlockPosition = blockPosition;
			Size = size;
			Pixels1d = pixels1d ?? throw new ArgumentNullException(nameof(pixels1d));

			SubdivisionId = subdivisionId;
			RepoBlockPosition = repoBlockPosition;

		}

	}
}


