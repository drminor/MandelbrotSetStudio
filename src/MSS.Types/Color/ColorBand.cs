using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorBand : ICloneable
    {
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
			ActualEndColor = BlendStyle == ColorBandBlendStyle.None ? StartColor : EndColor;
        }

		#endregion

		#region Public Properties

		public int CutOff { get; init; }
		public ColorBandColor StartColor { get; init; }
		public ColorBandBlendStyle BlendStyle { get; init; }
		public ColorBandColor EndColor { get; init; }


		public string BlendStyleAsString => BlendStyle switch
		{
			ColorBandBlendStyle.Next => "Next",
			ColorBandBlendStyle.None => "None",
			ColorBandBlendStyle.End => "End",
			_ => "None",
		};

		public int PreviousCutOff { get; set; }

		public int BucketWidth => CutOff - PreviousCutOff;

		public ColorBandColor ActualEndColor { get; set; }

		#endregion

		#region Public Methods

		public void UpdateWithNeighbors(ColorBand? predecssor, ColorBand? sucessor)
		{
			PreviousCutOff = predecssor == null ? 0 : predecssor.CutOff;

			if (BlendStyle == ColorBandBlendStyle.Next)
			{
				var preceedingStartColor = sucessor?.StartColor ?? throw new InvalidOperationException("Must have a successor if the blend style is set to Next.");
				ActualEndColor = preceedingStartColor;
			}
			else
			{
				ActualEndColor = BlendStyle == ColorBandBlendStyle.End ? EndColor : StartColor;
			}
		}

		public ColorBand Clone()
		{
            return new ColorBand(CutOff, StartColor.Clone(), BlendStyle, EndColor.Clone());
		}

		object ICloneable.Clone()
		{
            return Clone();
		}

		#endregion

		//#region Static Methods

		//public static ColorBand UpdateCutOff(ColorBand source, int cutOff)
  //      {
  //          return new ColorBand(cutOff, source.StartColor, source.BlendStyle, source.EndColor);
  //      }

		//#endregion
	}
}
