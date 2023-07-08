﻿using MSS.Types;
using System;

namespace MSetExplorer
{
	public class InitialDisplaySettingsEventArgs : EventArgs
	{
		public SizeDbl UnscaledExtent { get; init; }
		public VectorDbl ContentOffset { get; init; }
		public double MinContentScale { get; init; }
		public double MaxContentScale { get; init; }
		public double ContentScale { get; init; }

		public InitialDisplaySettingsEventArgs(SizeDbl unscaledExtent, VectorDbl contentOffset, double minContentScale, double maxContentScale, double contentScale)
		{
			UnscaledExtent = unscaledExtent;
			ContentOffset = contentOffset;
			MinContentScale = minContentScale;
			MaxContentScale = maxContentScale;
			ContentScale = contentScale;
		}
	}

}
