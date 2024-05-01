using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class ColorBandSetEditArgs
	{
		public ColorBandSetEditOperation Operation { get; init; }
		public int StartingIndex { get; init; }
		public int? EndingIndex { get; init; }
		public int NewColorBandsCount { get; init; }

		public ReservedColorBand[]? ReservedColorBands { get; set; }

		public ColorBand[]? NewColorBands { get; set; }
		public int[]? UpdatedCutoffs { get; set; }
		public int[]? UpdatedPreviousCutoffs { get; set; }

		public int NumberOfExistingBands { get; init; }
		public int DistributionExpansionAmount { get; init; }

		public int Index => StartingIndex;
		public ColorBand? NewColorBand => NewColorBands?[0];
		public ReservedColorBand? ReservedColorBand => ReservedColorBands?[0];

		public ColorBandSetEditArgs(ColorBandSetEditOperation operation, int index, ReservedColorBand? reservedColorBand = null)
			: this(operation, index, endingIndex: null, newColorBandsCount: 0, reservedColorBand == null ? null : new ReservedColorBand[] { reservedColorBand })
		{
		}

		public ColorBandSetEditArgs(ColorBandSetEditOperation operation, int startingIndex, int? endingIndex, int newColorBandsCount, ReservedColorBand[]? reservedColorBands = null)
		{
			Operation = operation;
			StartingIndex = startingIndex;
			EndingIndex = endingIndex;
			NewColorBandsCount = newColorBandsCount;
			NewColorBands = null;
			UpdatedCutoffs = null;
			UpdatedPreviousCutoffs = null;
			ReservedColorBands = reservedColorBands;

			if (newColorBandsCount > 0)
			{
				if (!endingIndex.HasValue)
				{
					throw new InvalidOperationException("The newColorBandsCount argument is > 0, but we have no endingIndex.");
				}

				Debug.Assert(endingIndex.Value >= startingIndex, "The starting index is > than the ending index.");

				NumberOfExistingBands = 1 + endingIndex.Value - startingIndex;
				DistributionExpansionAmount = newColorBandsCount - NumberOfExistingBands;
			}
		}

	}
}
