using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.MSetOld;
using System.Collections.Generic;

namespace MSetInfoRepo
{
	internal record MFileInfo(string Name, ApCoords ApCoords, bool IsHighRes, 
		MapCalcSettings MapCalcSettings, 
		IList<ColorMapEntry> ColorMapEntries, string HighColorCss);
}

