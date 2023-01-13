using MSS.Common.APValues;

namespace MSetGeneratorPrototype
{
	public ref struct SamplePointValues
	{
		public SamplePointValues(FP31Deck crs, FP31Deck cis, FP31Deck zrs, FP31Deck zis, Span<bool> hasEscapedFlags, Span<ushort> counts, Span<ushort> escapeVelocities)
		{
			Crs = crs ?? throw new ArgumentNullException(nameof(crs));
			Cis = cis ?? throw new ArgumentNullException(nameof(cis));
			Zrs = zrs ?? throw new ArgumentNullException(nameof(zrs));
			Zis = zis ?? throw new ArgumentNullException(nameof(zis));
			HasEscapedFlags = hasEscapedFlags;
			Counts = counts;
			EscapeVelocities = escapeVelocities;
		}

		public FP31Deck Crs { get; init; }
		public FP31Deck Cis { get; init; }
		public FP31Deck Zrs { get; init; }
		public FP31Deck Zis { get; init; }
		public Span<bool> HasEscapedFlags { get; init; }
		public Span<ushort> Counts { get; init; }
		public Span<ushort> EscapeVelocities { get; init; }

		public int ValueCount => Crs.ValueCount;
		public int VectorCount => Crs.VectorCount;
	}
}

