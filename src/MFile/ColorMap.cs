using System;

namespace MFile
{
    public class ColorMap
    {
        private readonly ColorMapEntry[] _colorMapEntries;

        public readonly int[] CutOffs;
        public readonly ColorMapEntry HighColorEntry;

        public ColorMap(ColorMapEntry[] colorMapEntries, string highColor)
        {
			HighColorEntry = new ColorMapEntry(-1, highColor, ColorMapBlendStyle.None, highColor);

			_colorMapEntries = colorMapEntries;
            CutOffs = BuildCutOffs(colorMapEntries);
			SetBucketWidths(colorMapEntries);
            SetEndColors(colorMapEntries);
        }

        public int[] GetColor(int countVal, double escapeVelocity)
        {
            ColorMapEntry cme = GetColorMapEntry(countVal);
            int[] result = GetBlendedColor(cme, countVal, escapeVelocity);
            return result;
        }
         
        private static int[] GetBlendedColor(ColorMapEntry cme, int countVal, double escapeVelocity)
        {
            int[] result;
            if(cme.BlendStyle == ColorMapBlendStyle.None)
            {
                result = cme.StartColor.ColorComps;
                return result;
            }

            int botBucketVal = cme.PrevCutOff;

   //         int[] cStart;

   //         if (countVal == botBucketVal)
   //         {
   //             cStart = cme.StartColor.ColorComps;
   //         }
   //         else
   //         {
			//	double stepFactor = (-1 + countVal - botBucketVal) / (double)cme.BucketWidth;
			//	cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);
			//}

			////double stepFactor = (countVal - botBucketVal) / (double)cme.BucketWidth;
			////cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

			//double intraStepFactor = escapeVelocity / cme.BucketWidth;

			//result = Interpolate(cStart, cme.StartColor.ColorComps, cme.EndColor.ColorComps, intraStepFactor);

			double stepFactor = (countVal + escapeVelocity - botBucketVal) / (double)cme.BucketWidth;
			result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);


			return result;
        }

        private static int[] Interpolate(int[] cStart, int[] c1, int[] c2, double factor)
        {
            if(factor == 0)
            {
                return cStart;
            }
            else
            {
                double rd = cStart[0] + (c2[0] - c1[0]) * factor;
                double gd = cStart[1] + (c2[1] - c1[1]) * factor;
                double bd = cStart[2] + (c2[2] - c1[2]) * factor;

                int r = (int) Math.Round(rd);
                int g = (int)Math.Round(gd);
                int b = (int)Math.Round(bd);

                if (r < 0 || r > 255)
                {
                    //console.log('Bad red value.');
                }

                if (g < 0 || g > 255)
                {
                    //console.log('Bad green value.');
                }

                if (b < 0 || b > 255)
                {
                    //console.log('Bad blue value.');
                }

                int[] result = new int[3];
                result[0] = r;
                result[1] = g;
                result[2] = b;

                return result;
            }
        }

        private ColorMapEntry GetColorMapEntry(int countVal)
        {
            ColorMapEntry result;
            int newIndex = System.Array.BinarySearch(CutOffs, countVal);

            if(newIndex < 0)
            {
                newIndex = ~newIndex;
            }
			else
			{
				newIndex++;
			}

            if (newIndex > CutOffs.Length - 1)
            {
                result = HighColorEntry;
            }
            else
            {
                result = _colorMapEntries[newIndex];
            }

            return result;
        }

        private static int[] BuildCutOffs(ColorMapEntry[] colorMapEntries)
        {
            int[] result = new int[colorMapEntries.Length];

            for (int ptr = 0; ptr < colorMapEntries.Length; ptr++)
            {
                result[ptr] = colorMapEntries[ptr].CutOff;
            }
            return result;
        }

        private static void SetBucketWidths(ColorMapEntry[] colorMapEntries)
        {
            colorMapEntries[0].PrevCutOff = 0;
            colorMapEntries[0].BucketWidth = colorMapEntries[0].CutOff;

            int prevCutOff = colorMapEntries[0].CutOff;

            for (int ptr = 1; ptr < colorMapEntries.Length; ptr++)
            {
                colorMapEntries[ptr].PrevCutOff = prevCutOff;
                colorMapEntries[ptr].BucketWidth = colorMapEntries[ptr].CutOff - prevCutOff;

                prevCutOff = colorMapEntries[ptr].CutOff;
            }

            //colorMapEntries[colorMapEntries.Length - 1].PrevCutOff = prevCutOff;
        }

        private void SetEndColors(ColorMapEntry[] colorMapEntries)
        {
            for (int ptr = 0; ptr < colorMapEntries.Length; ptr++)
            {
                if(colorMapEntries[ptr].BlendStyle == ColorMapBlendStyle.Next)
                {
					if (ptr == colorMapEntries.Length - 1)
					{
						colorMapEntries[ptr].EndColor = new ColorMapColor(HighColorEntry.StartColor.ColorComps);
					}
					else
					{
						colorMapEntries[ptr].EndColor = new ColorMapColor(colorMapEntries[ptr + 1].StartColor.ColorComps);
					}
                }
            }
        }

        public int GetCutOff(int countVal)
        {
            ColorMapEntry cme = GetColorMapEntry(countVal);
            return cme.CutOff;
        }

        public static ColorMap GetFromColorMapForExport(ColorMapForExport cmfe)
        {
            ColorMap result;
            if (cmfe.Version == 1 || cmfe.Version == -1)
            {
                result = GetFromColorMapForExportV1(cmfe);
            }
            else
            {
                result = GetFromColorMapForExportV2(cmfe);
            }

            return result;
        }

        private static ColorMap GetFromColorMapForExportV1(ColorMapForExport cmfe)
        {
            ColorMapEntry[] newRanges = new ColorMapEntry[cmfe.Ranges.Length];

            for (int ptr = 0; ptr < cmfe.Ranges.Length; ptr++)
            {
                ColorMapEntryForExport sourceCme = cmfe.Ranges[ptr];

                newRanges[ptr] = new ColorMapEntry(sourceCme.CutOff, sourceCme.CssColor);
            }

            ColorMap result = new(newRanges, cmfe.HighColorCss);

            return result;
        }

        private static ColorMap GetFromColorMapForExportV2(ColorMapForExport cmfe)
        {
            ColorMapEntry[] newRanges = new ColorMapEntry[cmfe.Ranges.Length];

            for (int ptr = 0; ptr < cmfe.Ranges.Length; ptr++)
            {
                ColorMapEntryForExport sourceCme = cmfe.Ranges[ptr];

                newRanges[ptr] = new ColorMapEntry(sourceCme.CutOff, sourceCme.StartCssColor, sourceCme.BlendStyle, sourceCme.EndCssColor);
            }

            ColorMap result = new(newRanges, cmfe.HighColorCss);

            return result;
        }
    }
}
