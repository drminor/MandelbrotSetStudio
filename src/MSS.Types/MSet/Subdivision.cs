using System;

namespace MSS.Types.MSet
{
	public class Subdivision
	{
		public RPoint Position { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }

		public Subdivision(RPoint position, SizeInt blockSize, RSize samplePointDelta)
		{
			Position = position ?? throw new ArgumentNullException(nameof(position));
			BlockSize = blockSize ?? throw new ArgumentNullException(nameof(blockSize));
			SamplePointDelta = samplePointDelta ?? throw new ArgumentNullException(nameof(samplePointDelta));
		}
	}
}
