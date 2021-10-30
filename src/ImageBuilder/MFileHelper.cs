using FSTypes;
using MFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public static class MFileHelper
	{
        public static ColorMap GetFromColorMapForExport(MFileInfo mFileInfo)
        {
            FSTypes.ColorMapEntry[] newRanges = new FSTypes.ColorMapEntry[mFileInfo.ColorMapEntries.Count];

            for (int ptr = 0; ptr < mFileInfo.ColorMapEntries.Count; ptr++)
            {
                MFile.ColorMapEntry sourceCme = mFileInfo.ColorMapEntries[ptr];

                ColorMapBlendStyle blendStyle = Enum.Parse<ColorMapBlendStyle>(sourceCme.BlendStyle.ToString());
                newRanges[ptr] = new FSTypes.ColorMapEntry(sourceCme.Cutoff, sourceCme.StartCssColor, blendStyle, sourceCme.EndCssColor);
            }

            ColorMap result = new(newRanges, mFileInfo.HighColorCss);

            return result;
        }

        //     private static ColorMap GetFromColorMapForExportV1(ColorMapForExport cmfe)
        //     {
        //FSTypes.ColorMapEntry[] newRanges = new FSTypes.ColorMapEntry[cmfe.Ranges.Length];

        //         for (int ptr = 0; ptr < cmfe.Ranges.Length; ptr++)
        //         {
        //             ColorMapEntry sourceCme = cmfe.Ranges[ptr];

        //             newRanges[ptr] = new FSTypes.ColorMapEntry(sourceCme.CutOff, sourceCme.CssColor);
        //         }

        //         ColorMap result = new(newRanges, cmfe.HighColorCss);

        //         return result;
        //     }

        //     private static ColorMap GetFromColorMapForExportV2(ColorMapForExport cmfe)
        //     {
        //FSTypes.ColorMapEntry[] newRanges = new FSTypes.ColorMapEntry[cmfe.Ranges.Length];

        //         for (int ptr = 0; ptr < cmfe.Ranges.Length; ptr++)
        //         {
        //             ColorMapEntry sourceCme = cmfe.Ranges[ptr];

        //             newRanges[ptr] = new FSTypes.ColorMapEntry(sourceCme.CutOff, sourceCme.StartCssColor, sourceCme.BlendStyle, sourceCme.EndCssColor);
        //         }

        //         ColorMap result = new(newRanges, cmfe.HighColorCss);

        //         return result;
        //     }
    }
}
