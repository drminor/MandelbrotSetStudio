using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorBand : IColorBand, ICloneable
    {
		#region Constructor

		public ColorBand(int cutOff, string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor) 
			: this(cutOff, new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
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

		public int CutOff { get; set; }
		public ColorBandColor StartColor { get; set; }
		public ColorBandBlendStyle BlendStyle { get; set; }
		public ColorBandColor EndColor { get; set; }

		public string BlendStyleAsString => GetBlendStyleAsString(BlendStyle);

		public int PreviousCutOff { get; set; }
		public ColorBandColor ActualEndColor { get; set; }
		public int BucketWidth => CutOff - PreviousCutOff;

		public double Percentage { get; set; }

		#endregion

		#region Public Methods

		public void UpdateWithNeighbors(IColorBand? predecessor, IColorBand? successor)
		{
			PreviousCutOff = predecessor == null ? 0 : predecessor.CutOff;

			if (BlendStyle == ColorBandBlendStyle.Next)
			{
				var followingStartColor = successor?.StartColor ?? throw new InvalidOperationException("Must have a successor if the blend style is set to Next.");
				ActualEndColor = followingStartColor;
			}
			else
			{
				ActualEndColor = BlendStyle == ColorBandBlendStyle.End ? EndColor : StartColor;
			}
		}


		object ICloneable.Clone()
		{
			return Clone();
		}

		IColorBand IColorBand.Clone()
		{
			return Clone();
		}

		public ColorBand Clone()
		{
			var result = new ColorBand(CutOff, StartColor, BlendStyle, EndColor);
			result.PreviousCutOff = PreviousCutOff;
			result.ActualEndColor = ActualEndColor;

			return result;
		}

		#endregion

		#region Static Methods

		private static string GetBlendStyleAsString(ColorBandBlendStyle blendStyle)
		{
			return blendStyle switch
			{
				ColorBandBlendStyle.Next => "Next",
				ColorBandBlendStyle.None => "None",
				ColorBandBlendStyle.End => "End",
				_ => "None",
			};
		}

		#endregion
	}
}
