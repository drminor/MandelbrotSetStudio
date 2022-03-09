using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorBand : ICloneable
    {
        public int CutOff { get; init; }
        public ColorMapColor StartColor { get; init; }
        public ColorMapBlendStyle BlendStyle { get; init; }
        public ColorMapColor EndColor { get; init; }

        //public ColorMapEntry(int cutOff, string startCssColor) : this(cutOff, startCssColor, ColorMapBlendStyle.None, startCssColor)
        //{ }

        public ColorBand(int cutOff, string startCssColor, ColorMapBlendStyle blendStyle, string endCssColor) : this(cutOff, new ColorMapColor(startCssColor), blendStyle, new ColorMapColor(endCssColor))
        {
        }

        [JsonConstructor]
        [BsonConstructor]
        public ColorBand(int cutOff, ColorMapColor startColor, ColorMapBlendStyle blendStyle, ColorMapColor endColor)
        {
            CutOff = cutOff;
            StartColor = startColor;
            BlendStyle = blendStyle;
            EndColor = endColor;
        }

        public ColorBand Clone()
		{
            return new ColorBand(CutOff, StartColor.CssColor, BlendStyle, EndColor.CssColor);
		}

		object ICloneable.Clone()
		{
            return Clone();
		}


        public static ColorBand UpdateCutOff(ColorBand source, int cutOff)
        {
            return new ColorBand(cutOff, source.StartColor, source.BlendStyle, source.EndColor);
        }

    }
}
