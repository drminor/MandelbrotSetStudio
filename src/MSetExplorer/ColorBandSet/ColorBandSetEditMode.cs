using System;

namespace MSetExplorer
{
	[Flags]
	public enum ColorBandSetEditMode
	{
		Cutoffs = 1,
		Colors = 2,
		Bands = 3
	}


	[Flags]
	public enum ColorBandSelectionType
	{
		None = 0,
		Cutoff = 1,
		Color = 2,
		Band = 3
	}
}
