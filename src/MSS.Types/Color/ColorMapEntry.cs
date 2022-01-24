using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorMapEntry : ICloneable
    {
        public int CutOff { get; init; }
        public ColorMapColor StartColor { get; init; }
        public ColorMapBlendStyle BlendStyle { get; init; }
        public ColorMapColor EndColor { get; init; }

        //public ColorMapEntry(int cutOff, string startCssColor) : this(cutOff, startCssColor, ColorMapBlendStyle.None, startCssColor)
        //{ }

        public ColorMapEntry(int cutOff, string startCssColor, ColorMapBlendStyle blendStyle, string endCssColor) : this(cutOff, new ColorMapColor(startCssColor), blendStyle, new ColorMapColor(endCssColor))
        {
        }

        [JsonConstructor]
        [BsonConstructor]
        public ColorMapEntry(int cutOff, ColorMapColor startColor, ColorMapBlendStyle blendStyle, ColorMapColor endColor)
        {
            CutOff = cutOff;
            StartColor = startColor;
            BlendStyle = blendStyle;
            EndColor = endColor;
        }

        public ColorMapEntry Clone()
		{
            return new ColorMapEntry(CutOff, StartColor.CssColor, BlendStyle, EndColor.CssColor);
		}

		object ICloneable.Clone()
		{
            return Clone();
		}
	}
}
