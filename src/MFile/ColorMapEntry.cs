namespace MFile
{
    public class ColorMapEntry
    {
        public readonly int CutOff;
        public readonly ColorMapColor StartColor;
        public readonly ColorMapBlendStyle BlendStyle;
        public ColorMapColor EndColor;

        public int PrevCutOff;
        public int BucketWidth;

        public ColorMapEntry(int cutOff, string startCssColor, ColorMapBlendStyle blendStyle, string endCssColor)
        {
            CutOff = cutOff;
            StartColor = new ColorMapColor(startCssColor);
            BlendStyle = blendStyle;
            EndColor = new ColorMapColor(endCssColor);
        }

        public ColorMapEntry(int cutOff, string startCssColor)
        {
            CutOff = cutOff;
            StartColor = new ColorMapColor(startCssColor);
            BlendStyle = ColorMapBlendStyle.None;
            EndColor = new ColorMapColor(startCssColor);
        }
    }
}
