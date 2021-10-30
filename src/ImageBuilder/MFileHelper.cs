using FSTypes;
using System;
using System.Collections.Generic;

namespace ImageBuilder
{
	public static class MFileHelper
	{
        public static MSetInfo GetMSetInfo(MFile.MFileInfo mFileInfo)
		{
            var result = new MSetInfo(mFileInfo.Name, GetSCoords(mFileInfo.Coords), mFileInfo.MaxIterations, mFileInfo.Threshold, mFileInfo.InterationsPerStep, GetColorMap(mFileInfo.ColorMapEntries, mFileInfo.HighColorCss));
            return result;
		}

        public static SCoords GetSCoords(MFile.SCoords sCoords)
		{
            var leftTop = new SPoint(sCoords.sx, sCoords.ey);
            var rightBot = new SPoint(sCoords.ex, sCoords.sy);
            var result = new SCoords(leftTop, rightBot);

            return result;
		}

        public static ColorMap GetColorMap(IList<MFile.ColorMapEntry> colorMapEntries, string highColorCss)
        {
            ColorMapEntry[] newRanges = new ColorMapEntry[colorMapEntries.Count];

            for (int ptr = 0; ptr < colorMapEntries.Count; ptr++)
            {
                MFile.ColorMapEntry sourceCme = colorMapEntries[ptr];

                ColorMapBlendStyle blendStyle = Enum.Parse<ColorMapBlendStyle>(sourceCme.BlendStyle.ToString());
                newRanges[ptr] = new ColorMapEntry(sourceCme.Cutoff, sourceCme.StartCssColor, blendStyle, sourceCme.EndCssColor);
            }

            ColorMap result = new(newRanges, highColorCss);

            return result;
        }
    }
}
