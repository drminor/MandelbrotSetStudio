using System;
using System.Collections.Generic;
using System.Linq;

namespace FSTypes
{
    public class ColorMap
    {
        private ColorMapEntry[] _colorMapEntries { get; init; }
        public ColorMapEntry HighColorEntry { get; init; }


        private readonly int[] _cutOffs;
        private readonly int[] _prevCutOffs;
        private readonly int[] _bucketWidths;

        public ColorMap(IList<ColorMapEntry> colorMapEntries, int maxIterations, string highColor)
        {
			HighColorEntry = new ColorMapEntry(maxIterations, highColor, ColorMapBlendStyle.None, highColor);

			_colorMapEntries = colorMapEntries.ToArray();
            _cutOffs = BuildCutOffs(_colorMapEntries);
			IList<Tuple<int, int>> pOffsetsAndBucketWidths = BuildPrevCutOffsAndBucketWidths(_colorMapEntries, HighColorEntry);

            _prevCutOffs = pOffsetsAndBucketWidths.Select(x => x.Item1).ToArray();
            _bucketWidths = pOffsetsAndBucketWidths.Select(x => x.Item2).ToArray();

            SetEndColors(_colorMapEntries);
        }

        public IList<ColorMapEntry> ColorMapEntries => _colorMapEntries.ToList();

        public int[] GetColor(int countVal, double escapeVelocity)
        {
            int colorMapIndex = GetColorMapIndex(countVal);
            int[] result = GetBlendedColor(colorMapIndex, countVal, escapeVelocity);
            return result;
        }
         
        private int[] GetBlendedColor(int colorMapIndex, int countVal, double escapeVelocity)
        {
            int[] result;

            ColorMapEntry cme = GetColorMapEntry(colorMapIndex);

            if (cme.BlendStyle == ColorMapBlendStyle.None)
            {
                result = cme.StartColor.ColorComps;
                return result;
            }

            int botBucketVal = _prevCutOffs[colorMapIndex];

            //int[] cStart;

            //if (countVal == botBucketVal)
            //{
            //	cStart = cme.StartColor.ColorComps;
            //}
            //else
            //{
            //	double stepFactor = (-1 + countVal - botBucketVal) / (double)cme.BucketWidth;
            //	cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);
            //}

            ////double stepFactor = (countVal - botBucketVal) / (double)cme.BucketWidth;
            ////cStart = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

            //double intraStepFactor = escapeVelocity / cme.BucketWidth;
            //result = Interpolate(cStart, cme.StartColor.ColorComps, cme.EndColor.ColorComps, intraStepFactor);

            int bucketWidth = _bucketWidths[colorMapIndex];
			double stepFactor = (countVal + escapeVelocity - botBucketVal) / bucketWidth;
			result = Interpolate(cme.StartColor.ColorComps, cme.StartColor.ColorComps, cme.EndColor.ColorComps, stepFactor);

			return result;
        }

        private int[] Interpolate(int[] cStart, int[] c1, int[] c2, double factor)
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

        private ColorMapEntry GetColorMapEntry(int colorMapIndex)
        {
            ColorMapEntry result;

            int newIndex = GetColorMapIndex(colorMapIndex);

            if (newIndex > _cutOffs.Length - 1)
            {
                result = HighColorEntry;
            }
            else
            {
                result = _colorMapEntries[newIndex];
            }

            return result;
        }

        private int GetColorMapIndex(int countVal)
        {
            int newIndex = Array.BinarySearch(_cutOffs, countVal);

            if (newIndex < 0)
            {
                newIndex = ~newIndex;
            }
            else
            {
                newIndex++;
            }

            return newIndex;
        }

        private int[] BuildCutOffs(ColorMapEntry[] colorMapEntries)
        {
            int[] result = new int[colorMapEntries.Length];

            for (int ptr = 0; ptr < colorMapEntries.Length; ptr++)
            {
                result[ptr] = colorMapEntries[ptr].CutOff;
            }

            return result;
        }

        private IList<Tuple<int, int>> BuildPrevCutOffsAndBucketWidths(ColorMapEntry[] colorMapEntries, ColorMapEntry highColorEntry)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();

            int prevCutOff = 0;

            for (int ptr = 0; ptr < colorMapEntries.Length; ptr++)
            {
                int cutOff = colorMapEntries[ptr].CutOff;
                result.Add(new Tuple<int, int>(prevCutOff, cutOff - prevCutOff));

                prevCutOff = cutOff;
            }

            result.Add(new Tuple<int, int>(prevCutOff, highColorEntry.CutOff - prevCutOff));

            return result;
        }

        private void SetEndColors(ColorMapEntry[] colorMapEntries)
        {
            for (int ptr = 0; ptr < colorMapEntries.Length; ptr++)
            {
                ColorMapEntry cmd = colorMapEntries[ptr];

                if (cmd.BlendStyle == ColorMapBlendStyle.Next)
                {
                    ColorMapColor endColor;
					if (ptr == colorMapEntries.Length - 1)
					{
						endColor = new ColorMapColor(HighColorEntry.StartColor.ColorComps);
					}
					else
					{
						endColor = new ColorMapColor(colorMapEntries[ptr + 1].StartColor.ColorComps);
					}

                    colorMapEntries[ptr] = new ColorMapEntry(cmd.CutOff, cmd.StartColor.CssColor, cmd.BlendStyle, endColor.CssColor);
                }
            }
        }

    }
}
