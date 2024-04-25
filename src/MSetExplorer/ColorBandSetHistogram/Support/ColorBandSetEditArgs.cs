using MSS.Types;

namespace MSetExplorer
{
	public class ColorBandSetEditArgs
	{
		public ColorBandSetEditOperation Operation { get; init; }
		public int StartingIndex { get; init; }
		public int? EndingIndex { get; init; }
		public int NewColorBandsCount { get; init; }
		public ReservedColorBand[]? ReservedColorBands { get; init; }

		public ColorBand? NewColorBand { get; set; }

		public int Index => StartingIndex;
		public ReservedColorBand? ReservedColorBand => ReservedColorBands?[0];

		public ColorBandSetEditArgs(ColorBandSetEditOperation operation, int index, ReservedColorBand? reservedColorBand = null)
		{
			Operation = operation;
			StartingIndex = index;
			EndingIndex = null;
			NewColorBandsCount = 0;
			NewColorBand = null;
			ReservedColorBands = reservedColorBand == null ? null: new ReservedColorBand[] { reservedColorBand };
		}

		public ColorBandSetEditArgs(ColorBandSetEditOperation operation, int startingIndex, int endingIndex, int newColorBandsCount, ReservedColorBand[]? reservedColorBands = null)
		{
			Operation = operation;
			StartingIndex = startingIndex;
			EndingIndex = endingIndex;
			NewColorBandsCount = newColorBandsCount;
			NewColorBand = null;
			ReservedColorBands = reservedColorBands;
		}

	}
}
