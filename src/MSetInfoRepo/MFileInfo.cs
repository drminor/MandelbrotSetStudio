﻿using MSS.Types;
using System.Collections.Generic;

// TODO: Use a ColorMap object instead of a IList<ColorMapEntry> and HighColorCss.
namespace MSetInfoRepo
{
	internal record MFileInfo(string Name, ApCoords ApCoords, bool isHighRes, 
		int MaxIterations, int Threshold, int InterationsPerStep, 
		IList<ColorMapEntry> ColorMapEntries, string HighColorCss);
}

