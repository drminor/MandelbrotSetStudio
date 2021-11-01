using FSTypes;
using System.Collections.Generic;

// TODO: Use a ColorMap object instead of a IList<ColorMapEntry> and HighColorCss.
namespace MFile
{
	public record MFileInfo(string Name, Coords Coords, bool isHighRes, 
		int MaxIterations, int Threshold, int InterationsPerStep, 
		IList<ColorMapEntry> ColorMapEntries, string HighColorCss);
}

