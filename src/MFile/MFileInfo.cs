using System.Collections.Generic;

namespace MFile
{
	public record MFileInfo(string Name, SCoords Coords, bool isHighRes, 
		int MaxIterations, int Threshold, int InterationsPerStep, 
		IList<ColorMapEntry> ColorMapEntries, string HighColorCss);
}

