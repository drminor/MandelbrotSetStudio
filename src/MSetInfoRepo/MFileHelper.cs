using FSTypes;
using System;
using System.Collections.Generic;

namespace MSetInfoRepo
{
	public static class MFileHelper
	{
        internal static MSetInfo GetMSetInfo(MFileInfo mFileInfo)
		{
            var colorMap = GetColorMap(mFileInfo.ColorMapEntries, mFileInfo.MaxIterations, mFileInfo.HighColorCss);
            var result = new MSetInfo(mFileInfo.Name, mFileInfo.Coords, mFileInfo.isHighRes, mFileInfo.MaxIterations, mFileInfo.Threshold, mFileInfo.InterationsPerStep, colorMap);
            return result;
		}

        public static ColorMap GetColorMap(IList<ColorMapEntry> colorMapEntries, int maxIterations, string highColorCss)
        {
            ColorMapEntry[] newRanges = new ColorMapEntry[colorMapEntries.Count];

            for (int ptr = 0; ptr < colorMapEntries.Count; ptr++)
            {
                ColorMapEntry sourceCme = colorMapEntries[ptr];

                ColorMapBlendStyle blendStyle = Enum.Parse<ColorMapBlendStyle>(sourceCme.BlendStyle.ToString());
                newRanges[ptr] = new ColorMapEntry(sourceCme.CutOff, sourceCme.StartColor, blendStyle, sourceCme.EndColor);
            }

            ColorMap result = new(newRanges, maxIterations, highColorCss);

            return result;
        }
    }
}
