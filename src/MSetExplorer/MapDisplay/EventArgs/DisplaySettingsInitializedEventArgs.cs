using MSS.Types;
using System;

namespace MSetExplorer
{
	public class DisplaySettingsInitializedEventArgs : EventArgs
	{
		public SizeDbl UnscaledExtent { get; init; }
		public VectorDbl ContentOffset { get; init; }
		public double ContentScale { get; init; }

		public DisplaySettingsInitializedEventArgs(SizeDbl unscaledExtent, VectorDbl contentOffset, double contentScale)
		{
			UnscaledExtent = unscaledExtent;
			ContentOffset = contentOffset;
			ContentScale = contentScale;
		}
	}

}
