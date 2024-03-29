﻿using MSS.Types;
using MSS.Types.MSetOld;
using System;
using System.Collections.Generic;

namespace MSetInfoRepo
{
	public static class MFileHelper
	{
        internal static MSetInfoOld GetMSetInfo(MFileInfo mFileInfo)
		{
            var colorMap = GetColorMap(mFileInfo.ColorMapEntries, mFileInfo.MapCalcSettings.MaxIterations, mFileInfo.HighColorCss);
            var result = new MSetInfoOld(mFileInfo.Name, mFileInfo.ApCoords, mFileInfo.IsHighRes, mFileInfo.MapCalcSettings, colorMap);
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
