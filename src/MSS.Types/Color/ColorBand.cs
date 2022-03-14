using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorBand : ICloneable
    {
        public int CutOff { get; init; }
        public ColorBandColor StartColor { get; init; }
        public ColorBandBlendStyle BlendStyle { get; init; }
        public ColorBandColor EndColor { get; init; }

		#region Constructor

		public ColorBand(int cutOff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor) : this(cutOff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
        {
        }

        [JsonConstructor]
        [BsonConstructor]
        public ColorBand(int cutOff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
        {
            CutOff = cutOff;
            StartColor = startColor;
            BlendStyle = blendStyle;
            EndColor = endColor;
        }

		#endregion

		#region Public Properties

		public string BlendStyleAsString => BlendStyle switch
		{
			ColorBandBlendStyle.Next => "Next",
			ColorBandBlendStyle.None => "None",
			ColorBandBlendStyle.End => "End",
			_ => "None",
		};

		#endregion

		#region Public Methods

		public ColorBand Clone()
		{
            return new ColorBand(CutOff, StartColor.CssColor, BlendStyle, EndColor.CssColor);
		}

		object ICloneable.Clone()
		{
            return Clone();
		}

		#endregion

		#region Static Methods

		public static ColorBand UpdateCutOff(ColorBand source, int cutOff)
        {
            return new ColorBand(cutOff, source.StartColor, source.BlendStyle, source.EndColor);
        }

		#endregion
	}
}
